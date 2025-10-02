using System;
using System.Collections.Generic;
using System.Numerics;
/* Because of System.Numerics */
using Vector2 = Godot.Vector2;

/**
  Describe a gps waypoint from a gpx file.
*/
public class GpxWaypoint
{
    // X as latitude, Y as longitude
    public Vector2 Coord { get; set; }

    // elevation in meter
    public float Elevation { get; set; }

    // litteral name (optional)
    public string Name = "";
}

/**
  Describe a list of gps waypoints.
*/
public class GpxWaypoints
{
    private readonly List<GpxWaypoint> _gpxWayPoints = [];

    /**
        Retrieve waypoint with a latitude/longitude coordinate.
        Use of  System.Device.Location;

        @param coord latitude and longitude

        @return waypoint if found, null otherwise.
    */
    public GpxWaypoint? GetWaypoint(Vector2 Coord)
    {
        foreach (GpxWaypoint waypoint in _gpxWayPoints)
        {
            if (waypoint.Coord == Coord)
            {
                return waypoint;
            }
        }

        return null;
    }

    /**
        Store a new waypoint.

        @param coord     latitude and longitude
        @param elevation elevation in meter
        @param name      litteral string
    */
    internal void Add(Vector2 Coord, float Elevation, string Name)
    {
        GpxWaypoint waypoint = new();

        waypoint.Coord = Coord;
        waypoint.Elevation = Elevation;
        waypoint.Name = Name;

        _gpxWayPoints.Add(waypoint);
    }

    /**
        Clear all stored waypoints.
    */
    internal void Clear()
    {
        _gpxWayPoints.Clear();
    }
}
