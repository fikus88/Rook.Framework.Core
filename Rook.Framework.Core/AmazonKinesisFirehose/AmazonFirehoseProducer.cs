using System;
using System.Globalization;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
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

		public AmazonFirehoseProducer(ILogger logger)
		{
			new CredentialProfileStoreChain().TryGetAWSCredentials(Environment.GetEnvironmentVariable("AWS_PROFILE"),
				out var creds);
			var conf = new KinesisProducerConfiguration()
			{
				Region = Environment.GetEnvironmentVariable("AWS_REGION"),
				LogLevel = "error",
				CredentialsProvider = creds
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