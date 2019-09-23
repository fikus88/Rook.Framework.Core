using System;
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

		public Guid Put<T>(T entity) where T : DataEntity
		{
			_mongoStore.Put(entity);

			return (Guid) entity.Id;
		}
	}
}