using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using Rook.Framework.Core.Application.Bus;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Application.ResponseHandlers;
using Rook.Framework.Core.Backplane;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core
{
    public class RequestMethodRegistration
    {
        public string Method { get; set; }
    }


    public sealed class RequestStore : BackplaneConsumer<MessageWrapper>, IRequestStore
    {
        private readonly int busTimeoutMilliseconds;

        private readonly IQueueWrapper queueWrapper;

        private readonly ILogger logger;
        private readonly IRequestMatcher requestMatcher;
        private readonly IDateTimeProvider dateTimeProvider;
        
        private readonly IBackplane backplane;

        public RequestStore(
            IDateTimeProvider dateTimeProvider,
            IQueueWrapper queueWrapper,
            ILogger logger,
            IRequestMatcher requestMatcher,
            IConfigurationManager config,
            IBackplane backplane   
            )
        {
            this.queueWrapper = queueWrapper;
            this.logger = logger;
            this.requestMatcher = requestMatcher;
            this.dateTimeProvider = dateTimeProvider;

            const int defaultBusTimeoutMilliseconds = 5000;
            busTimeoutMilliseconds = config.Get("BusTimeoutMilliseconds", defaultBusTimeoutMilliseconds);
            
            this.backplane = backplane;
            Methods = new List<string>();
        }

        public Func<Guid> CreateUniqueId { get; set; } = Guid.NewGuid;

        public List<string> Methods { get; private set; }
        
        public JsonBusResponse PublishAndWaitForResponse<TNeed, TSolution>(Message<TNeed, TSolution> message, ResponseStyle responseStyle = ResponseStyle.WholeSolution,
            Func<string, bool> solutionMatchFunction = null)
        {
            Guid requestId = CreateUniqueId.Invoke();
            if (message.Uuid == Guid.Empty)
                message.Uuid = requestId;

            message.LastModifiedBy = ServiceInfo.Name;
            message.LastModifiedTime = dateTimeProvider.UtcNow;

            if (!Methods.Contains(message.Method))
            {
                Methods.Add(message.Method);
                backplane.Send(new RequestMethodRegistration {Method = message.Method});
            }
            
            logger.Trace($"{nameof(RequestStore)}.{nameof(PublishAndWaitForTypedResponse)}",
                new LogItem("Event", "Preparing to publish message"),
                new LogItem("MessageId", message.Uuid.ToString),
                new LogItem("MessageMethod", message.Method));

            var response = new JsonBusResponse();

            using (DataWaitHandle dataWaitHandle = new DataWaitHandle(false, EventResetMode.AutoReset, solutionMatchFunction))
            {
                requestMatcher.RegisterWaitHandle(message.Uuid, dataWaitHandle, responseStyle);

                queueWrapper.PublishMessage(message, message.Uuid);

                if (dataWaitHandle.WaitOne(busTimeoutMilliseconds))
                {
                    List<ResponseError> errors = null;
                    if (dataWaitHandle.Errors != null)
                        errors = JsonConvert.DeserializeObject<List<ResponseError>>(dataWaitHandle.Errors);

                    logger.Trace($"{nameof(RequestStore)}.{nameof(PublishAndWaitForTypedResponse)}",
                        new LogItem("Event", "Published message and received response"),
                        new LogItem("MessageId", message.Uuid.ToString),
                        new LogItem("MessageMethod", message.Method));

                    if (errors != null && errors.Any())
                        response.Errors = dataWaitHandle.Errors;

                    response.Solution = dataWaitHandle.Solution;
                }

                if (string.IsNullOrEmpty(response.Errors) && string.IsNullOrEmpty(response.Solution))
                    logger.Trace($"{nameof(RequestStore)}.{nameof(PublishAndWaitForTypedResponse)}",
                        new LogItem("Event", "Published message and received no response"),
                        new LogItem("MessageId", message.Uuid.ToString),
                        new LogItem("MessageMethod", message.Method));

                return response;
            }
        }

        public BusResponse<TSolution> PublishAndWaitForTypedResponse<TNeed, TSolution>(Message<TNeed, TSolution> message, ResponseStyle responseStyle = ResponseStyle.WholeSolution, Func<string, bool> solutionMatchFunction = null)
        {
            var response = PublishAndWaitForResponse(message, responseStyle, solutionMatchFunction);

            var solution = default(IEnumerable<TSolution>);
            if (response.Solution != null)
            {
                solution = JsonConvert.DeserializeObject<IEnumerable<TSolution>>(response.Solution);
            }

            return new BusResponse<TSolution>
            {
                Solution = solution,
                Errors = response.Errors
            };
        }

        public override void Consume(MessageWrapper mw)
        {
            requestMatcher.RegisterMessageWrapper(mw.Uuid, mw);
        }
    }

    

    public class JsonBusResponse : BusResponse
    {
        public string Solution { get; set; }
    }

    public class BusResponse<T> : BusResponse
    {
        public IEnumerable<T> Solution { get; set; }
    }

    public abstract class BusResponse
    {
        public string Errors { get; set; }
    }
}