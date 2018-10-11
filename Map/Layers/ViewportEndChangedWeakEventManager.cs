// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Windows;


namespace Ptv.XServer.Controls.Map.Layers
{
    /// <summary>
    /// The weak event manager for the <see cref="MapView.ViewportEndChanged"/> event.
    /// An event manager administrates different event listeners for a certain event source. The listeners
    /// can be connected to the viewport end changed event and can be disconnected again if they are no more used. </summary>
    public class ViewportEndChangedWeakEventManager : WeakEventManager
    {
        #region private variables
        /// <summary> Gets the current viewport end changed weak event manager. Manages the different event listeners
        /// of one event source. </summary>
        private static ViewportEndChangedWeakEventManager CurrentManager
        {
            get
            {
                var managerType = typeof(ViewportEndChangedWeakEventManager);
                var manager = (ViewportEndChangedWeakEventManager)GetCurrentManager(managerType);
                if (manager != null) return manager;

                manager = new ViewportEndChangedWeakEventManager();
                SetCurrentManager(managerType, manager);
                return manager;
            }
        }
        #endregion

        #region protected methods
        /// <inheritdoc/>
        protected override void StartListening(Object source)
        {
            var mapView = (MapView)source;
            mapView.ViewportEndChanged += DeliverEvent;
        }
        /// <inheritdoc/>
        protected override void StopListening(Object source)
        {
            var mapView = (MapView)source;
            mapView.ViewportEndChanged -= DeliverEvent;
        }
        #endregion
        
        #region public methods
        /// <summary> Connects the listener to the event queue. </summary>
        /// <param name="mapView"> Map from which the viewport end changed events are delivered. </param>
        /// <param name="listener"> Event listener to be connected. </param>
        public static void AddListener(MapView mapView, IWeakEventListener listener)
        {
            CurrentManager.ProtectedAddListener(mapView, listener);
        }

        /// <summary> Disconnects the listener from the event queue. </summary>
        /// <param name="mapView"> Map from which the viewport end changed events are delivered. </param>
        /// <param name="listener"> Event listener to be disconnected. </param>
        public static void RemoveListener(MapView mapView, IWeakEventListener listener)
        {
            CurrentManager.ProtectedRemoveListener(mapView, listener);
        }
        #endregion
    }
}