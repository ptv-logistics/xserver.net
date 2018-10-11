// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ptv.XServer.Controls.Map.Tools.Reprojection;

namespace Ptv.XServer.Controls.Map.Layers.WmtsLayer
{
    /// <summary>
    /// Provides extension methods for WmtsMapService.
    /// </summary>
    public static class WmtsMapServiceExtensions
    {
        /// <summary>
        /// Sets the position of a stream to the beginning and returns the stream.
        /// </summary>
        /// <param name="stm">Stream to re-position.</param>
        /// <returns>Stream</returns>
        public static Stream Reset(this Stream stm)
        {
            stm.Seek(0, SeekOrigin.Begin);
            return stm;
        }

        /// <summary>
        /// Determines the minimum value for the x-coordinate of a tile.
        /// </summary>
        /// <param name="tileMatrix">The tile matrix to determine the value for.</param>
        /// <returns>Minimum value.</returns>
        public static int MinX(this ITileMatrix tileMatrix) => Math.Max(0, tileMatrix.MatrixMinX.GetValueOrDefault(0));

        /// <summary>
        /// Determines the maximum value for the x-coordinate of a tile.
        /// </summary>
        /// <param name="tileMatrix">The tile matrix to determine the value for.</param>
        /// <returns>Maximum value.</returns>
        public static int MaxX(this ITileMatrix tileMatrix) => Math.Min(tileMatrix.MatrixWidth - 1, tileMatrix.MatrixMaxX.GetValueOrDefault(tileMatrix.MatrixWidth - 1));

        /// <summary>
        /// Determines the minimum value for the y-coordinate of a tile.
        /// </summary>
        /// <param name="tileMatrix">The tile matrix to determine the value for.</param>
        /// <returns>Minimum value.</returns>
        public static int MinY(this ITileMatrix tileMatrix) => Math.Min(0, tileMatrix.MatrixMinY.GetValueOrDefault(0));

        /// <summary>
        /// Determines the maximum value for the y-coordinate of a tile.
        /// </summary>
        /// <param name="tileMatrix">The tile matrix to determine the value for.</param>
        /// <returns>Maximum value.</returns>
        public static int MaxY(this ITileMatrix tileMatrix) => Math.Min(tileMatrix.MatrixHeight - 1, tileMatrix.MatrixMaxY.GetValueOrDefault(tileMatrix.MatrixHeight - 1));

        /// <summary>
        /// Streams the logical extents of a bounding box. 
        /// This can be used check all values with a single Linq expression.
        /// </summary>
        /// <param name="bounds">The bounding box whose extents are to be streamed.</param>
        /// <returns>The bounding boxes' extents in enumerable form and in the order MinX, MinY, MaxX, MaxY.</returns>
        public static IEnumerable<double> Stream(this IBoundingBox bounds)
        {
            yield return bounds.MinX;
            yield return bounds.MinY;
            yield return bounds.MaxX;
            yield return bounds.MaxY;
        }

        /// <summary>
        /// Utility extension that determines the bounding box for a tile matrix set based on MapServiceExtensions.ApproximateBoundingBox.
        /// </summary>
        /// <param name="matrixSet">Tile matrix set to calculate the bounding box for.</param>
        /// <param name="sourceCrs">The CRS of the tile matrix set.</param>
        /// <param name="targetCrs">The CRS of the bounding box to return.</param>
        /// <param name="nSupportingPoints">Number of supporting points to use.</param>
        /// <param name="resizeFactor">An additional factor for resizing the resulting bounding box to be on the safe side.</param>
        /// <returns>Bounding box.</returns>
        /// <remarks>Refer to MapServiceExtensions.ApproximateBoundingBox for further documentation.</remarks>
        public static IBoundingBox ApproximateBoundingBox(this IEnumerable<ITileMatrix> matrixSet, string sourceCrs, string targetCrs, int nSupportingPoints, double resizeFactor)
        {
            // select all corner points
            var points = matrixSet.Select(m => new[] {m.TopLeftCorner, m.BottomRightCorner}).SelectMany(p => p).ToArray();

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
