using System;
using System.Collections.Generic;
using System.Numerics;
using Godot;
using Randonneur;
/* Because of System.Numerics */
using Vector2 = Godot.Vector2;

/**
  Describe a list of gps waypoints.
*/
public class GpxWaypoints
{
    private readonly List<Waypoint> _gpxWayPoints = [];

    private Waypoint? noWaypoint = null;

    /**
        Retrieve waypoint with a latitude/longitude coordinate.
        Use of  System.Device.Location;

        @param coord latitude and longitude

        @return waypoint if found, null otherwise.
    */
    public Waypoint? GetWaypoint(Vector2 Coord)
    {
        foreach (Waypoint waypoint in _gpxWayPoints)
        {
            if (waypoint.GeographicCoord == Coord)
            {
                return waypoint;
            }
        }

        return null;
    }

    /// <summary>
    /// Store a new waypoint.
    /// </summary>
    /// <param name="coord">latitude and longitude</param>
    /// <param name="elevation">elevation in meter</param>
    /// <param name="name">litteral string</param>
    /// <param name="traceName">name of the trace (gpx file)</param>
    internal void Add(Vector2 Coord, float Elevation, string Name, string TraceName)
    {
        Waypoint waypoint = new()
        {
            GeographicCoord = Coord,
            Elevation = Elevation,
            Name = Name,
            TraceName = TraceName,
        };

        _gpxWayPoints.Add(waypoint);
    }

    /**
        Clear all stored waypoints.
    */
    internal void Clear()
    {
        _gpxWayPoints.Clear();
    }

    /// <summary>
    /// Set the landmark of a waypoint.
    /// </summary>
    /// <param name="Coord">Coordinate of the waypoint.</param>
    /// <param name="Landmark">ColorRect of the landmark.</param>
    internal void SetWaypointLandmark(Vector2 Coord, ColorRect Landmark)
    {
        foreach (Waypoint waypoint in _gpxWayPoints)
        {
            if (waypoint.GeographicCoord == Coord)
            {
                waypoint.Landmark = Landmark;
            }
        }
    }

    /// <summary>
    /// Set the visibility of all waypoints' landmarks.
    /// </summary>
    /// <param name="value">true to show all landmarks, false to hide them.</param>
    internal void SetAllLandmarkVisibility(bool value)
    {
        foreach (Waypoint waypoint in _gpxWayPoints)
        {
            if (waypoint.Landmark != null)
                waypoint.Landmark.Visible = value;
        }
    }
}
