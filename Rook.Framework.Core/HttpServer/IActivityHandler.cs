namespace Rook.Framework.Core.HttpServer
{
    public interface IActivityHandler
    {
        void Handle(IHttpRequest request, IHttpResponse response);

        /// <summary>
        /// Self-documenting property: An example of the document expected to be passed with the request
        /// </summary>
        dynamic ExampleRequestDocument { get; }
        
        /// <summary>
        /// Self-documenting property: An example of the document expected to be passed in the response
        /// </summary>
        dynamic ExampleResponseDocument { get; }
    }
}