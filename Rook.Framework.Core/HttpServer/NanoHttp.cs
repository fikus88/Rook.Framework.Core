using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Services;
using Rook.Framework.Core.StructureMap;

namespace Rook.Framework.Core.HttpServer
{
    public class NanoHttp : IStartStoppable
    {
        private TcpListener listener;
        private Task allocator;
        private readonly IRequestBroker[] requestBrokers;
        private readonly IConfigurationManager _configurationManager;
        private readonly ILogger logger;
        private readonly IContainerFacade container;
        private readonly bool validJwtRequired;
        private readonly int port;
        private readonly int backlog;
        private readonly int requestTimeout;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken allocationCancellationToken;

        private TokenValidationParameters TokenValidationParameters;

        private IEnumerable<SecurityKey> GetSigningKeys()
        {
            var httpClient = new HttpClient();

            var identityServerAddress = _configurationManager.Get<string>("IdentityServerAddress");
            var identityServerPath = _configurationManager.Get("IdentityServerPath", string.Empty);

            var discoveryEndpoint = $"{identityServerAddress}{identityServerPath}/.well-known/openid-configuration";

            logger.Debug($"{nameof(NanoHttp)}.{nameof(GetSigningKeys)}",
                new LogItem("Event", "Retrieving identity server settings"),
                new LogItem("Url", discoveryEndpoint));
            var openIdConfigResult = httpClient.GetStringAsync(discoveryEndpoint).Result;
            var openIdConfig = JsonConvert.DeserializeObject<OpenIdConfig>(openIdConfigResult);

            logger.Debug($"{nameof(NanoHttp)}.{nameof(GetSigningKeys)}",
                new LogItem("Event", "Retrieving signing keys"),
                new LogItem("Url", openIdConfig.JwksUri));

            var jwksResult = httpClient.GetStringAsync(openIdConfig.JwksUri).Result;
            try
            {
                var keySet = new JsonWebKeySet(jwksResult);
                return keySet.GetSigningKeys();
            }
            catch (ArgumentException) { return null; }
        }

        public NanoHttp(IRequestBroker[] requestBrokers, IConfigurationManager configurationManager, ILogger logger, IContainerFacade container)
        {
            this.requestBrokers = requestBrokers;
            _configurationManager = configurationManager;
            this.logger = logger;
            this.container = container;

            const int defaultPort = -1;
            const int defaultBacklog = 16;
            const int defaultRequestTimeout = 500;

            port = configurationManager.Get("Port", defaultPort);
            backlog = configurationManager.Get("Backlog", defaultBacklog);
            requestTimeout = configurationManager.Get("RequestTimeout", defaultRequestTimeout);
            validJwtRequired = configurationManager.Get("RequiresJwtValidation", false);
        }

        public StartupPriority StartupPriority { get; } = StartupPriority.Lowest;
        public void Start()
        {
            int actualPort = port < 0 ? 80 : port;
            bool successful = false;
            do
            {
                try
                {
                    listener = new TcpListener(new IPEndPoint(IPAddress.Any, actualPort));
                    listener.Start(backlog);
                    successful = true;
                }
                catch (SocketException)
                {
                    if (port < 0 && actualPort < 250)
                        actualPort++;
                    else
                        throw;
                }
            } while (!successful);

            logger.Info($"{nameof(NanoHttp)}.{nameof(Start)}", new LogItem("Event", "Listener started"), new LogItem("Port", actualPort), new LogItem("Backlog", backlog));

            if (validJwtRequired)
            {
                TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKeys = GetSigningKeys(),
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateActor = false,
                };
            }

            allocationCancellationToken = cts.Token;
            allocator = new Task(AllocationMain, allocationCancellationToken);
            allocator.Start();
        }
        
        public void Stop()
        {
            cts.Cancel();
            allocator = null;
        }

        private void AllocationMain(object cancellationToken)
        {
            try
            {
                while (true)
                {
                    while (!listener.Pending())
                    {
                        ((CancellationToken)cancellationToken).ThrowIfCancellationRequested();
                        Thread.Sleep(1);
                    }

                    Socket s = listener.AcceptSocketAsync().Result;
                    s.NoDelay = true;

                    logger.Trace($"{nameof(NanoHttp)}.{nameof(AllocationMain)}", new LogItem("Event", "Evaluating client"));

                    // Get the client's connection details now rather than letting the logger evaluate it
                    // later - socket may be disposed by the time the logger invokes it
                    var client = s.RemoteEndPoint.ToString();

                    logger.Trace($"{nameof(NanoHttp)}.{nameof(AllocationMain)}", new LogItem("Event", "Accepted socket"),
                        new LogItem("Client", client));

                    Task.Run(() => Processor(s), allocationCancellationToken);

                    logger.Trace($"{nameof(NanoHttp)}.{nameof(AllocationMain)}", new LogItem("Event", "Processor started"),
                        new LogItem("Client", client));
                }
            }
            catch (OperationCanceledException)
            {
                listener.Stop();
                listener.Server.Dispose();
            }
            catch (Exception ex)
            {
                logger.Fatal($"{nameof(NanoHttp)}.{nameof(AllocationMain)}",
                    new LogItem("Event", "Unhandled exception"),
                    new LogItem("Exception", ex.Message),
                    new LogItem("StackTrace", ex.StackTrace));
            }
        }

        private void Processor(Socket s)
        {
            try
            {
                byte[] buffer = new byte[4096];
                byte[] content = null;
                int contentOffset = 0;
                HttpRequest request = null;

                Stopwatch connectionTimer = Stopwatch.StartNew();

                using (NetworkStream ns = new NetworkStream(s))
                {
                    while (true)
                    {
                        int bytesReceived;
                        do
                        {
                            bytesReceived = ns.Read(buffer, 0, buffer.Length);
                            if (bytesReceived != 0) break;
                            Thread.Sleep(1);
                        } while (connectionTimer.ElapsedMilliseconds < requestTimeout);

                        if (bytesReceived == 0)
                        {
                            s.Shutdown(SocketShutdown.Both);
                            s.Dispose();
                            ns.Dispose();
                            logger.Trace($"{nameof(NanoHttp)}.{nameof(Processor)}", new LogItem("Event", "Closed socket"), new LogItem("Reason", $"No request received in {requestTimeout}ms"));
                            return;
                        }

                        byte[] received = new byte[bytesReceived];
                        Array.Copy(buffer, 0, received, 0, bytesReceived);

                        if (request == null)
                        {
                            int i = received.FindPattern((byte)13, (byte)10, (byte)13, (byte)10);

                            // If we have a double CRLF then we have a complete header, otherwise keep looping
                            if (i == -1) continue;

                            request = ParseHeader(i, ref received, ref content, ref contentOffset, ref bytesReceived);

                            if (request == null)
                            {
                                s.Shutdown(SocketShutdown.Both);
                                s.Dispose();
                                ns.Dispose();
                                return;
                            }
                        }

                        Array.Copy(received, 0, content, contentOffset, bytesReceived);
                        contentOffset += bytesReceived;
                        if (contentOffset < content.Length - 1) continue;

                        // Completed loading body, which could have urlencoded content :(
                        TokenState tokenState = request.FinaliseLoad(request.Verb != HttpVerb.Options && validJwtRequired, TokenValidationParameters);

                        request.Body = content;
                        logger.Trace($"{nameof(NanoHttp)}.{nameof(Processor)}", new LogItem("Event", "HandleRequest started"));
                        Stopwatch responseTimer = Stopwatch.StartNew();
                        IHttpResponse response = null;
                        
                        foreach (IRequestBroker requestBroker in requestBrokers.OrderBy(x => x.Precedence))
                        {
                            response = requestBroker.HandleRequest(request, tokenState);
                            if (response != null) break;
                        }

                        if (response == null)
                        {
                            response = container.GetInstance<IHttpResponse>(true);
                            response.HttpStatusCode = HttpStatusCode.NotFound;
                            response.HttpContent = new NotFoundHttpContent();
                        }
                        
                        logger.Trace($"{nameof(NanoHttp)}.{nameof(Processor)}",
                            new LogItem("Event", "HandleRequest completed"),
                            new LogItem("DurationMilliseconds", responseTimer.Elapsed.TotalMilliseconds));
                        
                        response.Headers.Add("Access-Control-Allow-Origin", "*");
                        response.Headers.Add("Access-Control-Allow-Methods", "POST,GET,DELETE,PUT,OPTIONS");
                        response.Headers.Add("Access-Control-Allow-Headers", "authorization");
                        response.Headers.Add("Connection","close");

                        if (response.CachingDisabled)
                        {
                            if (!response.Headers.ContainsKey("Expires"))
                                response.Headers.Add("Expires","-1");

                            if (!response.Headers.ContainsKey("Pragma"))
                                response.Headers.Add("Pragma","no-cache");
                            
                            if(!response.Headers.ContainsKey("Cache-Control"))
                                response.Headers.Add("Cache-Control","no-cache, no-store, must-revalidate");
                        }

                        response.WriteToStream(ns);

                        ns.Flush();

                        if (response.HttpStatusCode == HttpStatusCode.SwitchingProtocols)
                        {
                            // #######################################
                            // #      WebSocket Stuff Goes Here      #
                            // # https://tools.ietf.org/html/rfc6455 #
                            // #######################################

                            // There's a blocking call here into a message loop
                            
                            // while true ...
                            //   wait for a websocket message
                            //   ... etc
                            // end
                        }

                        s.Shutdown(SocketShutdown.Both);

                        s.Dispose();
                        ns.Dispose();

                        logger.Trace($"{nameof(NanoHttp)}.{nameof(Processor)}",
                            new LogItem("Event", "Closed socket"),
                            new LogItem("Reason", "Response complete"),
                             new LogItem("DurationMilliseconds", connectionTimer.Elapsed.TotalMilliseconds));

                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"{nameof(NanoHttp)}.{nameof(Processor)}",
                    new LogItem("Event", "An error occurred processing request"),
                    new LogItem("Exception", ex.ToString));

                throw;
            }
        }

        private HttpRequest ParseHeader(int i, ref byte[] received, ref byte[] content, ref int contentOffset, ref int bytesReceived)
        {
            HttpRequest request;
            try
            {
                i += 1;
                request = new HttpRequest(received.SubArray(i), _configurationManager, logger);
            }
            catch (SecurityTokenException ex)
            {
                logger.Trace($"{nameof(NanoHttp)}.{nameof(Processor)}", new LogItem("Event", "Closed socket"),
                    new LogItem("Reason", $"Authorisation required, but invalid token supplied ({ex.GetType()})"));
                return null;
            }
            logger.Trace($"{nameof(NanoHttp)}.{nameof(Processor)}", new LogItem("Event", "Received request"),
                new LogItem("Verb", request.Verb.ToString), new LogItem("Path", request.Path));
            if (request.RequestHeader.ContainsKey("Content-Length"))
            {
                int contentLength = int.Parse(request.RequestHeader["Content-Length"]);
                content = new byte[contentLength];
                i += 4;
                Array.Copy(received, i, content, 0, Math.Min(received.Length - i, contentLength));
                contentOffset += received.Length - i;
            }
            else
                content = new byte[0];

            received = new byte[0];
            bytesReceived = 0;
            return request;
        }
    }
}
