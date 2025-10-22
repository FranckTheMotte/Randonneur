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
        public Dictionary<string, Waypoint> ConnectedWaypoints = [];
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
        /// Display all waypoints name and the related connected waypoints
        /// </summary>
        public void DisplayLinks()
        {
            GD.Print($"======================================");
            foreach (KeyValuePair<string, WaypointsLinks> link in Links)
            {
                GD.Print($"Waypoint {link.Key} connected to: ");
                foreach (
                    KeyValuePair<
                        string,
                        Waypoint
                    > connectedWaypoint in link.Value.ConnectedWaypoints
                )
                {
                    GD.Print($"({connectedWaypoint.Value.TraceName}) - {connectedWaypoint.Key}");
                }
            }
            GD.Print($"======================================");
        }

        /// <summary>
        /// Temporary dictionnaries to get reacheable waypoints by waypoint.
        /// </summary>
        private Dictionary<string, Dictionary<string, Waypoint>> _WbT = [];
        private Dictionary<string, Dictionary<string, Waypoint>> _TbW = [];

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
        public void Add(Waypoint newWaypoint)
        {
            Dictionary<string, int> traceProcessed = [];

            /* new waypoint is add to Trace by Waypoint and
               Waypoint by Trace dictionnaries.
            */
            if (!_TbW.ContainsKey(newWaypoint.Name))
                _TbW[newWaypoint.Name] = [];
            _TbW[newWaypoint.Name][newWaypoint.TraceName] = newWaypoint;
            if (!_WbT.ContainsKey(newWaypoint.TraceName))
                _WbT[newWaypoint.TraceName] = [];
            _WbT[newWaypoint.TraceName][newWaypoint.Name] = newWaypoint;

            // update waypoints connection with the new waypoint
            foreach (KeyValuePair<string, Dictionary<string, Waypoint>> trace in _TbW)
            {
                // List of traces
                Dictionary<string, Waypoint> traces = trace.Value;

                foreach (KeyValuePair<string, Waypoint> aTrace in traces)
                {
                    // already used?
                    if (traceProcessed.ContainsKey(aTrace.Key) == false)
                    {
                        // does the new waypoint is on this trace?
                        if (newWaypoint.TraceName == aTrace.Key)
                        {
                            foreach (
                                KeyValuePair<string, Waypoint> wpt in _WbT[newWaypoint.TraceName]
                            )
                            {
                                // Try to add reachable waypoint except itself
                                if (wpt.Key != newWaypoint.Name)
                                {
                                    Dictionary<string, Waypoint> connectedWpts;
                                    string wptName;

                                    // Don't add over existing connection
                                    if (!Links.ContainsKey(wpt.Key))
                                    {
                                        Links[wpt.Key] = new WaypointsLinks()
                                        {
                                            Waypoint = wpt.Value,
                                        };
                                    }
                                    connectedWpts = Links[wpt.Key].ConnectedWaypoints;
                                    if (!connectedWpts.ContainsKey(newWaypoint.Name))
                                    {
                                        connectedWpts[newWaypoint.Name] = newWaypoint;
                                    }

                                    // new ?
                                    if (!Links.ContainsKey(newWaypoint.Name))
                                    {
                                        Links[newWaypoint.Name] = new WaypointsLinks()
                                        {
                                            Waypoint = newWaypoint,
                                        };
                                    }
                                    connectedWpts = Links[newWaypoint.Name].ConnectedWaypoints;
                                    wptName = Links[wpt.Key].Waypoint.Name;
                                    if (!connectedWpts.ContainsKey(wptName))
                                    {
                                        connectedWpts[wptName] = Links[wpt.Key].Waypoint;
                                    }

                                    // to avoid useless loop
                                    traceProcessed[newWaypoint.TraceName] = 0;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
