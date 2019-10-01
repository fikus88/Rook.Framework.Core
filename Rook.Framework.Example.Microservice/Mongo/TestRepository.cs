using System;
using Rook.Framework.Example.Microservice.Objects;
using Rook.Framework.MongoDb.Data;

namespace Rook.Framework.Example.Microservice.Mongo
{
	public class TestRepository : ITestRepository
	{
		private readonly IMongoStore _mongoStore;

		public TestRepository(IMongoStore mongoStore)
		{
			_mongoStore = mongoStore;
		}

		public int GetHighest()
		{
			return (int)_mongoStore.Count<FirehoseDataSample>();
		}
		
		public Guid Put<T>(T entity) where T : DataEntity
		{
			_mongoStore.Put(entity, dataEntity => (Guid) dataEntity.Id == (Guid) entity.Id);

			return (Guid) entity.Id;
		}
	}
}