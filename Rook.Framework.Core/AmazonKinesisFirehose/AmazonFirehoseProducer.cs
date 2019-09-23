using System;
using System.Globalization;
using KafkaNet.Common;
using KinesisProducerNet;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.AmazonKinesisFirehose
{
	public class AmazonFirehoseProducer : IAmazonFirehoseProducer
	{

		private readonly KinesisProducer _kinesisProducer;

		public AmazonFirehoseProducer()
		{
			
			var conf = new KinesisProducerConfiguration()
			{
				Region = Environment.GetEnvironmentVariable("AWS_REGION")
			};

			_kinesisProducer = new KinesisProducer(conf);
		}

		public UserRecordResult PutRecord(string json, string streamName)
		{
			var rec = new UserRecord(streamName, ServiceInfo.Name,
				json.ToBytes());

			return _kinesisProducer.AddUserRecord(rec).Task.Result;
		}
	}
}