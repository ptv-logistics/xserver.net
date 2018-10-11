// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Threading;

namespace Ptv.XServer.Controls.Map.Tools.Reordering
{
    /// <summary> Arguments for the 'row moved' event. </summary>
    internal class RowMovedEventArgs : EventArgs
    {
        #region public variables
        /// <summary> The Grid. </summary>
        public UIElement Element;
        /// <summary> Source row. </summary>
        public int SourceRow;
        /// <summary> Target row. </summary>
        public int TargetRow;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="RowMovedEventArgs"/> class. </summary>
        /// <param name="Element"> Documentation in progress... </param>
        /// <param name="SourceRow"> Documentation in progress... </param>
        /// <param name="TargetRow"> Documentation in progress... </param>
        public RowMovedEventArgs(UIElement Element, int SourceRow, int TargetRow)
        {
            this.Element = Element;
            this.SourceRow = SourceRow;
            this.TargetRow = TargetRow;
        }
        #endregion
    }

    /// <summary> Argument for the routed row moved event. </summary>
    internal class RoutedRowMovedEventArgs : RoutedEventArgs
    {
        #region public variables
        /// <summary> Gets Documentation in progress... </summary>
        public int SourceRow { get; }

        /// <summary> Gets Documentation in progress... </summary>
        public int TargetRow { get; }
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="RoutedRowMovedEventArgs"/> class. </summary>
        /// <param name="ev"> Documentation in progress... </param>
        /// <param name="sourceRow"> Documentation in progress... </param>
        /// <param name="targetRow"> Documentation in progress... </param>
        public RoutedRowMovedEventArgs(RoutedEvent ev, int sourceRow, int targetRow)
            : base(ev)
        {
            SourceRow = sourceRow;
            TargetRow = targetRow;
        }
        #endregion
    }

    /// <summary> Event for querying if a row could be moved. </summary>
    internal class RoutedAllowMoveRowEventArgs : RoutedRowMovedEventArgs
    {
        #region public variables
        /// <summary> Gets or sets a value indicating whether Documentation in progress... </summary>
        public bool IsAllowed
        {
            get;
            set;
        }
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="RoutedAllowMoveRowEventArgs"/> class. </summary>
        /// <param name="ev"> Documentation in progress... </param>
        /// <param name="sourceRow"> Documentation in progress... </param>
        /// <param name="targetRow"> Documentation in progress... </param>
        public RoutedAllowMoveRowEventArgs(RoutedEvent ev, int sourceRow, int targetRow)
            : base(ev, sourceRow, targetRow)
        {
            IsAllowed = true;
        }
        #endregion
    }

    #region delegates
    /// <summary> Event handler: row moved. </summary>
    /// <param name="sender"> GridReorderPattern instance. </param>
    /// <param name="args"> Additional event arguments. </param>
    internal delegate void RowMovedEventHandler(object sender, RowMovedEventArgs args);

    /// <summary> Routed event handler: row moved. </summary>
    /// <param name="sender"> Documentation in progress... </param>
    /// <param name="args"> Documentation in progress... </param>
    internal delegate void RoutedRowMovedEventHandler(object sender, RoutedRowMovedEventArgs args);

    /// <summary> Routed event handler: query if row is moveable. </summary>
    /// <param name="sender"> Documentation in progress... </param>
    /// <param name="args"> Documentation in progress... </param>
    internal delegate void RoutedAllowMoveRowEventHandler(object sender, RoutedAllowMoveRowEventArgs args);
    #endregion

    /// <summary> Makes the rows of a grid available for reordering by a Drag&amp;Drop operation. </summary>
    internal class GridReordering
    {
        #region private variables
        /// <summary> List of real grid rows, containing the UI elements. </summary>
        private readonly List<GridRow> gridRows = new List<GridRow>();
        /// <summary> This marks the target index when element is dropped. Index is updated while element is being dragged. </summary>
        private int? dropIdx;
        /// <summary> Preferred drag column. </summary>
        private readonly int? dragCol;
        /// <summary> Preview adorner. </summary>
        private PreviewAdorner adorner;
        /// <summary> The grid itself. </summary>
        private readonly Grid grid;
        /// <summary> Drag&amp;Drop origin. </summary>
        private Point? dragDropOrigin;
        /// <summary> True while element is being dragged. </summary>
        private bool IsDragging;
        /// <summary> Lock for synchronizing events. </summary>
        private readonly object lockObject = new Object();
        /// <summary> Event reference counter. </summary>
        private long eventRef;
        /// <summary> Default color of text elements. Needed to reset the color. </summary>
        private static Brush defaultColor;
        /// <summary> Gets the explicit drag column, if any. </summary>
        private int? DragColumn => dragCol ?? GetDragColumn(grid);

        /// <summary> Gets the rectangles of the droppable regions, ordered by y-coordinate, ascending. </summary>
        private IEnumerable<Rect> ActualDropRegions
        {
            get
            {
                double lastY = 0;

                Func<double, Rect> makeRect = y => new Rect(0, lastY, grid.ActualWidth, Math.Max(0, y - lastY));

                foreach (var row in gridRows)
                {
                    double Y;
                    yield return makeRect(Y = row.AnimationThreshold);
                    lastY = Y;
                }

                yield return makeRect(grid.ActualHeight);
            }
        }
        #endregion

        #region public variables
        /// <summary> Event handler: row moved. </summary>
        public RowMovedEventHandler RowMoved;
        #endregion

        #region attached properties and events
        /// <summary> Attached pattern property used internally for storing pattern instances. </summary>
        private static readonly DependencyProperty GridReorderPatternProperty =
            DependencyProperty.RegisterAttached("GridReorderPattern", typeof(GridReordering), typeof(Grid));

        /// <summary> Set attached pattern property. </summary>
        /// <param name="grid"> Documentation in progress... </param>
        /// <param name="value"> Documentation in progress... </param>
        private static void SetGridReorderPattern(Grid grid, GridReordering value)
        {
            grid.SetValue(GridReorderPatternProperty, value);
        }

        /// <summary> Get attached pattern property. </summary>
        /// <param name="grid"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private static GridReordering GetGridReorderPattern(Grid grid)
        {
            return (GridReordering)grid.GetValue(GridReorderPatternProperty);
        }

        /// <summary> Attached enabled property. </summary>
        public static readonly DependencyProperty EnabledProperty =
            DependencyProperty.RegisterAttached("Enabled", typeof(bool), typeof(Grid));

        /// <summary> Set attached enabled property. </summary>
        /// <param name="grid"> Documentation in progress... </param>
        /// <param name="value"> Documentation in progress... </param>
        public static void SetEnabled(Grid grid, bool value)
        {
            if (value && GetGridReorderPattern(grid) == null)
                SetGridReorderPattern(grid, apply(grid, null, null));

            if (!value && GetGridReorderPattern(grid) != null)
            {
                GetGridReorderPattern(grid).Detach();
                SetGridReorderPattern(grid, null);
            }

            grid.SetValue(EnabledProperty, value);
        }

        /// <summary> Read attached enabled property. </summary>
        /// <param name="grid"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static bool GetEnabled(Grid grid)
        {
            return (bool)grid.GetValue(EnabledProperty);
        }

        /// <summary> Attached Drag column property. </summary>
        public static readonly DependencyProperty DragColumnProperty =
            DependencyProperty.RegisterAttached("DragColumn", typeof(int?), typeof(Grid));

        /// <summary> Set attached Drag column property. </summary>
        /// <param name="grid"> Documentation in progress... </param>
        /// <param name="value"> Documentation in progress... </param>
        public static void SetDragColumn(Grid grid, int? value)
        {
            grid.SetValue(DragColumnProperty, value);
        }

        /// <summary> Read attached Drag column property. </summary>
        /// <param name="grid"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static int? GetDragColumn(Grid grid)
        {
            return (int?)grid.GetValue(DragColumnProperty);
        }

        /// <summary> Attached row moved event. </summary>
        public static readonly RoutedEvent RowMovedEvent =
            EventManager.RegisterRoutedEvent("RowMoved", RoutingStrategy.Bubble, typeof(RoutedRowMovedEventHandler), typeof(GridReordering));

        /// <summary> Add event handler. </summary>
        /// <param name="d"> Documentation in progress... </param>
        /// <param name="handler"> Documentation in progress... </param>
        public static void AddRowMovedHandler(DependencyObject d, RoutedRowMovedEventHandler handler)
        {
            (d as UIElement)?.AddHandler(RowMovedEvent, handler);
        }

        /// <summary> Remove event handler. </summary>
        /// <param name="d"> Documentation in progress... </param>
        /// <param name="handler"> Documentation in progress... </param>
        public static void RemoveRowMovedHandler(DependencyObject d, RoutedRowMovedEventHandler handler)
        {
            (d as UIElement)?.RemoveHandler(RowMovedEvent, handler);
        }

        /// <summary> Attached row moved event. </summary>
        public static readonly RoutedEvent AllowMoveRowEvent =
            EventManager.RegisterRoutedEvent("AllowRowMove", RoutingStrategy.Bubble, typeof(RoutedAllowMoveRowEventHandler), typeof(GridReordering));

        /// <summary> Add event handler. </summary>
        /// <param name="d"> Documentation in progress... </param>
        /// <param name="handler"> Documentation in progress... </param>
        public static void AddAllowMoveRowHandler(DependencyObject d, RoutedAllowMoveRowEventHandler handler)
        {
            (d as UIElement)?.AddHandler(AllowMoveRowEvent, handler);
        }

        /// <summary> Remove event handler. </summary>
        /// <param name="d"> Documentation in progress... </param>
        /// <param name="handler"> Documentation in progress... </param>
        public static void RemoveAllowMoveRowHandler(DependencyObject d, RoutedAllowMoveRowEventHandler handler)
        {
            (d as UIElement)?.RemoveHandler(AllowMoveRowEvent, handler);
        }

        /// <summary> Determine if a row can be move to a certain destination. </summary>
        /// <param name="sourceRow"> Documentation in progress... </param>
        /// <param name="targetRow"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private bool allowMoveRow(int sourceRow, int targetRow)
        {
            var args = new RoutedAllowMoveRowEventArgs(AllowMoveRowEvent, sourceRow, (sourceRow < targetRow) ? targetRow - 1 : targetRow);

            grid.RaiseEvent(args);

            return args.IsAllowed;
        }

        /// <summary> Fire attached row moved event. </summary>
        /// <param name="sourceRow"> Documentation in progress... </param>
        /// <param name="targetRow"> Documentation in progress... </param>
        private void RaiseRowMoved(int sourceRow, int targetRow)
        {
            // call event delegate
            RowMoved?.Invoke(this, new RowMovedEventArgs(grid, sourceRow, targetRow));

            // fire attached routed event
            grid.RaiseEvent(new RoutedRowMovedEventArgs(RowMovedEvent, sourceRow, targetRow));
        }
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="GridReordering"/> class. Use apply for creating an instance of GridReorderPattern. </summary>
        /// <param name="grid"> Grid to apply pattern for. </param>
        /// <param name="dragCol"> Enable Drag&amp;Drop only for the FrameworkElements of this column. 
        /// If set to null, each FrameworkElement of the grid is applicable for Drag&amp;Drop. </param>
        private GridReordering(Grid grid, int? dragCol)
        {
            this.grid = grid;
            this.dragCol = dragCol;

            GetDefaultTextColor(grid);
            wireMouseEvents();
        }
        #endregion

        #region private methods
        /// <summary> Retrieves the default text color of the text block elements. Needed to reset the color. </summary>
        /// <param name="grid"> Documentation in progress... </param>
        private static void GetDefaultTextColor(Grid grid)
        {
            for (int childPos = 0; childPos < grid.Children.Count; childPos++)
            {
                if (!(grid.Children[childPos] is TextBlock textBlock))
                    continue;
                defaultColor = textBlock.Foreground;
                break;
            }
        }

        /// <summary> Creates GridRow objects out of the grid's child elements. </summary>
        private void createGridRows()
        {
            gridRows.Clear();

            for (int i = 0; i < grid.RowDefinitions.Count; i++)
                gridRows.Add(new GridRow(grid, i, AlwaysUseRowHeightAsDropThreshold ? null : DragColumn));
        }

        /// <summary> Sets up handlers for the mouse events of the elements which can be dragged. </summary>
        private void wireMouseEvents()
        {
            grid.PreviewMouseLeftButtonDown += PreviewMouseLeftButtonDown;
            grid.PreviewMouseLeftButtonUp += PreviewMouseLeftButtonUp;
            grid.PreviewMouseMove += PreviewMouseMove;
            wireTextBlockEvents();
        }

        /// <summary> Removes handlers for the mouse events of the elements which can be dragged. </summary>
        private void unwireMouseEvents()
        {
            grid.PreviewMouseLeftButtonDown -= PreviewMouseLeftButtonDown;
            grid.PreviewMouseLeftButtonUp -= PreviewMouseLeftButtonUp;
            grid.PreviewMouseMove -= PreviewMouseMove;
            unwireTextBlockEvents();

        }

        /// <summary> Adds mouse event handlers for all text block elements. </summary>
        private void wireTextBlockEvents()
        {
            for (int childPos = 0; childPos < grid.Children.Count; childPos++)
            {
                if (!(grid.Children[childPos] is TextBlock textBlock))
                    continue;

                textBlock.MouseEnter += TextBlock_MouseEnter;
                textBlock.MouseLeave += TextBlock_MouseLeave;
            }
        }

        /// <summary> Removes the mouse event handlers for all text block elements. </summary>
        private void unwireTextBlockEvents()
        {
            for (int childPos = 0; childPos < grid.Children.Count; childPos++)
            {
                if (!(grid.Children[childPos] is TextBlock textBlock))
                    continue;

                textBlock.MouseEnter -= TextBlock_MouseEnter;
                textBlock.MouseLeave -= TextBlock_MouseLeave;
            }
        }

        /// <summary> Inserts additional RowDefinitions that are animated while an element is being dragged. </summary>
        private void injectRowDefinitions()
        {
            for (int i = 0; i <= gridRows.Count; i++)
            {
                // insert new row definition
                grid.RowDefinitions.Insert(i * 2, new RowDefinition { Height = new GridLength(0) });

                // update row of existing elements
                if (i < gridRows.Count)
                    gridRows[i].Row = gridRows[i].Row * 2 + 1;
            }
        }

        /// <summary> Removes the previously inserted RowDefinitions, restores the original rows. </summary>
        private void removeRowDefinitions()
        {
            // remove row definitions
            for (int i = 0; i < grid.RowDefinitions.Count; i++)
                grid.RowDefinitions.RemoveAt(i);

            // update grid rows
            for (int i = 0; i < gridRows.Count; i++)
                gridRows[i].Row = i;
        }

        /// <summary> Drag element. </summary>
        /// <param name="element"> Documentation in progress... </param>
        private void StartDragDrop(UIElement element)
        {
            // fill in the borders that will be 
            // animated while element is being dragged
            injectRowDefinitions();

            // force initial region determination 
            // on first call to AnimateTargetRegion
            dropIdx = null;

            // fade out row of the element being dragged
            if (FadeDraggedRow)
                gridRows[IndexOf(element)].animateOpacity(DraggedRowOpacity, DraggedRowFadeOutDuration, DraggedRowFadeOutEasing);
        }

        /// <summary> Get grid row &amp; column from a UIElement or it's parent element. </summary>
        /// <param name="e"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private GridPosition PositionOf(UIElement e)
        {
            while (e != null && !grid.Children.Contains(e))
                e = VisualTreeHelper.GetParent(e) as UIElement;

            return e != null ? new GridPosition { Column = Grid.GetColumn(e), Row = Grid.GetRow(e) } : new GridPosition();
        }

        /// <summary> Get the index of the given grid element. </summary>
        /// <param name="e"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private int IndexOf(UIElement e)
        {
            return PositionOf(e).Row / 2;
        }

        /// <summary> Move dragged element. </summary>
        /// <param name="element"> Documentation in progress... </param>
        private void DragMove(UIElement element)
        {
            // index of dragged item
            int dragIdx = IndexOf(element);

            // check if element position is over the grid
            bool movesInGrid = grid.Contains(Mouse.GetPosition(grid));

            // create / update preview adorner
            if (UsePreviewAdorner)
            {
                PreviewAdorner.attach(ref adorner, () => element as FrameworkElement, () => gridRows[dragIdx].Snapshot, 0.5);
                adorner.Offset = Vector.Add(Point.Subtract(Mouse.GetPosition(element), dragDropOrigin.Value), gridRows[dragIdx].GetSnapshotOffsetForAdorner(element));
            }

            Point? p = movesInGrid ? Mouse.GetPosition(grid) : (Point?)null;

            // update target index, animate rows
            UpdateDropIndexAndAnimate(dragIdx, p, new[] { dragIdx, dragIdx + 1 });

            // set cursor
            Mouse.SetCursor(movesInGrid && reorderElements(FindDropRegion(p), dragIdx, true) ? Cursors.Arrow : Cursors.No);
        }

        /// <summary> Element dropped. </summary>
        /// <param name="element"> Documentation in progress... </param>
        private void DoneDragDrop(UIElement element)
        {
            unwireTextBlockEvents();

            // backup drop index now since the call to UpdateTargetRegion below will modify the index
            int? backupDropIdx = dropIdx;

            // list index of dragged element 
            int dragIdx = IndexOf(element);

            // reset default layout
            UpdateDropIndexAndAnimate(dragIdx, null, null);

            // remove preview adorner
            if (UsePreviewAdorner)
                PreviewAdorner.detach(ref adorner);

            // remove previously inserted border elements
            removeRowDefinitions();

            // reset mouse cursor
            Mouse.SetCursor(Cursors.Arrow);

            // no reordering necessary - at least fade dragged element in again
            if (FadeDraggedRow)
                gridRows[dragIdx].animateOpacity(1, DraggedRowFadeInDuration, DraggedRowFadeInEasing);

            // reorder elements if necessary 
            if (reorderElements(backupDropIdx, dragIdx, false))
            {
                // fire events
                // Adapting the dropIdx according the convention of ObservableCollection.Move():
                // If dropIdx > dragIdx, the position number AFTER the insertion has to be used.
                RaiseRowMoved(dragIdx, backupDropIdx.Value > dragIdx ? backupDropIdx.Value - 1 : backupDropIdx.Value);
            }

            wireTextBlockEvents();
        }

        /// <summary> Reorders elements, if necessary. </summary>
        /// <param name="dropIdx"> Drop index (= insert position). </param>
        /// <param name="dragIdx"> Index of dragged row (= source index). </param>
        /// <param name="checkOnly"> If set to true, reorderElements will only check if any reordering work has to be done. </param>
        /// <returns> True, if elements have been / have to be reordered. </returns>
        private bool reorderElements(int? dropIdx, int dragIdx, bool checkOnly)
        {
            return dropIdx.HasValue && reorderElements(dropIdx.Value, dragIdx, checkOnly);
        }

        /// <summary> Reorders elements, if necessary. </summary>
        /// <param name="dropIdx"> Drop index (= insert position). </param>
        /// <param name="dragIdx"> Index of dragged row (= source index). </param>
        /// <param name="checkOnly"> If set to true, reorderElements will only check if any reordering work has to be done. </param>
        /// <returns> True, if elements have been / have to be reordered. </returns>
        private bool reorderElements(int dropIdx, int dragIdx, bool checkOnly)
        {
            bool reorder = (dropIdx != -1) && (dropIdx < dragIdx || dropIdx > dragIdx + 1) && allowMoveRow(dragIdx, dropIdx);

            if (checkOnly || !reorder) return reorder;

            // fade out all elements if FlashOnReordering is set
            if (FlashOnReordering)
                gridRows.ForEach(gridRow => gridRow.animateOpacity(0, FlashFadeOutDuration, FlashFadeOutEasing));

            // wait until animations have finished
            GridRow.waitAnimatedOpacity();

            // now move dragged element
            gridRows.Insert(dropIdx, gridRows[dragIdx]);
            gridRows.RemoveAt(dragIdx + (dropIdx < dragIdx ? 1 : 0));

            // finally update grid position of each row
            for (int i = 0; i < gridRows.Count; i++)
            {
                gridRows[i].Row = i;

                // fade in all elements if FlashOnReordering is set
                if (FlashOnReordering)
                    gridRows[i].animateOpacity(1, FlashFadeInDuration, FlashFadeInEasing);
            }

            return true;
        }

        /// <summary> Gets the index of the drop region that contains the given point.  </summary>
        /// <param name="p"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private int FindDropRegion(Point? p)
        {
            return p.HasValue ? FindDropRegion(p.Value) : -1;
        }

        /// <summary> Gets the index of the drop region that contains the given point. </summary>
        /// <param name="p"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private int FindDropRegion(Point p)
        {
            int idx = 0;
            foreach (Rect rect in ActualDropRegions)
            {
                if (rect.Contains(p))
                    return idx;

                idx++;
            }

            return -1;
        }

        /// <summary> Gets the target height for the row animation.  </summary>
        /// <param name="dragIdx"> Index of row being dragged. </param>
        /// <returns> Documentation in progress... </returns>
        private double GetTargetHeight(int dragIdx)
        {
            // just use the value of AnimatedRowHeight, if its value is positive
            if (AnimatedRowHeight.HasValue && AnimatedRowHeight.Value >= 0)
                return AnimatedRowHeight.Value;

            // AnimatedRowHeight is null or negative, so use the height of the dragged row
            double height = gridRows[dragIdx].Bounds.Height;

            // use value of AnimatedRowHeight, if any, as an additional margin
            if (AnimatedRowHeight.HasValue)
                height += Math.Abs(AnimatedRowHeight.Value);

            // done
            return height;
        }

        /// <summary>
        /// UpdateDropIndexAndAnimate is called while element is being dragged. It 
        /// updates the target index and animates the rows to provide visual feedback.
        /// </summary>
        /// <param name="dragIdx"> Index of row being dragged. </param>
        /// <param name="p"> Current mouse position. Use null to reset the layout or to indicate a position outside of the grid. </param>
        /// <param name="skip"> Row indices that should not be animated (used for non-droppable regions). </param>
        private void UpdateDropIndexAndAnimate(int dragIdx, Point? p, int[] skip)
        {
            // find region
            int idx = FindDropRegion(p);

            // if region did not change we're already done
            if (dropIdx.HasValue && idx == dropIdx.Value)
                return;

            if (!allowMoveRow(dragIdx, idx))
                return;

            // store region
            dropIdx = idx;

            // update injected rows, animating their height where necessary
            for (int i = 0; AnimateRows && i < grid.RowDefinitions.Count / 2 + 1; i++)
            {
                // skip non droppable rows, those are never animated
                if (!AnimateNonDroppableRows && skip != null && skip.Contains(i))
                    continue;

                // corresponding row definition
                var rowDef = grid.RowDefinitions[i * 2];

                // get the target height and duration of animation
                double targetHeight = i == idx ? GetTargetHeight(dragIdx) : 0;
                double duration = i == idx ? ShowRowAnimationDuration : HideRowAnimationDuration;

                // get easing function
                IEasingFunction easing = (i == idx) ? ShowRowAnimationEasing : HideRowAnimationEasing;

                // animate only if necessary
                if (!rowDef.HasAnimatedProperties && !(Math.Abs(rowDef.ActualHeight - targetHeight) > 1e-4)) continue;

                var anim = new GridLengthAnimation
                {
                    From = new GridLength(rowDef.ActualHeight),
                    To = new GridLength(targetHeight),
                    Duration = new TimeSpan((int)(duration * 10000 + .5)),
                    EasingFunction = easing
                };

                rowDef.BeginAnimation(RowDefinition.HeightProperty, anim, HandoffBehavior.SnapshotAndReplace);
            }
        }
        #endregion

        #region public methods
        /// <summary> Detach from the grid. </summary>
        public void Detach()
        {
            unwireMouseEvents();
        }

        /// <summary> Static GridReordering factory. </summary>
        /// <param name="grid"> Grid to apply pattern for. </param>
        /// <param name="dragCol"> Enable Drag&amp;Drop only for the FrameworkElements of this column. 
        /// If set to null, each FrameworkElement of the grid is applicable for Drag&amp;Drop. </param>
        /// <param name="RowMoved"> Event handler which is added to the row moved event. </param>
        /// <returns> The <see cref="GridReordering"/>. </returns>
        public static GridReordering apply(Grid grid, int? dragCol, RowMovedEventHandler RowMoved)
        {
            var p = new GridReordering(grid, dragCol);
            p.RowMoved += RowMoved;
            GetDefaultTextColor(grid);

            return p;
        }
        #endregion

        #region event handling
        /// <summary> Handler for entering the mouse on a text block. </summary>
        /// <param name="sender"> The sender of the event. </param>
        /// <param name="e"> The event parameters. </param>
        private static void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is TextBlock textBlock)
                textBlock.Foreground = new SolidColorBrush(Color.FromRgb(224, 33, 41)); // PTV rot
        }

        /// <summary> Handler for leaving a text block with the mouse. </summary>
        /// <param name="sender"> The sender of the event. </param>
        /// <param name="e"> The event parameters. </param>
        private static void TextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is TextBlock textBlock)
                textBlock.Foreground = defaultColor;
        }

        /// <summary>
        /// Invokes the given event action in a secure way.
        /// </summary>
        /// <typeparam name="T"> Type of the event args that are passed to the action. </typeparam>
        /// <param name="t"> Event args. </param>
        /// <param name="a"> Action to invoke. </param>
        private void InvokeSecurely<T>(T t, Action<T> a)
        {
            lock (lockObject)
            {
                if (Interlocked.Increment(ref eventRef) == 1)
                    a(t);

                Interlocked.Decrement(ref eventRef);
            }
        }

        /// <summary> Left button up; ends Drag&amp;Drop. </summary>
        /// <param name="sender"> Documentation in progress... </param>
        /// <param name="e"> Documentation in progress... </param>
        internal void PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            InvokeSecurely(e, args =>
            {
                if (!(args.OriginalSource is FrameworkElement originalSource) || (DragColumn.HasValue && PositionOf(originalSource).Column != DragColumn.Value)) return;

                dragDropOrigin = args.GetPosition(originalSource);
                args.Handled = true;
            });
        }

        /// <summary> Left button down; initially store position. </summary>
        /// <param name="sender"> Documentation in progress... </param>
        /// <param name="e"> Documentation in progress... </param>
        private void PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            InvokeSecurely(e, args =>
            {
                dragDropOrigin = null;

                if (!IsDragging) return;

                IsDragging = false;
                args.Handled = true;

                // finished drag&drop
                var f = args.OriginalSource as FrameworkElement;
                DoneDragDrop(f);
                f?.ReleaseMouseCapture();
            });
        }
        
        /// <summary> Mouse move; starts Drag&amp;Drop or updates drag position. </summary>
        /// <param name="sender"> Documentation in progress... </param>
        /// <param name="e"> Documentation in progress... </param>
        private void PreviewMouseMove(object sender, MouseEventArgs e)
        {
            InvokeSecurely(e, args =>
            {
                FrameworkElement originalSource = args.OriginalSource as FrameworkElement;

                args.Handled = IsDragging || dragDropOrigin.HasValue;

                if (IsDragging)
                {
                    // we're dragging, update position
                    DragMove(originalSource);
                }
                else if (dragDropOrigin.HasValue && Point.Subtract(dragDropOrigin.Value, args.GetPosition(originalSource)).Length >= 3)
                {
                    // start drag&drop if mouse moved at least 
                    // 3 pixels while left mouse button is pressed
                    // we need to capture the mouse
                    if (originalSource == null || !originalSource.CaptureMouse()) return;

                    IsDragging = true;
                    createGridRows();
                    StartDragDrop(originalSource);
                }
            });
        }
        #endregion

        #region internal classes
        /// <summary> Grid position. </summary>
        internal class GridPosition
        {
            /// <summary> Documentation in progress... </summary>
            public int Row;
            /// <summary> Documentation in progress... </summary>
            public int Column;
        }
        #endregion

        #region cleanup
        /*
         * -------------------------
         * TODO: CLEAN UP & DOCUMENT
         * -------------------------
         */
        /// <summary> Documentation in progress... </summary>
        public bool AnimateRows = true;
        /// <summary> Documentation in progress... </summary>
        internal IEasingFunction ShowRowAnimationEasing = new CircleEase { EasingMode = EasingMode.EaseOut };
        /// <summary> Documentation in progress... </summary>
        internal IEasingFunction HideRowAnimationEasing = new CircleEase { EasingMode = EasingMode.EaseOut };
        /// <summary> Documentation in progress... </summary>
        public double ShowRowAnimationDuration = 150;
        /// <summary> Documentation in progress... </summary>
        public double HideRowAnimationDuration = 150;
        /// <summary> Documentation in progress... </summary>
        public int? AnimatedRowHeight = -4;
        /// <summary> Documentation in progress... </summary>
        public bool AnimateNonDroppableRows = false;
        /// <summary> Documentation in progress... </summary>
        public double DraggedRowOpacity = 0.25;
        /// <summary> Documentation in progress... </summary>
        public bool FadeDraggedRow = true;
        /// <summary> Documentation in progress... </summary>
        public int DraggedRowFadeOutDuration = 0; // 50;
        /// <summary> Documentation in progress... </summary>
        public int DraggedRowFadeInDuration = 0; // 50;
        /// <summary> Documentation in progress... </summary>
        internal IEasingFunction DraggedRowFadeOutEasing = new CircleEase { EasingMode = EasingMode.EaseOut };
        /// <summary> Documentation in progress... </summary>
        internal IEasingFunction DraggedRowFadeInEasing = new CircleEase { EasingMode = EasingMode.EaseOut };
        /// <summary> Documentation in progress... </summary>
        public bool FlashOnReordering = false;
        /// <summary> Documentation in progress... </summary>
        public int FlashFadeOutDuration = 75;
        /// <summary> Documentation in progress... </summary>
        public int FlashFadeInDuration = 75;
        /// <summary> Documentation in progress... </summary>
        internal IEasingFunction FlashFadeOutEasing = new CircleEase { EasingMode = EasingMode.EaseOut };
        /// <summary> Documentation in progress... </summary>
        internal IEasingFunction FlashFadeInEasing = new CircleEase { EasingMode = EasingMode.EaseOut };
        /// <summary> Documentation in progress... </summary>
        private const bool AlwaysUseRowHeightAsDropThreshold = true;
        /// <summary> Documentation in progress... </summary>
        private const bool UsePreviewAdorner = true;

        #endregion
    }
}
