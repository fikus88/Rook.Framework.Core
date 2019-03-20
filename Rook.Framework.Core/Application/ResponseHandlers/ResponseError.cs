using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Rook.Framework.Core.Application.ResponseHandlers
{
    /// <summary>
    /// This class represents an Error that may have occurred when processing a Message
    /// </summary>
    public sealed class ResponseError
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public ErrorType Type { get; set; }
		public string Field { get; set; }

		public enum ErrorType
		{
			Server = 0,
			Data,
			MissingField,
			EmptyField,
			UnexpectedField,
			InvalidField,
            ItemAlreadyExists
		}
	}
}
