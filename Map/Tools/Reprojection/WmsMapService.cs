// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;

namespace Ptv.XServer.Controls.Map.Tools.Reprojection
{
    /// <summary>
    /// Defines the placeholders names for WMS requests made by WmsMapService.
    /// </summary>
    public class WmsPlaceholders
    {
        /// <summary> Creates and initializes an instance of WmsPlaceholders, and defines some defaults. </summary>
        public WmsPlaceholders()
        {
            BoundingBox = "${boundingbox}";
            Width = "${width}";
            Height = "${height}";
        }

        /// <summary> Names the bounding box placeholder. </summary>
        public string BoundingBox { get; set; }

        /// <summary> Names the width placeholder. </summary>
        public string Width { get; set; }

        /// <summary> Names the height placeholder. </summary>
        public string Height { get; set; }

        /// <summary> Returns all placeholder names in list. </summary>
        public IList<string> Names => new[] { BoundingBox, Width, Height };
    }

    /// <summary> Delegate used by WmsMapServer; used for request customization. </summary>
    public delegate void RequestCreatedHandler(WebRequest request);

    /// <summary> Class encapsulating a WMS service. </summary>
    public class WmsMapService : MapService
    {
        /// <summary> Logging restricted to this class. </summary>
        private static readonly Logger logger = new Logger("WmsMapService");
        
        /// <summary> Names the placeholders </summary>
        protected readonly WmsPlaceholders Placeholders = new WmsPlaceholders();

        /// <summary> URL template </summary>
        protected readonly string UrlTemplate;

        /// <summary> Additional event that will be triggered after the request has been created. Used for request customization. </summary>
        public RequestCreatedHandler OnRequestCreated = null;

        /// <summary> Creates and initializes an instance of WmsMapService. </summary>
        /// <param name="urlTemplate">The URL template is expected to include an SRS/CRS parameter. It defines placeholders for bounding box, image width and height.</param>
        /// <param name="minAlignment">Position of (MinX|MinY) in resulting images. Defaults to ContentAlignment.BottomLeft. Refer to MinAlignment for details.</param>
        /// <param name="placeholders">Defines the placeholder names. May be null to use the internal defaults.</param>
        /// <exception cref="ArgumentException">
        /// Fails with an ArgumentException if the url template misses mandatory elements or if the specified alignment is set to an unsupported 
        /// value (such as ContentAlignment.MiddleCenter).
        /// </exception>
        public WmsMapService(string urlTemplate, ContentAlignment minAlignment = ContentAlignment.BottomLeft, WmsPlaceholders placeholders = null)
        {
            MinAlignment = minAlignment;
            // fail if minAlignment is set to an unsupported value
            if (!new[] { ContentAlignment.BottomLeft, ContentAlignment.TopLeft, ContentAlignment.TopRight, ContentAlignment.BottomRight }.Contains(MinAlignment))
                throw new ArgumentException("invalid source alignment");

            // store parameters in members
            UrlTemplate = urlTemplate;

            // Find query part of the URL template
            Dictionary<string, string> query = ParseQueryString(urlTemplate.Substring(urlTemplate.IndexOf('?') + 1));
            
            if (query.ContainsKey("srs"))
                Crs = query["srs"];
            else if (query.ContainsKey("crs"))
                Crs = query["crs"];
            else
                Crs = null;
            if (IsNullOrWhiteSpace(Crs))
                // fail, if CRS / SRS parameter was not included in query
                throw new ArgumentException("Missing SRS/CRS parameter in url template");

            Placeholders = placeholders ?? Placeholders;
            if (Placeholders.Names.Any(IsNullOrWhiteSpace))
                throw new ArgumentException("One or more placeholders are invalid");

            foreach (var placeholder in Placeholders.Names.Where(placeholder => !query.ContainsValue(placeholder))) 
            {
                if (placeholder == Placeholders.BoundingBox)
                    UrlTemplate += "&BBOX=${boundingbox}";
                else if (placeholder == Placeholders.Width)
                    UrlTemplate += "&WIDTH=${width}";
                else
                    UrlTemplate += "&HEIGHT=${height}";
            }
        }

        static bool IsNullOrWhiteSpace(string value) { return string.IsNullOrEmpty(value) || value.Trim().Length == 0; }

        static Dictionary<string, string> ParseQueryString(string queryString)
        {
            return queryString.Split('&').
                Select(keyValue => keyValue.Split('=')). // The query string is converted into a list of key-value-pairs. 
                ToLookup(splitKeyValue => splitKeyValue[0].ToLowerInvariant(), splitKeyValue => splitKeyValue[1]). // a Lookup is used to for duplicates of keys (happened in Euska)
                ToDictionary(kl => kl.Key, kl => kl.First()); // Only the first element of a potential duplicate is used.
        }


        /// <inheritdoc />
        public override Stream GetImageStream(IBoundingBox box, Size requestedSize, out Size effectiveSize)
        {
            try
            {
                // we're going to return requested size ...
                effectiveSize = requestedSize;

                // create web request
                var request = CreateRequest(InstantiateUrl(box, requestedSize));

                // trigger event handler, if any
                OnRequestCreated?.Invoke(request);

                // fetch response
                var response = request.GetResponse();

                // check mime type, fail if returned content is not an image
                if (!response.ContentType.ToLowerInvariant().StartsWith("image/"))
                    throw new InvalidDataException("delivered content is not an image");

                // should be an image ... return stream 
                return response.GetResponseStream();
            }
            catch (Exception exception)
            {
                logger.Writeline(TraceEventType.Error, UrlTemplate + ":" + Environment.NewLine + exception.Message);
                throw;
            }
        }

        /// <summary>
        /// Creates and initializes a url using the internal template, 
        /// given the request parameters that replace the placeholders in the template.
        /// </summary>
        /// <param name="box">Requested bounding box.</param>
        /// <param name="size">Requested image size.</param>
        /// <returns>Instantiated url that can be used to request the actual map image.</returns>
        /// <remarks>Derived classes may override this method to customize 
        /// the url being used for requesting map images.</remarks>
        protected virtual string InstantiateUrl(IBoundingBox box, Size size)
        {
            // instantiate url
            return UrlTemplate
                .Replace(Placeholders.BoundingBox, string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", box.MinX, box.MinY, box.MaxX, box.MaxY))
                .Replace(Placeholders.Width, size.Width.ToString(CultureInfo.InvariantCulture))
                .Replace(Placeholders.Height, size.Height.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary> Creates and initializes the WebRequest that requests the map image. </summary>
        /// <param name="url">The URL for the requested map image, as returned by InstantiateUrl.</param>
        /// <returns>WebRequest instance.</returns>
        /// <remarks>Derived classes may override this method to customize 
        /// the web request being used for requesting map images.</remarks>
        protected virtual WebRequest CreateRequest(string url)
        {
            return WebRequest.Create(url);   
        }
    }
}
