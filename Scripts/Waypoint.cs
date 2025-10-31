using System;
using System.Collections.Generic;
using System.IO;
using Godot;

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
        /// Level coordinates (x, y)
        /// </summary>
        public Vector2 LevelCoord;

        /// <summary>
        /// Elevation in meter.
        /// </summary>
        public required float Elevation { get; set; }

        /// <summary>
        /// List of trace destinations.
        /// </summary>
        public List<GpxDestination> Destinations = [];

        /// <summary>
        /// Order of waypoints by trace.
        /// Key: trace name
        /// Value: integer, lower value = close from start
        /// </summary>
        public Dictionary<string, int> LevelOrder { get; set; } = [];

        public Waypoint(string Name)
        {
            // Configure a label without X,Y position
            this.Name = Name;
        }

        internal void FocusLandmark(bool value)
        {
            //TODO
        }
    }

    /// <summary>
    /// Store the connected waypoint with trace used.
    /// </summary>
    public class ConnectedWaypoint
    {
        public required string TraceName;
        public required Waypoint Waypoint;
    }

    /// <summary>
    /// Store the connected waypoints of a waypoint.
    /// </summary>
    public class WaypointsLinks
    {
        public required Waypoint Waypoint;

        /// <summary>
        /// Connected waypoints with linked trace, can be empty.
        /// </summary>
        public Dictionary<string, ConnectedWaypoint> ConnectedWaypoints = [];
    }

    /// <summary>
    /// Interface to use Waypoints singleton.
    /// Allow MOCK uses for unit tests.
    /// </summary>
    public interface IWaypoints
    {
        Dictionary<string, WaypointsLinks>? Links { get; set; }
        void DisplayLinks();
        Waypoint? GetWaypoint(string waypointName);
        void Add(Waypoint newWaypoint);
    }

    /// <summary>
    /// Class to store all possible waypoints connections.
    /// </summary>
    public class Waypoints : IWaypoints
    {
        /// <summary>
        /// Store all possible connections between waypoints.
        /// </summary>
        public Dictionary<string, WaypointsLinks>? Links { get; set; } = [];

        /// <summary>
        /// Display all waypoints name and the related connected waypoints
        /// </summary>
        public void DisplayLinks()
        {
            if (Links == null)
                return;
            Console.Write("======================================\n");
            foreach (KeyValuePair<string, WaypointsLinks> link in Links)
            {
                Console.Write($"Waypoint {link.Key} connected to:\n");
                foreach (
                    KeyValuePair<
                        string,
                        ConnectedWaypoint
                    > connectedWaypoint in link.Value.ConnectedWaypoints
                )
                {
                    Console.Write(
                        $"(Use {connectedWaypoint.Value.TraceName}) to go from {link.Key} to {connectedWaypoint.Key}\n"
                    );
                }
            }
            Console.Write("======================================\n");
        }

        /// <summary>
        /// Temporary dictionaries to get reacheable waypoints by waypoint.
        /// </summary>
        private Dictionary<string, Dictionary<string, Waypoint>>? _WbT = [];
        private Dictionary<string, Dictionary<string, Waypoint>>? _TbW = [];

        // From xml it can exist several waypoint from different traces, but for
        // world map, we just need to have a single waypoint.
        // First waypoint inserted IS the reference.
        private Dictionary<string, Waypoint>? _AggregateWaypoint = [];

        /// <summary>
        /// Singleton.
        /// </summary>
        private static readonly Lazy<Waypoints> _instance = new Lazy<Waypoints>(() =>
            new Waypoints()
        );
        public static IWaypoints Instance => _instance.Value;

        /// <summary>
        /// Get a Waypoint from WaypointsLinks with a waypoint name.
        /// </summary>
        /// <param name="waypointName">The name of the waypoint to retrieve.</param>
        /// <returns>The Waypoint if found, null otherwise.</returns>
        public Waypoint? GetWaypoint(string waypointName)
        {
            return Links?[waypointName].Waypoint;
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

            if (_TbW == null || _WbT == null || Links == null || _AggregateWaypoint == null)
            {
                Console.Write(
                    "Failed to add a waypoint (missing reallocations) maybe a reset during unit test?"
                );
                return;
            }

            /* new waypoint is added to Trace by Waypoint and
               Waypoint by Trace dictionnaries. */
            if (!_TbW.ContainsKey(newWaypoint.Name))
                _TbW[newWaypoint.Name] = [];
            if (!_TbW[newWaypoint.Name].ContainsKey(newWaypoint.TraceName))
                _TbW[newWaypoint.Name][newWaypoint.TraceName] = newWaypoint;

            if (!_WbT.ContainsKey(newWaypoint.TraceName))
                _WbT[newWaypoint.TraceName] = [];
            if (!_WbT[newWaypoint.TraceName].ContainsKey(newWaypoint.Name))
                _WbT[newWaypoint.TraceName][newWaypoint.Name] = newWaypoint;

            // Only one waypoint can be kept
            if (!_AggregateWaypoint.ContainsKey(newWaypoint.Name))
                _AggregateWaypoint[newWaypoint.Name] = newWaypoint;

            Waypoint aggregateWaypoint = _AggregateWaypoint[newWaypoint.Name];

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
                                    Dictionary<string, ConnectedWaypoint> connectedWpts;
                                    string wptName;

                                    // Don't add over existing connection
                                    if (!Links.ContainsKey(wpt.Key))
                                    {
                                        Links[wpt.Key] = new() { Waypoint = wpt.Value };
                                    }
                                    connectedWpts = Links[wpt.Key].ConnectedWaypoints;
                                    if (!connectedWpts.ContainsKey(newWaypoint.Name))
                                    {
                                        ConnectedWaypoint connWaypoint = new()
                                        {
                                            // required to use the trace of target to
                                            // join the linked waypoint
                                            TraceName = newWaypoint.TraceName,
                                            Waypoint = aggregateWaypoint,
                                        };
                                        connectedWpts[newWaypoint.Name] = connWaypoint;
                                    }

                                    // new ?
                                    if (!Links.ContainsKey(newWaypoint.Name))
                                    {
                                        Links[newWaypoint.Name] = new()
                                        {
                                            Waypoint = aggregateWaypoint,
                                        };
                                    }
                                    connectedWpts = Links[newWaypoint.Name].ConnectedWaypoints;
                                    wptName = Links[wpt.Key].Waypoint.Name;
                                    if (!connectedWpts.ContainsKey(wptName))
                                    {
                                        ConnectedWaypoint connWaypoint = new()
                                        {
                                            // required to use the trace of target to
                                            // join the linked waypoint
                                            TraceName = newWaypoint.TraceName,
                                            Waypoint = Links[wpt.Key].Waypoint,
                                        };
                                        connectedWpts[wptName] = connWaypoint;
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

        /// <summary>
        /// Reset internal data (used for unit tests).
        /// </summary>
        internal static void Reset()
        {
            if (_instance.IsValueCreated)
            {
                _instance.Value.Links = [];
                _instance.Value._WbT = [];
                _instance.Value._TbW = [];
                _instance.Value._AggregateWaypoint = [];
            }
        }
    }
}
