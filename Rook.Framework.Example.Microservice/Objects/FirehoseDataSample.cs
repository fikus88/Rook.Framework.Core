using Rook.Framework.MongoDb.Data;

namespace Rook.Framework.Example.Microservice.Objects
{
	public class FirehoseDataSample : DataEntity
	{
		public int Number { get; set; }
		public string Word { get; set; }
	}
}