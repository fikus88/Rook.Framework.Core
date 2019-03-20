using System.Collections.Generic;
using System.IO;

namespace Rook.Framework.Core.HttpServer
{
    /// <summary>
    /// Interface for any HTTP Response Content
    /// </summary>
    public interface IHttpContent
    {
        /// <summary>
        /// Any content related HTTP headers for the response.
        /// Implementers should return an empty enumerable if no 
        /// content headers are required
        /// </summary>
        IEnumerable<KeyValuePair<string, string>> Headers { get; }

        /// <summary>
        /// Write the content to the stream.
        /// Implementers should write the content data only - not the headers.
        /// </summary>
        /// <param name="stream"></param>
        void WriteToStream(Stream stream);
    }
}
