// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Windows;


namespace Ptv.XServer.Controls.Map.Layers
{
    /// <summary>
    /// The weak event manager for the <see cref="MapView.ViewportWhileChanged"/> event.
    /// An event manager administrates different event listeners for a certain event source. The listeners
    /// can be connected to the viewport end changed event and can be disconnected again if they are no more used. </summary>
    public class ViewportWhileChangedWeakEventManager : WeakEventManager
    {
        /// <summary> Gets the current viewport while changed weak event manager. Manages the different event listeners
        /// of one event source. </summary>
        private static ViewportWhileChangedWeakEventManager CurrentManager
        {
            get
            {
                var managerType = typeof(ViewportWhileChangedWeakEventManager);
                var manager = (ViewportWhileChangedWeakEventManager)GetCurrentManager(managerType);
                if (manager != null) return manager;

                manager = new ViewportWhileChangedWeakEventManager();
                SetCurrentManager(managerType, manager);
                return manager;
            }
        }

        /// <summary>
        /// Starts listening for the event being managed. After this method is first called,
        /// the manager should be in the state of calling DeliverEvent(Object, EventArgs)
        /// or DeliverEventToList(Object, EventArgs, WeakEventManager+ListenerList) whenever the relevant event from the provided source is handled.
        /// </summary>
        /// <param name="source">The source to begin listening on.</param>
        protected override void StartListening(object source)
        {
            var mapView = (MapView)source;
            mapView.ViewportWhileChanged += DeliverEvent;
        }

        /// <summary>Stops listening for the event on the specified object. </summary>
        /// <param name="source">The object to that raises the event.</param>
        protected override void StopListening(object source)
        {
            var mapView = (MapView)source;
            mapView.ViewportWhileChanged -= DeliverEvent;
        }
        
        /// <summary> Connects the listener to the event queue. </summary>
        /// <param name="mapView"> Map from which the viewport while changed events are delivered. </param>
        /// <param name="listener"> Event listener to be connected. </param>
        public static void AddListener(MapView mapView, IWeakEventListener listener)
        {
            CurrentManager.ProtectedAddListener(mapView, listener);
        }

        /// <summary> Removes the listener from the event queue. </summary>
        /// <param name="mapView"> Map from which the viewport while changed events are delivered. </param>
        /// <param name="listener"> Event listener to be disconnected. </param>
        public static void RemoveListener(MapView mapView, IWeakEventListener listener)
        {
            CurrentManager.ProtectedRemoveListener(mapView, listener);
        }
    }
}