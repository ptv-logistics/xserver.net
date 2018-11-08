// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.IO;
using System.Net;
using System.Text;

namespace Ptv.XServer.Controls.Map.Layers.Xmap2
{
    internal class RequestBase
    {
        protected RequestBase(IXServerVersion xServerVersion)
        {
            this.xServerVersion = xServerVersion;
        }

        protected readonly IXServerVersion xServerVersion;

        public string CompleteUrl(string protocolShortcut, string moduleName, string service) => xServerVersion.WithServicePath(protocolShortcut, moduleName) + '/' + service.TrimStart('/');

        public string Response(string protocolShortcut, string moduleName, string service, string jsonRequest) 
            => new RequestBuilder(CompleteUrl(protocolShortcut, moduleName, service), xServerVersion.Token, jsonRequest).Response;

        public class RequestBuilder
        {
            public RequestBuilder(string uri, string xToken, string jsonRequest) => Uri(uri).XToken(xToken).JsonRequest(jsonRequest);

            public RequestBuilder Uri(string _uri)
            {
                uri = _uri;
                return this;
            }

            private string uri;

            public RequestBuilder XToken(string _xToken)
            {
                xToken = _xToken;
                return this;
            }

            private string xToken;

            public RequestBuilder JsonRequest(string _jsonRequest)
            {
                jsonRequest = _jsonRequest;
                return this;
            }

            private string jsonRequest;

            public RequestBuilder Method(string _method)
            {
                method = _method;
                return this;
            }

            private string method = "POST";

            public RequestBuilder ContentType(string _contentType)
            {
                contentType = _contentType;
                return this;
            }

            private string contentType = "application/json";

            public string Response
            {
                get
                {
                    const string EmptyJson = "null";

                    try
                    {
                        var request = WebRequest.Create(uri);
                        request.Method = method;
                        request.ContentType = contentType;

                        if (!string.IsNullOrEmpty(xToken))
                            request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes($"xtok:{xToken}"));

                        using (var stream = request.GetRequestStream())
                        using (var writer = new StreamWriter(stream))
                            writer.Write(jsonRequest);

                        request = LayerFactory.ModifyRequest?.Invoke(request) ?? request;

                        using (var webResponse = request.GetResponse())
                        using (var stream = webResponse.GetResponseStream())
                            if (stream != null)
                                using (var reader = new StreamReader(stream, Encoding.UTF8))
                                    return reader.ReadToEnd();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    return EmptyJson;
                }
            }
        }
    }
}
