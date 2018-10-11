// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

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
}
