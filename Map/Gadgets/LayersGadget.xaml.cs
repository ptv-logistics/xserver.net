// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ptv.XServer.Controls.Map.Layers;
using Ptv.XServer.Controls.Map.Localization;
using System.ComponentModel;
using System.Collections.Specialized;


namespace Ptv.XServer.Controls.Map.Gadgets
{
    /// <summary><para> Gadget listing the different layers of the map. </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> topic for an example. </para></summary>
    public partial class LayersGadget
    {
        #region private variables
        /// <summary> Grid column in which the selection check box resides. </summary>
        private const int selectionColumn = 3;
        /// <summary> Layer collection which knows all layers of the map control that should be listed. </summary>
        private LayerCollection layers;
        /// <summary> List of layer indices. The indices specify the row in which the layer is displayed. </summary>
        private readonly List<int> layerIndices = new List<int>();
        /// <summary> Flag showing that the next check operation on the selection checkbox is triggered by an exclusively selected layer. </summary>
        private bool updateByExclusiveSelection;
        ///// <summary> Fade in animation for layers gadget. </summary>
        //private Storyboard fadeInStoryboard = new Storyboard();
        ///// <summary> Fade out animation for layers gadget. </summary>
        //private Storyboard fadeOutStoryboard = new Storyboard();
        /// <summary> Stores the inactive LayersExpander header element. </summary>
        private object inactiveLayersExpanderHeader;
        /// <summary> Stores randomly a slider which is used to calculate the layout. </summary>
        private Slider referenceSlider;
        /// <summary> Stores the reference offset used for layout calculation. </summary>
        private double referenceOffset = double.NaN;
        /// <summary> Flag indicating whether a correction of the layout regarding the header text has been processed. </summary>
        private bool headerSizeSet;
        #endregion

        #region public variables
        /// <summary> Gets or sets a value indicating whether only layers of category "content" are to be shown. </summary>
        /// <value> Flag indicating whether only content layers are to be shown. </value>
        public bool ShowContentLayersOnly { get; set; }
        /// <summary> Gets or sets a value indicating whether the layer list is shown reverted. By default, the value
        /// is false and the layers are shown bottom up in the list. The layer which is painted on the bottom of the
        /// map is also shown at the last position in the map. If this parameter is true, the bottom layer is shown at
        /// the first position in the list. </summary>
        /// <value> Flag indicating whether the layer list should be displayed reverted. </value>
        public bool LayerListReverted { get; set; }
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="LayersGadget"/> class. </summary>
        public LayersGadget()
        {
            InitializeComponent();
            HeaderText.Content = MapLocalizer.GetString(MapStringId.Caption);
        }
        #endregion

        #region protected methods
        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();

            if (!IsInDesignMode)
            {
                layers = Map.Layers;
                layers.CollectionChanged += Layers_CollectionChanged;
                layers.LayerSelectabilityChanged += layers_LayerSelectabilityChanged;
                layers.LayerVisibilityChanged += layers_LayerVisibilityChanged;

                UpdateLayerList();

                Tools.Reordering.GridReordering.apply(LayersStack, 1, null);

                Tools.Reordering.GridReordering.AddRowMovedHandler(LayersStack, (s, e) => layers.Move(layerIndices[e.SourceRow], layerIndices[e.TargetRow]));
                Tools.Reordering.GridReordering.AddAllowMoveRowHandler(LayersStack, (s, e) =>
                {
                    // only allow to move within the same primary category
                    int targetRow = Math.Min(layers.Count - 1, Math.Max(e.TargetRow, 0));

                    var sourceLayer = layers[layerIndices[e.SourceRow]];
                    var targetLayer = layers[layerIndices[targetRow]];

                    e.IsAllowed = (sourceLayer != null) && (targetLayer != null) &&
                                  (sourceLayer.CanvasCategories != null) && (sourceLayer.CanvasCategories.Length != 0) &&
                                  (targetLayer.CanvasCategories != null) && (targetLayer.CanvasCategories.Length != 0) &&
                                  (sourceLayer.CanvasCategories[0] == targetLayer.CanvasCategories[0]);
                });
            }

            // Adds this gadget to the gadget collection of the map as a layers gadget.
            Map.Gadgets.Add(GadgetType.Layers, this);
        }

        void layers_LayerVisibilityChanged(object sender, LayerChangedEventArgs e)
        {
            foreach (var uiElement in LayersStack.Children.Cast<object>().Where(uiElement => uiElement is CheckBox box && Grid.GetColumn(box) == 2 
                                                                                             && box.Tag.ToString() == e.LayerName))
            {
                ((CheckBox)uiElement).IsChecked = layers.IsVisible(layers[e.LayerName]);
            }
        }

        void layers_LayerSelectabilityChanged(object sender, LayerChangedEventArgs e)
        {
            foreach (var uiElement in LayersStack.Children.Cast<object>().Where(uiElement => uiElement is CheckBox box && Grid.GetColumn(box) == 3 
                                                                                             && box.Tag.ToString() == e.LayerName))
            {
                ((CheckBox)uiElement).IsChecked = layers.IsSelectable(layers[e.LayerName]);
            }
        }

        /// <inheritdoc/>
        protected override void UnInitialize()
        {
            if (layers != null)
            {
                foreach (var layer in layers)
                    layer.PropertyChanged -= layer_PropertyChanged;
            }

            if (!IsInDesignMode && (layers != null))
            {
                layers.CollectionChanged -= Layers_CollectionChanged;
                layers.LayerSelectabilityChanged -= layers_LayerSelectabilityChanged;
                layers.LayerVisibilityChanged -= layers_LayerVisibilityChanged;
            }

            Map.Gadgets.Remove(GadgetType.Layers);

            base.UnInitialize();
        }

        /// <inheritdoc/>
        public override void UpdateContent()
        {
            base.UpdateContent();
            UpdateLayerList();
        }

        #endregion

        #region event handling
        /// <summary> Event handler for a change of the layers collection. Updates the layer list. </summary>
        /// <param name="sender"> Sender of the CollectionChanged event. </param>
        /// <param name="e"> The event parameters. </param>
        private void Layers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Unregister events for old layers
            if (e.OldItems != null)
                foreach (var layer in e.OldItems)
                    ((ILayer)layer).PropertyChanged -= layer_PropertyChanged;

            // unregister events for existing layers
            foreach (var layer in layers.Where(layer => e.NewItems == null || !e.NewItems.Contains(layer)))
                layer.PropertyChanged -= layer_PropertyChanged;

            UpdateLayerList();
        }

        /// <summary> Event handler for clicking with the left button on the text block showing the layer name. Shows
        /// the settings dialog on mouse up. </summary>
        /// <param name="sender"> Sender of the MouseLeftButtonUp event. </param>
        /// <param name="e"> The event parameters. </param>
        private void textBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            layers.ShowSettingsDialog(layers[(sender as TextBlock)?.Tag as string]);
            LayersExpander.IsExpanded = false;
        }

        /// <summary> Event handler for checking the selection checkbox. Updates the selection checkboxes. </summary>
        /// <param name="sender"> Sender of the Checked event. </param>
        /// <param name="e"> The event parameters. </param>
        private void selection_Checked(object sender, RoutedEventArgs e)
        {
            selection_Common(sender, true);
        }

        /// <summary> Event handler for un-checking the selection checkbox. Updates the selection checkboxes. </summary>
        /// <param name="sender"> Sender of the Unchecked event. </param>
        /// <param name="e"> The event parameters. </param>
        private void selection_Unchecked(object sender, RoutedEventArgs e)
        {
            selection_Common(sender, false);
        }

        /// <summary> Event handler for a click with the right mouse button on the selection checkbox. This makes the
        /// layer exclusive selectable. </summary>
        /// <param name="sender"> Sender of the right mouse button click event. </param>
        /// <param name="e"> The event parameters. </param>
        private void selection_Exclusive(object sender, RoutedEventArgs e)
        {
            var layer = layers[(sender as CheckBox)?.Tag as string];
            layers.ExclusiveSelectableLayer = (layer == layers.ExclusiveSelectableLayer) ? null : layer;
            UpdateSelection();
        }

        /// <summary> Event handler for un-checking the visibility toggle button of the layer. Makes the layer invisible. </summary>
        /// <param name="sender"> Sender of the Unchecked event. </param>
        /// <param name="e"> The event parameters. </param>
        private void visibility_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!(sender is ToggleButton toggleButton)) return;
            toggleButton.Opacity = 0.7;
            layers.SetVisible(layers[toggleButton.Tag as string], false);
        }

        /// <summary> Event handler for checking the visibility toggle button of the layer. Makes the layer visible. </summary>
        /// <param name="sender"> Sender of the Checked event. </param>
        /// <param name="e"> The event parameters. </param>
        private void visibility_Checked(object sender, RoutedEventArgs e)
        {
            if (!(sender is ToggleButton toggleButton)) return;
            toggleButton.Opacity = 1;
            layers.SetVisible(layers[toggleButton.Tag as string], true);
        }

        /// <summary> Event handler for a change of the dim slider. Sets a new opacity for the layer depending on the
        /// dim value. </summary>
        /// <param name="sender"> Sender of the ValueChanged event. </param>
        /// <param name="e"> Event parameters. </param>
        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            layers[(sender as Slider)?.Tag as string].Opacity = e.NewValue / 100.0;
        }
        #endregion

        #region helper methods

        private static ImageSource defaultImageSource;

        private static ImageSource DefaultImageSource
        {
            get
            {
                if (defaultImageSource != null) return defaultImageSource;

                var bmi = new BitmapImage();
                bmi.BeginInit();
                bmi.StreamSource = Application.GetResourceStream(new Uri("Ptv.XServer.Controls.Map;component/Resources/LayerDefault.png", UriKind.Relative))?.Stream;
                bmi.EndInit();
                bmi.Freeze();
                return defaultImageSource = bmi;
            }
        }

        /// <summary> Updates the list of layers to be shown in the gadget. </summary>
        private void UpdateLayerList()
        {
            LayersStack.Children.Clear();
            LayersStack.RowDefinitions.Clear();
            LayersStack.Margin = new Thickness(1);
            layerIndices.Clear();
            // Since the header size is calculated on the longest layer caption we have to ensure
            // that in case of a layer stack modification the header size is recalculated. We force this
            // by setting 'headerSizeSet' to false.
            headerSizeSet = false;

            int idx = -1;
            foreach (var layer in layers)
            {
                layer.PropertyChanged += layer_PropertyChanged;

                idx++;

                if (LayerListReverted)
                    layerIndices.Add(idx);
                else
                    layerIndices.Insert(0, idx);
            }

            int cnt = 0;
            foreach (var layer in layerIndices.Select(t => layers[t]))
            {
                LayersStack.RowDefinitions.Add(new RowDefinition());

                var label = new Label {Tag = layer.Name, Padding = new Thickness(-1), Margin = LayersStack.Margin};

                // opacityBinding is needed for the image and the layer name -> create once and bind it multiple times
                var opacityBinding = new Binding("Opacity") {Source = label};

                // Create the text block first because the image size depends on the fond size.
                var textBlock = new TextBlock
                {
                    Tag = layer.Name,
                    Text = layer.Caption,
                    Padding = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Normal,
                    FontSize = HeaderText.FontSize
                };

                textBlock.SetBinding(OpacityProperty, opacityBinding);

                if (layer.HasSettingsDialog)
                {
                    textBlock.MouseLeftButtonUp += textBlock_MouseLeftButtonUp;
                    textBlock.TextDecorations.Add(TextDecorations.Underline);
                    textBlock.ToolTip = MapLocalizer.GetString(MapStringId.Options);
                }

                var image = new Image
                {
                    Source = layer.Icon ?? DefaultImageSource,
                    Width = textBlock.FontSize + 8,
                    Height = textBlock.FontSize + 8,
                    Margin = new Thickness(2),
                    VerticalAlignment = VerticalAlignment.Center
                };

                image.SetBinding(OpacityProperty, opacityBinding);
                label.Content = image;

                // Einfügen ins Grid
                Grid.SetColumn(label, 0);
                Grid.SetRow(label, cnt);
                LayersStack.Children.Add(label);

                Grid.SetColumn(textBlock, 1);
                Grid.SetRow(textBlock, cnt);
                LayersStack.Children.Add(textBlock);

                var checkBox = new CheckBox
                {
                    IsChecked = layers.IsVisible(layer),
                    Tag = layer.Name,
                    Margin = new Thickness(3),
                    VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = MapLocalizer.GetString(MapStringId.Visibility)
                };
                checkBox.Checked += visibility_Checked;
                checkBox.Unchecked += visibility_Unchecked;
                Grid.SetColumn(checkBox, 2);
                Grid.SetRow(checkBox, cnt);
                LayersStack.Children.Add(checkBox);

                var slider = new Slider { Tag = layer.Name, Margin = new Thickness(3), Width = 40, Minimum = 0, Maximum = 100, VerticalAlignment = VerticalAlignment.Center };
                // store any slider (used for calculation of header items later on.
                referenceSlider = slider;
                slider.ValueChanged += slider_ValueChanged;
                slider.Value = layer.Opacity * 100;
                Grid.SetColumn(slider, 4);
                Grid.SetRow(slider, cnt);
                LayersStack.Children.Add(slider);

                if (layer is ILayerGeoSearch)
                {   // IsChecked = null for the existence of an exclusive selectable layer
                    // and this layer itself is not exclusive selectable.
                    checkBox = new CheckBox
                    {
                        IsChecked = true,
                        Tag = layer.Name,
                        Margin = new Thickness(3),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    // checkBox.IsThreeState = true; // By clicking it can be iterated through all three states.

                    checkBox.Checked += selection_Checked;
                    checkBox.Unchecked += selection_Unchecked;
                    checkBox.MouseRightButtonUp += selection_Exclusive;
                    checkBox.ToolTip = MapLocalizer.GetString(MapStringId.Selectability);

                    Grid.SetColumn(checkBox, selectionColumn);
                    Grid.SetRow(checkBox, cnt);
                    LayersStack.Children.Add(checkBox);
                }

                cnt++;
            }
            UpdateSelection();
        }

        void layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Opacity" || !(sender is ILayer layer)) return;
            foreach (var uiElement in LayersStack.Children.Cast<object>().Where(uiElement => uiElement is Slider slider && slider.Tag.ToString() == layer.Name))
            {
                ((Slider)uiElement).Value = layer.Opacity * 100;
            }
        }

        /// <summary> Helper method for checking a selection checkbox of one of the layers. Updates the selection checkboxes. </summary>
        /// <param name="sender"> Sender of the Checked event. </param>
        /// <param name="selectable"> Shows if the layer is marked to be selectable or not. </param>
        private void selection_Common(object sender, bool selectable)
        {
            if (updateByExclusiveSelection)
                return; // Nothing to do because the setting is applied indirectly und would cause errors.

            if (layers.ExclusiveSelectableLayer == null)
                // No exclusive layer: Toggle the selectable flag according the CheckBox
                layers.SetSelectable(layers[(sender as CheckBox)?.Tag as string], selectable);
            else
                // If an exclusive layer is activated and the LMB is clicked, 
                // the exclusive mode is reset. All standard selection settings
                // are restored.
                layers.ExclusiveSelectableLayer = null;

            UpdateSelection();
        }

        /// <summary> Updates the selection checkboxes. </summary>
        private void UpdateSelection()
        {
            foreach (UIElement element in LayersStack.Children)
            {
                if (Grid.GetColumn(element) != selectionColumn)
                    continue;

                if (element is CheckBox checkBox)
                {
                    var layer = layers[checkBox.Tag as string];

                    if (layers.ExclusiveSelectableLayer == null)
                        checkBox.IsChecked = layers.IsSelectableBase(layer);
                    else if (layers.ExclusiveSelectableLayer == layer)
                    {
                        updateByExclusiveSelection = true;
                        checkBox.IsChecked = true;
                        updateByExclusiveSelection = false;
                    }
                    else
                        checkBox.IsChecked = null;
                }
            }
        }
        #endregion

        #region event handling

        /// <summary> Event handler for expanding the layer dialog expander. Fades in the layers of the layer dialog. </summary>
        /// <param name="sender"> Sender of the Expanded event. </param>
        /// <param name="e"> Event parameters. </param>
        private void LayersExpander_Expanded(object sender, RoutedEventArgs e)
        {
            referenceSlider.LayoutUpdated += LayersExpanderLayoutUpdated;
            ExchangeLayersExpanderHeader();
            VisibiltyIcon.Visibility = SelectableIcon.Visibility = DimIcon.Visibility = Visibility.Visible;
        }

        /// <summary> Event handler for collapsing the layer dialog expander. Fades out the layers of the layer dialog. </summary>
        /// <param name="sender"> Sender of the Collapsed event. </param>
        /// <param name="e"> Event parameters. </param>
        private void LayersExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            referenceSlider.LayoutUpdated -= LayersExpanderLayoutUpdated;
            ExchangeLayersExpanderHeader();
            VisibiltyIcon.Visibility = SelectableIcon.Visibility = DimIcon.Visibility = Visibility.Hidden;
        }

        /// <summary> Exchanges the expander header depending on if the gadget is expanded or not. </summary>
        private void ExchangeLayersExpanderHeader()
        {
            if (inactiveLayersExpanderHeader == null)
            {
                var txtBlock = new TextBlock {Text = MapLocalizer.GetString(MapStringId.Caption)};
                inactiveLayersExpanderHeader = txtBlock;
            }

            (LayersExpander.Header as UIElement).Visibility = Visibility.Hidden;
            var tmpHeader = inactiveLayersExpanderHeader;
            inactiveLayersExpanderHeader = LayersExpander.Header;
            LayersExpander.Header = tmpHeader;
            (LayersExpander.Header as UIElement).Visibility = Visibility.Visible;
        }

        /// <summary> Sets the appropriate header on app startup. This helps at development time since the gadget is
        /// then shown like when it is active. </summary>
        /// <param name="sender"> Sender of the Loaded event. </param>
        /// <param name="e"> Event parameters. </param>
        private void LayersExpander_Loaded(object sender, RoutedEventArgs e)
        {
            if (LayersExpander.IsExpanded != (LayersExpander.Header is Grid))
                ExchangeLayersExpanderHeader();
        }

        /// <summary> Retrieves the layout of the layers gadget. The algorithm was developed by trial and error. </summary>
        /// <param name="sender"> Sender of the LayoutUpdated event. </param>
        /// <param name="e"> Event parameters. </param>
        private void LayersExpanderLayoutUpdated(object sender, EventArgs e)
        {
            try
            {
                // Hack for calculating the size of the layer caption to correct the layout!
                if (!headerSizeSet)
                {
                    HeaderText.UpdateLayout();

                    var aTextBlock = LayersStack.Children.OfType<TextBlock>().OrderByDescending(t => t.Text.Length).First();

                    aTextBlock.UpdateLayout();
                    aTextBlock.Measure(new Size(ActualWidth, ActualHeight));
                    HeaderText.Measure(new Size(ActualWidth, ActualHeight));

                    var headerSize = HeaderText.DesiredSize;
                    var textBlockSize = aTextBlock.DesiredSize;

                    while (textBlockSize.Width < headerSize.Width)
                    {
                        aTextBlock.Text = aTextBlock.Text.PadRight(aTextBlock.Text.Length + 2);
                        aTextBlock.UpdateLayout();
                        aTextBlock.Measure(new Size(ActualWidth, ActualHeight));
                        textBlockSize = aTextBlock.DesiredSize;
                        HeaderText.Measure(new Size(ActualWidth, ActualHeight));
                        headerSize = HeaderText.DesiredSize;
                    }
                    headerSizeSet = true;
                }

                // We can only calculate the icon positions if the containing grid is visible!
                if (HeaderGrid.Visibility != Visibility.Visible) return;

                // Hack for calculationg the positions of the icons in the expander header determine the slider position
                referenceSlider.TransformToAncestor(ExpanderGrid).Transform(new Point(0, 0));
                // determine the header position
                Point headerCoordinates = HeaderGrid.TransformToAncestor(Layers).Transform(new Point(0, 0));
                var headerStart = headerCoordinates.X;
                var headerEnd = headerStart + HeaderGrid.ActualWidth;

                // only calculate the reference offset if the gadget can be displayed reasonably
                if ((ExpanderGrid.ActualWidth - headerEnd) > 0)
                    referenceOffset = double.IsNaN(referenceOffset) ? headerCoordinates.X + ExpanderGrid.ActualWidth - LayersStack.ActualWidth - LayersStack.Margin.Right - LayersStack.Margin.Left - 2 : referenceOffset;
                else if (double.IsNaN(referenceOffset))
                    return;

                // check if we have calculated a reasonable diff, if not reset it!
                if (referenceOffset < 0)
                {
                    referenceOffset = double.NaN;
                    return;
                }

                // adapt the size until we have an acceptable difference between the header grid and the layer grid
                while (true)
                {
                    var prevWidth = HeaderGrid.ActualWidth;
                    HeaderGrid.Width = LayersExpander.ActualWidth - referenceOffset;
                    ExpanderGrid.UpdateLayout();
                    if (Math.Abs(HeaderGrid.ActualWidth - prevWidth) < 0.2d) break;
                }
            }
            catch { }
        }

        /// <summary> Event handler for an update of the layers expander. Adapts font size, margin and height to the
        /// currently contained text. </summary>
        /// <param name="sender"> Sender of the LayoutUpdated event. </param>
        /// <param name="e"> Event parameters. </param>
        private void LayersExpander_LayoutUpdated(object sender, EventArgs e)
        {
            if (LayersExpander.Header is TextBlock block && block.FontSize != HeaderText.FontSize)
            {
                block.FontSize = HeaderText.FontSize;
                block.Margin = new Thickness(0, 0, 4, 0);
            }

            if (LayersExpander.Header is TextBlock textBlock && textBlock.ActualHeight != HeaderText.ActualHeight)
            {
                textBlock.Height = HeaderText.ActualHeight;
            }
        }
        #endregion
    }
}
