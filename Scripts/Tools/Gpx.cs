using System;
// This class load a gpx file to:
// - convert lat and lon to coord (x,y) for a map (top view)
// - convert elevation to coord (x,y) for an elevation profile

// A gpx file can contains reference to another gpx files.

/*
 Licensed under the Apache License, Version 2.0

 http://www.apache.org/licenses/LICENSE-2.0
 */

using System.Collections.Generic;
using System.Device.Location;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Godot;
using static Godot.GD;

namespace XmlGpx
{
    [XmlRoot(ElementName = "copyright", Namespace = "http://www.topografix.com/GPX/1/1")]
    public class Copyright
    {
        [XmlElement(ElementName = "license", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? License { get; set; }

        [XmlAttribute(AttributeName = "author")]
        public string? Author { get; set; }
    }

    [XmlRoot(ElementName = "bounds", Namespace = "http://www.topografix.com/GPX/1/1")]
    public class Bounds
    {
        [XmlAttribute(AttributeName = "minlat")]
        public string? Minlat { get; set; }

        [XmlAttribute(AttributeName = "minlon")]
        public string? Minlon { get; set; }

        [XmlAttribute(AttributeName = "maxlat")]
        public string? Maxlat { get; set; }

        [XmlAttribute(AttributeName = "maxlon")]
        public string? Maxlon { get; set; }
    }

    [XmlRoot(ElementName = "metadata", Namespace = "http://www.topografix.com/GPX/1/1")]
    public class Metadata
    {
        [XmlElement(ElementName = "name", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? Name { get; set; }

        [XmlElement(ElementName = "desc", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? Desc { get; set; }

        [XmlElement(ElementName = "copyright", Namespace = "http://www.topografix.com/GPX/1/1")]
        public Copyright? Copyright { get; set; }

        [XmlElement(ElementName = "time", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? Time { get; set; }

        [XmlElement(ElementName = "bounds", Namespace = "http://www.topografix.com/GPX/1/1")]
        public Bounds? Bounds { get; set; }
    }

    [XmlRoot(ElementName = "extensions", Namespace = "http://www.topografix.com/GPX/1/1")]
    public class Extensions
    {
        [XmlElement(ElementName = "type", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? Type { get; set; }

        [XmlElement(ElementName = "junctions", Namespace = "http://www.topografix.com/GPX/1/1")]
        public Junctions? Junctions { get; set; }

        [XmlElement(ElementName = "start", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? Start { get; set; }
    }

    [XmlRoot(ElementName = "wpt", Namespace = "http://www.topografix.com/GPX/1/1")]
    public class Wpt
    {
        [XmlElement(ElementName = "ele", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? Ele { get; set; }

        [XmlElement(ElementName = "name", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? Name { get; set; }

        [XmlElement(ElementName = "cmt", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? Cmt { get; set; }

        [XmlElement(ElementName = "desc", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? Desc { get; set; }

        [XmlElement(ElementName = "sym", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? Sym { get; set; }

        [XmlElement(ElementName = "type", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? Type { get; set; }

        [XmlElement(ElementName = "extensions", Namespace = "http://www.topografix.com/GPX/1/1")]
        public Extensions? Extensions { get; set; }

        [XmlAttribute(AttributeName = "lat")]
        public string? Lat { get; set; }

        [XmlAttribute(AttributeName = "lon")]
        public string? Lon { get; set; }
    }

    [XmlRoot(ElementName = "junctions", Namespace = "http://www.topografix.com/GPX/1/1")]
    public class Junctions
    {
        [XmlElement(ElementName = "gpxfile", Namespace = "http://www.topografix.com/GPX/1/1")]
        public List<string>? Gpxfile { get; set; }
    }

    [XmlRoot(ElementName = "trkpt", Namespace = "http://www.topografix.com/GPX/1/1")]
    public class Trkpt
    {
        [XmlElement(ElementName = "ele", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? Ele { get; set; }

        [XmlAttribute(AttributeName = "lat")]
        public string? Lat { get; set; }

        [XmlAttribute(AttributeName = "lon")]
        public string? Lon { get; set; }
    }

    [XmlRoot(ElementName = "trkseg", Namespace = "http://www.topografix.com/GPX/1/1")]
    public class Trkseg
    {
        [XmlElement(ElementName = "trkpt", Namespace = "http://www.topografix.com/GPX/1/1")]
        public List<Trkpt>? Trkpt { get; set; }
    }

    [XmlRoot(ElementName = "trk", Namespace = "http://www.topografix.com/GPX/1/1")]
    public class Trk
    {
        [XmlElement(ElementName = "name", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? Name { get; set; }

        [XmlElement(ElementName = "desc", Namespace = "http://www.topografix.com/GPX/1/1")]
        public string? Desc { get; set; }

        [XmlElement(ElementName = "trkseg", Namespace = "http://www.topografix.com/GPX/1/1")]
        public Trkseg? Trkseg { get; set; }
    }

    [XmlRoot(ElementName = "gpx", Namespace = "http://www.topografix.com/GPX/1/1")]
    public class XmlGpx
    {
        [XmlElement(ElementName = "metadata", Namespace = "http://www.topografix.com/GPX/1/1")]
        public Metadata? Metadata { get; set; }

        [XmlElement(ElementName = "wpt", Namespace = "http://www.topografix.com/GPX/1/1")]
        public List<Wpt>? Wpt { get; set; }

        [XmlElement(ElementName = "trk", Namespace = "http://www.topografix.com/GPX/1/1")]
        public Trk? Trk { get; set; }

        [XmlAttribute(AttributeName = "version")]
        public string? Version { get; set; }

        [XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string? Xsi { get; set; }

        [XmlAttribute(AttributeName = "xmlns")]
        public string? Xmlns { get; set; }

        [XmlAttribute(
            AttributeName = "schemaLocation",
            Namespace = "http://www.w3.org/2001/XMLSchema-instance"
        )]
        public string? SchemaLocation { get; set; }

        [XmlAttribute(AttributeName = "gpx_style", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string? Gpx_style { get; set; }

        [XmlAttribute(AttributeName = "gpxtpx", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string? Gpxtpx { get; set; }

        [XmlAttribute(AttributeName = "gpxx", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string? Gpxx { get; set; }

        [XmlAttribute(AttributeName = "randonneur", Namespace = "http://www.w3.org/2000/xmlns/")]
        public string? Randonneur { get; set; }

        [XmlAttribute(AttributeName = "creator")]
        public string? Creator { get; set; }
    }

    public class GPXFile
    {
        [XmlArray("wpt")]
        [XmlArrayItem("wpt")]
        public Wpt[]? Waypoints { get; set; }
    }

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

    public struct GpxProperties
    {
        public Vector2 Coord { get; set; }
        public float DistanceToNext; // distance to next gps point (meter)
        public Vector2 Elevation { get; set; }
        public int TrailJunctionIndex;
        public GpxWaypoint? Waypoint; // waypoint linked to this coordinate (can be null)
    }

    public struct GpxDestination
    {
        public string GpxFile;
        public string Name;
        public float Distance;
        public Direction Direction;
        public string Trail;
    }

    public partial class GpxTrailJunction : GodotObject
    {
        public string? Name;
        public List<GpxDestination> Destinations = [];
        internal float Distance; // Distance from start (meter)
    }

    public class Gpx
    {
        // x : index, y : value
        public GpxProperties[]? TrackPoints { get; set; }

        // waypoints from the gpx file
        public readonly GpxWaypoints Waypoints = new();

        public List<GpxTrailJunction>? TrailJunctions;

        internal float MaxX; // length of the track (meters)

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
        internal const string JunctionType = "junction";

        // Waypoint tag to describe a Point of View
        internal const string PovType = "pov";

        /// <summary>
        /// Convert a string representation of a direction to a Direction enum value.
        /// </summary>
        /// <param name="directionStr">String representation of a direction.</param>
        /// <returns>Direction enum value corresponding to the input string.</returns>
        /// <remarks>
        /// Supported direction strings are:
        ///     "N", "S", "E", "W", "NE", "NW", "SE", "SW".
        ///     Any other string will result in Direction.NOWHERE being returned.
        /// </remarks>
        private static Direction StrToDirection(string directionStr)
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

        /// <summary>
        /// Retrieve distance in meter between two gps points.
        /// </summary>
        /// <param name="sLatitude">Latitude of start point</param>
        /// <param name="sLongitude">Longitude of start point</param>
        /// <param name="eLatitude">Latitude of end point</param>
        /// <param name="eLongitude">Longitude of end point</param>
        /// <returns>Distance in meter</returns>
        /// <remarks>
        /// Use of  System.Device.Location;
        /// </remarks>
        private static float GetDistance(
            float sLatitude,
            float sLongitude,
            float eLatitude,
            float eLongitude
        )
        {
            var sCoord = new GeoCoordinate(sLatitude, sLongitude);
            var eCoord = new GeoCoordinate(eLatitude, eLongitude);

            return (float)sCoord.GetDistanceTo(eCoord);
        }

        /// <summary>
        /// Load a gpx file.
        /// </summary>
        /// <param name="xmlFile">Full godot path to the gpx file.</param>
        /// <returns>true if the gpx file is loaded successfully, false otherwise.</returns>
        /// <remarks>
        /// The gpx file is deserialized into an XmlGpx object.
        /// The waypoints are then loaded into the Waypoints property.
        /// </remarks>
        public bool Load(string xmlFile)
        {
            XmlGpx gpx = new();
            xmlFile = ProjectSettings.GlobalizePath(xmlFile);

            // Deserialize to object
            XmlSerializer serializer = new(typeof(XmlGpx));
            using (FileStream stream = File.OpenRead(xmlFile))
            {
                gpx = (XmlGpx)serializer.Deserialize(stream)!;
            }

            if (gpx.Wpt != null)
            {
                foreach (Wpt waypoint in gpx.Wpt)
                {
                    var ele = waypoint.Ele;

                    // if some data are corrupted, the waypoint is ignored
                    if (waypoint.Ele is null)
                    {
                        GD.PushWarning(
                            $"${nameof(Load)}: a waypoint of ${xmlFile} is corrupted, skip it."
                        );
                        continue;
                    }

                    // elevation
                    var elevation = float.Parse(
                        waypoint.Ele,
                        CultureInfo.InvariantCulture.NumberFormat
                    );

                    // coordinates
                    float longitude = UnitializedCoord;
                    float latitude = UnitializedCoord;

                    if (waypoint.Lon is not null)
                        longitude = float.Parse(
                            waypoint.Lon,
                            CultureInfo.InvariantCulture.NumberFormat
                        );

                    if (waypoint.Lat is not null)
                        latitude = float.Parse(
                            waypoint.Lat,
                            CultureInfo.InvariantCulture.NumberFormat
                        );

                    if (longitude == UnitializedCoord || latitude == UnitializedCoord)
                    {
                        GD.PushWarning($"${nameof(Load)}: wrong waypoint coords (${xmlFile}).");
                        continue;
                    }
                    var coord = new Vector2(latitude, longitude);

                    // name (optional)
                    string wptName = DefaultName;
                    if (waypoint.Name is not null)
                        wptName = waypoint.Name;
                    Waypoints.Add(coord, elevation, wptName);

                    // Extensions specific to randonneur
                    if (waypoint.Extensions != null)
                    {
                        GD.Print($"extension type : {waypoint.Extensions.Type}");
                        string? type = waypoint.Extensions.Type;
                        if (type != null)
                        {
                            switch (type)
                            {
                                case JunctionType:
                                    TrailJunctions ??= [];
                                    GpxTrailJunction trailJunction = new() { Name = wptName };
                                    Junctions? junctions = waypoint.Extensions.Junctions;
                                    if (junctions != null && junctions.Gpxfile != null)
                                    {
                                        foreach (string gpxFile in junctions.Gpxfile)
                                        {
                                            GD.Print($"gpxFile : {gpxFile}");
                                            GpxDestination gpxDestination = new()
                                            {
                                                GpxFile = "res://data/Map1/" + gpxFile,
                                            };
                                            trailJunction.Destinations.Add(gpxDestination);
                                        }
                                        TrailJunctions.Add(trailJunction);
                                    }
                                    break;
                                case PovType:
                                    break;
                            }
                        }
                    }
                }
            }

            // no trackpoints? it's a problem
            if (gpx.Trk == null || gpx.Trk.Trkseg == null || gpx.Trk.Trkseg.Trkpt == null)
            {
                Waypoints.Clear();
                GD.PushError($"${nameof(Load)}: no trackpoint inside ${xmlFile}.");
                return false;
            }

            /* Load track points */
            TrackPoints = new GpxProperties[gpx.Trk.Trkseg.Trkpt.Count];

            int i = 0; // counter to store trackpoints
            float y_ele = 0.0f;
            foreach (Trkpt trackpoint in gpx.Trk.Trkseg.Trkpt)
            {
                // if some data are corrupted, the waypoint is ignored
                if (trackpoint.Ele is null)
                {
                    GD.PushWarning(
                        $"${nameof(Load)}: a trackpoint of ${xmlFile} is corrupted, skip it."
                    );
                    continue;
                }
                TrackPoints[i].TrailJunctionIndex = -1;
                float longitude = UnitializedCoord;
                float latitude = 0.00f;

                if (trackpoint.Lon is not null)
                    longitude = float.Parse(
                        trackpoint.Lon,
                        CultureInfo.InvariantCulture.NumberFormat
                    );
                if (trackpoint.Lat is not null)
                    latitude = float.Parse(
                        trackpoint.Lat,
                        CultureInfo.InvariantCulture.NumberFormat
                    );

                if (longitude == UnitializedCoord || latitude == UnitializedCoord)
                {
                    GD.PushWarning($"${nameof(Load)}: wrong trackpoint coords (${xmlFile}).");
                    continue;
                }

                TrackPoints[i].Coord = new Vector2(latitude, longitude);
                TrackPoints[i].Waypoint = Waypoints.GetWaypoint(TrackPoints[i].Coord);

                y_ele =
                    (
                        (
                            float.Parse(trackpoint.Ele, CultureInfo.InvariantCulture.NumberFormat)
                            * PixelMeter
                        ) - PixelElevationMax
                    ) * -1; // y axis is inverted
                // distance between previous point and current (stored in previous)
                if (i > 0)
                {
                    TrackPoints[i - 1].DistanceToNext =
                        GetDistance(
                            TrackPoints[i - 1].Coord.X,
                            TrackPoints[i - 1].Coord.Y,
                            latitude,
                            longitude
                        ) * PixelMeter;
                    //GD.Print($"distance {m_trackPoints[i-1].distanceToNext} maxX {maxX}");
                    TrackPoints[i].Elevation = new Vector2(
                        TrackPoints[i - 1].Elevation.X + TrackPoints[i - 1].DistanceToNext,
                        y_ele
                    );
                    MaxX += TrackPoints[i - 1].DistanceToNext;
                }
                else
                {
                    // First coord, no distance to evaluate
                    TrackPoints[i].Elevation = new Vector2(0, y_ele);
                }
                i++;
            }
            return true;
        }
    }
}
