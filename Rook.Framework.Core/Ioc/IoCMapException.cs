using System;

namespace Rook.Framework.Core.IoC{
	[Serializable]
	public sealed class IoCMapException : Exception
	{
		public IoCMapException(string message) : base(message)
		{
		}
	}
}