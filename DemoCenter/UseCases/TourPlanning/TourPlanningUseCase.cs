//--------------------------------------------------------------
// Copyright (c) PTV Group
// 
// For license details, please refer to the file COPYING, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Ptv.XServer.Controls.Map.Layers.Shapes;
using Ptv.XServer.Controls.Map.Symbols;


namespace Ptv.XServer.Demo.UseCases.TourPlanning
{
    /// <summary>
    /// This class concentrates all relevant activities and resources for planning a tour via xTour.
    /// </summary>
    public class TourPlanningUseCase : UseCase
    {
        private ShapeLayer tourLayer;
        private ShapeLayer orderLayer;
        private ShapeLayer depotLayer;
        private readonly Color unplannedColor = Color.FromRgb(255, 64, 64);
        private TourCalculationWrapper tourCalculationWrapper;

        /// <summary>
        /// Tries to create all relevant layers and other resources needed for this use case.
        /// </summary>
        protected override void Enable()
        {
            #region doc:CreateLayers
            tourLayer = new ShapeLayer("Tours");
            wpfMap.Layers.InsertBefore(tourLayer, "Labels"); // add before xmap labels

            orderLayer = new ShapeLayer("Orders");
            wpfMap.Layers.Add(orderLayer);

            depotLayer = new ShapeLayer("Depots");
            wpfMap.Layers.Add(depotLayer);
            #endregion doc:CreateLayers
        }

        /// <summary>
        /// Removes the tour layer from the WpfMap.
        /// </summary>
        protected override void Disable()
        {
            wpfMap.Layers.Remove(tourLayer);
            wpfMap.Layers.Remove(orderLayer);
            wpfMap.Layers.Remove(depotLayer);

            SendProgressInfo("Deactivated");
        }

        private Scenario scenario;

        /// <summary>
        /// Configure the tour planning with a scenario by specifying its size via <paramref name="scenarioSize"/>.
        /// </summary>
        /// <param name="scenarioSize">Specifies the amount of objects, which will be created randomly.</param>
        public void SetScenario(ScenarioSize scenarioSize)
        {
            SendProgressInfo("Initializing...");
            var center = new Point(8.4, 49); // KA, for Lux: Point(6.130833, 49.611389); 
            const double radius = .2; // radius in degrees of latitude

            Tools.AsyncUIHelper(() => ScenarioBuilder.CreateRandomScenario(scenarioSize, center, radius),
                                 s =>
                                 {
                                     wpfMap.SetMapLocation(center, 10); 
                                     SetScenario(s);
                                     if (Finished != null)
                                         Finished();
                                     SendProgressInfo("Ready");
                                     Initialized();
                                 },
                                ex => MessageBox.Show(ex.Message));
        }

        private void SetScenario(Scenario newScenario)
        {
            scenario = newScenario;

            #region doc:ConfigureLayers

            tourLayer.Shapes.Clear();
            orderLayer.Shapes.Clear();
            depotLayer.Shapes.Clear();

            foreach (var order in scenario.Orders)
            {
                var pin = new Cube { Color = unplannedColor };
                pin.Width = pin.Height = Math.Sqrt(order.Quantity) * 10;
                ShapeCanvas.SetLocation(pin, new Point(order.Longitude, order.Latitude));
                orderLayer.Shapes.Add(pin);
                pin.Tag = order;
            }

            foreach (var depot in scenario.Depots)
            {
                var pyramid = new Pyramid();
                pyramid.Width = pyramid.Height = 30;
                pyramid.Color = depot.Color;
                ShapeCanvas.SetLocation(pyramid, new Point(depot.Longitude, depot.Latitude));
                depotLayer.Shapes.Add(pyramid);
            }

            #endregion // doc:ConfigureLayers
        }

        private void RenderPlannedTours()
        {
            tourLayer.Shapes.Clear();
            foreach (var tour in scenario.Tours)
            {
                var pc = new PointCollection(from tp in tour.TourPoints select new Point(tp.Longitude, tp.Latitude));
                new RoutePolyline(tourLayer)
                {
                    Points = pc,
                    ToolTip = tour.Vehicle.Id,
                    Color = tour.Vehicle.Depot.Color,
                    Width = 7
                };
            }

            foreach (var frameworkElement in orderLayer.Shapes)
            {
                var cube = (Cube) frameworkElement;
                var order = (Order)cube.Tag;
                if (order.Tour != null)
                {
                    cube.Color = order.Tour.Vehicle.Depot.Color;
                    Panel.SetZIndex(cube, 1); // bring to front
                }
                else
                {
                    cube.Color = unplannedColor;
                    Panel.SetZIndex(cube, 0); // bring to back
                }
            }

            if (Finished != null)
                Finished();
        }

        private void SendProgressInfo(string message, int percentage = 0)
        {
            if (Progress == null)
                return;
            Progress(message, percentage);
        }

        /// <summary>Callback delegate for getting informed about changes in the progress of the tour planning.</summary>
        public Action<string, int> Progress;
        /// <summary>Callback delegate for getting informed about the termination of the tour planning. </summary>
        public Action Finished;
        /// <summary>Callback delegate for getting informed when a new scenario is set up. </summary>
        public Action Initialized;

        /// <summary> Triggers the asynchronous call of the tour planning.</summary>
        public void StartPlanning()
        {
            #region doc:StartTourPlanning
            tourCalculationWrapper = new TourCalculationWrapper();
            tourCalculationWrapper.Progress = () => SendProgressInfo(tourCalculationWrapper.ProgressMessage, tourCalculationWrapper.ProgressPercent);
            tourCalculationWrapper.Finished = RenderPlannedTours;
            tourCalculationWrapper.StartPlanScenario(scenario);
            #endregion // doc:StartTourPlanning
        }

        /// <summary> Stops the asynchronous called tour planning.</summary>
        public void StopPlanning()
        {
            tourCalculationWrapper.Cancel();
        }
    }
}
