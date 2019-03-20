using System;
using System.Collections.Generic;
using System.Linq;
using KafkaNet.Protocol;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.AnalyticsPump
{
    public class AnalyticsPump : IAnalyticsPump
    {
        private readonly IConfigurationManager config;
        private readonly IProducerWrapper producer;
        private readonly ILogger logger;

        public AnalyticsPump(ILogger logger, IConfigurationManager config, IProducerWrapper producer)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.producer = producer ?? throw new ArgumentNullException(nameof(producer));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void ForwardMessageToPump(string messageJson, byte[] key = null)
        {            
            string topic = config.Get<string>("AnalyticsPumpTopic");
            if (string.IsNullOrWhiteSpace(topic)) return;

            try
            {
                Message message = new Message(messageJson);
                if (key != null)
                {
                    message.Key = key;
                }

                List<ProduceResponse> responses = producer.SendMessageAsync(topic, new[] { message });
                
                IEnumerable<ProduceResponse> erroredResponses = responses.Where(r => r.Error != 0);
                
                if (erroredResponses.Any())
                {
                    IEnumerable<Exception> innerExceptions = erroredResponses.Select(r => new DataPumpException(((ErrorCode) r.Error).ToString()));
                    throw new AggregateException(innerExceptions);
                }
            }
            catch (Exception e)
            {
                logger.Error($"{GetType()}.{nameof(ForwardMessageToPump)}", new LogItem("Event", "Error AnalyticsPump SendMessageAsync Exception"), new LogItem("Exception", e.ToString));
                throw;
            }
        }
    }
}