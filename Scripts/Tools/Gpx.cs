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

public struct GpxWaypoint
{
    public Vector2 coord { get; set; }
    public float elevation { get; set; }
    public String waypoint;
}

public struct GpxProperties
{
    public Vector2 coord { get; set; }
    public float distanceToNext; // distance to next gps point (meter)
    public DMS longDMS;
    public DMS latDMS;
    public Vector2 elevation { get; set; }
    public int trailJunctionIndex;
    public String waypoint;
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
    public string name;
    public List<GpxDestination> destinations;
    internal float distance; // Distance from start (meter)
}

public class Gpx
{
    // x : index, y : value
    public GpxProperties[] m_trackPoints { get; set; }
    private GpxWaypoint[] m_gpxWayPoints;
    public List<GpxTrailJunction> m_trailJunctions;
    internal float maxX; // length of the track (meters)

    // Number of pixels by meter
    public const float PIXEL_METER = 1.0f;

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

    private String getWaypointName(float latitude, float longitude)
    {
        foreach (var waypoint in m_gpxWayPoints)
        {
            if (waypoint.coord.X == latitude && waypoint.coord.Y == longitude)
                return waypoint.waypoint;
        }
        return "";
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
        XmlDocument xmlDoc = new XmlDocument();

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
            XmlNode root = xmlDoc.FirstChild;

            var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
            namespaceManager.AddNamespace("a", "http://www.topografix.com/GPX/1/1");

            /* Load waypoints */
            XmlNodeList waypoints = xmlDoc.SelectNodes("//a:gpx/a:wpt", namespaceManager);
            m_gpxWayPoints = new GpxWaypoint[waypoints.Count];
            int i = 0; // counter to store trackpoints
            foreach (XmlNode waypoint in waypoints)
            {
                m_gpxWayPoints[i].elevation = float.Parse(
                    waypoint["ele"].InnerText,
                    CultureInfo.InvariantCulture.NumberFormat
                );
                float longitude = float.Parse(
                    waypoint.Attributes["lon"].Value,
                    CultureInfo.InvariantCulture.NumberFormat
                );
                float latitude = float.Parse(
                    waypoint.Attributes["lat"].Value,
                    CultureInfo.InvariantCulture.NumberFormat
                );
                m_gpxWayPoints[i].coord = new Vector2(latitude, longitude);
                m_gpxWayPoints[i].waypoint = waypoint["name"].InnerText;
                i++;
            }

            /* Load track points */
            XmlNodeList trackpoints = xmlDoc.SelectNodes(
                "//a:gpx/a:trk/a:trkseg/a:trkpt",
                namespaceManager
            );
            m_trackPoints = new GpxProperties[trackpoints.Count];

            i = 0; // counter to store trackpoints
            foreach (XmlNode trackpoint in trackpoints)
            {
                m_trackPoints[i].trailJunctionIndex = -1;
                float longitude = float.Parse(
                    trackpoint.Attributes["lon"].Value,
                    CultureInfo.InvariantCulture.NumberFormat
                );
                float latitude = float.Parse(
                    trackpoint.Attributes["lat"].Value,
                    CultureInfo.InvariantCulture.NumberFormat
                );

                var result = getDMSFromDecimal(longitude);
                m_trackPoints[i].longDMS.degree = result.degree;
                m_trackPoints[i].longDMS.min = result.min;
                m_trackPoints[i].longDMS.sec = result.sec;
                result = getDMSFromDecimal(latitude);
                m_trackPoints[i].latDMS.degree = result.degree;
                m_trackPoints[i].latDMS.min = result.min;
                m_trackPoints[i].latDMS.sec = result.sec;
                m_trackPoints[i].coord = new Vector2(latitude, longitude);
                m_trackPoints[i].waypoint = getWaypointName(latitude, longitude);

                // distance between previous point and current (stored in previous)
                if (i > 0)
                {
                    m_trackPoints[i - 1].distanceToNext =
                        getDistance(
                            m_trackPoints[i - 1].coord.X,
                            m_trackPoints[i - 1].coord.Y,
                            latitude,
                            longitude
                        ) * PIXEL_METER;
                    //GD.Print($"distance {m_trackPoints[i-1].distanceToNext} maxX {maxX}");
                    // TODO: don't use 2000.00f
                    m_trackPoints[i].elevation = new Vector2(
                        m_trackPoints[i - 1].elevation.X + m_trackPoints[i - 1].distanceToNext,
                        2000.00f
                            + (
                                float.Parse(
                                    trackpoint["ele"].InnerText,
                                    CultureInfo.InvariantCulture.NumberFormat
                                ) * -1.00f
                            )
                    );
                    maxX += m_trackPoints[i - 1].distanceToNext;
                }
                else
                {
                    // First coord
                    // TODO: don't use 2000.00f
                    m_trackPoints[i].elevation = new Vector2(
                        0,
                        2000.00f
                            + (
                                float.Parse(
                                    trackpoint["ele"].InnerText,
                                    CultureInfo.InvariantCulture.NumberFormat
                                ) * -1.00f
                            )
                    );
                }

                XmlNode xTrailJunction = trackpoint.SelectSingleNode(
                    "a:extensions/a:trailjunction",
                    namespaceManager
                );
                if (xTrailJunction != null)
                {
                    GD.Print($"Extensions-Trailjunction");
                    if (m_trailJunctions == null)
                        m_trailJunctions = new List<GpxTrailJunction>();

                    GpxTrailJunction trailJunction = new GpxTrailJunction();

                    if (xTrailJunction.SelectNodes("name") != null)
                        trailJunction.name = xTrailJunction["name"].InnerText;
                    else
                        trailJunction.name = "Elsewhere";
                    GD.Print($"trailjunction.name {trailJunction.name}");

                    XmlNodeList destinations = trackpoint.SelectNodes(
                        "a:extensions/a:trailjunction/a:destination",
                        namespaceManager
                    );
                    foreach (XmlNode destination in destinations)
                    {
                        GpxDestination dest = new GpxDestination();

                        dest.name = destination.Attributes["name"].Value;
                        dest.gpxFile = destination["gpx"].InnerText;
                        dest.distance = float.Parse(
                            destination["distance"].InnerText,
                            CultureInfo.InvariantCulture.NumberFormat
                        );
                        dest.direction = strToDirection(destination["direction"].InnerText);
                        dest.trail = destination["trail"].InnerText;

                        //GD.Print($"dest gpx: {destination["gpx"].InnerText} name: {destination.Attributes["name"].Value}");
                        if (trailJunction.destinations == null)
                            trailJunction.destinations = new List<GpxDestination>();
                        trailJunction.destinations.Add(dest);
                    }
                    trailJunction.distance = maxX;

                    int newIndex = m_trailJunctions.Count;
                    m_trackPoints[i].trailJunctionIndex = newIndex;
                    m_trailJunctions.Add(trailJunction);
                }
                i++;
            }
            GD.Print($"Maxx {maxX}");
        }
        catch (System.Exception e)
        {
            PrintErr("Failed to read XML: " + e.Message);
            return false;
        }
        return true;
    }
}
