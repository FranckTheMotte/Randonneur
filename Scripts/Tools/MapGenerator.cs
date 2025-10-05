using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Godot;
/* Because of System.Numerics */
using Vector2 = Godot.Vector2;

public partial class MapGenerator : Node
{
    public const int TRAIL_LINE_WIDTH = 10;

    public const string TRAIL_COLLISION_FILTER_NAME = "trail";

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

    /**
        Generate Nodes from all gpx files in specified directory.

        @param      dir    Location of gpx files.

        @return TrailsNodes a list of area2d where each one contains Nodes for a trail.
                TrailsGpx   an hashtable of trails gpx items.
    */
    public (List<Area2D>? TrailsNodes, Hashtable? TrailsGpx) generateMap(string dir)
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
        Hashtable Trails = [];

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
            if (trail.Load(gpxFileName) == true && trail.m_trackPoints is not null)
            {
                foreach (GpxProperties gpxPoint in trail.m_trackPoints)
                {
                    if (minLatitude > gpxPoint.coord.X)
                        minLatitude = gpxPoint.coord.X;
                    if (minLongitude > gpxPoint.coord.Y)
                        minLongitude = gpxPoint.coord.Y;
                    if (maxLatitude < gpxPoint.coord.X)
                        maxLatitude = gpxPoint.coord.X;
                    if (maxLongitude < gpxPoint.coord.Y)
                        maxLongitude = gpxPoint.coord.Y;
                }
            }
            else
            {
                GD.PushWarning($"${nameof(generateMap)}: Failure in gpx file : ${gpxFileName}");
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
            if (trail.Load(gpxFileName) == true && trail.m_trackPoints is not null)
            {
                Line2D trailLine = new Line2D();
                Vector2[] trace = new Vector2[trail.m_trackPoints.Length];
                int i = 0;
                MapArea area = new();
                string traceFileName = Path.GetFileName(gpxFileName);

                foreach (GpxProperties gpxPoint in trail.m_trackPoints)
                {
                    GpsPoint gpsPoint = new GpsPoint(gpxPoint.coord.X, gpxPoint.coord.Y);
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
                        //trail.Waypoints.SetWaypointLandmark(waypoint.Coord, colorRect);
                        area.AddChild(waypointLabel);
                        area.AddChild(junctionArea);
                    }
                    i++;
                }

                // TODO This default name must be public and global
                trailLine.Name = "TrailLine2D";

                trailLine.Points = trace;
                trailLine.Width = TRAIL_LINE_WIDTH;
                // TODO This default color must be public and global
                trailLine.DefaultColor = Colors.Orange;

                // Area2D for collisions detection
                area.Name = traceFileName;
                /* Store the real filename in editor description to skip StringName character
                   replacement rules : When changing the name, the following characters will
                   be replaced with an underscore: (. : @ / " %).
                */
                area.EditorDescription = traceFileName;
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
                GD.PushWarning($"${nameof(generateMap)}: (2) Failure in gpx file : ${gpxFileName}");
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

            rectangle.Size = new Vector2(TRAIL_LINE_WIDTH, length);

            // Position and collision angle
            collision.Shape = rectangle;
            collision.Position = (start + end) / 2.0f;
            // 90° shift because the default orientation of the collisionShape
            // is different of the godot reference.
            collision.Rotation = angle + Mathf.DegToRad(90);
            collision.Name = TRAIL_COLLISION_FILTER_NAME + "#" + i;

            // Add collision to Area2D
            area.AddChild(collision);
        }
    }
}
