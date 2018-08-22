using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Collections.Generic;
using System.ComponentModel;

namespace Ptv.XServer.Controls.Map
{
    /// <summary> The main WPF map. </summary>
    public partial class MapView
    {
        #region private variables

        /// <summary> X coordinate of the map center. </summary>
        private double x;
        /// <summary> Y coordinate of the map center. </summary>
        private double y;
        /// <summary> Zoom level of the map. </summary>
        private double z = 1;

        /// <summary> Transformation object for the pan offset. </summary>
        private readonly TranslateTransform translateTransform;
        /// <summary> Transformation object for the zoom scaling. </summary>
        private readonly ScaleTransform zoomTransform;
        /// <summary> Transformation object for the shift of the map center on screen. </summary>
        private readonly TranslateTransform screenOffsetTransform;
        /// <summary> Transformation object for the logical wheel offset. </summary>
        private readonly TranslateTransform logicalWheelOffsetTransform;
        /// <summary> Transformation object for the physical wheel offset. </summary>
        private readonly TranslateTransform physicalWheelOffsetTransform;
        /// <summary> Bounds of the map envelope. </summary>
        private MapRectangle envMapRectangle = new MapRectangle();
        /// <summary> Clock for the panning action signaling when panning has finished. </summary>
        private AnimationClock currentPanAnimationClock;
        /// <summary> Clock for the zooming action signaling when zooming has finished. </summary>
        private AnimationClock currentZoomAnimationClock;
        /// <summary> Maximum zoom level. </summary>
        private int maxZoom;
        /// <summary> Minimum zoom level. </summary>
        private int minZoom;
        #endregion

        #region public variables
        /// <summary> The earth radius of the map is 6371000.0 (so the main projection is PTV_Mercator). </summary>
        public const double EARTH_RADIUS = 6371000.0;

        /// <summary> The size of the map in logical coordinates. </summary>
        public static double LogicalSize = EARTH_RADIUS * 2.0 * Math.PI;
        /// <summary> An arbitrary value. </summary>
        public static double ReferenceSize = 512;
        /// <summary> The zoom adjust is used to minimize rounding errors in deep zoom levels. </summary>
        public static double ZoomAdjust = Math.Pow(2, 20);

        /// <summary> Gets or sets a value indicating whether the printing of a map is in process. </summary>
        public bool Printing { get; set; }

        /// <summary> Gets or sets the current zoom scale value. </summary>
        public double ZoomScale
        {
            get { return (double)GetValue(ScaleProperty); }
            set { SetValue(ScaleProperty, value); }
        }

        /// <summary> Gets or sets a value indicating whether the map should be fitted in the window or not. </summary>
        public bool FitInWindow { get; set; }

        /// <summary> Gets or sets the maximal level of detail according to the standard tiling scheme. The current
        /// detail level (see <see cref="FinalZoom"/> property) is corrected, if it is higher than the new maximum value. </summary>
        public int MaxZoom
        {
            get { return maxZoom; }
            set
            {
                maxZoom = value;

                if (FinalZoom > maxZoom)
                    SetZoom(maxZoom, false);
            }
        }

        /// <summary>
        /// The internal offset for deep zoom
        /// </summary>
        private Point canvasOffset { get; set; }

        /// <summary>
        /// Gets the origin offset value for Deep Zoom
        /// </summary>
        public Point OriginOffset => new Point(
            canvasOffset.X * LogicalSize / ReferenceSize / ZoomAdjust,
            canvasOffset.Y * LogicalSize / ReferenceSize / ZoomAdjust);

        /// <summary> Gets or sets the minimal level of detail according to the standard tiling scheme. The current
        /// detail level (see <see cref="FinalZoom"/> property) is corrected, if it is lower than the new minimum value. </summary>
        public int MinZoom
        {
            get { return minZoom; }
            set
            {
                if (FinalZoom < minZoom)
                    SetZoom(minZoom, false);
            }
        }

        /// <summary> Gets the level of detail according to the standard tiling scheme (float value). </summary>
        public double FinalZoom => z;

        /// <summary> Sets the level of detail according to the standard tiling scheme. </summary>
        /// <param name="value"> The new level. </param>
        /// <param name="useAnimation"> If true the zooming is animated. </param>
        public void SetZoom(double value, bool useAnimation)
        {
            ZoomAround(CurrentEnvelope.Center, value, useAnimation);
        }

        /// <summary> Gets the floating tile level while in animation mode. </summary>
        public double CurrentZoom => Math.Log(LogicalSize / CurrentScale, 2) - 8;

        /// <summary> Gets a value indicating whether an animation is in progress. Returns true while the map performs
        /// a transition to a new map section. </summary>
        public bool IsAnimating => currentPanAnimationClock != null || currentZoomAnimationClock != null;

        /// <summary> Gets the logical x-coordinate while the map is in animation mode. </summary>
        public double CurrentX => -(translateTransform.X + logicalWheelOffsetTransform.X + canvasOffset.X) * LogicalSize / ReferenceSize / ZoomAdjust - physicalWheelOffsetTransform.X * CurrentScale;

        /// <summary> Gets the logical y-coordinate while the map is in animation mode. </summary>
        public double CurrentY => (translateTransform.Y + logicalWheelOffsetTransform.Y + canvasOffset.Y) * LogicalSize / ReferenceSize / ZoomAdjust + physicalWheelOffsetTransform.Y * CurrentScale;

        /// <summary> Gets the x-coordinate after the map was in animation mode / the anticipated x-coordinate while the map is in animation mode. </summary>
        public double FinalX => x - (logicalWheelOffsetTransform.X + canvasOffset.X) * LogicalSize / ReferenceSize / ZoomAdjust - physicalWheelOffsetTransform.X * FinalScale;

        /// <summary> Gets the y-coordinate after the map was in animation mode / the anticipated y-coordinate while the map is
        /// in animation mode. </summary>
        public double FinalY => y + (logicalWheelOffsetTransform.Y + canvasOffset.Y) * LogicalSize / ReferenceSize / ZoomAdjust + physicalWheelOffsetTransform.Y * FinalScale;

        /// <summary> Gets the scale factor while the map is in animation mode. Defined in Logical units per pixel. </summary>
        public double CurrentScale => 1.0 / zoomTransform.ScaleX * LogicalSize / ReferenceSize / ZoomAdjust;

        /// <summary> Gets the scale factor after the map was in animation mode / the anticipated scale factor while the map is
        /// in animation mode. Defined in logical units per pixel. </summary>
        public double FinalScale => 1.0 / (Math.Pow(2, z + 8) / ReferenceSize / ZoomAdjust) * LogicalSize / ReferenceSize / ZoomAdjust;

        /// <summary> Gets the number of meters spanned by one pixel. </summary>
        public double MetersPerPixel => CurrentScale * Math.Cos((Math.Atan(Math.Exp(CurrentY / 6371000.0)) - (Math.PI / 4)) / 0.5);

        /// <summary> Gets or sets the center of the map. </summary>
        public Point Center
        {
            get { return (Point)GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }
        #endregion

        #region dependency properties
        /// <summary> Dependency property for the scale value of the map object. </summary>
        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("ZoomScale", typeof(double), typeof(MapView), new PropertyMetadata(OnZoomScaleChanged));

        /// <summary> Dependency property for the center of the map. This enables animation, styling, binding, etc...</summary>
        public static readonly DependencyProperty CenterProperty =
            DependencyProperty.Register("Center", typeof(Point), typeof(MapView),
            new FrameworkPropertyMetadata(OnCenterChanged));
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="MapView"/> class. </summary>
        public MapView()
        {
            MaxZoom = 19;
            MinZoom = 0;

            // unset UseLayoutRounding, by-name cause it's a 4.0 property
            var useLayoutRoundingDescriptor = DependencyPropertyDescriptor.FromName(
                "UseLayoutRounding", typeof(MapView), typeof(MapView));
            useLayoutRoundingDescriptor?.SetValue(this, false);

            InitializeComponent();

            double zoomScale = Math.Pow(2, z + 8) / ReferenceSize / ZoomAdjust;

            // initialize transformation stack
            var logicalOffsetTransform = new TranslateTransform(-ReferenceSize / 2, -ReferenceSize / 2); // Transformation object for the logical offset.
            logicalOffsetTransform.Freeze();
            translateTransform = new TranslateTransform();
            zoomTransform = new ScaleTransform(zoomScale, zoomScale);
            screenOffsetTransform = new TranslateTransform();
            logicalWheelOffsetTransform = new TranslateTransform();
            physicalWheelOffsetTransform = new TranslateTransform();

            // The transformation group which combines all transformations.
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(logicalOffsetTransform);
            transformGroup.Children.Add(translateTransform);
            transformGroup.Children.Add(logicalWheelOffsetTransform);
            transformGroup.Children.Add(zoomTransform);
            transformGroup.Children.Add(physicalWheelOffsetTransform);
            transformGroup.Children.Add(screenOffsetTransform);
            GeoCanvas.RenderTransform = transformGroup;

            ClipToBounds = true;

            SizeChanged += MapView_SizeChanged;
            Loaded += MapView_Loaded;
        }
        #endregion

        #region private methods
        /// <summary> Fires the ViewportBeginChanged event. </summary>
        private void FireViewportBeginChanged()
        {
            ViewportBeginChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary> Fires the ViewportWhileChanged event. </summary>
        private void FireViewportWhileChanged()
        {
            ViewportWhileChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary> Fires the ViewportEndChanged event. </summary>
        private void FireViewportEndChanged()
        {
            ResetOrigin();

            if (ViewportEndChanged != null && !IsAnimating)
                ViewportEndChanged(this, EventArgs.Empty);
        }
        
        /// <summary> Starts a panning in the map. </summary>
        /// <param name="animate"> Flag showing if panning should be animated or not. </param>
        private void DoPan(bool animate)
        {
            double tmpX = -x * ZoomAdjust * ReferenceSize / LogicalSize;
            double tmpY = y * ZoomAdjust * ReferenceSize / LogicalSize;
            tmpX = tmpX + canvasOffset.X;
            tmpY = tmpY + canvasOffset.Y;

            if (!animate)
            {
                if (currentPanAnimationClock != null)
                {
                    currentPanAnimationClock.Completed -= panAnimation_Completed;
                    currentPanAnimationClock = null;
                }

                BeginAnimation(CenterProperty, null);
                Center = new Point(tmpX, tmpY);
            }
            else
            {
                var pointAnimation = new PointAnimation(new Point(tmpX, tmpY), new Duration(TimeSpan.FromMilliseconds(500)))
                {
                    IsCumulative = true,
                    AccelerationRatio = 0.45,
                    DecelerationRatio = 0.45,
                    FillBehavior = FillBehavior.HoldEnd
                };

                if (currentPanAnimationClock != null)
                {
                    currentPanAnimationClock.Completed -= panAnimation_Completed;
                }
                else
                {
                    pointAnimation.From = new Point(
                        -CurrentX * ZoomAdjust * ReferenceSize / LogicalSize, 
                        CurrentY * ZoomAdjust * ReferenceSize / LogicalSize);
                }
                pointAnimation.Freeze();

                currentPanAnimationClock = pointAnimation.CreateClock();
                runningPanClocks.Add(currentPanAnimationClock);
                currentPanAnimationClock.Completed += panAnimation_Completed;
                currentPanAnimationClock.Controller?.Begin();
                ((IAnimatable)this).ApplyAnimationClock(CenterProperty, currentPanAnimationClock, HandoffBehavior.Compose);
            }
        }

        /// <summary> List of all running clocks which are signaling the end of different panning actions. </summary>
        readonly List<AnimationClock> runningPanClocks = new List<AnimationClock>();
        /// <summary> List of all running clocks which are signaling the end of different zooming actions. </summary>
        readonly List<AnimationClock> runningZoomClocks = new List<AnimationClock>();

        /// <summary> Starts a zooming in the map. </summary>
        /// <param name="animate"> Flag showing if zooming should be animated or not. </param>
        private void DoZoom(bool animate)
        {
            double zoomScale = Math.Pow(2, z + 8) / ReferenceSize / ZoomAdjust;

            if (!animate)
            {
                if (currentZoomAnimationClock != null)
                {
                    currentZoomAnimationClock.Completed -= zoomAnimation_Completed;
                    currentZoomAnimationClock = null;
                }

                BeginAnimation(ScaleProperty, null);
                ZoomScale = zoomScale;
            }
            else
            {
                var da = new DoubleAnimation(zoomScale, new Duration(TimeSpan.FromMilliseconds(500)))
                {
                    AccelerationRatio = .10,
                    DecelerationRatio = .80,
                    IsCumulative = true,
                    FillBehavior = FillBehavior.HoldEnd
                };

                if (currentZoomAnimationClock != null)
                    currentZoomAnimationClock.Completed -= zoomAnimation_Completed;
                else
                {
                    da.From = Math.Pow(2, CurrentZoom + 8) / ReferenceSize / ZoomAdjust;
                }
                da.Freeze();

                currentZoomAnimationClock = da.CreateClock();
                runningZoomClocks.Add(currentZoomAnimationClock);
                currentZoomAnimationClock.Completed += zoomAnimation_Completed;
                currentZoomAnimationClock.Controller?.Begin();
                ((IAnimatable)this).ApplyAnimationClock(ScaleProperty, currentZoomAnimationClock, HandoffBehavior.Compose);
            }
        }

        /// <summary> Sets the zoom scale of the map and fires the viewport while changed event. </summary>
        /// <param name="zoomScale"> New zoom scale to be set. </param>
        private void SetZoomScale(double zoomScale)
        {
            zoomTransform.ScaleX = zoomScale;
            zoomTransform.ScaleY = zoomScale;

            FireViewportWhileChanged();
        }

        private void ResetOrigin()
        {
            if (!GlobalOptions.InfiniteZoom)
                return;

            var tx = FinalX;
            var ty = FinalY;

            canvasOffset = new Point(-tx * ZoomAdjust * ReferenceSize / LogicalSize,  ty * ZoomAdjust * ReferenceSize / LogicalSize);

            translateTransform.X = 0;
            translateTransform.Y = 0;
            logicalWheelOffsetTransform.X = 0;
            logicalWheelOffsetTransform.Y = 0;
            physicalWheelOffsetTransform.X = 0;
            physicalWheelOffsetTransform.Y = 0;
            x = 0;
            y = 0;
        }

        /// <summary> Resets the offset values in the map. </summary>
        private void ResetOffset()
        {
            var tx = CurrentX;
            var ty = CurrentY;

            translateTransform.X = -tx * ZoomAdjust * ReferenceSize / LogicalSize - canvasOffset.X;
            translateTransform.Y = ty * ZoomAdjust * ReferenceSize / LogicalSize - canvasOffset.Y;

            logicalWheelOffsetTransform.X = 0;
            logicalWheelOffsetTransform.Y = 0;
            physicalWheelOffsetTransform.X = 0;
            physicalWheelOffsetTransform.Y = 0;
        }

        /// <summary> Event handler for a change of the zoom scale. </summary>
        /// <param name="d"> Dependency object which has been changed. </param>
        /// <param name="e"> Event parameters. </param>
        private static void OnZoomScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var mapView = d as MapView;
            var zoomScale = (double)e.NewValue;

            mapView.zoomTransform.ScaleX = zoomScale;
            mapView.zoomTransform.ScaleY = zoomScale;

            mapView.FireViewportWhileChanged();
        }

        /// <summary> Event handler for a change of the map center. </summary>
        /// <param name="obj"> Dependency object which has been changed. </param>
        /// <param name="args"> Event parameters. </param>
        private static void OnCenterChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var mapView = obj as MapView;
            var center = (Point)args.NewValue;

            mapView.translateTransform.X = center.X - mapView.canvasOffset.X;
            mapView.translateTransform.Y = center.Y - mapView.canvasOffset.Y;

            mapView.FireViewportWhileChanged();
        }
        #endregion

        #region private event handling methods
        /// <summary> Event handler for a completion of the zoom animation. Stops the clock and fires the viewport end changed event. </summary>
        /// <param name="sender"> Sender of the Completed event. </param>
        /// <param name="e"> Event parameters. </param>
        private void zoomAnimation_Completed(object sender, EventArgs e)
        {
            var zoomScale = ZoomScale;
            currentZoomAnimationClock.Completed -= zoomAnimation_Completed;
            currentZoomAnimationClock = null;

            runningZoomClocks.ForEach(clock => { if (clock.Controller == null) return; clock.Controller.Stop(); clock.Controller.Remove(); });
            runningZoomClocks.Clear();

            //            BeginAnimation(ScaleProperty, null);
            ZoomScale = zoomScale;

            if (runningPanClocks.Count == 0)
                FireViewportEndChanged();
        }

        /// <summary> Event handler for a completion of the pan animation. Stops the clock and fires the viewport end changed event. </summary>
        /// <param name="sender"> Sender of the Completed event. </param>
        /// <param name="e"> Event parameters. </param>
        private void panAnimation_Completed(object sender, EventArgs e)
        {
            var center = Center;
            currentPanAnimationClock.Completed -= panAnimation_Completed;
            currentPanAnimationClock = null;

            runningPanClocks.ForEach(clock => { if (clock.Controller == null) return; clock.Controller.Stop(); clock.Controller.Remove(); });
            runningPanClocks.Clear();

            //            BeginAnimation(CenterProperty, null);
            Center = center;

            if(runningZoomClocks.Count == 0)
                FireViewportEndChanged();
        }

        /// <summary> Event handler for a completion of the map loading. Sets the map envelope to the current values. </summary>
        /// <param name="sender"> Sender of the Loaded event. </param>
        /// <param name="e"> Event parameters. </param>
        private void MapView_Loaded(object sender, RoutedEventArgs e)
        {
            screenOffsetTransform.X = ActualWidth / 2;
            screenOffsetTransform.Y = ActualHeight / 2;

            if (envMapRectangle.IsEmpty) return;

            SetEnvelope(envMapRectangle, false);
            envMapRectangle = new MapRectangle();
        }

        /// <summary> Event handler for a change of the map size. Adapts the zoom level and sends the viewport changed events. </summary>
        /// <param name="sender"> Sender of the SizeChanged event. </param>
        /// <param name="e"> Event parameters. </param>
        private void MapView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            screenOffsetTransform.X = ActualWidth / 2;
            screenOffsetTransform.Y = ActualHeight / 2;

            FireViewportBeginChanged();
            FireViewportWhileChanged();
            FireViewportEndChanged();
        }
        #endregion

        #region private helper methods
        /// <summary> Helper method which sets the map envelope depending on the different parameters. </summary>
        /// <param name="rect"> Envelope of the map. </param>
        /// <param name="animate"> Flag showing if the envelope change is to be animated or not. </param>
        /// <param name="maxAutoZoom"> Maximum automatic zoom. </param>
        private void SetEnvelopeHelper(MapRectangle rect, bool animate, double maxAutoZoom)
        {
            if (ActualHeight == 0 || ActualWidth == 0)
            {
                envMapRectangle = new MapRectangle(rect);
                return;
            }

            double dx = rect.Width;
            double dy = rect.Height;

            double zoomX;
            if (dx == 0)
                zoomX = MaxZoom;
            else
            {
                double scale = (dx / LogicalSize) * 256 / ActualWidth;
                zoomX = -Math.Log(scale, 2);
            }

            double zoomY;
            if (dy == 0)
                zoomY = MaxZoom;
            else
            {
                double scale = (dy / LogicalSize) * 256 / ActualHeight;
                zoomY = -Math.Log(scale, 2);
            }

            double tmpZoom = Math.Min(zoomX, zoomY);
            double maxZoom = Math.Min(MaxZoom, maxAutoZoom);
            maxZoom = Math.Max(z, maxZoom);
            tmpZoom = Math.Min(tmpZoom, maxZoom);
            tmpZoom = Math.Max(tmpZoom, MinZoom);

            SetXYZ(rect.Center.X, rect.Center.Y, tmpZoom, animate);
        }

        /// <summary> Helper method which sets a new map region depending on the given parameters. </summary>
        /// <param name="xCenter"> X coordinate of the map center. </param>
        /// <param name="yCenter"> Y coordinate of the map center. </param>
        /// <param name="zoom"> Zoom level to be displayed. </param>
        /// <param name="animatePan"> Flag showing if panning should be animated or not. </param>
        /// <param name="animateZoom"> Flag showing if zooming should be animated or not. </param>
        private void SetXYZHelper(double xCenter, double yCenter, double zoom, bool animatePan, bool animateZoom)
        {
            if (zoomTransform == null)
                return; // Needed in design mode

            zoom = Math.Max(minZoom, Math.Min(zoom, maxZoom));

            if (!GlobalOptions.InfiniteZoom && zoom > 19)
                animatePan = animateZoom = false;

            // reset map rectangle if XYZ is set after Rect while map is not loaded
            envMapRectangle = new MapRectangle();

            if (FitInWindow)
            {
                var scale = 1.0/(Math.Pow(2, z + 8)/ReferenceSize/ZoomAdjust)*LogicalSize/ReferenceSize/ZoomAdjust;
                var r = new MapRectangle(new Point(xCenter, yCenter), new Size(ActualWidth*scale, ActualHeight*scale));
                if (r.West > -20015087 && r.East > 20015087)
                    xCenter = xCenter - Math.Min(r.East - 20015087, r.West + 20015087);
                else if (r.East < 20015087 && r.West < -20015087)
                    xCenter = xCenter - Math.Max(r.East - 20015087, r.West + 20015087);
                if (r.South > -10000000 && r.North > 20015087)
                    yCenter = yCenter - Math.Min(r.North - 20015087, r.South + 10000000);
                else if (r.North < 20015087 && r.South < -10000000)
                    yCenter = yCenter - Math.Max(r.North - 20015087, r.South + 10000000);
            }

            if (animatePan && animateZoom)
            {
                // animating zoom and pan at the same time somehow looks strange
                // a special fly-to mode only uses the zoom animation with a speciial offset
                double zTo = Math.Pow(2, zoom + 8) / ReferenceSize / ZoomAdjust;
                double zCurrent = zoomTransform.ScaleX;

                if (Math.Abs(zTo - zCurrent) > .000001)
                {
                    FlyTo(new Point(xCenter, yCenter), zoom);
                    return;
                }
            }

            bool doPan = (FinalX != xCenter || FinalY != yCenter);
            bool doZoom = (z != zoom);

            x = xCenter + canvasOffset.X / ZoomAdjust / ReferenceSize * LogicalSize;
            y = yCenter - canvasOffset.Y / ZoomAdjust / ReferenceSize * LogicalSize;
            z = zoom;

            if (doPan)
                ResetOffset();

            FireViewportBeginChanged();

            if (doPan)
                DoPan(animatePan);

            if (doZoom)
                DoZoom(animateZoom);

            if ((doPan && !animatePan) || (doZoom && !animateZoom))
                FireViewportEndChanged();
        }

        /// <summary> Helper method which scrolls the map to a certain point and zooms to the given zoom level. </summary>
        /// <param name="point"> New center point of the map. </param>
        /// <param name="zoom"> New zoom level to show. </param>
        private void FlyTo(Point point, double zoom)
        {
            double zTo = Math.Pow(2, zoom + 8) / ReferenceSize / ZoomAdjust;
            double zCurrent = zoomTransform.ScaleX;

            double dZ = (zTo / zCurrent);
            dZ = dZ / (dZ - 1);

            double newX = (point.X - CurrentX) * dZ + CurrentX;
            double newY = (point.Y - CurrentY) * dZ + CurrentY;

            ZoomAround(new Point(newX, newY), zoom, true);
        }
        #endregion

        #region event handlers
        /// <summary> Event indicating the beginning of a change of the visible map section. This event is intended for
        /// more longtime actions (for example reading DB-objects), when the map section will change. </summary>
        public event EventHandler ViewportBeginChanged;

        /// <summary> Event indicating the ending of a change of the visible map section. It is the counterpart of the
        /// <seealso cref="ViewportBeginChanged"/> event. </summary>
        public event EventHandler ViewportEndChanged;

        /// <summary> Event indicating an intermediate view, when animation mode is active. It can be used to adapt the
        /// size of WPF objects, or other actions, which are not time-consuming. This event may called multiple times,
        /// when animation mode is active. </summary>
        public event EventHandler ViewportWhileChanged;
        #endregion

        #region public methods
        /// <summary> Gets the bounding box of the visible map section while the map is in animation mode. </summary>
        public MapRectangle CurrentEnvelope => new MapRectangle(new Point(CurrentX, CurrentY), new Size(ActualWidth * CurrentScale, ActualHeight * CurrentScale));


        /// <summary> Gets the anticipated bounding box of the visible map section after the map was in animation mode / the
        /// current box while the map is in animation mode. </summary>
        public MapRectangle FinalEnvelope => new MapRectangle(new Point(FinalX, FinalY), new Size(ActualWidth * FinalScale, ActualHeight * FinalScale));

        /// <summary> Sets the visible map section to a bounding box, so the box is contained in the map section. </summary>
        /// <param name="rect"> The bounding box. </param>
        /// <param name="animate"> Animate the transition to the new map section. </param>
        public void SetEnvelope(MapRectangle rect, bool animate)
        {
            SetEnvelopeHelper(rect, animate, 99.0);
        }

        /// <summary> Sets the visible map section to a bounding box, so the box is contained in the map section. </summary>
        /// <param name="rect"> The bounding box. </param>
        /// <param name="animate"> Animate the transition to the new map section. </param>
        /// <param name="maxAutoZoom"> Additional specification of the maximal zoom. </param>
        public void SetEnvelope(MapRectangle rect, bool animate, double maxAutoZoom)
        {
            SetEnvelopeHelper(rect, animate, maxAutoZoom);
        }

        /// <summary> Positions the map to a center and zoom factor. </summary>
        /// <param name="xLogicalUnit"> X-coordinate in logical units. </param>
        /// <param name="yLogicalUnit"> Y-coordinate in logical units. </param>
        /// <param name="zoom"> The zoom factor according to the standard tiling/zooming scheme. </param>       
        /// <param name="animate"> Animates the transition. </param>
        public void SetXYZ(double xLogicalUnit, double yLogicalUnit, double zoom, bool animate)
        {
            SetXYZHelper(xLogicalUnit, yLogicalUnit, zoom, animate, animate);
        }

        /// <summary> Positions the map to a center and zoom factor. </summary>
        /// <param name="xLogicalUnit"> X-coordinate in logical units. </param>
        /// <param name="yLogicalUnit"> Y-coordinate in logical units. </param>
        /// <param name="zoom"> The zoom factor according to the standard tiling/zooming scheme. </param>
        /// <param name="animatePan"> Animates the pan transition. </param>
        /// <param name="animateZoom"> Animates the zoom transition. </param>
        public void SetXYZ(double xLogicalUnit, double yLogicalUnit, double zoom, bool animatePan, bool animateZoom)
        {
            SetXYZHelper(xLogicalUnit, yLogicalUnit, zoom, animatePan, animateZoom);
        }

        /// <summary> Zooms around a point. </summary>
        /// <param name="point"> Center of the new map section. </param>
        /// <param name="zoom"> The zoom factor according to the standard tiling/zooming scheme. </param>       
        /// <param name="animate"> Animate the transition to the new map section. </param>
        public void ZoomAround(Point point, double zoom, bool animate)
        {
            if (zoom > MaxZoom)
                zoom = MaxZoom;

            if (zoom < MinZoom)
                zoom = MinZoom;

            if (!GlobalOptions.InfiniteZoom && zoom > 19)
                animate = false;

            z = Math.Max(minZoom, Math.Min(zoom, maxZoom));

            double dxa = point.X * ZoomAdjust / LogicalSize * ReferenceSize + canvasOffset.X;
            double dya = -point.Y * ZoomAdjust / LogicalSize * ReferenceSize + canvasOffset.Y;

            double logX = dxa + ReferenceSize / 2;
            double logY = dya + ReferenceSize / 2;
            Point pp = GeoCanvas.RenderTransform.Transform(new Point(logX, logY));

            logicalWheelOffsetTransform.X = -translateTransform.X - dxa;
            logicalWheelOffsetTransform.Y = -translateTransform.Y - dya;

            physicalWheelOffsetTransform.X = pp.X - ActualWidth / 2;
            physicalWheelOffsetTransform.Y = pp.Y - ActualHeight / 2;

            FireViewportBeginChanged();
            DoZoom(animate);
            if (!animate)
                FireViewportEndChanged();
        }
        #endregion
    }
}
