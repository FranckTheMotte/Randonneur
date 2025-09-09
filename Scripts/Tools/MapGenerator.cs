using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

/* Because of System.Numerics */
using Vector2 = Godot.Vector2;

public partial class MapGenerator : Node
{
	// Structure pour représenter un point GPS
	public struct GpsPoint
	{
		public double Latitude;  // -90 à +90 (Sud à Nord)
		public double Longitude; // -180 à +180 (Ouest à Est)

		public GpsPoint(double lat, double lon)
		{
			Latitude = lat;
			Longitude = lon;
		}
	}

	// Structure pour représenter une zone géographique
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
	Vector2 screenSize = new Vector2(100, 200);
	GeoBounds mapArea = new GeoBounds(
		minLat: 42.765229, maxLat: 42.766700,  // Sud à Nord
		minLon: 1.482700, maxLon: 1.483030     // Ouest à Est
	);

	// MÉTHODE 1: Projection linéaire simple (pour petites zones)
	public static Vector2 GpsToScreenLinear(GpsPoint gpsPoint, GeoBounds bounds, Vector2 screenSize)
	{
		// Normaliser les coordonnées GPS entre 0 et 1
		double normalizedLon = (gpsPoint.Longitude - bounds.MinLongitude) /
							  (bounds.MaxLongitude - bounds.MinLongitude);

		double normalizedLat = (gpsPoint.Latitude - bounds.MinLatitude) /
							  (bounds.MaxLatitude - bounds.MinLatitude);

		// Convertir en coordonnées écran
		float screenX = (float)(normalizedLon * screenSize.X);
		float screenY = (float)((1.0 - normalizedLat) * screenSize.Y); // Inverser Y car écran (0,0) en haut
		return new Vector2(screenX, screenY);
	}
	public Area2D generateMap(string dir)
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

		string[] gpxFiles = Directory.GetFiles(ProjectSettings.GlobalizePath(dir));
		foreach (string gpxFileName in gpxFiles)
		{
			if (!gpxFileName.Contains("traceG.gpx"))
				continue;
			Gpx trail;
			/* Generate a profil from a gpx file */
			trail = new Gpx();
			trail.Load(gpxFileName);
			Line2D trailLine = new Line2D();
			Vector2[] trace = new Vector2[trail.TrackPoints.Length];
			int i = 0;

			foreach (GpxProperties gpxPoint in trail.TrackPoints)
			{
				GpsPoint gpsPoint = new GpsPoint(gpxPoint.coord.X, gpxPoint.coord.Y);
				trace[i] = GpsToScreenLinear(gpsPoint, mapArea, screenSize);
				GD.Print($" gpxPoint.coord.X: {gpxPoint.coord.X} gpxPoint.coord.Y:  {gpxPoint.coord.Y}");
				GD.Print($" X: {trace[i].X} Y:  {trace[i].Y}");
				i++;
			}

			// TODO This default name must be public and global
			trailLine.Name = "theTrail";

			trailLine.Points = trace;
			// TODO 10 must be global
			trailLine.Width = 10;
			// TODO This default color must be public and global
			trailLine.DefaultColor = Colors.Orange;

			// Area2D for collisions detection
			Area2D area = new Area2D();
			area.AddChild(trailLine);

			// Add collisions
			CreateSegmentCollisions(area, trailLine);

			return area;
		}
		return new Area2D();
	}

    private List<CollisionShape2D> collisions = new List<CollisionShape2D>();

	private void CreateSegmentCollisions(Area2D area, Line2D line)
	{
		collisions.Clear();
		// Créer une collision pour chaque segment de la ligne
		for (int i = 0; i < line.Points.Length - 1; i++)
		{
			Vector2 start = line.Points[i];
			Vector2 end = line.Points[i + 1];

			CollisionShape2D collision = new CollisionShape2D();
			RectangleShape2D rectangle = new RectangleShape2D();

			// Calculer la longueur et l'angle du segment
			Vector2 direction = end - start;
			float length = direction.Length();
			float angle = direction.Angle();

			// TODO: put 10 as a global parameter
			rectangle.Size = new Vector2(10, length);


			// Position and collision angle
			collision.Shape = rectangle;
			collision.Position = (start + end) / 2.0f;
			// 90° shift because the default orientation of the collisionShape
			// is different of the godot reference.
			collision.Rotation = angle + Mathf.DegToRad(90);

			// Ajouter la collision à l'Area2D
			area.AddChild(collision);
			collisions.Add(collision);
		}
	}
}
