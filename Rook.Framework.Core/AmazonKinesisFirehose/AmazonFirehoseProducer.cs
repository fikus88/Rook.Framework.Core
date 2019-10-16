using System;
using System.Globalization;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using KafkaNet.Common;
using KinesisProducerNet;
using KinesisProducerNet.Protobuf;
using Newtonsoft.Json;
using Rook.Framework.Core.Common;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Rook.Framework.Core.AmazonKinesisFirehose
{
	public class AmazonFirehoseProducer : IAmazonFirehoseProducer
	{
		private readonly KinesisProducer _kinesisProducer;
		private readonly ILogger _logger;

		public AmazonFirehoseProducer(ILogger logger, IConfigurationManager config)
		{
			LogLevel kinesisLogLevel;

			try
			{
				kinesisLogLevel = (LogLevel) Enum.Parse(typeof(LogLevel), config.Get<string>("KinesisLogLevel"));
			}
			catch
			{
				kinesisLogLevel = LogLevel.Information;
			}

			var conf = new KinesisProducerConfiguration()
			{
				Region = Environment.GetEnvironmentVariable("AWS_REGION"),
				LogLevel = kinesisLogLevel
			};

			_logger = logger;

			_kinesisProducer = new KinesisProducer(conf);

			_logger.Info($"{JsonConvert.SerializeObject(conf.CredentialsProvider.GetCredentials())}");
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