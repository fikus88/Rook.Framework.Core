using System;
using System.Globalization;
using KafkaNet.Common;
using KinesisProducerNet;
using KinesisProducerNet.Protobuf;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.AmazonKinesisFirehose
{
	public class AmazonFirehoseProducer : IAmazonFirehoseProducer
	{

		private readonly KinesisProducer _kinesisProducer;
		private readonly ILogger _logger;
		
		public AmazonFirehoseProducer( ILogger logger)
		{
			
			var conf = new KinesisProducerConfiguration()
			{
				Region = Environment.GetEnvironmentVariable("AWS_REGION"),
				LogLevel = "error",
			};

			_logger = logger;
			
			_kinesisProducer = new KinesisProducer(conf);
			
		}

		public void PutRecord(string streamName, string json)
		{
			var rec = new UserRecord(streamName, ServiceInfo.Name,
				json.ToBytes());

			_logger.Info("FIREHOSE", new LogItem("Data Pushed", json));
			
			_kinesisProducer.AddUserRecord(rec);
		}
	}
}