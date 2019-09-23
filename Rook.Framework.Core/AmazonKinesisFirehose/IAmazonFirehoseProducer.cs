using KinesisProducerNet;

namespace Rook.Framework.Core.AmazonKinesisFirehose
{
	/// <summary>
	/// Amazon Kinesis Firehose Wrapper
	/// </summary>
	public interface IAmazonFirehoseProducer
	{
		/// <summary>
		/// Method to put record into Kinesis stream
		/// </summary>
		/// <param name="streamName">Kinesis Stream Name</param>
		/// <param name="json">message</param>
		/// <returns></returns>
		UserRecordResult PutRecord(string streamName, string json);
	}
}