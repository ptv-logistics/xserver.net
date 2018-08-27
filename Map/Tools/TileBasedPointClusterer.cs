// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;


namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary> This class holds the information for a cluster. </summary>
    /// <typeparam name="T"> The data item for the cluster. </typeparam>
    public class Cluster<T>
    {
        #region public variables
        /// <summary> Gets or sets the number of points in a cluster. </summary>
        public int NumPoints { get; set; }
        /// <summary> Gets or sets the cluster level. </summary>
        public int Level { get; set; }
        /// <summary> Gets or sets the x-key of the cluster. </summary>
        public int TileX { get; set; }
        /// <summary> Gets or sets the y-key of the cluster. </summary>
        public int TileY { get; set; }
        /// <summary> Gets or sets the centroid x value of the cluster. </summary>
        public double SumX { get; set; }
        /// <summary> Gets or sets the centroid y value of the cluster. </summary>
        public double SumY { get; set; }
        /// <summary> Gets or sets the aggregated value of the cluster. </summary>
        public double Aggregate { get; set; }
        /// <summary> Gets the list of items in a cluster. </summary>
        public List<T> Tags { get; }
        /// <summary> Gets the aggregated centroid x value of the cluster. </summary>
        public double CentroidX => SumX / Aggregate;

        /// <summary> Gets the aggregated centroid y value of the cluster. </summary>
        public double CentroidY => SumY / Aggregate;

        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="Cluster{T}"/> class. </summary>
        /// <param name="tileX"> The x key of the cluster. </param>
        /// <param name="tileY"> The y key of the cluster. </param>
        public Cluster(int tileX, int tileY)
        {
            TileX = tileX;
            TileY = tileY;

            Tags = new List<T>();
        }
        #endregion

        #region public methods
        /// <summary> Adds a point to the cluster. </summary>
        /// <param name="point"> The object containing the point information. </param>
        public void AddPoint(PointInfo<T> point)
        {          
            NumPoints += 1;
            Aggregate += point.Aggregate;

            SumX += point.X * point.Aggregate;
            SumY += point.Y * point.Aggregate;

            if (point.Tag != null)
                Tags.Add(point.Tag);
        }

        /// <summary> Adds a cluster. </summary>
        /// <param name="cluster"> The cluster object. </param>
        public void AddCluster(Cluster<T> cluster)
        {
            SumX += cluster.SumX;
            SumY += cluster.SumY;

            NumPoints += cluster.NumPoints;
            Aggregate += cluster.Aggregate;

            Tags.AddRange(cluster.Tags);
        }
        #endregion
    }

    /// <summary> Struct for a tile key. </summary>
    public struct Tile
    {
        #region public variables
        /// <summary> Gets or sets the x key. </summary>
        public int X { get; set; }
        /// <summary> Gets or sets the y key. </summary>
        public int Y { get; set; }
        #endregion

        #region public methods
        /// <summary> Documentation in progress... </summary>
        /// <param name="obj"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public override bool Equals(Object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;
            
            var tile = (Tile)obj;
            return (X == tile.X) && (Y == tile.Y);
        }

        /// <inheritdoc/>  
        public override int GetHashCode()
        {
            return X ^ Y;
        }
        #endregion
    }
  
    /// <summary> This class holds the information for a single point. </summary>
    /// <typeparam name="T"> The item type for the cluster. </typeparam>
    public class PointInfo<T>
    {
        #region public variables
        /// <summary> Gets or sets the x coordinate of the point. </summary>
        public double X { get; set; }
        /// <summary> Gets or sets the y coordinate of the point. </summary>
        public double Y { get; set; }
        /// <summary> Gets or sets the aggregate value of the point. </summary>
        public double Aggregate { get; set; }
        /// <summary> Gets or sets the item of the point. </summary>
        public T Tag { get; set; }
        #endregion
    }

    /// <summary><para> This class creates cluster objects for points. </para>
    /// <para> See the <conceptualLink target="4926f311-8333-4b18-b509-70c1d876d5eb"/> topic for an example. </para></summary>
    /// <typeparam name="T"> The type of the items which should be clustered. </typeparam>
    public class TileBasedPointClusterer<T> : IPointClusterQuery<T>
    {
        #region private variables
        /// <summary> Documentation in progress... </summary>
        private readonly int maxLevel;
        /// <summary> Documentation in progress... </summary>
        private readonly int minLevel;
        /// <summary> Documentation in progress... </summary>
        private readonly double referenceSize;
        /// <summary> Documentation in progress... </summary>
        private readonly List<PointInfo<T>> points = new List<PointInfo<T>>();
        /// <summary> Documentation in progress... </summary>
        private readonly Dictionary<int, Dictionary<Tile, Cluster<T>>> clusterLevels = new Dictionary<int, Dictionary<Tile, Cluster<T>>>();
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="TileBasedPointClusterer{T}"/> class. </summary>
        /// <param name="referenceSize"> The logical size of the cluster area. </param>
        /// <param name="minLevel"> The minimum level for which clusters should be created. </param>
        /// <param name="maxLevel"> The maximum level for which clusters should be created. </param>
        public TileBasedPointClusterer(double referenceSize, int minLevel, int maxLevel)
        {
            this.referenceSize = referenceSize;

            this.minLevel = minLevel;
            this.maxLevel = maxLevel;
        }
        #endregion

        #region public methods
        /// <summary> Adds a point to the clusterer. </summary>
        /// <param name="x"> The x coordinate of the point. </param>
        /// <param name="y"> The y coordinate of the point. </param>
        /// <param name="aggregate"> A value to aggregate for the points. </param>
        /// <param name="tag"> The item for the point. </param>
        public void AddPoint(double x, double y, double aggregate, T tag)
        {
           points.Add(new PointInfo<T>{
                X = x, 
                Y = y, 
                Aggregate = aggregate,
                Tag = tag
           });
        }

        /// <summary> Clusters the elements. </summary>
        public void Cluster()
        {
            var baseClusters = new Dictionary<Tile, Cluster<T>>();
            
            // calculate cluster size for base level
            double clusterSize = referenceSize / (1 << maxLevel);

            // add all points to base cluster
            foreach (PointInfo<T> pointInfo in points)
            {
                // calculate tile key for a point
                int tileX = (int)(pointInfo.X / clusterSize);
                int tileY = (int)(pointInfo.Y / clusterSize);
                var tmpTile = new Tile { X = tileX, Y = tileY };

                // try to get the cluster from the dictionary.
                // If not, create a new one
                Cluster<T> cluster;
                if (!(baseClusters.TryGetValue(tmpTile, out cluster)))
                {
                    cluster = new Cluster<T>(tmpTile.X, tmpTile.Y);
                    baseClusters[tmpTile] = cluster;
                }

                // add point to cluster
                cluster.AddPoint(pointInfo);
            }

            // add base cluster at max level
            clusterLevels[maxLevel] = baseClusters;

            // now build the higher clusters recursively
            for (int i = maxLevel - 1; i >= minLevel; i--)
            {
                var childClusters = new Dictionary<Tile, Cluster<T>>();

                foreach (Cluster<T> baseCluster in baseClusters.Values)
                {
                    // calculate tile key of the child cluster
                    int tileX = baseCluster.TileX / 2;
                    int tileY = baseCluster.TileY / 2;
                    var tmpTile = new Tile { X = tileX, Y = tileY };

                    // try to get the cluster from the dictionary.
                    // If not, create a new one
                    Cluster<T> childCluster;
                    if (!(childClusters.TryGetValue(tmpTile, out childCluster)))
                    {
                        childCluster = new Cluster<T>(tmpTile.X, tmpTile.Y);
                        childClusters[tmpTile] = childCluster;
                    }

                    // add base cluster info to child cluster
                    childCluster.AddCluster(baseCluster);
                }

                // add at tile level
                clusterLevels[i] = childClusters;

                // next loop with child cluster as base cluster
                baseClusters = childClusters;
            }
        }

        /// <summary> Gets the clusters for a bounding box and a level. </summary>
        /// <param name="xMin"> The minimum x coordinate of the bounding box. </param>
        /// <param name="yMin"> The minimum y coordinate of the bounding box. </param>
        /// <param name="xMax"> The maximum x coordinate of the bounding box. </param>
        /// <param name="yMax"> The maximum y coordinate of the bounding box. </param>
        /// <param name="level"> The level of the clusters. </param>
        /// <returns> The list of cluster objects. </returns>
        public List<Cluster<T>> GetClusters(double xMin, double yMin, double xMax, double yMax, int level)
        {
            // truncate level at bounds
            if (level < minLevel)
                level = minLevel;
            if (level > maxLevel)
                level = maxLevel;

            // calculate cluster size for level
            double clusterSize = referenceSize / (1 << level);

            var result = new List<Cluster<T>>();

            // calculate bounds for cluster candidates
            int xMinTile = (int)(xMin / clusterSize) - 1;
            int yMinTile = (int)(yMin / clusterSize) - 1;
            int xMaxTile = (int)(xMax / clusterSize) + 1;
            int yMaxTile = (int)(yMax / clusterSize) + 1;

            Dictionary<Tile, Cluster<T>> clusters = clusterLevels[level];
            Cluster<T> cluster;

             // add all clusters within window 
            for (int i = xMinTile; i < xMaxTile; i++)
                for (int j = yMinTile; j < yMaxTile; j++)
                    if (clusters.TryGetValue(new Tile { X = i, Y = j }, out cluster))
                        result.Add(cluster);

            return result;
        }
        #endregion
    }

    /// <summary> Generic interface for querying clusters. </summary>
    /// <typeparam name="T"> The item type of the objects. </typeparam>
    public interface IPointClusterQuery<T>
    {
        /// <summary> Gets the clusters for a bounding box and a level. </summary>
        /// <param name="xMin"> The minimum x coordinate of the bounding box. </param>
        /// <param name="yMin"> The minimum y coordinate of the bounding box. </param>
        /// <param name="xMax"> The maximum x coordinate of the bounding box. </param>
        /// <param name="yMax"> The maximum y coordinate of the bounding box. </param>
        /// <param name="level"> The level of the clusters. </param>
        /// <returns> The list of cluster objects. </returns>
        List<Cluster<T>> GetClusters(double xMin, double yMin, double xMax, double yMax, int level);
    }
}
