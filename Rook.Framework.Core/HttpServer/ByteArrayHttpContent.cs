using System.Collections.Generic;
using System.IO;

namespace Rook.Framework.Core.HttpServer
{
    public class ByteArrayHttpContent : IHttpContent
    {
        private readonly string _contentType;

        public byte[] ContentBytes { get; }

        public IEnumerable<KeyValuePair<string, string>> Headers { get; }

        public ByteArrayHttpContent(byte[] content, string contentType)
        {
            ContentBytes = content;
            _contentType = contentType;

            Headers = new[]
            {
                new KeyValuePair<string, string>("Content-Length", ContentBytes.Length.ToString()),
                new KeyValuePair<string, string>("Content-Type", _contentType)
            };
        }

        public void WriteToStream(Stream stream)
        {
            stream.Write(ContentBytes, 0, ContentBytes.Length);
        }
    }
}
