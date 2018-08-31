// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Windows;
using System.Windows.Controls;
using Ptv.XServer.Controls.Map.Tools;
using System.Windows.Input;
using System;


namespace Ptv.XServer.Controls.Map.Gadgets
{
    /// <summary> To interact with the map visualization, there is a collection of
    /// <see cref="Ptv.XServer.Controls.Map.Gadgets"/>. </summary>
    [System.Runtime.CompilerServices.CompilerGenerated]
    class NamespaceDoc
    {
        // NamespaceDoc is used for providing the root documentation for Ptv.Components.Projections
        // hint taken from http://stackoverflow.com/questions/156582/namespace-documentation-on-a-net-project-sandcastle
    }

    /// <summary><para> Defines the mode of the focus behavior. By default the MapGadget hands over the focus if an event is
    /// received (same as HandoverToMap mode). In the 'Retain' mode the focus is not handed over. </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> topic for an example. </para></summary>
    public enum FocusBehaviorMode
    {
        /// <summary> Default. Hands over the focus to the map if an event is received. </summary>
        HandoverToMap = 0,
        /// <summary> Do not hand over the focus just leave it where it is. </summary>
        Retain
    }

    /// <summary> Different types of gadgets which can be added to the map. </summary>
    public enum GadgetType
    {
        /// <summary> Scale gadget showing the current map scale in km or miles. </summary>
        Scale,
        /// <summary> Zoom slider gadget for zooming in the map. </summary>
        ZoomSlider,
        /// <summary> Coordinates gadget showing the current mouse positions coordinates. </summary>
        Coordinates,
        /// <summary> Overview map gadget showing a map overview additional to the map. </summary>
        Overview,
        /// <summary> Layers gadget showing all layers which are contained in the map and its properties. </summary>
        Layers,
        /// <summary> Magnifier gadget showing more details of the map. </summary>
        Magnifier,
        /// <summary> Navigation gadget for scrolling in the map. </summary>
        Navigation,
        /// <summary> Copyright gadget showing the map copyrights in the map. </summary>
        Copyright
    }

    /// <summary><para> Interface containing all properties of the gadget which are client configurable. </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> topic for an example. </para></summary>
    public interface IGadget
    {
        /// <summary> Gets or sets a value indicating whether the gadget is to be displayed or hidden. </summary>
        /// <value> Flag showing whether the gadget is visible. </value>
        bool Visible { get; set; }
    }

    /// <summary><para> Parent class of all map gadgets. A map gadget is an add-on to the map like a coordinates gadget, a scale gadget etc.. </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> topic for an example. </para></summary>
    public class MapGadget : UserControl, IGadget
    {
        #region constructor
        /// <summary> Initializes a new instance of the <see cref="MapGadget"/> class. </summary>
        public MapGadget()
        {
            Loaded += MapGadget_Loaded;
            Unloaded += MapGadget_Unloaded;
        }
        #endregion

        #region public methods
        /// <summary> Gets or sets the map content object which can be used to edit the layers and all map properties. </summary>
        /// <value> Map content object. </value>
        public MapView MapView { get; set; }

        /// <summary> Gets or sets the map core object holding the gadgets collection for example. </summary>
        /// <value> Map core object. </value>
        public Map Map { get; set; }

        /// <summary> Gets a value indicating whether the gadget is used in design mode. </summary>
        public static bool IsInDesignMode => DesignTest.DesignMode;

        #endregion

        #region dependency properties
        /// <summary> Gets or sets a property which controls if Mouse.PreviewMouseUpEvents forward the focus to the map
        /// or not. If set to <see cref="FocusBehaviorMode.HandoverToMap"/>, every mouse click on the gadget or one of
        /// its children sets the focus to the map. The focus can still be retained if it is set e.g. by using the
        /// 'tab' key if 'KeyboardFocusBehavior' is set to 'Retain'. </summary>
        /// <value> FocusBehaviorMode which defines whether PreviewMouseUpEvents forward the focus to the map. </value>
        public FocusBehaviorMode MouseFocusBehavior { get; set; }

        /// <summary> Gets or sets a value which controls if Keyboard.PreviewKeyDownEvents forward the focus to the map
        /// or not. If set to <see cref="FocusBehaviorMode.HandoverToMap"/> every key press on the gadget or one of its
        /// children sets the focus to the map. As a result the gadget and all of its children is not able to gain the
        /// focus any more. </summary>
        /// <value> FocusBehaviorMode which defines whether PreviewKeyDownEvents forward the focus to the map. </value>
        public FocusBehaviorMode KeyboardFocusBehavior { get; set; }
        #endregion

        #region IGadget
        /// <summary> Gets or sets a value indicating whether the gadget is to be displayed or hidden. By default, the
        /// gadget is visible. </summary>
        /// <value> Flag showing whether the gadget is visible.</value>
        public virtual bool Visible
        {
            get => (Visibility == Visibility.Visible);
            set => Visibility = (value ? Visibility.Visible : Visibility.Collapsed);
        }
        #endregion

        #region event handling
        /// <summary> Event handler for a successful loading of the map gadget. Retrieves the corresponding map core
        /// and map content object. </summary>
        /// <param name="sender"> Sender of the Loaded event. </param>
        /// <param name="e"> The event parameters. </param>
        private void MapGadget_Loaded(object sender, RoutedEventArgs e)
        {
            if (wasInitialized)
                return;

            // get parent 
            var frameworkElement = Parent as FrameworkElement;
            if (frameworkElement == null)
                return;

            MapView = frameworkElement.FindRelative<MapView>();
            Map = frameworkElement.FindRelative<Map>();

            Initialize();
        }

        /// <summary> Event handler for having unloaded the map gadget. </summary>
        /// <param name="sender"> Sender of the Unloaded event. </param>
        /// <param name="e"> The event parameters. </param>
        private void MapGadget_Unloaded(object sender, RoutedEventArgs e)
        {
            if (wasInitialized)
                 UnInitialize();
        }

        /// <summary> Event handler for a mouse up event. Forwards the focus in case of a PreviewMouseUpEvent. This
        /// method is used to implement that the children of the MapGadget do not grab the focus if they were clicked. </summary>
        /// <param name="sender"> Sender of the PreviewMouseUp event. </param>
        /// <param name="e"> Event parameters. </param>
        private void source_PreviewMouseUp(object sender, RoutedEventArgs e)
        {
            switch (MouseFocusBehavior)
            {
                case FocusBehaviorMode.HandoverToMap: Dispatcher.BeginInvoke(new Action(() => MapView.Focus())); break;
            }
        }

        /// <summary> Event handler for a key down event. Forwards the focus in case of a PreviewMouseUpEvent. This
        /// method is used to implement that the children of the MapGadget do not grab the focus any more. </summary>
        /// <param name="sender"> Sender of the PreviewKeyDown event. </param>
        /// <param name="e"> Event parameters. </param>
        private void source_PreviewKeyDown(object sender, RoutedEventArgs e)
        {
            switch (KeyboardFocusBehavior)
            {
                case FocusBehaviorMode.HandoverToMap: Dispatcher.BeginInvoke(new Action(() => MapView.Focus())); break;
            }
        }
        #endregion

        #region initialization

        private bool wasInitialized;
        /// <summary> Initializes the map gadget. The mouse events are connected and the gadget is added to the map
        /// gadgets list of the parent map. </summary>
        protected virtual void Initialize()
        {
            AddHandler(Mouse.PreviewMouseUpEvent, new RoutedEventHandler(source_PreviewMouseUp));
            AddHandler(Keyboard.PreviewKeyDownEvent, new RoutedEventHandler(source_PreviewKeyDown));

            wasInitialized = true;
        }

        /// <summary> Uninitializes the map gadget. The mouse events are disconnected and the gadget is removed from
        /// the parent map gadget list. </summary>
        protected virtual void UnInitialize() 
        {
            RemoveHandler(Mouse.PreviewMouseUpEvent, new RoutedEventHandler(source_PreviewMouseUp));
            RemoveHandler(Keyboard.PreviewKeyDownEvent, new RoutedEventHandler(source_PreviewKeyDown));

            wasInitialized = false;
        }

        /// <summary> Updates the inner content of the map gadget according to the currently set colors. </summary>
        public virtual void UpdateContent()
        {
        }
        #endregion
    }
}
