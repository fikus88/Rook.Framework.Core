using System;
using Newtonsoft.Json;
using Rook.Framework.Core.AnalyticsPump;
using Rook.Framework.Core.Application.Message;
using Rook.Framework.Core.Application.MessageHandlers;
using Rook.Framework.Core.Attributes;

namespace Rook.Framework.Example.Microservice.MessageHandlers
{
    [Handler("KafkaTest", AcceptanceBehaviour = AcceptanceBehaviour.OnlyWithoutSolution)]
    public class KakfaPushMessageHandler : IMessageHandler2<KafkaNeed, object>
    {
        private readonly IAnalyticsPump _analyticsPump;

        public KakfaPushMessageHandler(IAnalyticsPump analyticsPump)
        {
            _analyticsPump = analyticsPump;
        }

        public CompletionAction Handle(Message<KafkaNeed, object> message)
        {
            var key = Guid.NewGuid();
            
            Console.WriteLine($"Key: {key}");

            _analyticsPump.ForwardMessageToPump(JsonConvert.SerializeObject(message.Need), key.ToByteArray());
            return CompletionAction.DoNothing;
        }
    }

    public class KafkaNeed
    {
        public int Number { get; set; }
        public string Word { get; set; }
    }
}
