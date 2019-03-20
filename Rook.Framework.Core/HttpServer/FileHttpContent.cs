using System.Collections.Generic;
using System.IO;

namespace Rook.Framework.Core.HttpServer
{
    /// <summary>
    /// Implementation of IHttpContent that allows a file to streamed from disk to the 
    /// HTTP response without being entirely loaded into memory
    /// </summary>
    public class FileHttpContent : IHttpContent
    {
        private readonly string _filename;
        private readonly int _bufferSize;
        
        /// <summary>
        /// Contains Content-Type and Content-Length HTTP response headers
        /// </summary>
        public IEnumerable<KeyValuePair<string, string>> Headers { get; }

        /// <summary>
        /// Creates a new FileHttpContent object for an existing file
        /// </summary>
        /// <param name="filename">Name of the file to stream to the response. Can be relative or absolute path</param>
        /// <param name="contentType">The string to use as the Content-Type HTTP header.  Default is application/octet-stream</param>
        /// <param name="bufferSize">Buffer size in bytes to use when copying the file to the response stream. Default is 81920 (80KB)</param>
        /// <exception cref="FileNotFoundException">If <paramref name="filename"/> cannot be found</exception>
        public FileHttpContent(string filename, 
            string contentType = "application/octet-stream", 
            int bufferSize = 81920)
        {
            var fileInfo = new FileInfo(filename);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("File not found", filename);
            }

            _filename = fileInfo.FullName;
            _bufferSize = bufferSize;

            Headers = new[]
            {
                new KeyValuePair<string, string>("Content-Length", fileInfo.Length.ToString()),
                new KeyValuePair<string, string>("Content-Type", contentType),
                new KeyValuePair<string, string>("Last-Modified", $"{fileInfo.LastWriteTimeUtc:F}")
            };
        }

        /// <summary>
        /// Copies a FileStream to the
        /// </summary>
        /// <param name="stream"></param>
        public void WriteToStream(Stream stream)
        {
            using (var fileStream = new FileStream(_filename, FileMode.Open, FileAccess.Read))
            {
                fileStream.CopyTo(stream, _bufferSize);
            }
        }
    }
}
