using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rook.Framework.Core.HttpServer
{
    public class EmptyHttpContent : IHttpContent
    {
        /// <summary>
        /// No additional headers required when no content
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> Headers { get; } = Enumerable.Empty<KeyValuePair<string, string>>();

        public void WriteToStream(Stream stream)
        {
            // No content to write
        }
    }
}
