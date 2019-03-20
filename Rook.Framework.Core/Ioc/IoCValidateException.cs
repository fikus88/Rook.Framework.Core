using System;

namespace Rook.Framework.Core.IoC {
	[Serializable]
	public sealed class IoCValidateException : Exception
	{
		public IoCValidateException(string message) : base(message) { }

		public static IoCValidateException NewMultipleImplementationsFoundException(Type root)
		{
			return new IoCValidateException($"More than one possible implementation of {root.Name} found.");
		}
	}
}