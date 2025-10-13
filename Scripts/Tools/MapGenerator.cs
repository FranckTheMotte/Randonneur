using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Godot;
using Randonneur;
using XmlGpx;
/* Because of System.Numerics */
using Vector2 = Godot.Vector2;

public partial class MapGenerator : Node
{
    public const int TrailLineWidth = 10;

    public Color TrailLinceColor = Colors.Orange;

    public const string TrailCollisionFilterName = "trail";

    public struct GpsPoint
    {
        public double Latitude; // -90 à +90 (South To North)
        public double Longitude; // -180 à +180 (West to East)

        public GpsPoint(double lat, double lon)
        {
            Latitude = lat;
            Longitude = lon;
        }
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
    public Vector2 displaySize { get; }

    public MapGenerator(float width, float height)
    {
        displaySize = new Vector2(width, height);
    }

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
        // pour chaque fichier dans dir:
        // - parser le xml
        // - récupérer lat/long/ele
        // - transcrire lat/long to x,y coord :
        //    - faire une line avec la liaison entre chaque point consécutif, avec une épaisseur.
        //      Un polygone de collision identique pour checker le passage du curseur de la souris.
        // - ele => TODO

        // ex DMS coord of x1 & x2 (2 consecutive points)
        // x1(long, lat) 45.7907840 - 6.9719600 => 45° 47' 26.822" - 6° 58' 19.056"
        // x2(long, lat) 45.790742 - 6.971974 => 45° 47' 26.671" - 6° 58' 19.106"
        List<Area2D> areasList = new();
        Dictionary<string, Gpx> Trails = [];

        /* First run to get min and max values for longitude and latiture*/
        string[] gpxFiles = Directory.GetFiles(ProjectSettings.GlobalizePath(dir));
        double minLatitude,
            maxLatitude;
        double minLongitude,
            maxLongitude;

        minLatitude = 90;
        minLongitude = 180;
        maxLatitude = -90;
        maxLongitude = 0;

        foreach (string gpxFileName in gpxFiles)
        {
            Gpx trail;
            /* Generate a profil from a gpx file */
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

        GD.Print(
            $"minLatitude {minLatitude} maxLatitude {maxLatitude} minLongitude {minLongitude} maxLongitude {maxLongitude}"
        );

        GeoBounds mapBounds = new GeoBounds(
            minLat: minLatitude,
            maxLat: maxLatitude, // South To North
            minLon: minLongitude,
            maxLon: maxLongitude // West to East
        );

        foreach (string gpxFileName in gpxFiles)
        {
            Gpx trail;
            /* Generate a profil from a gpx file */
            trail = new Gpx();
            if (trail.Load(gpxFileName) == true && trail.TrackPoints is not null)
            {
                Line2D trailLine = new Line2D();
                Vector2[] trace = new Vector2[trail.TrackPoints.Length];
                int i = 0;
                MapArea area = new();
                string traceFileName = Path.GetFileName(gpxFileName);

                foreach (GpxProperties gpxPoint in trail.TrackPoints)
                {
                    GpsPoint gpsPoint = new GpsPoint(gpxPoint.Coord.X, gpxPoint.Coord.Y);
                    trace[i] = GpsToScreenLinear(gpsPoint, mapBounds, displaySize);
                    /*GD.Print(
                        $" gpxPoint.coord.X: {gpxPoint.coord.X} gpxPoint.coord.Y:  {gpxPoint.coord.Y} gpx.waypoint {gpxPoint.Waypoint}"
                    );
                    GD.Print($" X: {trace[i].X} Y:  {trace[i].Y}");*/
                    GpxWaypoint? waypoint = gpxPoint.Waypoint;
                    if (waypoint != null)
                    {
                        Label waypointLabel = new Label();
                        waypointLabel.Name = waypointLabel.Text = waypoint.Name;
                        waypointLabel.Position = trace[i];
                        waypointLabel.ZIndex = 2;
                        waypointLabel.LabelSettings = new LabelSettings();
                        waypointLabel.LabelSettings.FontColor = Colors.Black;

                        // TEST: just to check where waypoints are located on map
                        // This marker can only be add now because the
                        // lat/lon to screen coord conversion is done here.
                        JunctionArea junctionArea = new(trace[i], waypoint.Name, traceFileName);
                        // Put a black rect for the player position
                        ColorRect landmark = new()
                        {
                            Position = trace[i],
                            Size = new Vector2(10, 10),
                            Visible = false,
                            ZIndex = 5,
                        };
                        trail.Waypoints.SetWaypointLandmark(waypoint.Coord, landmark);
                        area.AddChild(landmark);
                        area.AddChild(waypointLabel);
                        area.AddChild(junctionArea);
                    }
                    i++;
                }

                trailLine.Name = Global.TrailLineName;
                trailLine.Points = trace;
                trailLine.Width = TrailLineWidth;
                trailLine.DefaultColor = TrailLinceColor;

                // Area2D for collisions detection
                area.Name = traceFileName;
                area.SetMeta("TraceName", traceFileName);
                area.AddChild(trailLine);
                area.SetCollisionLayerValue(1, false);
                area.SetCollisionLayerValue(4, true);
                area.ZIndex = 2;

                // Add collisions
                CreateSegmentCollisions(area, trailLine);

                areasList.Add(area);
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

    private void CreateSegmentCollisions(Area2D area, Line2D line)
    {
        // Create collition for each line segment
        for (int i = 0; i < line.Points.Length - 1; i++)
        {
            Vector2 start = line.Points[i];
            Vector2 end = line.Points[i + 1];

            CollisionShape2D collision = new CollisionShape2D();
            RectangleShape2D rectangle = new RectangleShape2D();

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
