using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using System.Collections;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Application.MessageHandlers;
using Rook.Framework.Core.Attributes;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Deduplication;
using Rook.Framework.Core.Monitoring;
using Rook.Framework.Core.Services;
using Rook.Framework.Core.StructureMap;

namespace Rook.Framework.Core.Application.Subscribe
{
    /// <summary>
    /// Automaticly subscribes to messages, by locating consumers for message types
    /// </summary>
    public sealed class MessageSubscriber : IMessageSubscriber, IStartStoppable
    {
        private Dictionary<Type, HandlerAttribute[]> _messageHandlerTypes;

        private readonly IQueueWrapper _queueWrapper;
        private readonly ILogger _logger;
        private readonly IServiceMetrics _metrics;
        private readonly IMethodInspectorManager _methodInspectorManager;
        private readonly IContainerFacade _container;

        public MessageSubscriber(IQueueWrapper queueWrapper, ILogger logger, IConfigurationManager config, IServiceMetrics metrics,
            IMethodInspectorManager methodInspectorManager, IContainerFacade container)
        {
            _queueWrapper = queueWrapper;
            _logger = logger;
            _metrics = metrics;
            _methodInspectorManager = methodInspectorManager;
            _container = container;
        }

        private static string ConsumerTag { get; set; }

        public StartupPriority StartupPriority { get; } = StartupPriority.Low;
        public void Start()
        {
            ConsumerTag = _queueWrapper.StartMessageConsumer(ConsumeMessages);
        }

        public void Stop()
        {
            int count;
            while ((count = GetInProgressMessageCount()) > 0)
            {
                _logger.Trace(nameof(Service) + "." + nameof(Stop), new LogItem("Activity", "Waiting for in-progress message count to reach 0, currently " + count));
                Thread.Sleep(100);
            }

            _queueWrapper.StopMessageConsumer(ConsumerTag);
        }

        private static readonly AutoDictionary<string, Type> MethodHandlerDictionary = new AutoDictionary<string, Type>();
        private static readonly Dictionary<Type, PropertyInfo> NeedPropertyCache = new Dictionary<Type, PropertyInfo>();
        private static readonly Dictionary<Type, PropertyInfo> SolutionPropertyCache = new Dictionary<Type, PropertyInfo>();
        private static readonly Dictionary<Type, PropertyInfo> ErrorsPropertyCache = new Dictionary<Type, PropertyInfo>();

        private Task ConsumeMessages(object sender, BasicDeliverEventArgs eventDetails)
        {
            Task.Run(() => ConsumeMessage(sender, eventDetails));
            return Task.CompletedTask;
        }

        public Task ConsumeMessage(object sender, BasicDeliverEventArgs eventDetails)
        {
            try
            {
                inProgressMessages++;
                
                if (eventDetails?.Body == null || eventDetails.Body.Length < 1)
                {
                    return RejectMessageWith(DiscardedMessageReason.MissingMessageBody, eventDetails);
                }

                // Deserialise into a class containing only Method and UUID - this also generates a hash
                MethodInspector inspector = GetMethodInspector(eventDetails);
                if (inspector == null)
                {
                    return RejectMessageWith(DiscardedMessageReason.MethodDeserialisationError, eventDetails);
                }

                _logger.Trace($"{nameof(MessageSubscriber)}.{nameof(ConsumeMessage)}",
                    new LogItem("Event", "Message routed"),
                    new LogItem("Calculated hashcode", inspector.Hash),
                    new LogItem("MessageId", inspector.Uuid.ToString),
                    new LogItem("MessageMethod", inspector.Method));

                if (MessageIsDuplicate(inspector))
                {
                    return RejectMessageWith(DiscardedMessageReason.Duplicate, eventDetails);
                }
                
                Type handlerType = GetMessageHandlers(inspector);

                if (handlerType == null)
                {
                    _logger.Trace($"{nameof(MessageSubscriber)}.{nameof(ConsumeMessage)}", new LogItem("Event", "No suitable handler found for Method " + inspector.Method));
                    // We can't process this message, so we'll Ack it.
                    _methodInspectorManager.Register(inspector); // we don't want to see it again

                    return RejectMessageWith(DiscardedMessageReason.NoHandler, eventDetails);
                }

                Type[] genericArguments = GetGenericArguments(handlerType, inspector.Uuid);

                if (handlerType.GetTypeInfo().ContainsGenericParameters)
                    handlerType = handlerType.MakeGenericType(genericArguments);

                string bodyAsString = Encoding.UTF8.GetString(eventDetails.Body);
                if (!ConstructMessage(out object message, genericArguments, bodyAsString, inspector.Uuid))
                {
                    _logger.Trace($"{nameof(MessageSubscriber)}.{nameof(ConsumeMessage)}",
                        new LogItem("Event", "The Message could not be constructed, so it's been discarded."));
                    
                    _methodInspectorManager.Register(inspector); // we don't want to see it again

                    // This is a Permanent Failure; if we couldn't construct the Message object now, processing it again won't help.
                    return RejectMessageWith(DiscardedMessageReason.MessageDeserialisationError, eventDetails);
                }

                Type messageType = message.GetType();
                PropertyInfo need;
                lock (NeedPropertyCache)
                {
                    if (!NeedPropertyCache.ContainsKey(messageType))
                        NeedPropertyCache.Add(messageType,
                            messageType.GetProperty(nameof(Message<object, object>.Need)));
                    need = NeedPropertyCache[messageType];
                }

                if (need.GetValue(message) == null)
                {
                    _logger.Warn($"{nameof(MessageSubscriber)}.{nameof(ConsumeMessage)}",
                        new LogItem("Event", "Message has no Need"), new LogItem("MessageId", inspector.Uuid.ToString));

                    _methodInspectorManager.Register(inspector); // we don't want to see it again

                    // This is a Permanent Failure; if there's no Need in the message now, processing it again won't change anything.
                    return RejectMessageWith(DiscardedMessageReason.MissingNeed, eventDetails);
                }

                HandlerAttribute handlerAttribute = handlerType.GetTypeInfo().GetCustomAttributes<HandlerAttribute>().FirstOrDefault(a => a.Method == inspector.Method);

                if (!ProcessAcceptanceBehaviours(handlerAttribute, messageType, message))
                {
                    _methodInspectorManager.Register(inspector); // we don't want to see it again
                    return RejectMessageWith(DiscardedMessageReason.AcceptanceBehaviourPrecondition, eventDetails);
                }

                _logger.Trace($"{nameof(MessageSubscriber)}.{nameof(ConsumeMessage)}",
                    new LogItem("Event", "Constructing handler"), new LogItem("MessageId", inspector.Uuid.ToString), new LogItem("HandlerName", handlerType.Name));

                var stopWatch = Stopwatch.StartNew();
                CompletionAction invocationResult = InvokeHandleMethod(inspector.Uuid, handlerType, message);
                stopWatch.Stop();

                _metrics.RecordProcessedMessage(handlerType.Name, stopWatch.Elapsed.TotalMilliseconds);

                if (invocationResult == CompletionAction.Republish)
                {
                    publishMethod = publishMethod ?? _queueWrapper.GetType().GetMethod(nameof(_queueWrapper.PublishMessage));
                    publishMethod.MakeGenericMethod(genericArguments).Invoke(_queueWrapper, new object[] { message, inspector.Uuid });
                }

                // Any exceptions thrown up to this point *are probably* Temporary Failures:
                // Permanent Failures will have been dealt with by local Exception Handling,
                // and the code will continue to this point.

                _methodInspectorManager.Register(inspector);

                _queueWrapper.AcknowledgeMessage(eventDetails);
            }
            catch (Exception e)
            {
                _logger.Exception($"{nameof(MessageSubscriber)}.{nameof(ConsumeMessage)}",
                    "Failure during consuming of message",
                    e);

                throw;
            }
            finally
            {
                inProgressMessages--;
            }

            return Task.CompletedTask;
        }

        private Task RejectMessageWith(DiscardedMessageReason reason, BasicDeliverEventArgs eventDetails)
        {
            _queueWrapper.RejectMessage(eventDetails);
            _metrics.RecordDiscardedMessage(reason);

            return Task.CompletedTask;
        }

        private MethodInfo publishMethod = null;

        private int inProgressMessages = 0;

        public bool ProcessAcceptanceBehaviours(HandlerAttribute handlerAttribute, Type messageType, object message)
        {
            if (handlerAttribute == null || handlerAttribute.AcceptanceBehaviour == AcceptanceBehaviour.Always) return true;

            bool rejectOnError = handlerAttribute.ErrorsBehaviour == ErrorsBehaviour.RejectIfErrorsExist;
            bool acceptOnlyOnError = handlerAttribute.ErrorsBehaviour == ErrorsBehaviour.AcceptOnlyIfErrorsExist;
            if (rejectOnError || acceptOnlyOnError)
            {
                PropertyInfo errors;
                lock (ErrorsPropertyCache)
                {
                    if (!ErrorsPropertyCache.ContainsKey(messageType))
                        ErrorsPropertyCache.Add(messageType, messageType.GetProperty(nameof(Message<object, object>.Errors)));

                    errors = ErrorsPropertyCache[messageType];
                }

                object errorsValue = errors.GetValue(message);
                if (rejectOnError && errorsValue != null)
                {
                    // if it's not null and not IList then we have to assume that there are errors in whatever it is we've got.
                    if (!(errorsValue is IList list) || list.Count > 0)
                        return false;
                }
                if (acceptOnlyOnError)
                    if (errorsValue == null || errorsValue is IList list && list.Count == 0)
                        return false;
            }

            PropertyInfo solution;
            lock (SolutionPropertyCache)
            {
                if (!SolutionPropertyCache.ContainsKey(messageType))
                    SolutionPropertyCache.Add(messageType,
                        messageType.GetProperty(nameof(Message<object, object>.Solution)));
                solution = SolutionPropertyCache[messageType];
            }

            object solutionValue = solution.GetValue(message);

            if (handlerAttribute.AcceptanceBehaviour == AcceptanceBehaviour.OnlyWithSolution && solutionValue == null) return false;

            if (handlerAttribute.AcceptanceBehaviour == AcceptanceBehaviour.OnlyWithoutSolution && solutionValue != null) return false;

            return true;
        }

        private CompletionAction InvokeHandleMethod(Guid uuid, Type handlerType, object message)
        {
            object handler = _container.GetInstance(handlerType);

            _logger.Trace($"{nameof(MessageSubscriber)}.{nameof(ConsumeMessage)}",
                new LogItem("Event", "Invoking Handler"), new LogItem("MessageId", uuid.ToString),
                new LogItem("HandlerName", handlerType.Name));
            MethodInfo handleMethod = handlerType.GetMethod(nameof(IMessageHandler2<object, object>.Handle));
            CompletionAction invocationResult = CompletionAction.DoNothing;

            if (handleMethod != null)
            {
                invocationResult = (CompletionAction)handleMethod.Invoke(handler, new[] { message });
            }

            return invocationResult;
        }

        public MethodInspector GetMethodInspector(BasicDeliverEventArgs eventDetails)
        {
            try
            {
                ulong CalculateHashCode()
                {
                    var resultBytes = new byte[8];

                    for (int i = 0; i < eventDetails.Body.Length; i++)
                        resultBytes[i % 8] ^= eventDetails.Body[i];

                    return BitConverter.ToUInt64(resultBytes, 0);
                }

                Task<ulong> ghc = new Task<ulong>(CalculateHashCode);
                ghc.Start();
                MethodInspector inspector = JsonConvert.DeserializeObject<MethodInspector>(Encoding.UTF8.GetString(eventDetails.Body));
                if (inspector != null)
                    inspector.Hash = ghc.Result;
                return inspector;
            }
            catch (Exception)
            {
                // This message is so badly formatted we'd never be able to do anything with it. Get rid.
                _logger.Trace($"{nameof(MessageSubscriber)}.{nameof(ConsumeMessage)}",
                    new LogItem("Event", "Message failed to deserialise to MethodInspector, so it is rejected"),
                    new LogItem("MessageBody", () => Encoding.UTF8.GetString(eventDetails.Body)));
                return null;
            }
        }

        private bool ConstructMessage(out object message, Type[] genericArguments, string bodyAsString, Guid id)
        {
            bool getMessageSuccess;
            Type messageType = typeof(Message<,>).MakeGenericType(genericArguments);

            try
            {
                message = JsonConvert.DeserializeObject(bodyAsString, messageType);
                getMessageSuccess = true;
            }
            catch (Exception exception)
            {
                message = null;
                // This happens if there was a mismatch between the message schema and object schema - we should probably log this, althought it might just be that we got a V2 message in service at V1
                _logger.Warn($"{nameof(MessageSubscriber)}.{nameof(ConstructMessage)}",
                    new LogItem("Event", $"Could not deserialise into a Message<{string.Join(",", genericArguments.Select(ga => ga.Name))}>"),
                    new LogItem("MessageId", id.ToString),
                    new LogItem("ExceptionMessage", exception.Message));
                getMessageSuccess = false;
            }
            return getMessageSuccess;
        }

        private Type[] GetGenericArguments(Type handlerType, Guid id)
        {
            Type intrface = handlerType.GetInterfaces().First(i => i.GetGenericTypeDefinition() == typeof(IMessageHandler2<,>));

            if (intrface == null)
                throw new NotImplementedException(
                    $"Operation=\"{nameof(MessageSubscriber)}.{nameof(GetGenericArguments)}\" Event=\"Message handler class {handlerType.Name} which is decorated with HandlerAttribute must implement IMessageHandler<TNeed, TSolution> or IMessageHandler<TNeed> (but does not)\" MessageId=\"{id}\"");

            // From the handler's implementation of IMessageHandler<,> we can get the type arguments for the message, construct it, and attempt to deserialise into it.
            Type[] genericArguments = intrface.GetGenericArguments().Select(ga => ga.IsGenericParameter ? typeof(object) : ga).ToArray();
            return genericArguments;
        }

        private Type GetMessageHandlers(MethodInspector inspector)
        {


            lock (MethodHandlerDictionary)
            {
                if (!MethodHandlerDictionary.ContainsKey(inspector.Method))
                {
                    var bestHandler = GetBestHandlerFor(inspector);
                    if (bestHandler != null)
                        MethodHandlerDictionary[inspector.Method] = bestHandler;
                }

                return MethodHandlerDictionary[inspector.Method];
            }
        }

        private Type GetBestHandlerFor(MethodInspector inspector)
        {
            var messageHandlerTypes = _messageHandlerTypes ?? (_messageHandlerTypes = _container.GetAttributedTypes<HandlerAttribute>(typeof(IMessageHandler2<,>)));

            _logger.Trace($"{nameof(MessageSubscriber)}.{nameof(GetMessageHandlers)}",
                new LogItem("Event", $"Found the following types that might be able to handle this message: {string.Join(",", messageHandlerTypes.Select(t => t.Key.Name))}"),
                new LogItem("MessageId", inspector.Uuid.ToString));

            Type handlersMatchingMethod = messageHandlerTypes.FirstOrDefault(mht => mht.Value.Any(ha => ha.Method == inspector.Method)).Key;
            if (handlersMatchingMethod == null)
            {
                var blanketHandlers =
                    messageHandlerTypes.Where(mht => mht.Key.GetGenericArguments().All(arg => arg.IsGenericParameter) && mht.Value.Any(ha => ha.Method == "*"));
                return blanketHandlers.First().Key;
            }
                
            return handlersMatchingMethod;
        }

        private bool MessageIsDuplicate(MethodInspector inspector)
        {
            bool duplicate = _methodInspectorManager.DuplicateCheck(inspector);

            if (duplicate)
                _logger.Trace($"{nameof(MessageSubscriber)}.{nameof(MessageIsDuplicate)}", new LogItem("Event", "Duplicate message found"), new LogItem("MessageId", inspector.Uuid.ToString));

            return duplicate;
        }

        public int GetInProgressMessageCount() => inProgressMessages;

        // ReSharper disable once ClassNeverInstantiated.Local
        public class MethodInspector
        {
            public MethodInspector()
            {
            }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            [JsonRequired]
            public string Method { get; set; }

            [JsonRequired]
            public Guid Uuid { get; set; }

            public dynamic Errors { get; set; }

            public ulong Hash { get; set; }
        }
    }
}