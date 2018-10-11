// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Ptv.XServer.Controls.Map.TileProviders;

// ReSharper disable once CheckNamespace
namespace Ptv.XServer.Controls.Map
{
    /// <summary>
    /// Separate interface of the map control, dedicated to the management of tool tips.
    /// </summary>
    public interface IToolTipManagement
    {
        /// <summary> Enables/disables the management for tool tips. </summary>
        bool IsEnabled { get; set; }

        /// <summary> Reads or writes the value (in [ms]) to delay tool tip display. </summary>
        int ToolTipDelay { get; set; }

        /// <summary>
        /// Distance (specified in Pixels) between the current mouse position and a layer object, for which tooltip information should be shown.
        /// Each layer has to interpolate to its own coordinate format, what is meant by the pixel sized distance.  
        /// </summary>
        double MaxPixelDistance { get; set; }
    }

    /// <summary>Object concentrating all functionality relevant for tool tip management.</summary>
    public class ToolTipManagement : IToolTipManagement
    {
        /// <inheritdoc/>
        public bool IsEnabled { get; set; } = true;

        /// <inheritdoc/>
        public int ToolTipDelay { get; set; } = ToolTipService.GetInitialShowDelay(new Canvas());

        /// <inheritdoc/>
        public double MaxPixelDistance { get; set; } = 10;

        /// <summary>Constructor establishes the relationship to the parent map object. </summary>
        /// <param name="map">Parent map object</param>
        public ToolTipManagement(Map map)
        {
            toolTipTimer.Tick += ShowToolTip;

            map.MouseMove += (s, mouseEventArgs) => StartToolTipTimer(map, mouseEventArgs); // raised when the mouse pointer moves.
            map.MouseLeave += (s, mouseEventArgs) => StartToolTipTimer(map); // raised when the mouse pointer leaves the bounds of the Map object.
        }

        /// <summary> Tests if the cursor position has changed. If so, stores the new position and triggers the tool tip timer. </summary>
        /// <param name="map">Map object needed for position.</param>
        /// <param name="mouseEventArgs">Mouse event arguments, a null value represents an invalid position.</param>
        private void StartToolTipTimer(Map map, MouseEventArgs mouseEventArgs = null)
        {
            if (!IsEnabled)
                return;

            // test if position has changed
            var currentPosition = mouseEventArgs?.GetPosition(map);
            if ((LatestPosition.HasValue == currentPosition.HasValue) && (!currentPosition.HasValue || ((LatestPosition.Value - currentPosition.Value).Length <= 1e-4)))
                return;

            ClearToolTip();

            // trigger update, if not any mouse button is pressed
            if (Mouse.LeftButton != MouseButtonState.Released || Mouse.MiddleButton != MouseButtonState.Released || Mouse.RightButton != MouseButtonState.Released)
                return;

            LatestPosition = currentPosition;

            toolTipTimer.Stop();

            // flag indicating if a tool tip is to be shown. Initially, specified position must be valid.
            bool showToolTips = currentPosition.HasValue && (ToolTipDelay >= 0) && IsHitTestOKFunc((Point)currentPosition);
            if (!showToolTips) return;

            GetToolTipMapObjects();
            if (toolTipMapObjects.Count <= 0) return;

            toolTipTimer.Interval = TimeSpan.FromMilliseconds(ToolTipDelay);
            toolTipTimer.Start();
        }

        /// <summary>Callback for parent map object to indicate that a mouse position can be used for showing a tool tip. </summary>
        public Func<Point, bool> IsHitTestOKFunc { get; set; }

        private void ShowToolTip(object sender, EventArgs e)
        {
            toolTipTimer.Stop();
            ClearToolTip();

            if (CreateCustomizedToolTipsFunc != null)
            {
                CreateCustomizedToolTipsFunc(toolTipMapObjects);
                return;
            }

            toolTip = new ToolTip { Content = CreateToolTipControl(), IsOpen = true };
        }

        private StackPanel CreateToolTipControl()
        {
            var result = new StackPanel();

            foreach (var item in toolTipMapObjects.Select((toolTipMapObject, index) => new { index, toolTipMapObject } ))
            {
                string content = item.toolTipMapObject.Count == 1
                    ? item.toolTipMapObject.First().Value // XMap.Map.UntiledLayer provides only one key/value pair and all structuring is made in the value field.
                    : item.toolTipMapObject.ToString(); // Needed for layers which uses the data dictionary with multiple key/value pairs for structuring their data.

                var label = new Label { Margin = new Thickness(1), Content = content };
                result.Children.Add((item.index == 0) 
                    ? (UIElement) label 
                    : new Border {
                        BorderThickness = new Thickness(0, 1, 0, 0),
                        BorderBrush = new SolidColorBrush(Colors.White),
                        Child = label });
            }

            return result;
        }

        /// <summary>Callback for indicating the rendering of a tool tip. When this callback is set, 
        /// the default rendering is avoided.</summary>
        public Action<List<IMapObject>> CreateCustomizedToolTipsFunc { get; set; }
        /// <summary>Callback for indicating the removal of a tool tip. When this callback is set, 
        /// the default removal operation is avoided.</summary>
        public Action<List<IMapObject>> DestroyCustomizedToolTipsFunc { get; set; }

        /// <summary> Removes the latest tool tip created by this layer. </summary>
        private void ClearToolTip()
        {
            if (DestroyCustomizedToolTipsFunc != null)
            {
                DestroyCustomizedToolTipsFunc?.Invoke(toolTipMapObjects);
                return;
            }

            if (toolTip == null) return;

            // close tool tip
            toolTip.IsOpen = false;
            toolTip = null;
        }

        private void GetToolTipMapObjects()
        {
            toolTipMapObjects.Clear();
            if (LatestPosition.HasValue)
                toolTipMapObjects.AddRange(FillToolTipMapObjectsFunc(LatestPosition.Value, MaxPixelDistance));
        }

        /// <summary>Callback for ordering the tool tip entries from the layers of a map. Commonly the
        /// map object implements this callback. </summary>
        public Func<Point, double, IEnumerable<IMapObject>> FillToolTipMapObjectsFunc { get; set; }

        private ToolTip toolTip;

        private readonly DispatcherTimer toolTipTimer = new DispatcherTimer();

        private readonly List<IMapObject> toolTipMapObjects = new List<IMapObject>();

        /// <summary> 
        /// Stores the latest known position handled by method <see cref="StartToolTipTimer"/>. If the mouse position is outside the bounds
        ///  of the map control, its value is null. 
        /// </summary>
        private Point? LatestPosition { get; set; }
    }
}
