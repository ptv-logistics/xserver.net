// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Windows;
using System.Text;

namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary> 
    /// This class fetches xMapServer meta-information by requesting the xmap .properties file
    /// and includes other information needed to operate on XMapServer. 
    /// </summary>
    public class XMapMetaInfo
    {
        /// <summary> Logging restricted to this class. </summary>
        private static readonly Logger logger = new Logger("XMapMetaInfo");

        #region Properties

        /// <summary> Gets or sets the xMapServer url (e.g. http://127.0.0.1:50010/xmap/ws/XMap). </summary>
        public string Url { get; }

        /// <summary> Gets or sets the newUser name for basic Http authentication. </summary>
        public string User { get; private set; }

        /// <summary> Gets or sets the password for basic Http authentication. </summary>
        public string Password { get; private set; }

        /// <summary> Gets or sets the copyright text. </summary>
        public string CopyrightText { get; }

        /// <summary> Gets or sets the maximum request size for an xMapServer image. </summary>
        public Size MaxRequestSize { get; }

        #endregion

        #region constructor

        /// <summary> 
        /// Initializes a new instance of the <see cref="XMapMetaInfo"/> class.
        /// </summary>
        public XMapMetaInfo()
        {
            // set default values;
            CopyrightText = "PTV, NAVTEQ, AND";
            MaxRequestSize = new Size(2048, 2048);
        }

        /// <summary> 
        /// Initializes a new instance of the <see cref="XMapMetaInfo"/> class by requesting the xmap .properties file,
        /// initializing the <see cref="CopyrightText"/> and <see cref="MaxRequestSize"/>, if this file could be requested.
        /// The <see cref="User"/> and <see cref="Password"/> remains empty.
        /// </summary>
        /// <param name="baseUrl"> The base url for the xMapServer. It consists of the first part of the complete XMapServer URL,
        /// for example eu-n-test.</param>
        public XMapMetaInfo(string baseUrl) : this()
        {
            Url = baseUrl = XServerUrl.Complete(baseUrl, "XMap");

            if (baseUrl.ToUpper().EndsWith("/XMAP/WS/XMAP"))
                baseUrl = baseUrl.Substring(0, baseUrl.Length - "/XMAP/WS/XMAP".Length);

            try // Customers get Index-out-of-range
            {
                var jspFound = false;
                try
                {
                    if (WebRequest.Create(baseUrl + "/server-info.jsp") is HttpWebRequest request)
                    {
                        request.Timeout = 10000;

                        using (var response = request.GetResponse())
                        using (var reader =
                            new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException(),
                                Encoding.ASCII))
                        {
                            var content = reader.ReadToEnd();

                            var copyrightResult = content;
                            jspFound = SearchValueInJSON(
                                new[] {"\"modules\"", "\"xmap\"", "\"map\"", "\"copyright\" : \""},
                                ref copyrightResult);
                            if (!jspFound)
                            {
                                copyrightResult = content;
                                jspFound = SearchValueInJSON(
                                    new[]
                                    {
                                        "\"modules\"", "\"xmap\"", "\"profiles\"", "\"default\"", "\"map\"",
                                        "\"copyright\" : \""
                                    }, ref copyrightResult);
                            }

                            if (jspFound)
                                CopyrightText = copyrightResult;

                            ///////

                            var maxSizeResult = content;
                            if (SearchValueInJSON(
                                new[]
                                {
                                    "\"modules\"", "\"xmap\"", "\"profiles\"", "\"default\"", "\"image\"",
                                    "\"maxSize\" : \""
                                }, ref maxSizeResult))
                            {
                                var values = maxSizeResult.Split(',');
                                MaxRequestSize = new Size(Int32.Parse(values[0]), Int32.Parse(values[1]));
                                jspFound = true;
                            }
                        }
                    }
                }
                catch (WebException) { }

                if (jspFound) return;
                string defaultPropertiesUrl = baseUrl + "/pages/viewConfFile.jsp?name=xmap-default.properties";
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(defaultPropertiesUrl);
                    request.Timeout = 10000;
                    request.KeepAlive = false;

                    string metaInfo;
                    using (var response = request.GetResponse())
                    using (var stream = response.GetResponseStream())
                    {
                        if (stream == null)
                            return;
                        using (var reader = new StreamReader(stream))
                            metaInfo = reader.ReadToEnd();
                    }

                    // copyright
                    const string copyrightTag = "doNotEdit.map.copyright=";
                    // The tag in the configuration for the copyright text.
                    var i1 = metaInfo.IndexOf(copyrightTag, StringComparison.Ordinal) + copyrightTag.Length;
                    if (i1 > -1)
                    {
                        var i2 = metaInfo.IndexOf('\r', i1);
                        CopyrightText = "© " + metaInfo.Substring(i1, i2 - i1);
                    }

                    // max size
                    const string maxSizeTag = "doNotEdit.image.maxSize=";
                    // The tag in the configuration for the max size value.
                    i1 = metaInfo.IndexOf(maxSizeTag, StringComparison.Ordinal) + maxSizeTag.Length;
                    if (i1 > -1)
                    {
                        var i2 = metaInfo.IndexOf('\r', i1);
                        var values = metaInfo.Substring(i1, i2 - i1).Split(',');
                        MaxRequestSize = new Size(Convert.ToInt32(values[0]), Convert.ToInt32(values[1]));
                    }
                }
                catch (WebException exception)
                {
                    logger.Writeline(TraceEventType.Error, defaultPropertiesUrl + ":" + Environment.NewLine + exception.Message);
                }
            }
            catch { }
        }

        /// <summary>
        /// Searches for a value in a json string at the given path step by step.
        /// </summary>
        /// <param name="path">Path description to the wanted value.</param>
        /// <param name="result">Stores the json string at the beginning and will be set every step.</param>
        /// <returns>Returns true if whole path succeeded and false if not.</returns>
        private static bool SearchValueInJSON(IEnumerable<string> path, ref string result)
        {
            foreach (String pathElement in path)
            {
                if (!result.Contains(pathElement))
                    return false;

                result = result.Substring(result.IndexOf(pathElement, StringComparison.Ordinal) + pathElement.Length);
            }

            result = result.Substring(0, result.IndexOf("\"", StringComparison.Ordinal));
            return true;
        }
        #endregion

        /// <summary>
        /// Setting credential parameters for HTTP authentication, which are necessary for an xServer configuration running in an Azure environment.
        /// After registering in the Customer Centre, you received your personal account data. This data needed for authentication consists of the
        /// username, password and a token. Meanwhile the username and password is needed for every authentication aspect, the token is restricted
        /// to programming reasons. We recommend to use the token only in your further development steps.  
        /// </summary>
        /// <remarks>Further details can be found in http://xserver.ptvgroup.com/de/cookbook/getting-started .</remarks>
        /// <param name="newUser">User name of the HTTP authentication. In combination with the usage of the token, this parameter has to be set
        /// to 'xtok'.</param>
        /// <param name="newPassword">Password of the HTTP authentication. The token value can be used here in combination with the user 'xtok'.</param>
        public void SetCredentials(string newUser, string newPassword)
        {
            User = newUser;
            Password = newPassword;
        }

        /// <summary>
        /// Checking credential parameters for HTTP authentication, which are necessary for an xServer configuration running in an Azure environment.
        /// After registering in the Customer Centre, you received your personal account data. This data needed for authentication consists of the
        /// username, password and a token. Meanwhile the username and password is needed for every authentication aspect, the token is restricted
        /// to programming reasons. We recommend to use the token only in your further development steps.  
        /// </summary>
        /// <param name="user">User name of the HTTP authentication. In combination with the usage of the token, this parameter has to be set to 'xtok'.</param>
        /// <param name="password">Password of the HTTP authentication. The token value can be used here in combination with the user 'xtok'.</param>
        public void CheckCredentials(string user, string password)
        {
            string baseUrl = Url;
            if (baseUrl.ToUpper().EndsWith("/XMAP/WS/XMAP"))
                baseUrl = baseUrl.Substring(0, baseUrl.Length - "/XMAP/WS/XMAP".Length);

            if (XServerUrl.IsXServerInternet(baseUrl) && CheckSampleRequest(baseUrl, password))
                return; // Check OK

            if (new XServer2Version(Url).IsValidUrl())
                throw new ArgumentException("The XMap url addresses an XServer 2 service. This is currently not supported.");

            if (!CheckSampleRequest(baseUrl, null))
                throw new ArgumentException("The XMap url is not configured correctly. Maybe the corresponding server does not exist or is currently not available.");

            if (XServerUrl.IsXServerInternet(baseUrl))
                // The token-free request works, but the token-request failed --> Token is not valid
                throw new AuthenticationException("The specified authentication data does not validate against the specified url. The url itself is working as expected. " +
                                                  "Please note that the pre-supplied access token (15-day-test-license) may have expired.");
        }

        private bool CheckSampleRequest(string baseUrl, string password)
        {
            try
            {
                string url = baseUrl + "/WMS/GetTile/xmap-ajaxfg/0/0/0.png";
                if (!string.IsNullOrEmpty(password))
                    url += "?xtok=" + password;

                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);
                request.Timeout = 5000;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    return response?.StatusCode == HttpStatusCode.OK;
            }
            catch { return false; }
        }
    }
}