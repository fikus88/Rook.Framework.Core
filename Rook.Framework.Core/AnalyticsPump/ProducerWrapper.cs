using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KafkaNet;
using KafkaNet.Model;
using KafkaNet.Protocol;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.AnalyticsPump
{
    internal sealed class ProducerWrapper : IProducerWrapper
    {
        private readonly Producer producer;
        private readonly IConfigurationManager config;
        
        public ProducerWrapper(IConfigurationManager config)
        {
            this.config = config;
            
            string kafkaServerUri = config.Get<string>("KafkaServerUri", null);
            if (string.IsNullOrWhiteSpace(kafkaServerUri)) return;

            KafkaOptions options = new KafkaOptions(new Uri(kafkaServerUri));
            BrokerRouter router = new BrokerRouter(options);
            producer = new Producer(router);
        }

        public List<ProduceResponse> SendMessageAsync(string topic, Message[] messages)
        {
            if (producer == null)
                throw new DataPumpException("SendMessageAsync was called, but no KafkaServerUri has been configured.");

            Task<List<ProduceResponse>> sendMessageAsync = producer.SendMessageAsync(topic, messages);
            int kafkaTimeoutSeconds = config.Get("KafkaTimeoutSeconds", 5);
            sendMessageAsync.Wait(TimeSpan.FromSeconds(kafkaTimeoutSeconds));
            
            if (sendMessageAsync.Status == TaskStatus.RanToCompletion)
                return sendMessageAsync.Result;

            if (sendMessageAsync.IsFaulted)
            {
                if (sendMessageAsync.Exception == null)
                    throw new DataPumpException("Kafka SendMessageAsync task in Faulted state but did not produce an Exception. This should never happen.");
                
                throw sendMessageAsync.Exception;
            }

            if (sendMessageAsync.IsCanceled)
                throw new DataPumpException($"Kafka SendMessageAsync failed after {kafkaTimeoutSeconds} seconds. This can be configured as KafkaTimeoutSeconds");
            
            throw new DataPumpException(
                $"Kafka SendMessageAsync Task Finished, but is not Completed, Faulted or Canceled. Status is {sendMessageAsync.Status}");
        }
    }
}