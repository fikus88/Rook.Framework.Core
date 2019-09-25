using System;
using Rook.Framework.MongoDb.Data;

namespace Rook.Framework.Example.Microservice.Objects
{
	public class FirehoseDataSample : DataEntity
	{
		public int IdInt { get; set; }
		public string Name { get; set; }
	}
}