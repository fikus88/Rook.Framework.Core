namespace Rook.Framework.Core.HttpServer
{
	public interface IRequestBroker
	{
		IHttpResponse HandleRequest(IHttpRequest header, TokenState tokenState);

        // Order in which request brokers should try to handle request. 0 - first.
	    int Precedence { get; }
	}
}