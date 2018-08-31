// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Linq;


namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary> This class holds the information for a cluster. </summary>
    /// <typeparam name="T"> Documentation in progress... </typeparam>
    public class LineCluster<T>
    {
        #region public variables
        /// <summary> Gets or sets Documentation in progress... </summary>
        public int NumLines { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public int Level { get; set; }

        /// <summary> Gets or sets Documentation in progress... </summary>
        public int TileX1 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public int TileY1 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public int TileX2 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public int TileY2 { get; set; }

        /// <summary> Gets or sets Documentation in progress... </summary>
        public double SumX1 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public double SumY1 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public double SumX2 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public double SumY2 { get; set; }

        /// <summary> Gets or sets Documentation in progress... </summary>
        public double Aggregate1 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public double Aggregate2 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public double AggregateLine { get; set; }
        /// <summary> Gets Documentation in progress... </summary>
        public List<T> Tags { get; }

        /// <summary> Gets Documentation in progress... </summary>
        public double CentroidX1 => SumX1 / Aggregate1;

        /// <summary> Gets Documentation in progress... </summary>
        public double CentroidY1 => SumY1 / Aggregate1;

        /// <summary> Gets Documentation in progress... </summary>
        public double CentroidX2 => SumX2 / Aggregate2;

        /// <summary> Gets Documentation in progress... </summary>
        public double CentroidY2 => SumY2 / Aggregate2;

        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="LineCluster{T}"/> class. </summary>
        /// <param name="tileX1"> Documentation in progress... </param>
        /// <param name="tileY1"> Documentation in progress... </param>
        /// <param name="tileX2"> Documentation in progress... </param>
        /// <param name="tileY2"> Documentation in progress... </param>
        public LineCluster(int tileX1, int tileY1, int tileX2, int tileY2)
        {
            TileX1 = tileX1;
            TileY1 = tileY1;
            TileX2 = tileX2;
            TileY2 = tileY2;

            Tags = new List<T>();
        }
        #endregion

        #region public methods
        /// <summary> Documentation in progress... </summary>
        /// <param name="line"> Documentation in progress... </param>
        public void AddPoint(LineInfo<T> line)
        {
            NumLines += 1;
            Aggregate1 += line.Aggregate1;
            Aggregate2 += line.Aggregate2;
            AggregateLine += line.AggregateLine;

            // ToDo: make inclusion of aggregate value for centroid calculation optional?
            SumX1 += line.X1 * line.Aggregate1;
            SumY1 += line.Y1 * line.Aggregate1;
            SumX2 += line.X2 * line.Aggregate2;
            SumY2 += line.Y2 * line.Aggregate2;

            if (line.Tag != null)
                Tags.Add(line.Tag);
        }

        /// <summary> Documentation in progress... </summary>
        /// <param name="cluster"> Documentation in progress... </param>
        public void AddCluster(LineCluster<T> cluster)
        {
            SumX1 += cluster.SumX1;
            SumY1 += cluster.SumY1;
            SumX2 += cluster.SumX2;
            SumY2 += cluster.SumY2;

            NumLines += cluster.NumLines;
            Aggregate1 += cluster.Aggregate1;
            Aggregate2 += cluster.Aggregate2;
            AggregateLine += cluster.AggregateLine;

            Tags.AddRange(cluster.Tags);
        }
        #endregion
    }

    /// <summary> This class holds the information for a single point. </summary>
    /// <typeparam name="T"> Documentation in progress... </typeparam>
    public class LineInfo<T>
    {
        /// <summary> Gets or sets Documentation in progress... </summary>
        public double X1 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public double Y1 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public double X2 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public double Y2 { get; set; }

        /// <summary> Gets or sets Documentation in progress... </summary>
        public double Aggregate1 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public double Aggregate2 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public double AggregateLine { get; set; }

        /// <summary> Gets or sets Documentation in progress... </summary>
        public T Tag { get; set; }
    }

    /// <summary> Struct for a tile key. </summary>
    public struct LineTile
    {
        #region public variables
        /// <summary> Gets or sets Documentation in progress... </summary>
        public int X1 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public int Y1 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public int X2 { get; set; }
        /// <summary> Gets or sets Documentation in progress... </summary>
        public int Y2 { get; set; }
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

            var lineTile = (LineTile)obj;
            return (X1 == lineTile.X1) && (Y1 == lineTile.Y1) && (X2 == lineTile.X2) && (Y2 == lineTile.Y2);
        }

        /// <summary> Documentation in progress... </summary>
        /// <returns> Documentation in progress... </returns>
        public override int GetHashCode()
        {
            return X1 ^ Y1 ^ X2 ^ Y2;
        }
        #endregion
    }

    /// <summary> Documentation in progress... </summary>
    /// <typeparam name="T"> Documentation in progress... </typeparam>
    public class TileBasedLineClusterer<T>
    {
        #region public variables
        /// <summary> Documentation in progress... </summary>
        public int maxLevel;
        /// <summary> Documentation in progress... </summary>
        public int minLevel;
        /// <summary> Documentation in progress... </summary>
        public double referenceSize;

        /// <summary> Documentation in progress... </summary>
        public List<LineInfo<T>> points = new List<LineInfo<T>>();
        /// <summary> Documentation in progress... </summary>
        public Dictionary<int, Dictionary<LineTile, LineCluster<T>>> clusterLevels = new Dictionary<int, Dictionary<LineTile, LineCluster<T>>>();
        /// <summary> Documentation in progress... </summary>
        public Dictionary<int, Dictionary<Tile, List<LineCluster<T>>>> clusterLevels1 = new Dictionary<int, Dictionary<Tile, List<LineCluster<T>>>>();
        /// <summary> Documentation in progress... </summary>
        public Dictionary<int, Dictionary<Tile, List<LineCluster<T>>>> clusterLevels2 = new Dictionary<int, Dictionary<Tile, List<LineCluster<T>>>>();
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="TileBasedLineClusterer{T}"/> class. </summary>
        /// <param name="referenceSize"> Documentation in progress... </param>
        /// <param name="minLevel"> Documentation in progress... </param>
        /// <param name="maxLevel"> Documentation in progress... </param>
        public TileBasedLineClusterer(double referenceSize, int minLevel, int maxLevel)
        {
            this.referenceSize = referenceSize;

            this.minLevel = minLevel;
            this.maxLevel = maxLevel;
        }
        #endregion

        #region public methods
        /// <summary> Documentation in progress... </summary>
        /// <param name="x1"> Documentation in progress... </param>
        /// <param name="y1"> Documentation in progress... </param>
        /// <param name="aggregate1"> Documentation in progress... </param>
        /// <param name="x2"> Documentation in progress... </param>
        /// <param name="y2"> Documentation in progress... </param>
        /// <param name="aggregate2"> Documentation in progress... </param>
        /// <param name="aggregateLine"> Documentation in progress... </param>
        /// <param name="tag"> Documentation in progress... </param>
        public void AddLine(double x1, double y1, double aggregate1, 
            double x2, double y2, double aggregate2, double aggregateLine, T tag)
        {
            points.Add(new LineInfo<T>
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Aggregate1 = aggregate1,
                Aggregate2 = aggregate2,
                AggregateLine = aggregateLine,
                Tag = tag
            });
        }

        /// <summary> Documentation in progress... </summary>
        public void Cluster()
        {
            var baseClusters = new Dictionary<LineTile, LineCluster<T>>();

            // calculate cluster size for base level
            double clusterSize = referenceSize / (1 << maxLevel);

            // add all points to base cluster
            foreach (LineInfo<T> pointInfo in points)
            {
                // calculate tile key for a point
                int tileX1 = (int)(pointInfo.X1 / clusterSize);
                int tileY1 = (int)(pointInfo.Y1 / clusterSize);
                int tileX2 = (int)(pointInfo.X2 / clusterSize);
                int tileY2 = (int)(pointInfo.Y2 / clusterSize);
                var tmpTile = new LineTile { X1 = tileX1, Y1 = tileY1, X2 = tileX2, Y2 = tileY2 };

                // try to get the cluster from the dictionary.
                // If not, create a new one
                if (!(baseClusters.TryGetValue(tmpTile, out var cluster)))
                {
                    cluster = new LineCluster<T>(tmpTile.X1, tmpTile.Y1, tmpTile.X2, tmpTile.Y2);
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
                var childClusters = new Dictionary<LineTile, LineCluster<T>>();

                foreach (LineCluster<T> baseCluster in baseClusters.Values)
                {
                    // calculate tile key of the child cluster
                    int tileX1 = baseCluster.TileX1 / 2;
                    int tileY1 = baseCluster.TileY1 / 2;
                    int tileX2 = baseCluster.TileX2 / 2;
                    int tileY2 = baseCluster.TileY2 / 2;
                    var tmpTile = new LineTile { X1 = tileX1, Y1 = tileY1, X2 = tileX2, Y2 = tileY2 };

                    // try to get the cluster from the dictionary.
                    // If not, create a new one
                    if (!(childClusters.TryGetValue(tmpTile, out var childCluster)))
                    {
                        childCluster = new LineCluster<T>(tmpTile.X1, tmpTile.Y1, tmpTile.X2, tmpTile.Y2);
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

            foreach (KeyValuePair<int, Dictionary<LineTile, LineCluster<T>>> clusterLevel in clusterLevels)
            {
                var clusterLevel1 = new Dictionary<Tile, List<LineCluster<T>>>();
                var clusterLevel2 = new Dictionary<Tile, List<LineCluster<T>>>();
                clusterLevels1.Add(clusterLevel.Key, clusterLevel1);
                clusterLevels2.Add(clusterLevel.Key, clusterLevel2);

                foreach (KeyValuePair<LineTile, LineCluster<T>> lineTile in clusterLevel.Value)
                {
                    var tile1 = new Tile { X = lineTile.Key.X1, Y = lineTile.Key.Y1 };
                    if (!(clusterLevel1.ContainsKey(tile1)))
                        clusterLevel1.Add(tile1, new List<LineCluster<T>>());
                    clusterLevel1[tile1].Add(lineTile.Value);

                    var tile2 = new Tile { X = lineTile.Key.X2, Y = lineTile.Key.Y2 };
                    if (!(clusterLevel2.ContainsKey(tile2)))
                        clusterLevel2.Add(tile2, new List<LineCluster<T>>());
                    clusterLevel2[tile2].Add(lineTile.Value);
                }
            }
        }

        /// <summary> Documentation in progress... </summary>
        /// <param name="xMin"> Documentation in progress... </param>
        /// <param name="yMin"> Documentation in progress... </param>
        /// <param name="xMax"> Documentation in progress... </param>
        /// <param name="yMax"> Documentation in progress... </param>
        /// <param name="level"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public List<LineCluster<T>> GetClusters(double xMin, double yMin, double xMax, double yMax, int level)
        {
            // truncate level at bounds
            if (level < minLevel)
                level = minLevel;
            if (level > maxLevel)
                level = maxLevel;

            // calculate cluster size for level
            double clusterSize = referenceSize / (1 << level);

            var result = new HashSet<LineCluster<T>>();

            // calculate bounds for cluster candidates
            int xMinTile = (int)(xMin / clusterSize) - 1;
            int yMinTile = (int)(yMin / clusterSize) - 1;
            int xMaxTile = (int)(xMax / clusterSize) + 1;
            int yMaxTile = (int)(yMax / clusterSize) + 1;

            Dictionary<Tile, List<LineCluster<T>>> clusters1 = clusterLevels1[level];
            Dictionary<Tile, List<LineCluster<T>>> clusters2 = clusterLevels2[level];

            // add all clusters within window 
            for (int i = xMinTile; i < xMaxTile; i++)
                for (int j = yMinTile; j < yMaxTile; j++)
                {
                    if (clusters1.TryGetValue(new Tile { X = i, Y = j }, out var clusterList))
                        clusterList.ForEach(cluster => result.Add(cluster));
                    if (clusters2.TryGetValue(new Tile { X = i, Y = j }, out clusterList))
                        clusterList.ForEach(cluster => result.Add(cluster));
                }

            return result.ToList();
        }
        #endregion
    }

    /// <summary> Documentation in progress... </summary>
    /// <typeparam name="T"> Documentation in progress... </typeparam>
    public class StartPointQuery<T> : IPointClusterQuery<T>
    {
        #region private variables
        /// <summary> Documentation in progress... </summary>
        private readonly TileBasedLineClusterer<T> clusterer;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="StartPointQuery{T}"/> class. </summary>
        /// <param name="clusterer"> Documentation in progress... </param>
        public StartPointQuery(TileBasedLineClusterer<T> clusterer)
        {
            this.clusterer = clusterer;
        }
        #endregion

        #region IPointClusterQuery<T> Members
        /// <summary> Documentation in progress... </summary>
        /// <param name="xMin"> Documentation in progress... </param>
        /// <param name="yMin"> Documentation in progress... </param>
        /// <param name="xMax"> Documentation in progress... </param>
        /// <param name="yMax"> Documentation in progress... </param>
        /// <param name="level"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public List<Cluster<T>> GetClusters(double xMin, double yMin, double xMax, double yMax, int level)
        {
            // truncate level at bounds
            if (level < clusterer.minLevel)
                level = clusterer.minLevel;
            if (level > clusterer.maxLevel)
                level = clusterer.maxLevel;

            // calculate cluster size for level
            double clusterSize = clusterer.referenceSize / (1 << level);

            var result = new List<Cluster<T>>();

            // calculate bounds for cluster candidates
            int xMinTile = (int)(xMin / clusterSize) - 1;
            int yMinTile = (int)(yMin / clusterSize) - 1;
            int xMaxTile = (int)(xMax / clusterSize) + 1;
            int yMaxTile = (int)(yMax / clusterSize) + 1;

            Dictionary<Tile, List<LineCluster<T>>> clusters1 = clusterer.clusterLevels1[level];

            // add all clusters within window 
            for (int i = xMinTile; i < xMaxTile; i++)
                for (int j = yMinTile; j < yMaxTile; j++)
                {
                    if (clusters1.TryGetValue(new Tile { X = i, Y = j }, out var clusterList))
                    {
                        var cluster = new Cluster<T>(i, j) {Level = level};
                        result.Add(cluster);

                        foreach (LineCluster<T> lineCluster in clusterList)
                        {
                            cluster.NumPoints += lineCluster.NumLines;
                            cluster.Tags.AddRange(lineCluster.Tags);
                            cluster.SumX += lineCluster.SumX1;
                            cluster.SumY += lineCluster.SumY1;
                            cluster.Aggregate += lineCluster.Aggregate1;
                        }
                    }
                }

            return result;
        }
        #endregion
    }

    /// <summary> Documentation in progress... </summary>
    /// <typeparam name="T"> Documentation in progress... </typeparam>
    public class EndPointQuery<T> : IPointClusterQuery<T>
    {
        #region private variables
        /// <summary> Documentation in progress... </summary>
        private readonly TileBasedLineClusterer<T> clusterer;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="EndPointQuery{T}"/> class. </summary>
        /// <param name="clusterer"> Documentation in progress... </param>
        public EndPointQuery(TileBasedLineClusterer<T> clusterer)
        {
            this.clusterer = clusterer;
        }
        #endregion

        #region IPointClusterQuery<T> Members
        /// <summary> Documentation in progress... </summary>
        /// <param name="xMin"> Documentation in progress... </param>
        /// <param name="yMin"> Documentation in progress... </param>
        /// <param name="xMax"> Documentation in progress... </param>
        /// <param name="yMax"> Documentation in progress... </param>
        /// <param name="level"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public List<Cluster<T>> GetClusters(double xMin, double yMin, double xMax, double yMax, int level)
        {
            // truncate level at bounds
            if (level < clusterer.minLevel)
                level = clusterer.minLevel;
            if (level > clusterer.maxLevel)
                level = clusterer.maxLevel;

            // calculate cluster size for level
            double clusterSize = clusterer.referenceSize / (1 << level);

            var result = new List<Cluster<T>>();

            // calculate bounds for cluster candidates
            int xMinTile = (int)(xMin / clusterSize) - 1;
            int yMinTile = (int)(yMin / clusterSize) - 1;
            int xMaxTile = (int)(xMax / clusterSize) + 1;
            int yMaxTile = (int)(yMax / clusterSize) + 1;

            Dictionary<Tile, List<LineCluster<T>>> clusters2 = clusterer.clusterLevels2[level];

            // add all clusters within window 
            for (int i = xMinTile; i < xMaxTile; i++)
                for (int j = yMinTile; j < yMaxTile; j++)
                {
                    if (!clusters2.TryGetValue(new Tile {X = i, Y = j}, out var clusterList)) continue;

                    var cluster = new Cluster<T>(i, j) {Level = level};
                    result.Add(cluster);

                    foreach (LineCluster<T> lineCluster in clusterList)
                    {
                        cluster.NumPoints += lineCluster.NumLines;
                        cluster.Tags.AddRange(lineCluster.Tags);
                        cluster.SumX += lineCluster.SumX2;
                        cluster.SumY += lineCluster.SumY2;
                        cluster.Aggregate += lineCluster.Aggregate2;
                    }
                }

            return result;
        }
        #endregion
    }
}
