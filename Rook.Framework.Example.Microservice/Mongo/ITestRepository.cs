using System;
using Rook.Framework.MongoDb.Data;

namespace Rook.Framework.Example.Microservice.Mongo
{
	public interface ITestRepository
	{
		Guid Put<T>(T entity) where T : DataEntity;
	}
}