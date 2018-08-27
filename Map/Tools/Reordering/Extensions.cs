// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace Ptv.XServer.Controls.Map.Tools.Reordering
{
    /// <summary> Extension methods &amp; statics. </summary>
    internal static class Extensions
    {
        #region public methods
        /// <summary> Gets the rectangle of the given UIElement, relative to the given visual. </summary>
        /// <param name="e"> Documentation in progress... </param>
        /// <param name="v"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static Rect transformBoundsToVisual(this UIElement e, Visual v)
        {
            return e.TransformToVisual(v).TransformBounds(VisualTreeHelper.GetDescendantBounds(e));
        }

        /// <summary> Gets the combined rectangle of the given elements, relative to the given visual. </summary>
        /// <param name="elements"> Documentation in progress... </param>
        /// <param name="v"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static Rect transformBoundsToVisual(this IEnumerable<UIElement> elements, Visual v)
        {
            var rc = new Rect();

            foreach (UIElement e in elements)
            {
                if (rc.Width < 1e-4 && rc.Height < 1e-4)
                    rc = e.transformBoundsToVisual(v);
                else
                    rc = Rect.Union(rc, e.transformBoundsToVisual(v));
            }

            return rc;
        }
        
        /// <summary> Constructs a new point with the minimum of the x- and y-coordinate. </summary>
        /// <param name="p"> Documentation in progress... </param>
        /// <param name="q"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static Point Min(this Point p, Point q)
        {
            return new Point(Math.Min(p.X, q.X), Math.Min(p.Y, q.Y));
        }

        /// <summary> Adds an element to a grid. </summary>
        /// <param name="elem"> Documentation in progress... </param>
        /// <param name="g"> Documentation in progress... </param>
        /// <param name="row"> Documentation in progress... </param>
        /// <param name="col"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static UIElement AddToGrid(this UIElement elem, Grid g, int row, int col)
        {
            g.Children.Add(elem);
            elem.SetGridPosition(row, col);

            return elem;
        }

        /// <summary> Adds an element to a grid. </summary>
        /// <param name="elem"> Documentation in progress... </param>
        /// <param name="g"> Documentation in progress... </param>
        /// <param name="row"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static UIElement AddToGrid(this UIElement elem, Grid g, int row)
        {
            return elem.AddToGrid(g, row, 0, g.ColumnDefinitions.Count);
        }

        /// <summary> Adds an element to a grid. </summary>
        /// <param name="elem"> Documentation in progress... </param>
        /// <param name="g"> Documentation in progress... </param>
        /// <param name="row"> Documentation in progress... </param>
        /// <param name="col"> Documentation in progress... </param>
        /// <param name="colSpan"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static UIElement AddToGrid(this UIElement elem, Grid g, int row, int col, int colSpan)
        {
            g.Children.Add(elem);
            elem.SetGridPosition(row, col, colSpan);

            return elem;
        }

        /// <summary> Sets the grid position of an element. </summary>
        /// <param name="elem"> Documentation in progress... </param>
        /// <param name="row"> Documentation in progress... </param>
        /// <param name="col"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static UIElement SetGridPosition(this UIElement elem, int row, int col)
        {
            Grid.SetRow(elem, row);
            Grid.SetColumn(elem, col);

            return elem;
        }

        /// <summary> Set the grid position of an element. </summary>
        /// <param name="elem"> Documentation in progress... </param>
        /// <param name="row"> Documentation in progress... </param>
        /// <param name="col"> Documentation in progress... </param>
        /// <param name="colSpan"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static UIElement SetGridPosition(this UIElement elem, int row, int col, int colSpan)
        {
            Grid.SetRow(elem, row);
            Grid.SetColumn(elem, col);
            Grid.SetColumnSpan(elem, colSpan);

            return elem;
        }

        /// <summary> Adds row definitions. </summary>
        /// <param name="g"> Documentation in progress... </param>
        /// <param name="n"> Documentation in progress... </param>
        /// <param name="length"> Documentation in progress... </param>
        public static void AddRowDefinitions(this Grid g, int n, GridLength length)
        {
            while (n-- > 0)
                g.RowDefinitions.Add(new RowDefinition { Height = length });
        }

        /// <summary> Adds column definitions. </summary>
        /// <param name="g"> Documentation in progress... </param>
        /// <param name="n"> Documentation in progress... </param>
        /// <param name="length"> Documentation in progress... </param>
        public static void AddColumnDefinitions(this Grid g, int n, GridLength length)
        {
            while (n-- > 0)
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = length });
        }

        /// <summary> Checks if the mouse moves within the visible region of a framework element. </summary>
        /// <param name="elem"> Documentation in progress... </param>
        /// <param name="args"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static bool Contains(this FrameworkElement elem, MouseEventArgs args)
        {
            return elem.Contains(args.GetPosition(elem));
        }

        /// <summary> Checks if the given point is within the visible region of a framework element. </summary>
        /// <param name="elem"> Documentation in progress... </param>
        /// <param name="p"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static bool Contains(this FrameworkElement elem, Point p)
        {
            return new Rect(0, 0, elem.ActualWidth, elem.ActualHeight).Contains(p);
        }

        /// <summary> Take a snapshot of a visual. </summary>
        /// <param name="v"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static Image takeSnapshot(this Visual v)
        {
            var drw = new DrawingVisual();

            Rect bounds = VisualTreeHelper.GetDescendantBounds(v);

            using (DrawingContext ctx = drw.RenderOpen())
                ctx.DrawRectangle(new VisualBrush(v), null, new Rect(bounds.Size));

            Matrix m = PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice;

            double dpiX = m.M11 * 96;
            double dpiY = m.M22 * 96;

            var render = new RenderTargetBitmap((int)(bounds.Width * dpiX / 96 + .5), (int)(bounds.Height * dpiY / 96 + .5), dpiX, dpiY, PixelFormats.Pbgra32);

            render.Render(drw);

            return new Image { Source = render, Width = bounds.Width, Height = bounds.Height };
        }
        #endregion
    }
}
