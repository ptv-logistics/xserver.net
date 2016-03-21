using System.Collections;
using System.Collections.Generic;
using System.Windows;


namespace Ptv.XServer.Controls.Map.Gadgets
{
    /// <summary><para> Struct containing all relevant data needed for each result of a nearest search. </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> topic for an example. </para></summary>
    public struct NearestSearchResult
    {
        /// <summary> Gets or sets the identifier of a resulting object. </summary>
        /// <value> Identifier of a resulting object. </value>
        public object Id { get; set; }
        /// <summary>
        /// Gets or sets the distance of the object to the point from which the search is started. This field allows
        /// sorting multiple results according their (ascending) distance order.
        /// </summary>
        /// <value> Distance of the object to the search point. </value>
        public double Distance { get; set; }
    }

    /// <summary><para> Collection of geographical search methods for layer objects. </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> topic for an example. </para></summary>
    public interface ILayerGeoSearch
    {
        /// <summary> Search for layer objects which reside nearest to a point specified in <paramref name="point"/>. </summary>
        /// <param name="point"> Point used as 'kick-off' of this search. </param>
        /// <param name="maxNumElements"> Maximal number of results to look for. </param>
        /// <param name="includeAllOfSameDistance"> Flag indicating that the sorted list can exceed the maximal number
        /// of elements, when one or more objects exist having the same distance as the element at the
        /// <paramref name="maxNumElements"/> position. Especially when only the nearest object should be returned
        /// (<paramref name="maxNumElements"/> equals 1), all objects at the same location should be returned too. </param>
        /// <param name="maxDistance"> Delimiter for reducing the search area, commonly used to improve performance. </param>
        /// <returns> Collection of nearest layer objects, provided with additional distance information. </returns>
        ICollection<NearestSearchResult> NearestSearch(Point point, int maxNumElements, bool includeAllOfSameDistance, double maxDistance);

        /// <summary> Search for layer objects nearest to a point specified in <paramref name="point"/>. </summary>
        /// <param name="point"> Point used as 'kick-off' of this search. </param>
        /// <param name="minNumElements"> Minimal number of results to look for. This parameter acts as a delimiter for
        /// terminating the search. </param>
        /// <param name="maxNumElements"> Maximal number of results to look for. </param>
        /// <param name="includeAllOfSameDistance"> Flag indicating that the sorted list can exceed the maximal number 
        /// of elements, when one or more objects exist having the same distance as the element at the
        /// <paramref name="maxNumElements"/> position. Especially when only the nearest object should be returned
        /// (<paramref name="maxNumElements"/> equals 1), all objects at the same location should be returned too. </param>
        /// <returns> Collection of nearest layer objects, provided with additional distance information. </returns>
        ICollection<NearestSearchResult> NearestSearch(Point point, int minNumElements, int maxNumElements, bool includeAllOfSameDistance);

        /// <summary> Search for layer objects contained inside the rectangle specified in the <paramref name="rectangle"/>
        /// parameter. </summary>
        /// <param name="rectangle"> Rectangle used to search for all layer objects contained in it. </param>
        /// <returns> Collection of layer objects, located inside the specified rectangle. </returns>
        ICollection RectangleSearch(MapRectangle rectangle);

        /// <summary> Search for layer objects contained inside the ellipse specified in the
        /// <paramref name="boundingRectangle"/> parameter. </summary>
        /// <param name="boundingRectangle"> Bounding rectangle of an ellipse used to search for all layer objects contained
        /// in it. </param>
        /// <returns> Collection of layer objects, located inside the specified ellipse. </returns>
        ICollection EllipseSearch(MapRectangle boundingRectangle);

        /// <summary> Search for layer objects contained inside the polygon specified in the <paramref name="points"/>
        /// parameter. </summary>
        /// <param name="points"> Collection of points defining a polygon, which is used to search for all layer
        /// objects contained in it. </param>
        /// <returns> Collection of layer objects, located inside the specified polygon. </returns>
        ICollection PolygonSearch(IEnumerable<Point> points);

        /// <summary> Search for layer objects cut by the line defined by the points <paramref name="p1"/> and 
        /// <paramref name="p2"/>. Especially for point objects, its part of the layer specification, when an object is
        /// regarded as 'cut' by a line. </summary>
        /// <param name="p1"> First point of 'cutting' line. </param>
        /// <param name="p2"> Second point of 'cutting' line. </param>
        /// <returns> Collection of layer objects, which are cut by the specified line. </returns>
        ICollection IntersectionSearch(Point p1, Point p2);
    }
}
