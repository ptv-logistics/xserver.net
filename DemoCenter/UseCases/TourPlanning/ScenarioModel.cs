//--------------------------------------------------------------
// Copyright (c) PTV Group
// 
// For license details, please refer to the file COPYING, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using System.Collections.Generic;
using System.Windows.Media;

namespace Ptv.XServer.Demo.UseCases.TourPlanning
{
    /// <summary>
    /// This class represents our scenario which models our tour planning objects
    /// Unlike the objects used in xTour, which are modelled to build-up our xtour requests, 
    /// these objects are modelled to build-up our application domain. 
    /// They are usually mapped to some kind of database
    /// </summary>
    public class Scenario
    {
        /// <summary>List of orders</summary>
        public List<Order> Orders;
        /// <summary>List of depots</summary>
        public List<Depot> Depots;
        /// <summary>List of tours</summary>
        public List<Tour> Tours;
        /// <summary>Number of actually existing depots.</summary>
        public int NumDepots { get; set; }
        /// <summary>Number of actually existing vehicles.</summary>
        public int NumVehiclesPerDepot { get; set; }
        /// <summary>Number of orders per vehicle.</summary>
        public int NumOrdersPerVehicle { get; set; }
        /// <summary>Actually used operating period.</summary>
        public int OperatingPeriod { get; set; }
    }

    /// <summary>Represents the business case vehicle.</summary>
    public class Vehicle
    {
        /// <summary>Vehicle Id</summary>
        public string Id { get; set; }
        /// <summary>Vehicle capacity</summary>
        public int Capacity { get; set; }
        /// <summary>Depot to which this vehicle belongs too</summary>
        public Depot Depot { get; set; }
    }

    /// <summary>Represents the business case depot.</summary>
    public class Depot
    {
        /// <summary>Depot Id</summary>
        public string Id { get; set; }
        /// <summary>Latitude of the depot</summary>
        public double Latitude { get; set; }
        /// <summary>Longitude of the depot</summary>
        public double Longitude { get; set; }
        /// <summary>Set of vehicles belonging to the depot</summary>
        public List<Vehicle> Fleet { get; set; }
        /// <summary>Color of the depot used during rendering in a map.</summary>
        public Color Color { get; set; }
    }

    /// <summary>Represents the business case order.</summary>
    public class Order
    {
        /// <summary>Order Id</summary>
        public string Id { get; set; }
        /// <summary>Latitude of the order</summary>
        public double Latitude { get; set; }
        /// <summary>Longitude of the order</summary>
        public double Longitude { get; set; }
        /// <summary>Quantity of the order</summary>
        public int Quantity { get; set; }
        /// <summary>Tour to which the order belongs too</summary>
        public Tour Tour { get; set; }
    }

    /// <summary>Represents the business case tour.</summary>
    public class Tour
    {
        /// <summary>Vehicle used for the tour</summary>
        public Vehicle Vehicle { get; set; }
        /// <summary>Geometry of the tour</summary>
        public List<TourPoint> TourPoints { get; set; }
    }

    /// <summary>Geometry point of a tour.</summary>
    public class TourPoint
    {
        /// <summary>Latitude of the tour point</summary>
        public double Latitude { get; set; }
        /// <summary>Longitude of the tour point</summary>
        public double Longitude { get; set; }
    }
}
