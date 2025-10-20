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
        public MapJunctionArea MapJunctionGfx = new();

        public readonly Label Label = new();

        public Waypoint(string Name)
        {
            // Configure a label without X,Y position
            this.Name = Name;
            Label.Name = Label.Text = Name;
            Label.ZIndex = 2;
            Label.LabelSettings = new LabelSettings { FontColor = Colors.Black };
        }

        internal void FocusLandmark(bool value)
        {
            //TODO
        }
    }

    /// <summary>
    /// Store the connected waypoints of a waypoint.
    /// </summary>
    internal class WaypointsLinks
    {
        public required Waypoint Waypoint;

        /// <summary>
        /// Connected waypoints, can be empty.
        /// </summary>
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

        /// <summary>
        /// Singleton.
        /// </summary>
        private static Waypoints? _instance;

        public static Waypoints Instance
        {
            get
            {
                _instance ??= new Waypoints();
                return _instance;
            }
        }

        /// <summary>
        /// Add a new waypoint to the list of connections.
        /// If the waypoint already exists, do nothing.
        /// Otherwise, add the new waypoint to the list of connections and update the existing links.
        /// </summary>
        /// <param name="newWaypoint">The new waypoint to add.</param>
        internal void Add(Waypoint newWaypoint)
        {
            string key = newWaypoint.Name;

            // try to find the junction in the dictionary
            if (Links.ContainsKey(key))
            {
                // nothing to do
                return;
            }

            // Update the existing links
            WaypointsLinks newWaypointsLinks = new() { Waypoint = newWaypoint };

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
