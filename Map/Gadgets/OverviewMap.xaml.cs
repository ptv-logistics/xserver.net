// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Windows;


namespace Ptv.XServer.Controls.Map.Gadgets
{
    /// <summary><para> Gadget for showing the overview map. An overview map shows the whole map in small. A rectangle on
    /// the overview map shows the borders of the currently displayed map section. </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> topic for an example. </para></summary>
    public partial class OverviewMap
    {
        #region constructor
        /// <summary> Initializes a new instance of the <see cref="OverviewMap"/> class. </summary>
        public OverviewMap()
        {
            InitializeComponent();
        }
        #endregion

        #region protected methods
        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();

            overviewMap.ParentMapView = MapView;
            Map.Gadgets.Add(GadgetType.Overview, this);
        }

        /// <inheritdoc/>
        protected override void UnInitialize()
        {
            Map.Gadgets.Remove(GadgetType.Overview);

            base.UnInitialize();
        }
        #endregion

        #region event handling
        /// <summary> Event handler for clicking the overview map button. Expands or collapses the overview map
        /// depending on its current state. </summary>
        /// <param name="sender"> Sender of the Click event. </param>
        /// <param name="e"> The event parameters. </param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OverViewMapGrid.Visibility = (OverViewMapGrid.Visibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
            overviewMap.IsEnabled = overviewMap.Visibility == Visibility.Visible;

            if (OverViewMapGrid.Visibility != Visibility.Visible) return;

            overviewMap.UpdateOverviewMap(false);
            overviewMap.UpdateRect();
        }
        #endregion
    }
}
