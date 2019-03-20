using System;
using Microlise.MicroService.Core.Data;

namespace Microlise.Example.Microservice.Objects
{
	public class Widget : DataEntity
	{
		public Guid WidgetId = Guid.NewGuid();

		public string Process()
		{
			return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
		}
	}
}
