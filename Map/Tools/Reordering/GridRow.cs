using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Collections.Generic;


namespace Ptv.XServer.Controls.Map.Tools.Reordering
{
    /// <summary> Stores the UI elements of a single grid row. </summary>
    internal class GridRow
    {
        #region private variables
        /// <summary> The grid. </summary>
        private readonly Grid grid;
        /// <summary> UI elements of the row. </summary>
        private readonly List<UIElement> elements;
        /// <summary> Initial opacities of the UI elements. </summary>
        private readonly List<double> opacities = new List<double>();
        /// <summary> Index of the reference element in the elements list. -1 if there is no reference element. </summary>
        private readonly int referenceElementIndex;
        /// <summary> Number of running opacity animations, counting all GridRow instances. </summary>
        private static int currentOpacityAnimations;
        /// <summary> Lock for updating currentOpacityAnimations. </summary>
        private static readonly object currentOpacityAnimationsLock = Guid.NewGuid();
        /// <summary> Gets the reference element of this row. Returns null if there is no explicit reference element. </summary>
        private FrameworkElement ReferenceElement => referenceElementIndex == -1 ? null : elements[referenceElementIndex] as FrameworkElement;

        #endregion

        #region public variables
        /// <summary>
        /// Gets the animation threshold. The animation threshold is the y-coordinate 
        /// of this row to be used when checking if the mouse position is above or 
        /// below this row. Coordinate is relative to the grid.
        /// </summary>
        public double AnimationThreshold
        {
            get
            {
                if (ReferenceElement != null)
                    return ReferenceElement.TranslatePoint(new Point(0, ReferenceElement.ActualHeight/2), grid).Y;

                // if there is no ReferenceElement, use the corresponding RowDefinition

                // found no way to determine the actual position of a row in one step.
                // Must sum up heights to get position.
                double y = 0;
                for (int i = 0; i < Row; i++)
                    y += grid.RowDefinitions[i].ActualHeight;

                return y + RowDefinition.ActualHeight / 2;
            }
        }

        /// <summary> Gets the row bounds. </summary>
        public Rect Bounds
        {
            get
            {
                // found no way to determine the bounds of a row in one step.
                // Must sum up heights to get bounds.

                double y = 0;

                for (int i = 0; i < Row - 1; i++)
                    y += grid.RowDefinitions[i].ActualHeight;

                return new Rect(0, y, grid.ActualWidth, RowDefinition.ActualHeight);
            }
        }

        /// <summary> Gets the corresponding row definition. </summary>
        public RowDefinition RowDefinition => grid.RowDefinitions[Row];

        /// <summary> Gets or sets the grid row that this instance represents. </summary>
        public int Row
        {
            // Just need to get row for element #0, all elements are (should be) in the same row
            get { return Grid.GetRow(elements[0]); }
            // Update the row, keeping the column of each element
            set { elements.ForEach(element => element.SetGridPosition(value, Grid.GetColumn(element))); }
        }

        /// <summary> Gets a snapshot of the row. </summary>
        public Image Snapshot
        {
            get
            {
                // rectangle bounds
                Rect bounds = elements.transformBoundsToVisual(grid);

                // render snapshot into a DrawingVisual, using a VisualBrush
                var drw = new DrawingVisual();

                using (DrawingContext ctx = drw.RenderOpen())
                {
                    var brush = new VisualBrush(grid)
                    {
                        Viewbox = bounds,
                        ViewboxUnits = BrushMappingMode.Absolute,
                        Stretch = Stretch.UniformToFill
                    };

                    ctx.DrawRectangle(brush, null, new Rect(bounds.Size));
                }

                // render the image, taking system resolution in to account

                Matrix m = PresentationSource.FromVisual(grid).CompositionTarget.TransformToDevice;

                double dpiX = m.M11 * 96;
                double dpiY = m.M22 * 96;

                var render = new RenderTargetBitmap((int)(bounds.Width * dpiX / 96 + .5), (int)(bounds.Height * dpiY / 96 + .5), dpiX, dpiY, PixelFormats.Pbgra32);

                render.Render(drw);

                // return image
                return new Image { Source = render, Width = bounds.Width, Height = bounds.Height };
            }
        }
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="GridRow"/> class. </summary>
        /// <param name="grid"> The grid. </param>
        /// <param name="row"> Grid row this instance should represent. </param>
        /// <param name="refCol"> Number of the column that contains the reference 
        /// elements. -1 if there is no explicit reference column. </param>
        public GridRow(Grid grid, int row, int? refCol)
        {
            this.grid = grid;
            elements = (from UIElement e in grid.Children where Grid.GetRow(e) == row select e).ToList();
            referenceElementIndex = !refCol.HasValue ? -1 : elements.FindIndex(e => Grid.GetColumn(e) == refCol);

            // backup opacities
            elements.ForEach(element => opacities.Add(element.Opacity));
        }
        #endregion

        #region private methods
        /// <summary> Update animatedOpacityCount. </summary>
        /// <param name="delta"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private static int updateAnimatedOpacityCount(int delta)
        {
            lock (currentOpacityAnimationsLock)
                return currentOpacityAnimations += delta;
        }

        /// <summary> DoEvent for WPF - used in waitAnimatedOpacity. </summary>
        private static void DoEvents()
        {
            if (Application.Current != null)
                Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background,
                    new System.Threading.ThreadStart(delegate { }));
            else
                System.Windows.Forms.Application.DoEvents();
        }
        #endregion

        #region public methods
        /// <summary> Waits for opacity animations to finish. </summary>
        public static void waitAnimatedOpacity()
        {
            while (updateAnimatedOpacityCount(0) > 0)
                DoEvents();
        }

        /// <summary>
        /// Animates the opacity values of each element in the grid row. Keeps
        /// updateAnimatedOpacityCount up to date.
        /// </summary>
        /// <param name="to"> The final opacity of the animation. </param>
        /// <param name="duration"> The duration of the animation. </param> 
        /// <param name="easing"> The EasingFunction of the animation. </param>
        public void animateOpacity(double to, int duration, IEasingFunction easing)
        {
            // for each element in row
            foreach (UIElement e in elements)
            {
                if (duration < 1)
                {
                    e.Opacity = to * opacities[elements.IndexOf(e)];
                }
                else
                {
                    // create animation
                    var animation = new DoubleAnimation
                    {
                        From = e.Opacity,
                        To = to * opacities[elements.IndexOf(e)],
                        Duration = new TimeSpan(duration * 10000),
                        EasingFunction = easing
                    };

                    // keep animation count up to date
                    updateAnimatedOpacityCount(1);
                    animation.Completed += (s, ev) => updateAnimatedOpacityCount(-1);

                    // start animation
                    e.BeginAnimation(UIElement.OpacityProperty, animation, HandoffBehavior.SnapshotAndReplace);
                }
            }
        }

        /// <summary> Determines an additional offset for the preview adorner. </summary>
        /// <param name="dragged"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public Vector GetSnapshotOffsetForAdorner(UIElement dragged)
        {
            Point p = dragged.TranslatePoint(new Point(), grid), min = p;

            foreach (Point q in from e in elements where e != dragged select e.TranslatePoint(new Point(), grid))
            {
                min.X = Math.Min(min.X, q.X);
                min.Y = Math.Min(min.Y, q.Y);
            }

            return Point.Subtract(min, p);
        }
        #endregion
    }
}
