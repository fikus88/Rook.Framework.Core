using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Rook.Framework.Core.Common;
using Rook.Framework.Core.Utils;

namespace Rook.Framework.Core.HttpServer
{
    public interface IHttpRequest
    {
        Uri Uri { get; }
        HttpVerb Verb { get; }
        string Path { get; }
        string HttpVersion { get; }
        CaseInsensitiveDictionary RequestHeader { get; }
        JwtSecurityToken SecurityToken { get; }
        byte[] Body { get; }
        AutoDictionary<string, string> Parameters { get; }
        Guid UserId { get; }

        /// <summary>
        /// Contains a complete list of Organisation Ids which the user is authorised to view.
        /// </summary>
        IEnumerable<Guid> OrganisationIds { get; }

        /// <summary>
        /// Contains a subset list of Organisation Ids which the user is authorised to administer.
        /// </summary>
        IEnumerable<Guid> OrganisationIdsAdministers { get; }

        IEnumerable<T> GetClaimsFromSecurityToken<T>(string claimType);
        void SetUriPattern(string value);        
    }

    public class HttpRequest : IHttpRequest
    {
        private readonly IConfigurationManager _configurationManager;
        private readonly ILogger logger;
        private const string UserIdClaimTypeForId3 = "userid";
        private const string UserIdClaimTypeForId4 = "sub";
        private const string OrganisationIdClaimType = "organisationids";
        private const string OrganisationIdAdministersClaimType = "organisationidsadministers";

        public Uri Uri { get; }
        public HttpVerb Verb { get; }
        public string Path { get; }
        public string HttpVersion { get; }
        public CaseInsensitiveDictionary RequestHeader { get; } = new CaseInsensitiveDictionary();
        public JwtSecurityToken SecurityToken { get; set; }
        public byte[] Body { get; internal set; }
        public AutoDictionary<string, string> Parameters { get; private set; }

        private string uriPattern;

        public Guid UserId
        {
            get
            {
                /*
                 * User Id guid is populated as "userid" in identity server 3.
                 * User Id guid is populated as "sub" in identity server 4.
                 * If it isn't populated from 3, then we'll require it to be populated from 4.
                 */

                var userIdForId3 = GetClaimsFromSecurityToken<Guid>(UserIdClaimTypeForId3).SingleOrDefault();

                return Guid.Empty == userIdForId3
                    ? GetClaimsFromSecurityToken<Guid>(UserIdClaimTypeForId4).Single()
                    : userIdForId3;
            }
        }

        public IEnumerable<Guid> OrganisationIds => GetClaimsFromSecurityToken<Guid>(OrganisationIdClaimType);

        public IEnumerable<Guid> OrganisationIdsAdministers => GetClaimsFromSecurityToken<Guid>(OrganisationIdAdministersClaimType);

        public IEnumerable<T> GetClaimsFromSecurityToken<T>(string claimType)
        {
            if (SecurityToken?.Claims == null)
                return new List<T>();

            return SecurityToken.Claims
                .Where(claim => claim.Type.Equals(claimType, StringComparison.OrdinalIgnoreCase))
                .Select(claim => (T) (System.ComponentModel.TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(claim.Value) ?? default(T)));
        }

        public void SetUriPattern(string value)
        {
            uriPattern = value;
            if (Parameters == null && uriPattern != null)
            {
                string[] pathParts = Path.Split('?');

                // parse UriPattern
                // UriPattern will be like:
                // "/rest/{version}/driver/{driverId}"
                string[] tokens = uriPattern.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                string[] values = pathParts[0].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length != values.Length)
                    throw new InvalidOperationException("An error has occurred during validation of the URI Pattern");

                Parameters = new AutoDictionary<string, string>();

                for (int i = 0; i < tokens.Length; i++)
                {
                    string token = tokens[i];
                    if (token.StartsWith("{") && token.EndsWith("}"))
                    {
                        string key = token.Trim('{', '}');
                        Parameters.Add(key, Uri.UnescapeDataString(values[i]));
                    }
                }

                // parse Get params
                if (pathParts.Length > 1)
                {
                    string paramsString = pathParts[1];
                    ParseParameters(paramsString);
                }
            }
        }

        private void ParseParameters(string paramsString)
        {
            string[] parameters = paramsString.Split('&');
            foreach (string parameter in parameters)
            {
                string[] parts = parameter.Split('=');
                if (parts.Length == 2) Parameters.Add(parts[0], Uri.UnescapeDataString(parts[1]));
            }
        }

        private readonly JwtSecurityTokenHandler _securityTokenHandler = new JwtSecurityTokenHandler();

        public HttpRequest(byte[] headerBytes, IConfigurationManager configurationManager, ILogger logger)
        {
            _configurationManager = configurationManager;
            this.logger = logger;

            string data = Encoding.ASCII.GetString(headerBytes);

            // construct header from http data
            string[] datas = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // First line
            string[] parts = datas[0].Split(' ');

            Verb = (HttpVerb)Enum.Parse(typeof(HttpVerb), parts[0], true);
            Path = parts[1];
            HttpVersion = parts[2];

            // Subsequent lines
            for (int i = 1; i < datas.Length; i++)
            {
                string key = datas[i].Substring(0, datas[i].IndexOf(':'));
                string value = datas[i].Substring(datas[i].IndexOf(' ') + 1);
                RequestHeader.Add(key, value);
            }

            // Uri
            Uri = new Uri($"http://{RequestHeader["Host"]}{Path}");
        }

        public TokenState FinaliseLoad(bool validJwtRequired, TokenValidationParameters tokenValidationParameters)
        {
            if (RequestHeader["Content-Type"] == "application/x-www-form-urlencoded")
            {
                // body contains &-separated parameters
                ParseParameters(Encoding.ASCII.GetString(Body));
            }
            
            //no token required and no token supplied.
            if (!validJwtRequired && !RequestHeader.ContainsKey("Authorization"))
                return TokenState.NotRequired;

            //no token required but token supplied and invalid.
            if (!validJwtRequired && RequestHeader.ContainsKey("Authorization") && !RequestHeader["Authorization"].StartsWith("Bearer "))
                return TokenState.Invalid;

            //token required but valid token not supplied
            if (validJwtRequired && (!RequestHeader.ContainsKey("Authorization") || !RequestHeader["Authorization"].StartsWith("Bearer ")))
                return TokenState.Invalid;

            var payload = RequestHeader["Authorization"].Substring(7);

            try
            {
                SecurityToken token;

                if (validJwtRequired)
                {
                    _securityTokenHandler.ValidateToken(payload, tokenValidationParameters, out token);
                }
                else
                    token = _securityTokenHandler.ReadToken(payload);

                SecurityToken = (JwtSecurityToken)token;

                return TokenState.Ok;
            }
            catch (SecurityTokenExpiredException ex)
            {
                logger.Trace($".net ValidateToken threw {ex.GetType().Name}");
                return TokenState.Expired;
            }
            catch (SecurityTokenNotYetValidException ex)
            {
                logger.Trace($".net ValidateToken threw {ex.GetType().Name}");
                return TokenState.NotYetValid;
            }
            catch (SecurityTokenException ex) // The order of these is important: SecurityTokenException is a base class of SecurityTokenExpiredException and SecurityTokenNotYetValidException as well as others.
            {
                logger.Trace($".net ValidateToken threw {ex.GetType().Name}");
                return TokenState.Invalid;
            }
            catch (ArgumentException ex) // Base class of ArgumentNullException
            {
                logger.Trace($".net ValidateToken threw {ex.GetType().Name}");
                return TokenState.Invalid;
            }
        }        
    }

    internal class OpenIdConfig
    {
        [JsonProperty("jwks_uri")]
        public string JwksUri { get; set; }
    }

    public enum TokenState
    {
        NotRequired,
        Ok,
        Expired,
        NotYetValid,
        Invalid
    }
}
