using KinesisProducerNet;

namespace Rook.Framework.Core.AmazonKinesisFirehose
{
	public interface IAmazonFirehoseProducer
	{
		UserRecordResult PutRecord(string streamName, string json, string messageId);
	}
}