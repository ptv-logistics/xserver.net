// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Net;

namespace Ptv.XServer.Controls.Map.TileProviders
{
    /// <summary>
    /// Class which wraps the xServer web proxy and implements <see cref="xserver.IXMapWSBinding"/> interface.
    /// </summary>
    public class XMapWSServiceImpl : xserver.XMapWSService, xserver.IXMapWSBinding
    {
        /// <summary> Initializes a new instance of the <see cref="XMapWSServiceImpl"/> class. </summary>
        /// <param name="url"> The url of the xMapServer. </param>
        public XMapWSServiceImpl(string url)
        {
            Url = url;
        }
    }

    /// <summary>
    /// Class which wraps the xServer web proxy and implements <see cref="xserver.IXMapWSBinding"/> interface.
    /// This implementation has some optimizations, like KeepAlive 
    /// </summary>
    public class XMapWSServiceImplEx : xserver.XMapWSService, xserver.IXMapWSBinding
    {
        /// <summary>
        /// The xserver user name.
        /// </summary>
        string User;

        /// <summary>
        /// The xserver password.
        /// </summary>
        string Password;

        /// <summary> Initializes a new instance of the <see cref="XMapWSServiceImpl"/> class. </summary>
        /// <param name="url"> The url of the xMapServer. </param>
        /// <param name="user"> The user of the xMapServer. </param>
        /// <param name="password"> The password of the xMapServer. </param>
        public XMapWSServiceImplEx(string url, string user, string password)
        {
            Url = url;
            User = user;
            Password = password;
        }

        /// <inheritdoc/>
        protected override WebRequest GetWebRequest(Uri uri)
        {
            HttpWebRequest webRequest = WebRequest.Create(uri) as HttpWebRequest;
            webRequest.KeepAlive = true;
            if (!string.IsNullOrEmpty(User) && !string.IsNullOrEmpty(Password))
            {
                webRequest.PreAuthenticate = true;
                webRequest.Credentials = new CredentialCache { { uri, "Basic", new NetworkCredential(User, Password) } };
            }

            return webRequest;
        }
    }
}
