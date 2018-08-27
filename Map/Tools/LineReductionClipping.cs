// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Windows;


namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary> This class can be used to clip polylines to a given rectangle. </summary>
    public class LineReductionClipping
    {
        /// <summary>
        /// Clips the given polyline to the given clipping region. Using the specified viewport extents, this
        /// routine also eliminates duplicate points that would otherwise occur after the viewport transformation.
        /// </summary>
        /// <typeparam name="P"> Output polyline type. </typeparam>
        /// <typeparam name="T"> Input point type. </typeparam>
        /// <param name="sz"> Viewport extents. </param>
        /// <param name="rc"> Logical clipping rectangle. </param>
        /// <param name="polyline"> Input polyline. </param>
        /// <param name="convPnt"> Function converting an input point of type T to an System.Windows.Point. </param>
        /// <param name="addPnt"> Procedure adding a System.Windows.Point to the resulting polyline of type P. </param>
        /// <returns> The reduced and clipped polyline. </returns>
        public static ICollection<P> ClipPolylineReducePoints<P, T>(Size sz, Rect rc, ICollection<T> polyline, Func<T, Point> convPnt, Action<P, Point> addPnt) where P : class, new()
        {
            return ClipPolylineReducePoints(sz, rc, new[] { polyline }, convPnt, addPnt);
        }

        /// <summary>
        /// Clips the given polyline to the given clipping region. Using the specified viewport extents, this
        /// routine also eliminates duplicate points that would otherwise occur after the viewport transformation.
        /// </summary>
        /// <typeparam name="P"> Output polyline type. </typeparam>
        /// <typeparam name="T"> Input point type. </typeparam>
        /// <param name="sz"> Viewport extents. </param>
        /// <param name="rc"> Logical clipping rectangle. </param>
        /// <param name="polyline"> Input polyline. </param>
        /// <param name="convPnt"> Function converting an input point of type T to an System.Windows.Point. </param>
        /// <param name="addPnt"> Procedure adding a System.Windows.Point to the resulting polyline of type P. </param>
        /// <param name="reductionOnly">Specifies that only the reduction should be performed without clipping.</param>
        /// <returns> The reduced and optionally clipped polyline. </returns>
        public static ICollection<P> ClipPolylineReducePoints<P, T>(Size sz, Rect rc, ICollection<T> polyline, Func<T, Point> convPnt, Action<P, Point> addPnt, bool reductionOnly) where P : class, new()
        {
            return ClipPolylineReducePoints(sz, rc, new[] { polyline }, convPnt, addPnt, reductionOnly);
        }

        /// <summary>
        /// Clips the given polylines to the given clipping region. Using the specified viewport extents, this
        /// routine also eliminates duplicate points that would otherwise occur after the viewport transformation.
        /// </summary>
        /// <typeparam name="P"> Output polyline type. </typeparam>
        /// <typeparam name="T"> Input point type. </typeparam>
        /// <param name="sz"> Viewport extents. </param>
        /// <param name="rc"> Logical clipping rectangle. </param>
        /// <param name="polylines"> Input polylines. </param>
        /// <param name="convPnt"> Function converting an input point of type T to System.Windows.Point. </param>
        /// <param name="addPnt"> Procedure adding a System.Windows.Point to the resulting polyline of type P. </param>
        /// <returns> The reduced and clipped polyline. </returns>
        public static ICollection<P> ClipPolylineReducePoints<P, T>(Size sz, Rect rc, ICollection<ICollection<T>> polylines, Func<T, Point> convPnt, Action<P, Point> addPnt) where P : class, new()
        {
            return ClipPolylineReducePoints(sz, rc, polylines, convPnt, addPnt, false);
        }

        /// <summary>
        /// Clips the given polylines to the given clipping region. Using the specified viewport extents, this
        /// routine also eliminates duplicate points that would otherwise occur after the viewport transformation.
        /// </summary>
        /// <typeparam name="P"> Output polyline type. </typeparam>
        /// <typeparam name="T"> Input point type. </typeparam>
        /// <param name="sz"> Viewport extents. </param>
        /// <param name="rc"> Logical clipping rectangle. </param>
        /// <param name="polylines"> Input polylines. </param>
        /// <param name="convPnt"> Function converting an input point of type T to System.Windows.Point. </param>
        /// <param name="addPnt"> Procedure adding a System.Windows.Point to the resulting polyline of type P. </param>
        /// <param name="reductionOnly">Specifies that only the reduction should be performed without clipping.</param>
        /// <returns> The reduced and optionally clipped polyline. </returns>
        public static ICollection<P> ClipPolylineReducePoints<P, T>(Size sz, Rect rc, ICollection<ICollection<T>> polylines, Func<T, Point> convPnt, Action<P, Point> addPnt, bool reductionOnly) where P : class, new()
        {
            // re-initialize rc, assuring left <= right and top <= bottom
            rc = new Rect(Math.Min(rc.Left, rc.Right), Math.Min(rc.Top, rc.Bottom), Math.Abs(rc.Width), Math.Abs(rc.Height));

            // create result object storing the clipped lines
            var polylineBuilder = new PolylineBuilder<P>(addPnt, new Size(rc.Width / sz.Width, rc.Height / sz.Height));
            if (polylines == null) return polylineBuilder.Polyline;

            // loop through given polylines
            foreach (ICollection<T> polyline in polylines)
            {
                // enumerator for accessing points
                IEnumerator<T> e = polyline?.GetEnumerator();

                // fetch first point
                if (e == null || !e.MoveNext()) continue;

                // initialize starting point
                var p0 = convPnt(e.Current);

                // number of points in current polyline
                int lastPointIndex = polyline.Count - 1;

                // loop through remaining points
                for (int pointIndex = 1; e.MoveNext(); ++pointIndex)
                {
                    // fetch end point. p0 and p1 now mark the start and end point of the current line.
                    var p1 = convPnt(e.Current);

                    // clip the current line. CohenSutherland.clip returns true, if any section of the current line is visible.
                    if (reductionOnly || CohenSutherlandClipping.Clip(rc, ref p0, ref p1))
                    {
                        // Append current line. Append also does the magic of point reduction and line splitting polylines where necessary.
                        polylineBuilder.Append(p0, pointIndex == 1, p1, pointIndex == lastPointIndex);
                    }

                    // current end point is the next starting point
                    p0 = convPnt(e.Current);
                }
            }

            // return the polyline
            return polylineBuilder.Polyline;
        }

        /// <summary>
        /// Class for building a polyline out of several single line snippets. 
        /// Internally used by the clipping algorithm, this class does all the work 
        /// of storing and reducing the points passed in from the clipping algorithm.
        /// </summary>
        /// <typeparam name="P"> Polyline type. </typeparam>
        public class PolylineBuilder<P> where P : class, new()
        {
            #region private variables
            /// <summary> The resulting polylines. </summary>
            private readonly List<P> polylineList = new List<P>();
            /// <summary> Threshold used for point reduction. </summary>
            private readonly Size pointReductionThreshold;
            /// <summary> Threshold used to check if two points differ. </summary>
            private readonly Size differThreshold = new Size(1e-4, 1e-4);
            /// <summary> The current polyline, initialized when the first line is appended. </summary>
            private P polyline;
            /// <summary> Delegate for adding a point to a polyline of type P. </summary>
            private readonly Action<P, Point> addPnt;
            /// <summary> End point of the last line appended. </summary>
            private Point last_p1;
            /// <summary> End point of current polyline. </summary>
            private Point polylineEnd;
            #endregion

            #region constructor
            /// <summary> Initializes a new instance of the <see cref="PolylineBuilder{P}"/> class. </summary>
            /// <param name="addPnt"> Procedure for adding a point to a generic polyline of type P. </param>
            /// <param name="pointReductionThreshold"> Threshold used for point reduction when adding points. </param>
            public PolylineBuilder(Action<P, Point> addPnt, Size pointReductionThreshold)
            {
                this.addPnt = addPnt;
                this.pointReductionThreshold = pointReductionThreshold;
            }
            #endregion

            #region public methods
            /// <summary>
            /// Appends the line specified by p0 and p1 to the polyline. The given points are added only if 
            /// necessary; that is, if their corresponding pixel coordinates differ from the pixel coordinates 
            /// of the tail of the polyline. Setting either force_p0 or force_p1 to true forces the points to be 
            /// added without further checks.
            /// </summary>
            /// <param name="p0"> Start point. </param>
            /// <param name="force_p0"> Specifies if p0 is to be added without further checks. </param>
            /// <param name="p1"> End point. </param>
            /// <param name="force_p1"> Specifies if p1 is to be added without further checks. </param>
            public void Append(Point p0, bool force_p0, Point p1, bool force_p1)
            {
                // We need to start a new polyline if we have not created one yet or if p0 is not equal to the 
                // end point of the last line added.
                // Otherwise we simply check if we need to insert either p0 or p1. A point will be inserted 
                // if its the first or the last point of an input polyline (force_p0, force_p1 set to accordingly), 
                // or if the point differs from the current end point by pointReductionThreshold.

                if (polyline == null || IsDifferent(last_p1, p0, differThreshold))
                {
                    // due to the point reduction it may happen that the last end 
                    // point was not added. Do that now.

                    Append(AppendCheck.EvaluateThreshold, differThreshold, last_p1);
                    Append(AppendCheck.ForceAppendToNewPolyline, differThreshold, p0);
                }
                else
                {
                    // append p0, if necessary
                    Append(force_p0 ? AppendCheck.ForceAppend : AppendCheck.EvaluateThreshold, pointReductionThreshold, p0);
                }

                // append p1, if necessary. Also update end point.
                Append(force_p1 ? AppendCheck.ForceAppend : AppendCheck.EvaluateThreshold, pointReductionThreshold, last_p1 = p1);
            }

            /// <summary> Gets the resulting polylines, after appending several single lines using Append. </summary>
            public ICollection<P> Polyline => polylineList;

            #endregion

            #region private methods
            /// <summary> Flags used for checkAppend. </summary>
            private enum AppendCheck
            {
                /// <summary> Evaluate a given threshold to decide if a point should be added to the polyline. </summary>
                EvaluateThreshold,
                /// <summary> Append a point without further checks. </summary>
                ForceAppend,
                /// <summary> Begin a new polyline and append the point without further checks. </summary>
                ForceAppendToNewPolyline
            };

            /// <summary>
            /// Appends the given point to the current polyline, also updating the end point of the current polyline. 
            /// Certain conditions apply before the point is added; see also parameter description.
            /// </summary>
            /// <param name="appendCheck"> Flag indicating if the given point is to be added without further 
            /// checks or if the given threshold is to be evaluated. </param>
            /// <param name="threshold"> If appendCheck is set to AppendCheck.EvaluateThreshold, the given point must
            /// differ from the current end point by this threshold before it is added to the polyline. </param>
            /// <param name="p"> Point to add. </param>
            private void Append(AppendCheck appendCheck, Size threshold, Point p)
            {
                if (appendCheck == AppendCheck.ForceAppendToNewPolyline)
                    polylineList.Add(polyline = new P());

                if (appendCheck != AppendCheck.EvaluateThreshold || IsDifferent(polylineEnd, p, threshold))
                    addPnt(polyline, polylineEnd = p);
            }

            /// <summary> Helper method indicating if the two input points are regarded as different according the provided size threshold. </summary>
            /// <param name="point1">First of two points to check for threshold difference. </param>
            /// <param name="point2">Second of two points to check for threshold difference. </param>
            /// <param name="threshold">Threshold values for x and y coordinates. The difference of both points is compared against these threshold values.</param>
            /// <returns>True if one of x or y difference is larger than the corresponding threshold value.</returns>
            private static bool IsDifferent(Point point1, Point point2, Size threshold)
            {
                return (Math.Abs(point1.X - point2.X) >= threshold.Width) || (Math.Abs(point1.Y - point2.Y) >= threshold.Height);
            }

            #endregion
        }
    }
}
