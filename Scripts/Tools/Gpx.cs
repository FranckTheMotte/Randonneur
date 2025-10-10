using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Globalization;
using System.IO;
using System.Xml;
using Godot;
using static Godot.GD;

// This class load a gpx file to:
// - convert lat and lon to coord (x,y) for a map (top view)
// - convert elevation to coord (x,y) for an elevation profile

// A gpx file can contains reference to another gpx files.

public enum Direction
{
    E,
    W,
    N,
    S,
    NE,
    NW,
    SE,
    SW,
    NOWHERE,
}

public struct DMS
{
    public int degree;
    public int min;
    public float sec;
}

public struct GpxProperties
{
    public Vector2 coord { get; set; }
    public float distanceToNext; // distance to next gps point (meter)
    public DMS longDMS;
    public DMS latDMS;
    public Vector2 elevation { get; set; }
    public int trailJunctionIndex;
    public GpxWaypoint? Waypoint; // waypoint linked to this coordinate (can be null)
}

public struct GpxDestination
{
    public string gpxFile;
    public string name;
    public float distance;
    public Direction direction;
    public string trail;
}

public partial class GpxTrailJunction : GodotObject
{
    public string? name;
    public List<GpxDestination> destinations = [];
    internal float distance; // Distance from start (meter)
}

public class Gpx
{
    // x : index, y : value
    public GpxProperties[]? m_trackPoints { get; set; }

    // waypoints from the gpx file
    public readonly GpxWaypoints Waypoints = new();

    public List<GpxTrailJunction>? m_trailJunctions;

    internal float maxX; // length of the track (meters)

    // Number of pixels by meter
    public const float PixelMeter = 5.0f;

    // Max elevation in meters
    public const float ElevationMax = 10000.0f;

    // Max elevation in pixels
    public const float PixelElevationMax = ElevationMax * PixelMeter;

    // Default name for a missing name
    internal const string DefaultName = "Elsewhere...";

    // Impossible value for a geographical coordinate
    internal const float UnitializedCoord = 777.77f;

    // Waypoint tag to describe a junction
    internal const string TagJunction = "Croisement -";

    private Direction strToDirection(string directionStr)
    {
        Direction result;

        switch (directionStr)
        {
            case "N":
                result = Direction.N;
                break;
            case "S":
                result = Direction.S;
                break;
            case "E":
                result = Direction.E;
                break;
            case "W":
                result = Direction.W;
                break;
            case "NE":
                result = Direction.NE;
                break;
            case "NW":
                result = Direction.NW;
                break;
            case "SE":
                result = Direction.SE;
                break;
            case "SW":
                result = Direction.SW;
                break;
            default:
                result = Direction.NOWHERE;
                break;
        }

        return result;
    }

    private (int degree, int min, float sec) getDMSFromDecimal(float coord)
    {
        float sec = (float)Math.Round(coord * 3600);
        int deg = (int)sec / 3600;
        sec = Math.Abs(sec % 3600);
        int min = (int)sec / 60;
        sec %= 60;

        return (deg, min, sec);
    }

    /**
        Retrieve distance in meter between two gps points.
        Use of  System.Device.Location;

        @param sLatitude  latitude of start point
        @param sLongitude longitude of start point
        @param sLatitude  latitude of end point
        @param sLongitude longitude of end point

        @return distance in meter
    */
    private float getDistance(float sLatitude, float sLongitude, float eLatitude, float eLongitude)
    {
        var sCoord = new GeoCoordinate(sLatitude, sLongitude);
        var eCoord = new GeoCoordinate(eLatitude, eLongitude);

        return (float)sCoord.GetDistanceTo(eCoord);
    }

    public bool Load(string filePath)
    {
        XmlDocument xmlDoc = new();

        // Read the file with godot (as res:// is not known by .NET framework)
        using var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            PrintErr("Failed to open xml file.");
            return false;
        }
        string xmlContent = file.GetAsText();

        // Load in XmlDocument through a string
        xmlDoc.Load(new StringReader(xmlContent));

        try
        {
            if (xmlDoc.FirstChild == null)
            {
                GD.PushWarning($"${nameof(Load)}: xml file is empty");
                return false;
            }
            XmlNode root = xmlDoc.FirstChild;
            var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
            namespaceManager.AddNamespace("a", "http://www.topografix.com/GPX/1/1");

            /* Load waypoints */
            XmlNodeList waypoints = xmlDoc.SelectNodes("//a:gpx/a:wpt", namespaceManager)!;
            if (waypoints != null)
            {
                foreach (XmlNode waypoint in waypoints)
                {
                    var eleWaypoint = waypoint["ele"];

                    // if some data are corrupted, the waypoint is ignored
                    if (eleWaypoint is null || waypoint.Attributes is null)
                    {
                        GD.PushWarning(
                            $"${nameof(Load)}: a waypoint of ${filePath} is corrupted, skip it."
                        );
                        continue;
                    }

                    // elevation
                    var elevation = float.Parse(
                        eleWaypoint.InnerText,
                        CultureInfo.InvariantCulture.NumberFormat
                    );

                    // coordinates
                    float longitude = UnitializedCoord;
                    float latitude = UnitializedCoord;
                    var lonWaypoint = waypoint.Attributes["lon"];
                    var latWaypoint = waypoint.Attributes["lat"];

                    if (lonWaypoint is not null)
                        longitude = float.Parse(
                            lonWaypoint.Value,
                            CultureInfo.InvariantCulture.NumberFormat
                        );

                    if (latWaypoint is not null)
                        latitude = float.Parse(
                            latWaypoint.Value,
                            CultureInfo.InvariantCulture.NumberFormat
                        );

                    if (longitude == UnitializedCoord || latitude == UnitializedCoord)
                    {
                        GD.PushWarning($"${nameof(Load)}: wrong waypoint coords (${filePath}).");
                        continue;
                    }
                    var coord = new Vector2(latitude, longitude);

                    // name (optional)
                    var nameWaypoint = waypoint["name"];
                    string name = DefaultName;
                    if (nameWaypoint is not null)
                        name = nameWaypoint.InnerText;
                    Waypoints.Add(coord, elevation, name);

                    //GD.Print($"wpt : ${name}");

                    if (name.Contains(TagJunction) == true)
                    {
                        m_trailJunctions ??= [];
                        GpxTrailJunction trailJunction = new()
                        {
                            name = name[(name.Find(TagJunction) + TagJunction.Length)..],
                        };

                        // desc section contains a list of connected traces (through their gpx file name)
                        string desc = "";
                        var descWaypoint = waypoint["desc"];
                        if (descWaypoint is not null)
                            desc = descWaypoint.InnerText;
                        string[] connectedTraces = desc.Split(';');
                        foreach (string trace in connectedTraces)
                        {
                            GpxDestination gpxDestination = new()
                            {
                                gpxFile = "res://data/Map1/" + trace,
                            };
                            trailJunction.destinations.Add(gpxDestination);
                        }
                        m_trailJunctions.Add(trailJunction);
                    }
                }
            }

            int i = 0; // counter to store trackpoints

            /* Load track points */
            XmlNodeList trackpoints = xmlDoc.SelectNodes(
                "//a:gpx/a:trk/a:trkseg/a:trkpt",
                namespaceManager
            )!;

            // no trackpoints? it's a problem
            if (trackpoints is null)
            {
                Waypoints.Clear();
                GD.PushError($"${nameof(Load)}: no trackpoint inside ${filePath}.");
                return false;
            }

            m_trackPoints = new GpxProperties[trackpoints.Count];

            i = 0; // counter to store trackpoints
            float y_ele = 0.0f;
            foreach (XmlNode trackpoint in trackpoints)
            {
                var eleTrackpoint = trackpoint["ele"];

                // if some data are corrupted, the waypoint is ignored
                if (eleTrackpoint is null || trackpoint.Attributes is null)
                {
                    GD.PushWarning(
                        $"${nameof(Load)}: a trackpoint of ${filePath} is corrupted, skip it."
                    );
                    continue;
                }
                var lonTrackpoint = trackpoint.Attributes["lon"];
                var latTrackpoint = trackpoint.Attributes["lat"];
                m_trackPoints[i].trailJunctionIndex = -1;
                float longitude = UnitializedCoord;
                float latitude = 0.00f;

                if (lonTrackpoint is not null)
                    longitude = float.Parse(
                        lonTrackpoint.Value,
                        CultureInfo.InvariantCulture.NumberFormat
                    );
                if (latTrackpoint is not null)
                    latitude = float.Parse(
                        latTrackpoint.Value,
                        CultureInfo.InvariantCulture.NumberFormat
                    );

                if (longitude == UnitializedCoord || latitude == UnitializedCoord)
                {
                    GD.PushWarning($"${nameof(Load)}: wrong trackpoint coords (${filePath}).");
                    continue;
                }

                var result = getDMSFromDecimal(longitude);
                m_trackPoints[i].longDMS.degree = result.degree;
                m_trackPoints[i].longDMS.min = result.min;
                m_trackPoints[i].longDMS.sec = result.sec;
                result = getDMSFromDecimal(latitude);
                m_trackPoints[i].latDMS.degree = result.degree;
                m_trackPoints[i].latDMS.min = result.min;
                m_trackPoints[i].latDMS.sec = result.sec;
                m_trackPoints[i].coord = new Vector2(latitude, longitude);
                m_trackPoints[i].Waypoint = Waypoints.GetWaypoint(m_trackPoints[i].coord);

                y_ele =
                    (
                        (
                            float.Parse(
                                eleTrackpoint.InnerText,
                                CultureInfo.InvariantCulture.NumberFormat
                            ) * PixelMeter
                        ) - PixelElevationMax
                    ) * -1; // y axis is inverted
                // distance between previous point and current (stored in previous)
                if (i > 0)
                {
                    m_trackPoints[i - 1].distanceToNext =
                        getDistance(
                            m_trackPoints[i - 1].coord.X,
                            m_trackPoints[i - 1].coord.Y,
                            latitude,
                            longitude
                        ) * PixelMeter;
                    //GD.Print($"distance {m_trackPoints[i-1].distanceToNext} maxX {maxX}");
                    m_trackPoints[i].elevation = new Vector2(
                        m_trackPoints[i - 1].elevation.X + m_trackPoints[i - 1].distanceToNext,
                        y_ele
                    );
                    maxX += m_trackPoints[i - 1].distanceToNext;
                }
                else
                {
                    // First coord, no distance to evaluate
                    m_trackPoints[i].elevation = new Vector2(0, y_ele);
                }
                i++;
            }
        }
        catch (System.Exception e)
        {
            PrintErr("Failed to read XML: " + e.Message);
            return false;
        }
        return true;
    }
}
