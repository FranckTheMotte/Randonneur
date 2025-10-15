using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using XmlGpx;

namespace Randonneur
{
    /// <summary>
    /// Class to define a waypoint.
    /// </summary>
    public class Waypoint
    {
        /// <summary>
        /// Name of the waypoint.
        /// </summary>
        public string Name = "NULL";

        /// <summary>
        /// Linked trace name.
        /// </summary>
        public string TraceName = "nothing.gpx";

        /// <summary>
        /// Geographic coordinates (lat, lon)
        /// </summary>
        public Vector2 GeographicCoord;

        /// <summary>
        /// Elevation in meter.
        /// </summary>
        public required float Elevation { get; set; }

        /// <summary>
        /// List of trace destinations.
        /// </summary>
        public List<GpxDestination> Destinations = [];

        /// <summary>
        /// Gfx of junction (TODO unlock from junction type).
        /// </summary>
        public JunctionArea? JunctionGfx;

        /// <summary>
        /// Gfx of player marker.
        /// </summary>
        public ColorRect? Landmark;
    }

    internal class WaypointsLinks
    {
        public required Waypoint Waypoint;
        public List<Waypoint> ConnectedWaypoints = [];
    }

    /// <summary>
    /// Class to store all possible waypoints connections.
    /// </summary>
    public class Waypoints
    {
        /// <summary>
        /// Store all possible connections between waypoints.
        /// </summary>
        internal Dictionary<string, WaypointsLinks> Links { get; } = [];

        /// <summary>
        /// Store the currently selected waypoint.
        /// </summary>
        public Waypoint? SelectedWaypoint { get; set; }

        /// <summary>
        /// List of all reachable waypoints from the currently selected waypoint.
        /// </summary>
        public List<Waypoint>? ReachableWaypoints { get; set; }

        private static Waypoints? _instance;

        public static Waypoints Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Waypoints();
                }
                return _instance;
            }
        }

        internal void Add(Waypoint newWaypoint)
        {
            string key = newWaypoint.TraceName + newWaypoint.Name;

            // try to find the junction in the dictionary
            if (Links.ContainsKey(key))
            {
                // nothing to do
                return;
            }
            WaypointsLinks newWaypointsLinks = new() { Waypoint = newWaypoint };

            // Update the existing links
            foreach (KeyValuePair<string, WaypointsLinks> link in Links)
            {
                WaypointsLinks currentLink = link.Value;
                Waypoint waypoint = link.Value.Waypoint;
                /* Does the new waypoint is close to the fetched?
                Too close (less than 10 meters), means it's the same
                waypoint.
                */
                if (Gpx.GetDistance(newWaypoint.GeographicCoord, waypoint.GeographicCoord) > 10.0)
                {
                    // Is it on the same trace?
                    if (newWaypoint.TraceName == waypoint.TraceName)
                    {
                        // new link
                        newWaypointsLinks.ConnectedWaypoints.Add(waypoint);
                        // add new junction to the existing list
                        currentLink.ConnectedWaypoints.Add(newWaypoint);
                    }
                    // Is the new junction Trace connected to the fetched waypoint?
                    foreach (GpxDestination newDestination in newWaypoint.Destinations)
                    {
                        if (Path.GetFileName(newDestination.GpxFile) == waypoint.TraceName)
                        {
                            // connect both junctions
                            currentLink.ConnectedWaypoints.Add(newWaypoint);
                            newWaypointsLinks.ConnectedWaypoints.Add(waypoint);
                        }
                    }
                }
            }

            // Add the new junction
            Links[key] = newWaypointsLinks;
        }
    }
}
