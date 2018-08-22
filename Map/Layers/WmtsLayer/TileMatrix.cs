using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Ptv.XServer.Controls.Map.Tools.Reprojection;

namespace Ptv.XServer.Controls.Map.Layers.WmtsLayer
{
    using Point = System.Windows.Point;

    /// <summary>
    /// Defines a tile matrix as it is basically part of the WMTS service specification, modified to provide 
    /// all necessary parameters for the WmtsMapService implementation to work properly. 
    /// </summary>
    public interface ITileMatrix
    {
        /// <summary>
        /// Tile matrix identifier; value of the {z} URL template parameter.
        /// </summary>
        string Identifier { get; }

        /// <summary>
        /// Number of horizontal tiles.
        /// </summary>
        /// <remarks>
        /// Limits the value of the {x} URL template parameter; 0 ... (MatrixWidth-1) tiles can be requested from the WMTS services
        /// if no further limits have been specified by MatrixMinX and MatrixMaxX.
        /// </remarks>
        int MatrixWidth { get; }

        /// <summary>
        /// Number of vertical tiles.
        /// </summary>
        /// <remarks>
        /// Limits the value of the {y} URL template parameter; 0 ... (MatrixHeight-1) tiles can be requested from the WMTS services 
        /// if no further limits have been specified by MatrixMinY and MatrixMaxY.
        /// </remarks>
        int MatrixHeight { get; }

        /// <summary>
        /// Pixel width of a tile returned by the WMTS service.
        /// </summary>
        int TileWidth { get; }

        /// <summary>
        /// Pixel height of a tile returned by the WMTS service.
        /// </summary>
        int TileHeight { get; }

        /// <summary>
        /// The top left coordinate of the area covered by the tiles of the matrix set (with top &gt; bottom).
        /// </summary>
        Point TopLeftCorner { get; }

        /// <summary>
        /// The bottom right coordinate of the area covered by the tiles of the matrix set (with bottom &lt; top).
        /// </summary>
        /// <remarks>
        /// The WMTS specification does not include the bottom right corner but suggests to calculate it like this:
        /// <code>
        ///    var pixelSpan = ScaleDenominator * 0.28 * 1e-3 / metersPerUnit(CRS);
        ///    var tileSpanX = TileWidth * pixelSpan;
        ///    var tileSpanY = TileHeight * pixelSpan;
        /// 
        ///    return new Point(
        ///       TopLeftCorner.X + tileSpanX* MatrixWidth,
        ///       TopLeftCorner.Y - tileSpanY* MatrixHeight
        ///    );
        /// </code>
        /// For the bottom right corner to be correct, it is crucial to have a proper pixel span. However,
        /// the formula above incorporates additional resolution parameters (both device and CRS) that are not 
        /// entirely clear or seem to provide fixed values in any circumstances. We therefore decided to 
        /// explicitely include the BottomRightCorner as a property in the interface in order to make 
        /// implementations robust against misinterpretations. As the ScaleDenominator within a single matrix 
        /// set is solely used to calculate the bottom right corner, we also decided to drop that property.
        /// </remarks>
        Point BottomRightCorner { get; }

        /// <summary>
        /// Optionally limits the number of tiles by specifying a minimum value for the {x} URL template parameter.
        /// </summary>
        int? MatrixMinX { get; }

        /// <summary>
        /// Optionally limits the number of tiles by specifying a maximum value for the {x} URL template parameter.
        /// </summary>
        int? MatrixMaxX { get; }

        /// <summary>
        /// Optionally limits the number of tiles by specifying a minimum value for the {y} URL template parameter.
        /// </summary>
        int? MatrixMinY { get; }

        /// <summary>
        /// Optionally limits the number of tiles by specifying a maximum value for the {y} URL template parameter.
        /// </summary>
        int? MatrixMaxY { get; }
    }

    /// <summary>
    /// Provides an ITileMatrix implementation that does not limit the tile coordinates and works with a fixed 256x256 image size.
    /// It calculates BottomRightCorner based on the ScaleDenominator that is part of the WMTS service capabilities.
    /// </summary>
    public class TileMatrix : ITileMatrix
    {
        /// <summary>
        /// Creates and initializes an instance of TileMatrix.
        /// </summary>
        /// <param name="identifier">The identifier of the matrix set.</param>
        /// <param name="scaleDenominator">The scale denominator of the matrix set.</param>
        /// <param name="topLeftCornerX">The x-coordinate of the top left corner.</param>
        /// <param name="topLeftCornerY">The y-coordinate of the top left corner.</param>
        /// <param name="matrixWidth">The matrix width.</param>
        /// <param name="matrixHeight">The matrix height.</param>
        public TileMatrix(string identifier, double scaleDenominator, double topLeftCornerX, double topLeftCornerY, int matrixWidth, int matrixHeight)
        {
            Identifier = identifier;
            ScaleDenominator = scaleDenominator;
            TopLeftCorner = new Point(topLeftCornerX, topLeftCornerY);
            MatrixWidth = matrixWidth;
            MatrixHeight = matrixHeight;
        }

        /// <inheritdoc/>
        public string Identifier { get; set; }

        /// <summary>
        /// Scale denominator as it it part of the WMTS service capabilities.
        /// </summary>
        public double ScaleDenominator { get; set; }

        /// <inheritdoc/>
        public int MatrixWidth { get; set; }

        /// <inheritdoc/>
        public int MatrixHeight { get; set; }

        /// <inheritdoc/>
        public int? MatrixMaxX => null;

        /// <inheritdoc/>
        public int? MatrixMaxY => null;

        /// <inheritdoc/>
        public int? MatrixMinX => null;

        /// <inheritdoc/>
        public int? MatrixMinY => null;

        /// <inheritdoc/>
        public int TileHeight => 256;

        /// <inheritdoc/>
        public int TileWidth => 256;

        /// <inheritdoc/>
        public Point TopLeftCorner { get; set; }

        /// <inheritdoc/>
        public Point BottomRightCorner
        {
            get
            {
                // use the formula from the WMTS specification to calculate the 
                // bottom right corner. Assume metersPerUnit is "1".

                var metersPerUnit = 1;
                var pixelSpan = ScaleDenominator * 0.28 * 1e-3 / metersPerUnit;

                var tileSpanX = TileWidth * pixelSpan;
                var tileSpanY = TileHeight * pixelSpan;

                return new Point(
                    TopLeftCorner.X + tileSpanX * MatrixWidth,
                    TopLeftCorner.Y - tileSpanY * MatrixHeight
                );
            }
        }
    }

    /// <summary>
    /// Basically defines a tile matrix set as a CRS-bound list of tile matrices. Provides additional methods for 
    /// selecting a tile matrix based on a map request (see SelectTileMatrixDelegate).
    /// </summary>
    public class TileMatrixSet : List<TileMatrix>
    {
        /// <summary>
        /// Creates and initializes an instance of TileMatrixSet.
        /// </summary>
        /// <param name="crs">The CRS of the tile matrix set.</param>
        public TileMatrixSet(string crs)
        {
            CRS = crs;
        }

        /// <summary>
        /// The CRS of this tile matrix set.
        /// </summary>
        public string CRS { get; }

        /// <summary>
        /// Calculates a zoom factor of a tile matrix.
        /// </summary>
        /// <param name="tileMatrix">Tile matrix to calculate the zoom for.</param>
        /// <returns>Zoom factor</returns>
        private static double CalculateZoom(ITileMatrix tileMatrix)
        {
            // calculate the logical bounds of a single tile
            var logicalSize = new SizeF(
                (float)(Math.Abs(tileMatrix.BottomRightCorner.X - tileMatrix.TopLeftCorner.X) / tileMatrix.MatrixWidth),
                (float)(Math.Abs(tileMatrix.TopLeftCorner.Y - tileMatrix.BottomRightCorner.Y) / tileMatrix.MatrixHeight)
            );

            // get the pixel size of a tile directly provided by the tile matrix
            var pixelSize = new Size(tileMatrix.TileWidth, tileMatrix.TileHeight);

            // use CalculateZoom with the logical bounds and the pixel size to 
            // calculate a representative zoom factor for the tile matrix
            return CalculateZoom(logicalSize, pixelSize);
        }

        /// <summary>
        /// Calculates a zoom factor given the logical bounds and the pixel size of a map section.
        /// </summary>
        /// <param name="boundingBox">Logical bounds of the map section.</param>
        /// <param name="pixelSize">Pixel size of the map section.</param>
        /// <returns>Zoom factor</returns>
        private static double CalculateZoom(IBoundingBox boundingBox, Size pixelSize)
        {
            return CalculateZoom(boundingBox.Size(), pixelSize);
        }

        /// <summary>
        /// Calculates a zoom factor given the logical bounds and the pixel size of a map section.
        /// </summary>
        /// <param name="logicalSize">Logical bounds of the map section.</param>
        /// <param name="pixelSize">Pixel size of the map section.</param>
        /// <returns>Zoom factor</returns>
        private static double CalculateZoom(SizeF logicalSize, Size pixelSize)
        {
            // return the logical extent of a single pixel 
            // from the top left to the bottom right corner

            var w = logicalSize.Width / pixelSize.Width;
            var h = logicalSize.Height / pixelSize.Height;

            return Math.Sqrt(w * w + h * h);
        }

        /// <summary>
        /// Provides an implementation of the SelectTileMatrixDelegate, 
        /// selecting the best matching tile matrix for the requested map section.
        /// </summary>
        /// <param name="boundingBox">Requested logical bounds of the map section.</param>
        /// <param name="size">Requested pixel size.</param>
        /// <returns></returns>
        public ITileMatrix SelectTileMatrix(IBoundingBox boundingBox, Size size)
        {
            // fail, if we have no tile matrices
            if (Count < 1)
                return null;

            // first, calculate the zoom factor for the requested map section
            var zoom = CalculateZoom(boundingBox, size);

            // second, calculate a zoom factor for all tile matrices and with that the |delta| to the zoom factor of the requested map section.
            // Order by |delta|, ascending - the top element then is the closest tile matrix we have. 
            var idx = this
                .Select((matrixSet, index) => new {index, delta = Math.Abs(CalculateZoom(matrixSet) - zoom) })
                .OrderBy(item => item.delta)
                .First()
                .index;

            // return the closest tile matrix
            return this[idx];
        }

        /// <summary>
        /// Determines the bounding box covering the tile matrix set.
        /// </summary>
        /// <param name="targetCRS">Target CRS</param>
        /// <param name="nSupportingPoints">Number of supporting points; see ApproximateBoundingBox extension</param>
        /// <param name="resizeFactor">ResizeFactor; see ApproximateBoundingBox extension</param>
        /// <returns>Approximated bounding box</returns>
        public IBoundingBox ApproximateBoundingBox(string targetCRS, int nSupportingPoints = 8, double resizeFactor = 1.025)
        {
            return ApproximateBoundingBox(sourceCrs: CRS, targetCrs: targetCRS, nSupportingPoints: nSupportingPoints, resizeFactor: resizeFactor);
        }


        /// <summary>
        /// Utility extension that determines the bounding box for a tile matrix set based on MapServiceExtensions.ApproximateBoundingBox.
        /// </summary>
        /// <param name="sourceCrs">The CRS of the tile matrix set.</param>
        /// <param name="targetCrs">The CRS of the bounding box to return.</param>
        /// <param name="nSupportingPoints">Number of supporting points to use.</param>
        /// <param name="resizeFactor">An addtional factor for resizing the resulting bounding box to be on the safe side.</param>
        /// <returns>Bounding box.</returns>
        /// <remarks>Refer to MapServiceExtensions.ApproximateBoundingBox for further documentation.</remarks>
        public IBoundingBox ApproximateBoundingBox(string sourceCrs, string targetCrs, int nSupportingPoints, double resizeFactor)
        {
            // select all corner points
            var points = this.Select(m => new[] { m.TopLeftCorner, m.BottomRightCorner }).SelectMany(p => p).ToArray();

            // create unified map rectangle
            var mapRect = new Tools.Reprojection.MapRectangle(
                points.Min(p => p.X),
                points.Max(p => p.Y),
                points.Max(p => p.X),
                points.Min(p => p.Y)
            );

            // approximate bounding box
            return mapRect.ApproximateBoundingBox(sourceCrs, targetCrs, nSupportingPoints, resizeFactor);
        }

    }
}
