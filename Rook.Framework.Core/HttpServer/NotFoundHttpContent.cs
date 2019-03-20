using System.Collections.Generic;
using System.IO;
using System.Text;
using Rook.Framework.Core.Common;

namespace Rook.Framework.Core.HttpServer
{
    internal class NotFoundHttpContent : IHttpContent
    {
        public IEnumerable<KeyValuePair<string, string>> Headers => new[] {new KeyValuePair<string, string>("Content-Length",Content.Length.ToString())};

        private readonly byte[] buffer = Encoding.ASCII.GetBytes(Content);

        private static readonly string Content = $"<html><head><title>404 Not Found</title></head><body bgcolor=\"white\"><center><h1>404 Not Found</h1></center><hr><center>MicroService.Core/{ServiceInfo.MicroServiceCoreVersion}</center></body></html>";

        public void WriteToStream(Stream stream)
        {
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}