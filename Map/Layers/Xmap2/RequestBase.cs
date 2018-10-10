// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.IO;
using System.Net;
using System.Text;

namespace Ptv.XServer.Controls.Map.Layers.Xmap2
{
    internal class RequestBase
    {
        protected RequestBase(string baseUrl, string token)
        {
            this.baseUrl = baseUrl;
            this.token = token;
        }

        protected readonly string baseUrl;
        protected readonly string token;

        public string CompleteUrl(string rightUrl) => baseUrl + '/' + rightUrl.TrimStart('/');

        public string Response(string serviceUrl, string jsonRequest) => new Builder(CompleteUrl(serviceUrl), token, jsonRequest).Response
        ;
        public class Builder
        {
            public Builder(string uri, string xToken, string jsonRequest) => Uri(uri).XToken(xToken).JsonRequest(jsonRequest);

            public Builder Uri(string _uri)
            {
                uri = _uri;
                return this;
            }

            private string uri;

            public Builder XToken(string _xToken)
            {
                xToken = _xToken;
                return this;
            }

            private string xToken;

            public Builder JsonRequest(string _jsonRequest)
            {
                jsonRequest = _jsonRequest;
                return this;
            }

            private string jsonRequest;

            public Builder Method(string _method)
            {
                method = _method;
                return this;
            }

            private string method = "POST";

            public Builder ContentType(string _contentType)
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
