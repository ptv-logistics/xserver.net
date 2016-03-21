//--------------------------------------------------------------
// Copyright (c) PTV Group
// 
// For license details, please refer to the file COPYING, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ComponentModel;

using Ptv.XServer.Demo.Tools;
using Ptv.XServer.Demo.XtourService;

namespace Ptv.XServer.Demo.UseCases.TourPlanning
{
    /// <summary>
    /// Encapsulates the handling of the xTour server by more convenient methods. Especially 
    /// dictionaries are used to map the business entities to xtour entities.
    /// This is needed, because the xtour entities can only be identified by integer IDs.
    /// </summary>
    public class TourCalculationWrapper
    {
        /// <summary>Dictionary for orders, encapsulating the xTour's identification via IDs.</summary>
        public BusinessToX<Order, string> orderMap;
        /// <summary>Dictionary for depots, encapsulating the xTour's identification via IDs.</summary>
        public BusinessToX<Depot, string> depotMap;
        /// <summary>Dictionary for vehicles, encapsulating the xTour's identification via IDs.</summary>
        public BusinessToX<Vehicle, string> vehicleMap;

        /// <summary>Delegate, which is called during the tour calculation for intermediate steps.</summary>
        public Action Progress;
        /// <summary>Delegate, which is called after termination of the tour calculation.</summary>
        public Action Finished;

        /// <summary>Message text for each individual intermediate step. </summary>
        public string ProgressMessage;
        /// <summary>Percentage value indicating the qualitative progress of the tour calculation. </summary>
        public int ProgressPercent;

        private BackgroundWorker bw;

        /// <summary>Aborts the tour calculation.</summary>
        public void Cancel()
        {
            bw.CancelAsync();
        }

        /// <summary>Scenario containing the orders, depots and vehicles used for tour calculation.</summary>
        public Scenario scenario;

        /// <summary> Start planning according the settings of the specified scenario. </summary>
        /// <param name="newScenario"></param>
        public void StartPlanScenario(Scenario newScenario)
        {
            scenario = newScenario;

            orderMap = new BusinessToX<Order, string>();
            depotMap = new BusinessToX<Depot, string>();
            vehicleMap = new BusinessToX<Vehicle, string>();

            bw = new BackgroundWorker {WorkerReportsProgress = true, WorkerSupportsCancellation = true};
            bw.DoWork += bw_DoWork;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerAsync();
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (Progress == null) return;

            var job = e.UserState as Job;
            if (job.progress == null)
            {
                var status = job.status.ToString();
                ProgressMessage = status[0].ToString(CultureInfo.InvariantCulture).ToUpper() + status.Substring(1).ToLower();
            }
            else if (job.progress is PlanProgress)
            {
                var planProgress = job.progress as PlanProgress;
                switch (planProgress.action)
                {
                    case "DistanceMatrix.Calculation":
                        var dimaProgress = planProgress.distanceMatrixCalculationProgress.currentDistanceMatrixProgress;
                        var currentRowIndex = dimaProgress.currentRowIndex;
                        var lastRowIndex = dimaProgress.lastRowIndex;
                        ProgressPercent = 50*currentRowIndex/lastRowIndex;
                        ProgressMessage = string.Format("Calculating distance matrix: {0}/{1}", currentRowIndex, lastRowIndex);
                        break;
                    case "Optimization.Improvement":
                        var improvementProgress = planProgress.improvementProgress;
                        var availableMachineTime = improvementProgress.availableMachineTime;
                        var usedMachineTime = improvementProgress.usedMachineTime;
                        var iterationIndex = improvementProgress.iterationIndex;
                        ProgressPercent = 50 + 50*usedMachineTime/availableMachineTime;
                        ProgressMessage = string.Format("Improving plan, iteration index: {0}, machine time: {1}/{2}", iterationIndex, usedMachineTime, availableMachineTime);
                        break;
                    default:
                        ProgressMessage = planProgress.action;
                        break;
                }
            }
            else
                ProgressMessage = job.progress.ToString();

            Progress();
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(Progress != null)
            {
                ProgressMessage = e.Cancelled ? "Cancelled" : "Finished";
                ProgressPercent = e.Cancelled ? 0 : 100;
                Progress();
            }

            if (Finished != null)
                Finished();
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            // reset old plan
            scenario.Tours = new List<Tour>();
            scenario.Orders.ForEach((order => order.Tour = null));

            var xtour = XServerClientFactory.CreateXTourClient(Properties.Settings.Default.XUrl);
            var orders = (from o in scenario.Orders
                          select new TransportDepot
                          {
                              id = orderMap.MapObject(o, o.Id),
                              transportPoint = new TransportPoint
                              {
                                  id = orderMap.MapObject(o, o.Id),                                  
                                  servicePeriod = 0,  // 0sec; unrealistic but okay for this sample                                  
                                  location = new Point
                                  {
                                      point = new PlainPoint
                                      {
                                          x = o.Longitude,
                                          y = o.Latitude
                                      }
                                  },
                                  openingIntervalConstraint = OpeningIntervalConstraint.START_OF_SERVICE,
                              },
                              deliveryQuantities = new Quantities { wrappedQuantities = new[] { o.Quantity } }
                          }).ToArray();

            var depots = (from d in scenario.Depots
                          select new XtourService.Depot
                          {
                              id = depotMap.MapObject(d, d.Id),
                              location = new Point
                              {
                                  point = new PlainPoint
                                  {
                                      x = d.Longitude,
                                      y = d.Latitude
                                  }
                              }                          
                          }).ToArray();

            var yy = (from d in scenario.Depots select d.Fleet).SelectMany(x => x);

            var interval = new Interval { from = 0, till = scenario.OperatingPeriod*200 };

            var vehicles = (from v in yy
                            select new XtourService.Vehicle
                            {
                                id = vehicleMap.MapObject(v, v.Id),
                                depotIdStart = depotMap.bTos[v.Depot.Id],
                                depotIdEnd = depotMap.bTos[v.Depot.Id],
                                isPreloaded = false,                        
                                capacities = new Capacities
                                {
                                    wrappedCapacities = new[] { new Quantities { wrappedQuantities = new[] { v.Capacity } } }
                                },
                                wrappedOperatingIntervals = new[] { interval },
                                dimaId = 1,
                                dimaIdSpecified = true,
                            }).ToArray();

            var fleet = new Fleet { wrappedVehicles = vehicles };

            var planningParams = new StandardParams
            {
                wrappedDistanceMatrixCalculation = new DistanceMatrixCalculation[] {new DistanceMatrixByRoad
                {
                    dimaId = 1,                    
                    deleteBeforeUsage = true,
                    deleteAfterUsage = true,
                    profileName = "dimaTruck",
                }},
                availableMachineTime = 15,
                availableMachineTimeSpecified = true
            };

            var xtourJob = xtour.startPlanBasicTours(orders, depots, fleet, planningParams, null,
                new CallerContext
                {
                    wrappedProperties = new[] { 
                    new CallerContextProperty { key = "CoordFormat", value = "OG_GEODECIMAL" },
                    new CallerContextProperty { key = "TenantId", value = Guid.NewGuid().ToString() }}
                });

            bw.ReportProgress(-1, xtourJob);
            var status = xtourJob.status;
            while (status == JobStatus.QUEUING || status == JobStatus.RUNNING)
            {
                if (bw.CancellationPending)
                {
                    xtour.stopJob(xtourJob.id, null);
                    e.Cancel = true;
                    return;
                }

                xtourJob = xtour.watchJob(xtourJob.id, new WatchOptions
                {
                    maximumPollingPeriod = 250,
                    maximumPollingPeriodSpecified = true,
                    progressUpdatePeriod = 250,
                    progressUpdatePeriodSpecified = true
                }, null);
                status = xtourJob.status;

                bw.ReportProgress(-1, xtourJob);
            }

            var result = xtour.fetchPlan(xtourJob.id, null);

            scenario.Tours = new List<Tour>();
            foreach (var c in result.wrappedChains)
                foreach (var wt in c.wrappedTours)
                {
                    var tour = new Tour();
                    var tourPoints = new List<TourPoint>();
                    foreach (var wrappedTourPoint in wt.wrappedTourPoints)
                    {
                        switch (wrappedTourPoint.type)
                        {
                            case TourPointType.DEPOT:
                                tourPoints.Add(new TourPoint
                                {
                                    Longitude = depotMap.sTob[wrappedTourPoint.id].Longitude,
                                    Latitude = depotMap.sTob[wrappedTourPoint.id].Latitude
                                });
                                break;
                            case TourPointType.TRANSPORT_POINT:
                                orderMap.sTob[wrappedTourPoint.id].Tour = tour;
                                tourPoints.Add(new TourPoint
                                {
                                    Longitude = orderMap.sTob[wrappedTourPoint.id].Longitude,
                                    Latitude = orderMap.sTob[wrappedTourPoint.id].Latitude
                                });
                                break;
                        }
                    }

                    tour.Vehicle = vehicleMap.sTob[c.vehicleId];
                    tour.TourPoints = tourPoints;
                    scenario.Tours.Add(tour);
                }   
        }
    }

    /// <summary>
    /// A helper class which maps business objects (usually identified by a unique string) To xServer objects
    /// identified by an int.
    /// </summary>
    /// <typeparam name="B">Object type which should be mapped.</typeparam>
    /// <typeparam name="K">Key value type used for mapping.</typeparam>
    public class BusinessToX<B, K>
    {
        private int idx;

        /// <summary>Mapping of the xTour id to an internally used identification.</summary>
        public Dictionary<K, int> bTos = new Dictionary<K, int>();
        /// <summary>Mapping of the internally used identification to the addressed object.</summary>
        public Dictionary<int, B> sTob = new Dictionary<int, B>();

        /// <summary>Returns the internally used identification of the object.</summary>
        /// <param name="obj">Business object, i.e. an order, depot or vehicle.</param>
        /// <param name="key">xTour id of the business object.</param>
        /// <returns></returns>
        public int MapObject(B obj, K key)
        {
            if (bTos.ContainsKey(key))
                return bTos[key];

            idx++;

            bTos[key] = idx;
            sTob[idx] = obj;

            return idx;
        }
    }
}
