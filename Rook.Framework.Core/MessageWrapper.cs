using System;

namespace Rook.Framework.Core {
    public sealed class MessageWrapper
	{
		public Guid Uuid;
		public string SolutionJson;
	    public string FirstOrDefaultJson;
        public string ErrorsJson;
	}
}