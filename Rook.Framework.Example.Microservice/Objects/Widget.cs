using System;

namespace Rook.Framework.Example.Microservice.Objects
{
	public class Widget
	{
		public Guid WidgetId = Guid.NewGuid();

		public string Process()
		{
			return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
		}
	}
}
