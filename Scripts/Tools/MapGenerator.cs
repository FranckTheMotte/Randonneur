using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Godot;
using Randonneur;
using XmlGpx;
/* Because of System.Numerics */
using Vector2 = Godot.Vector2;

public partial class MapGenerator(float width, float height) : Node
{
    public const int TrailLineWidth = 10;

    public Color TrailLinceColor = Colors.Orange;

    public const string TrailCollisionFilterName = "trail";

    public struct GpsPoint(double lat, double lon)
    {
        public double Latitude = lat; // -90 à +90 (South To North)
        public double Longitude = lon; // -180 à +180 (West to East)
    }

    // Geographic area
    public struct GeoBounds
    {
        public double MinLatitude;
        public double MaxLatitude;
        public double MinLongitude;
        public double MaxLongitude;

        public GeoBounds(double minLat, double maxLat, double minLon, double maxLon)
        {
            MinLatitude = minLat;
            MaxLatitude = maxLat;
            MinLongitude = minLon;
            MaxLongitude = maxLon;
        }
    }

    /* TODO: make this dynamic */
    public Vector2 DisplaySize { get; } = new Vector2(width, height);

    public static Vector2 GpsToScreenLinear(GpsPoint gpsPoint, GeoBounds bounds, Vector2 screenSize)
    {
        // Normalize GPS coord between 0 and 1
        double normalizedLon =
            (gpsPoint.Longitude - bounds.MinLongitude)
            / (bounds.MaxLongitude - bounds.MinLongitude);

        double normalizedLat =
            (gpsPoint.Latitude - bounds.MinLatitude) / (bounds.MaxLatitude - bounds.MinLatitude);

        // screen coord conversion
        float screenX = (float)(normalizedLon * screenSize.X);
        // Reverse Y because screen (0,0) is at top left
        float screenY = (float)((1.0 - normalizedLat) * screenSize.Y);
        return new Vector2(screenX, screenY);
    }

    /// <summary>
    /// Generate Nodes from all gpx files in specified directory.
    /// </summary>
    /// <param name="dir">Location of gpx files.</param>
    /// <returns>A tuple containing a list of area2d where each one contains Nodes for a trail, and an Hashtable of trails gpx items.</returns>
    public (List<Area2D>? TrailsNodes, Dictionary<string, Gpx>? TrailsGpx) GenerateMap(string dir)
    {
        List<Area2D> areasList = [];
        Dictionary<string, Gpx> Trails = [];

        // First run to get min and max values for longitude and latiture
        string[] gpxFiles = Directory.GetFiles(ProjectSettings.GlobalizePath(dir));
        double minLatitude,
            maxLatitude;
        double minLongitude,
            maxLongitude;

        minLatitude = 90;
        minLongitude = 180;
        maxLatitude = -90;
        maxLongitude = 0;

        // Get bounds of all gpx files
        foreach (string gpxFileName in gpxFiles)
        {
            Gpx trail;
            // Generate a profil from a gpx file
            trail = new Gpx();
            if (trail.Load(gpxFileName) == true && trail.TrackPoints is not null)
            {
                foreach (GpxProperties gpxPoint in trail.TrackPoints)
                {
                    if (minLatitude > gpxPoint.Coord.X)
                        minLatitude = gpxPoint.Coord.X;
                    if (minLongitude > gpxPoint.Coord.Y)
                        minLongitude = gpxPoint.Coord.Y;
                    if (maxLatitude < gpxPoint.Coord.X)
                        maxLatitude = gpxPoint.Coord.X;
                    if (maxLongitude < gpxPoint.Coord.Y)
                        maxLongitude = gpxPoint.Coord.Y;
                }
            }
            else
            {
                GD.PushWarning($"${nameof(GenerateMap)}: Failure in gpx file : ${gpxFileName}");
                return (null, null);
            }
        }

        GeoBounds mapBounds = new(
            minLat: minLatitude,
            maxLat: maxLatitude, // South To North
            minLon: minLongitude,
            maxLon: maxLongitude // West to East
        );

        foreach (string gpxFileName in gpxFiles)
        {
            Gpx trail = new();
            // Generate a profil from a gpx file
            if (trail.Load(gpxFileName) == true && trail.TrackPoints is not null)
            {
                Line2D trailLine = new();
                Vector2[] trace = new Vector2[trail.TrackPoints.Length];
                int i = 0;
                MapArea mapTracearea = new();
                string traceFileName = Path.GetFileName(gpxFileName);

                foreach (GpxProperties gpxPoint in trail.TrackPoints)
                {
                    GpsPoint gpsPoint = new(gpxPoint.Coord.X, gpxPoint.Coord.Y);
                    trace[i] = GpsToScreenLinear(gpsPoint, mapBounds, DisplaySize);
                    Waypoint? waypoint = gpxPoint.Waypoint;
                    if (waypoint != null)
                    {
                        // Complete the waypoint with graphical position
                        Waypoints waypoints = Waypoints.Instance;
                        string key = waypoint.Name;
                        if (waypoints.Links.TryGetValue(key, out WaypointsLinks? links))
                        {
                            Waypoint connectedWaypoint = links.Waypoint;
                            connectedWaypoint.Label.Position = trace[i];
                            connectedWaypoint.MapJunctionGfx.Setup(
                                trace[i],
                                connectedWaypoint.Name,
                                connectedWaypoint.TraceName
                            );

                            // waypoint already added (Waypoint.Name is also the label Name)
                            if (
                                mapTracearea.FindChild(waypoint.Name) == null
                                && connectedWaypoint.Label.GetParent() == null
                            )
                            {
                                mapTracearea.AddChild(connectedWaypoint.Label);
                                mapTracearea.AddChild(connectedWaypoint.MapJunctionGfx);
                            }
                        }
                    }
                    i++;
                }

                trailLine.Name = Global.TrailLineName;
                trailLine.Points = trace;
                trailLine.Width = TrailLineWidth;
                trailLine.DefaultColor = TrailLinceColor;

                // Area2D for collisions detection
                mapTracearea.Name = traceFileName;
                mapTracearea.SetMeta(Global.MetaWaypointName, traceFileName);
                mapTracearea.AddChild(trailLine);
                mapTracearea.SetCollisionLayerValue(1, false);
                mapTracearea.SetCollisionLayerValue(4, true);
                mapTracearea.ZIndex = 2;

                // Add collisions
                CreateSegmentCollisions(mapTracearea, trailLine);

                areasList.Add(mapTracearea);
                Trails.Add(traceFileName, trail);
            }
            else
            {
                GD.PushWarning($"${nameof(GenerateMap)}: (2) Failure in gpx file : ${gpxFileName}");
                return (null, null);
            }
        }
        return (areasList, Trails);
    }

    /// <summary>
    /// Create collisions for each line segment of the given Line2D.
    /// </summary>
    /// <param name="area">The Area2D to which the collisions will be added.</param>
    /// <param name="line">The Line2D from which the collisions will be generated.</param>
    /// <remarks>
    /// The collisions will be named according to the format "TrailCollisionFilterName#i" where i is the index of the line segment.
    /// </remarks>
    private void CreateSegmentCollisions(Area2D area, Line2D line)
    {
        // Create collision for each line segment
        for (int i = 0; i < line.Points.Length - 1; i++)
        {
            Vector2 start = line.Points[i];
            Vector2 end = line.Points[i + 1];

            CollisionShape2D collision = new();
            RectangleShape2D rectangle = new();

            // Length and segment angle
            Vector2 direction = end - start;
            float length = direction.Length();
            float angle = direction.Angle();

            rectangle.Size = new Vector2(TrailLineWidth, length);

            // Position and collision angle
            collision.Shape = rectangle;
            collision.Position = (start + end) / 2.0f;
            // 90° shift because the default orientation of the collisionShape
            // is different of the godot reference.
            collision.Rotation = angle + Mathf.DegToRad(90);
            collision.Name = TrailCollisionFilterName + "#" + i;

            // Add collision to Area2D
            area.AddChild(collision);
        }
    }
}
