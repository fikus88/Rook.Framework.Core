using System;
using System.Globalization;
using KafkaNet.Common;
using KinesisProducerNet;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.AmazonKinesisFirehose
{
	public class AmazonFirehoseProducer : IAmazonFirehoseProducer, IDisposable
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

		public UserRecordResult PutRecord(string streamName, string json)
		{
			var rec = new UserRecord(streamName, ServiceInfo.Name,
				json.ToBytes());

			return _kinesisProducer.AddUserRecord(rec).Task.Result;
		}

		public void Dispose()
		{
			_kinesisProducer.Destroy();
		}
	}
}