// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using Ptv.XServer.Controls.Map.Layers;
using System.ComponentModel;

namespace Ptv.XServer.Controls.Map
{
    /// <summary> Interface provided by the WPFClient to open an application specific dialog box to manipulate the
    /// properties/settings of a layer object. </summary>
    public interface ILayerSettings
    {
        /// <summary> The corresponding object shows a dialog box with all the settings of the specified layer. </summary>
        /// <param name="layer"> Layer providing the settings, which should be shown in dialog. </param>
        void ShowSettingsDialog(ILayer layer);
    }

    /// <summary> Container class managing the set of all available <see cref="ILayer"/> objects. Especially the
    /// visibility and selection property is managed by this class. </summary>
    public class LayerCollection : ObservableCollection<ILayer>, ICloneable, IDisposable
    {
        #region private variables
        /// <summary> Dictionary of visibility settings for each layer. </summary>
        private readonly Dictionary<ILayer, bool> visibilities = new Dictionary<ILayer, bool>();
        /// <summary> Dictionary of selectability settings for each layer. </summary>
        private readonly Dictionary<ILayer, bool> selectabilities = new Dictionary<ILayer, bool>();
        /// <summary> List of maps in which the layers can be shown. </summary>
        private readonly List<MapView> mapViews = new List<MapView>();
        /// <summary> The single layer which is exclusive selectable (if existent). </summary>
        private ILayer exclusiveSelectableLayer;
        /// <summary> Indicates the collection is changed from within the CollectionChanged. </summary>
        private bool selfNotify;
        #endregion

        #region public variables
        /// <summary> Gets or sets the interface, which is used when the settings of a layer have to be shown.
        /// Due to the fact, that this class does not know the styles of the GUI, the dialogs are deferred to the
        /// client application implementing such an <see cref="ILayerSettings"/>-interface. </summary>
        public ILayerSettings LayerSettings { get; set; }
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="LayerCollection"/> class. </summary>
        public LayerCollection()
        {
            CollectionChanged += LayerCollection_CollectionChanged;
            LayerAdded += Layer_Added;
            LayerRemoved += Layer_Removed;
        }
        #endregion

        #region public methods
        /// <summary> Index operator for the set of layers, accepting the layer name for comparison. </summary>
        /// <param name="layerName"> Name of the layer to search for. The search is case-sensitive. </param>
        /// <returns> The layer with the specified name, if available, otherwise null. </returns>
        public ILayer this[string layerName]
        {
            get { return this.FirstOrDefault(layer => layer.Name == layerName); }
        }

        /// <summary> Insert a layer before another layer. </summary>
        /// <param name="layer"> The layer. </param>
        /// <param name="name"> The name of the other layer. </param>
        public void InsertBefore(ILayer layer, string name)
        {
            if (this[name] != null)
                Insert(IndexOf(this[name]), layer);
            else
                Add(layer);
        }

        /// <summary> Activates the settings dialog, if an <see cref="ILayerSettings"/>-interface is set by the client
        /// application. </summary>
        /// <param name="layer"> Layer object, for which the settings have to be shown. </param>
        public void ShowSettingsDialog(ILayer layer)
        {
            LayerSettings?.ShowSettingsDialog(layer);
        }

        /// <summary> Connect a <see cref="MapView"/>-object to the called LayerCollection. In return, only visible
        /// layers are connected to the Map object. </summary>
        /// <param name="mapView"> Map to connect with the visible layers. </param>
        public void Register(MapView mapView)
        {
            mapView.IsVisibleChanged += mapView_IsVisibleChanged;
            mapViews.Add(mapView);
            if (!mapView.IsVisible) return;

            foreach (var layer in this.Where(IsVisible))
                layer.AddToMapView(mapView);
        }

        /// <summary> Disconnect a <see cref="MapView"/>-object from the called LayerCollection. In return, all visible
        /// layers are disconnected from the Map object. </summary>
        /// <param name="mapView"> Map to disconnect from the visible layers. </param>
        public void Unregister(MapView mapView)
        {
            mapView.IsVisibleChanged -= mapView_IsVisibleChanged;
            mapViews.Remove(mapView);
            if (!mapView.IsVisible) return;

            foreach (var layer in this.Where(IsVisible))
                layer.RemoveFromMapView(mapView);
        }

        /// <summary> Retrieves for a layer whether it is visible. </summary>
        /// <param name="layer"> Layer to look for its visibility. </param>
        /// <returns> True, if the layer exists and it is set to visible, otherwise false. </returns>
        public bool IsVisible(ILayer layer)
        {
            return visibilities.ContainsKey(layer) && visibilities[layer];
        }

        /// <summary> Set the visibility of the specified layer. </summary>
        /// <remarks> The visibility is set for all maps added to the LayerCollection via <see cref="Register"/>.
        /// A layer can not be hidden in one map and shown in another one. </remarks>
        /// <param name="layer"> Layer of which the visibility should be modified. </param>
        /// <param name="visible"> Flag indicating the (in-)visibility of the layer. </param>
        public void SetVisible(ILayer layer, bool visible)
        {
            if (IsVisible(layer) == visible)
                return;

            visibilities[layer] = visible;
            foreach (var mapView in mapViews.Where(mapView => mapView.IsVisible))
                if (visible)
                    layer.AddToMapView(mapView);
                else
                    layer.RemoveFromMapView(mapView);

            LayerVisibilityChanged?.Invoke(this, new LayerChangedEventArgs(layer));
        }

        /// <summary> Retrieves if the layer is selectable without taking into account whether it is exclusively
        /// selectable. </summary>
        /// <param name="layer"> Layer, which is requested for its selection behavior. </param>
        /// <returns> True, if the selectable flag is set to true without taking the exclusive selectable flag into
        /// account.</returns>
        public bool IsSelectableBase(ILayer layer)
        {
            return selectabilities.ContainsKey(layer) && selectabilities[layer];
        }

        /// <summary> Retrieves if the layer is selectable taking into account whether it is exclusively selectable. </summary>
        /// <param name="layer"> Layer, which is requested for its selection behavior. </param>
        /// <returns> True, if the selectable flag is set to true, taking the exclusive selectable flag into account.
        /// It is false if another layer is marked as exclusive selectable. </returns>
        public bool IsSelectable(ILayer layer)
        {
            return exclusiveSelectableLayer == null ? IsSelectableBase(layer) : layer == exclusiveSelectableLayer;
        }

        /// <summary> Sets the selectable flag of the layer, if the layer is not exclusive selectable. </summary>
        /// <param name="layer"> Layer, which should be modified in its selection behavior. </param>
        /// <param name="selectable"> If the selectable flag is set to true, the layer selects objects. </param>
        public void SetSelectable(ILayer layer, bool selectable)
        {
            if (IsSelectable(layer) == selectable)
                return;

            selectabilities[layer] = selectable;

            LayerSelectabilityChanged?.Invoke(this, new LayerChangedEventArgs(layer));
        }

        /// <summary> Gets or sets the layer which is the one-and-only selectable layer. </summary>
        public ILayer ExclusiveSelectableLayer
        {
            get => exclusiveSelectableLayer;
            set
            {
                if (value != null && !selectabilities.ContainsKey(value))
                    value = null;

                exclusiveSelectableLayer = value;
            }
        }
        #endregion

        #region events

        /// <summary>Callback for indicating the insertion of a new layer to this collection.
        /// This is useful when layer specific callbacks have to be set, for example to indicate a layer's property change.
        /// </summary>
        public event EventHandler<LayerChangedEventArgs> LayerAdded;

        /// <summary>Callback for indicating the removal of a layer from this collection.
        /// It becomes important when layer specific callbacks were set in <see cref="LayerAdded"/>, 
        /// and have to be removed now.
        /// </summary>
        public event EventHandler<LayerChangedEventArgs> LayerRemoved;

        /// <summary>Callback for indicating the visibility changing. </summary>
        public event EventHandler<LayerChangedEventArgs> LayerVisibilityChanged;

        /// <summary>Callback for indicating the selectability changing. </summary>
        public event EventHandler<LayerChangedEventArgs> LayerSelectabilityChanged;

        #endregion

        #region event handling
        /// <summary> Event handler for a change of the layer collection. Adds and removes the layers to/from the
        /// internal collections for selectability and visibility due to the change action. </summary>
        /// <param name="sender"> Sender of the CollectionChanged event. </param>
        /// <param name="e"> Event parameters. </param>
        private void LayerCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    foreach (var mapView in mapViews.Where(mapView => mapView.IsVisible))
                        foreach (var layer in visibilities.Keys.Where(IsVisible))
                            layer.RemoveFromMapView(mapView);

                    foreach (var layer in visibilities.Keys.ToList())
                        LayerRemoved?.Invoke(sender, new LayerChangedEventArgs(layer));

                    break;

                case NotifyCollectionChangedAction.Add:
                    foreach (ILayer layer in e.NewItems)
                    {
                        LayerAdded?.Invoke(sender, new LayerChangedEventArgs(layer));

                        foreach (var mapView in mapViews.Where(mapView => mapView.IsVisible && IsVisible(layer) && LayerNameIsUnique(layer, e.NewStartingIndex, e.NewItems.Count)))
                            layer.AddToMapView(mapView);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (ILayer layer in e.OldItems)
                    {
                        foreach (var mapView in mapViews.Where(mapView => mapView.IsVisible && IsVisible(layer)))
                            layer.RemoveFromMapView(mapView);

                        LayerRemoved?.Invoke(sender, new LayerChangedEventArgs(layer));
                    }
                    break;
            }

            // update z-index
            selfNotify = true;

            for (int i = 0; i < Count; i++)
                this[i].Priority = i;

            selfNotify = false;
        }

        private void Layer_Added(object sender, LayerChangedEventArgs e)
        {
            selectabilities[e.Layer] = true;
            visibilities[e.Layer] = true;
            e.Layer.PropertyChanged += layer_PropertyChanged;
        }

        private void Layer_Removed(object sender, LayerChangedEventArgs e)
        {
            selectabilities.Remove(e.Layer);
            visibilities.Remove(e.Layer);
            e.Layer.PropertyChanged -= layer_PropertyChanged;
        }

        private void layer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Priority" || !(sender is ILayer layer) || selfNotify) return;

            if (IndexOf(layer) != layer.Priority)
                Move(IndexOf(layer), layer.Priority);
        }

        /// <summary> Event handler for a change of the map visibility property. Adds or removes the layers of the map. </summary>
        /// <param name="sender"> Sender of the IsVisibleChanged event. </param>
        /// <param name="e"> Event parameters. </param>
        private void mapView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var mapView = sender as MapView;
            foreach (var layer in this.Where(IsVisible))
                if ((bool)e.NewValue)
                    layer.AddToMapView(mapView);
                else
                    layer.RemoveFromMapView(mapView);
        }
        #endregion

        #region private methods
        /// <summary> Checks if the layer name of the newly added layer already exists in this map. </summary>
        /// <param name="layer"> The layer for which we want to check the name. </param>
        /// <param name="newStartingIndex"> Starting index of the newly inserted elements. </param>
        /// <param name="newItemCount"> Number of newly inserted elements. </param>
        /// <returns> Flag which shows if the layer name already exists. </returns>
        private bool LayerNameIsUnique(ILayer layer, int newStartingIndex, int newItemCount)
        {
            int occurrenceCounter = 0;
            // Iterates all layers.
            for (int index = 0; index < Count; index++)
            {
                // Counts how many times the name occurs in the newly inserted layers.
                if (index >= newStartingIndex && index < newStartingIndex + newItemCount)
                {
                    if (this[index].Name == layer.Name)
                        occurrenceCounter++;

                    // Only one occurrence in the new layers is allowed.
                    if (occurrenceCounter > 1)
                        return false;
                }
                else
                {
                    // Checks if the layer name of the new layer already exists in the old layer names.
                    if (this[index].Name == layer.Name)
                        return false;
                }
            }
            return true;
        }
        #endregion

        #region ICloneable Members

        /// <summary> Creates a new object that is a copy of the current instance. </summary>
        /// <returns> A new object that is a copy of this instance. </returns>
        public object Clone()
        {
            var clone = new LayerCollection();

            foreach (var layer in this)
                clone.Add(layer);

            return clone;
        }
        #endregion

        #region IDisposable Members
        /// <summary> Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. </summary>
        public void Dispose()
        {
            foreach (var layer in this)
            {
                layer.PropertyChanged -= layer_PropertyChanged;
                (layer as IDisposable)?.Dispose();
            }
        }
        #endregion
    }

    /// <summary>Additional argument class for events concerning some changes in the context of an individual layer. </summary>
    public class LayerChangedEventArgs : EventArgs
    {
        /// <summary>Constructor needed for defining which layer is addressed concerning its changes. </summary>
        /// <param name="layer">Layer which changes its properties or is added to/removed from the layer collection.</param>
        public LayerChangedEventArgs(ILayer layer)
        {
            Layer = layer;
            LayerName = layer.Name;
        }

        /// <summary>Legacy constructor containing only the name of the layer. </summary>
        /// <remarks>The property <see cref="Layer"/> remains uninitialized.</remarks>
        /// <param name="layerName">Name of the layer which was changed one of its properties or is added to /removed from the
        /// collection list.</param>
        public LayerChangedEventArgs(string layerName) { LayerName = layerName; }

        /// <summary>Name of the layer.</summary>
        public virtual string LayerName { get; }

        /// <summary>Layer which was changed one of its properties or is added to /removed from the collection list.</summary>
        public virtual ILayer Layer { get; }
    }
}
