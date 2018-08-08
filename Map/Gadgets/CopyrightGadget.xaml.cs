using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ptv.XServer.Controls.Map.Tools.Reprojection;

namespace Ptv.XServer.Controls.Map.Gadgets
{
    /// <summary><para> Gadget showing the copyright text of the map. </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> topic for an example. </para></summary>
    public partial class CopyrightGadget
    {
        #region private variables
        /// <summary> Collection of all layers. </summary>
        private LayerCollection layers;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="CopyrightGadget"/> class. </summary>
        public CopyrightGadget()
        {
            InitializeComponent();
            Loaded += CopyrightGadget_Loaded;
            IsVisibleChanged += CopyrightGadget_IsVisibleChanged;
        }
        #endregion

        #region protected methods
        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();
            Map.Gadgets.Add(GadgetType.Copyright, this);
        }

        /// <inheritdoc/>
        protected override void UnInitialize()
        {
            Map.Gadgets.Remove(GadgetType.Copyright);
            base.UnInitialize();
        }

        /// <inheritdoc/>
        public override void UpdateContent()
        {
            base.UpdateContent();
            UpdateCopyrightText();
        }
        #endregion

        #region event handling
        /// <summary> Event handler for a change of the visibility property. This method prevents the gadget from
        /// being hidden. It always sets the visibility property to visible. </summary>
        /// <param name="sender"> Sender of the IsVisibleChanged event. </param>
        /// <param name="e"> The event parameters. </param>
        private void CopyrightGadget_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Visibility = Visibility.Visible;
        }

        /// <summary> Event handler for having loaded the gadget successfully. Initializes the layer collection and updates the copyright text. </summary>
        /// <param name="sender"> Sender of the Loaded event. </param>
        /// <param name="e"> The event parameters. </param>
        private void CopyrightGadget_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= CopyrightGadget_Loaded;

            layers = Map.Layers;
            // For all already inserted layers the missing calls to Layers_LayerAdded have to be caught up on everything.
            layers.ForEach(null, layer => Layers_LayerAdded(null, new LayerChangedEventArgs(layer)));

            layers.LayerAdded += Layers_LayerAdded;
            layers.LayerRemoved += Layers_LayerRemoved;
            layers.LayerVisibilityChanged += Layers_LayerVisibilityChanged;
            UpdateCopyrightText();
        }

        private void Layers_LayerAdded(object sender, LayerChangedEventArgs e)
        {
            UpdateCopyrightText();
            e.Layer.PropertyChanged += Layer_PropertyChanged;
        }

        private void Layers_LayerRemoved(object sender, LayerChangedEventArgs e)
        {
            UpdateCopyrightText();
            e.Layer.PropertyChanged -= Layer_PropertyChanged;
        }

        private void Layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Copyright")
                UpdateCopyrightText();
        }

        private void Layers_LayerVisibilityChanged(object sender, LayerChangedEventArgs e)
        {
            UpdateCopyrightText();
        }
        #endregion

        #region Treats the visible property.
        /// <inheritdoc/>
        public override bool Visible
        {
            get { return false; }
            set { base.Visible = false; }
        }
        #endregion

        #region private methods
        /// <summary> Updates the copyright text which is shown in this gadget. </summary>
        private void UpdateCopyrightText()
        {
            TextStack.Children.Clear();

            var copyrightTexts = new HashSet<string>(layers
                .Where(layer => layers.IsVisible(layer))
                .Where(layer => !string.IsNullOrEmpty(layer.Copyright))
                .SelectMany(layer => layer.Copyright.Split('|')));

            // Removal to be discussed
            // copyrightTexts.Sort();

            TextStack.Children.Add(new TextBlock
            {
                Padding = new Thickness(4, 1, 4, 1),
                Text = string.Join(", ", copyrightTexts.ToArray()),
                FontSize = 10,
                TextAlignment = copyrightTexts.Count > 1 ? TextAlignment.Left : TextAlignment.Center
            });


            //copyrightTexts.ForEach(null, text => TextStack.Children.Add(new TextBlock
            //{
            //    Padding = new Thickness(4, 1, 4, 1),
            //    Text = text,
            //    FontSize = 10,
            //    TextAlignment = copyrightTexts.Count > 1 ? TextAlignment.Left : TextAlignment.Center
            //}));
        }
        #endregion
    }
}
