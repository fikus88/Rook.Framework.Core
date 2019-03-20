using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Utils;

namespace Rook.Framework.Core.HttpServer
{
    public interface IHttpResponse
    {
        HttpStatusCode HttpStatusCode { get; set; }
        CaseInsensitiveDictionary Headers { get; }
        /// <summary>
        /// Explicitly sets cache control headers (Pragma, Cache-Control, Expires) to prevent client-side caching.
        /// If your implementation of IHttpResponse explicitly caches, set this to false and provide your own headers.
        /// </summary>
        bool CachingDisabled { get; set; }
        
        [Obsolete("Use HttpContent property instead")]
        byte[] Content { get; set; }

        string ContentType { get; set; }

        IHttpContent HttpContent { get; set; }

        /// <summary>
        /// Sets ASCII string content from a C# string (could be unicode)
        /// </summary>
        /// <param name="content"></param>
        void SetStringContent(string content);

        /// <summary>
        /// Sets serialised JSON string content (UTF8 encoded) from an object (could be dynamic, or a strongly typed object)
        /// </summary>
        /// <param name="content"></param>
        void SetObjectContent(object content);

        void WriteToStream(Stream stream);
    }

    public sealed class HttpResponse : IHttpResponse
    {
		private readonly IDateTimeProvider _dateTimeProvider;
        private readonly ILogger _logger;

        public HttpStatusCode HttpStatusCode { get; set; } = HttpStatusCode.OK;
        public CaseInsensitiveDictionary Headers { get; } = new CaseInsensitiveDictionary();
        public bool CachingDisabled { get; set; } = true;

        public IHttpContent HttpContent { get; set; } = new EmptyHttpContent();

        /// <summary>
        /// It is not necessary to set this field. Default is application/json
        /// </summary>
        public string ContentType { get; set; }

        public HttpResponse(IDateTimeProvider dateTimeProvider, ILogger logger)
        {
            _dateTimeProvider = dateTimeProvider;
            _logger = logger;
        }

        [Obsolete("Use HttpContent instead")]
        public byte[] Content
        {
            get
            {
                var byteContent = HttpContent as ByteArrayHttpContent;

                return byteContent?.ContentBytes;
            }
            set => HttpContent = new ByteArrayHttpContent(value, ContentType ?? "application/json");
        }

        /// <summary>
        /// Sets ASCII string content from a C# string (could be unicode)
        /// </summary>
        /// <param name="content"></param>
        public void SetStringContent(string content)
		{
            // Most callers use this method to pass a JSON string in - so json content type is expected            
		    HttpContent = new ByteArrayHttpContent(Encoding.ASCII.GetBytes(content), ContentType ?? "application/json");
		}

		/// <summary>
		/// Sets serialised JSON string content (UTF8 encoded) from an object (could be dynamic, or a strongly typed object)
		/// </summary>
		/// <param name="content"></param>
		public void SetObjectContent(object content)
		{   
            HttpContent = new ByteArrayHttpContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content)), ContentType ?? "application/json");
        }
        
        public void WriteToStream(Stream stream)
        {
            try
            {
                WriteHeader(stream);

                // Write the content data (may not write anything if the IHttpContent implementation doesn't write anything)
                HttpContent.WriteToStream(stream);
            }
            catch (Exception e)
            {                
                if (e.InnerException is SocketException se && se.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    // if the client has aborted the socket then just warn, else rethrow the exception
                    _logger.Warn($"{nameof(HttpResponse)}.{nameof(WriteToStream)}",
                        new LogItem("Event", "WriteToStream failed. Socket aborted"),
                        new LogItem("Exception", se.Message),
                        new LogItem("StackTrace", se.StackTrace));
                    return;
                }
                throw;
            }
        }

        private void WriteHeader(Stream stream)
        {
            StringBuilder responseString = new StringBuilder();
            responseString.Append($"HTTP/1.1 {(int)HttpStatusCode} {Enum.GetName(typeof(HttpStatusCode), HttpStatusCode)}\r\n");
            responseString.Append($"Date: {_dateTimeProvider.UtcNow:F}\r\n");

            // Content specific headers (could be empty):
            foreach (KeyValuePair<string, string> pair in HttpContent.Headers)
                responseString.Append($"{pair.Key}: {pair.Value}\r\n");

            // Headers on the response itself:
            foreach (KeyValuePair<string, string> keyValuePair in Headers)
                responseString.Append($"{keyValuePair.Key}: {keyValuePair.Value}\r\n");

            responseString.Append("\r\n");

            var headerBytes = Encoding.ASCII.GetBytes(responseString.ToString());

            stream.Write(headerBytes, 0, headerBytes.Length);
        }
	}
}