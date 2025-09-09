using Godot;
using static Godot.GD;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Xml;
using System.ComponentModel;
using System.Collections.Generic;

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
	NOWHERE
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
	public DMS longDMS;
	public DMS latDMS;
	public Vector2 elevation { get; set; }
	public int trailJunctionIndex;
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
}

public class Gpx
{
	// x : index, y : value	
	public GpxProperties[] TrackPoints { get; set; }
	public List<GpxTrailJunction> trailJunctions;

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
		int deg = (int) sec / 3600;
		sec = Math.Abs(sec % 3600);
		int min = (int) sec / 60;
		sec %= 60;

		return (deg, min, sec);
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
			Print($"First node: {root.Name}");

			var namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
			namespaceManager.AddNamespace("a", "http://www.topografix.com/GPX/1/1");

			XmlNodeList segments = xmlDoc.SelectNodes("//a:gpx/a:trk/a:trkseg/a:trkpt", namespaceManager);
			Print($"nb segments: {segments.Count}");
			TrackPoints = new GpxProperties[segments.Count];

			int i = 0; // counter to store trackpoints
			foreach (XmlNode segment in segments)
			{
				// TODO: don't use 2000.00f
				TrackPoints[i].elevation = new Vector2(i, 2000.00f + (float.Parse(segment["ele"].InnerText, CultureInfo.InvariantCulture.NumberFormat) * -1.00f));
				TrackPoints[i].trailJunctionIndex = -1;
				float longitude = float.Parse(segment.Attributes["lon"].Value, CultureInfo.InvariantCulture.NumberFormat);
				float latitude = float.Parse(segment.Attributes["lat"].Value, CultureInfo.InvariantCulture.NumberFormat);

				var result = getDMSFromDecimal(longitude);
				TrackPoints[i].longDMS.degree = result.degree;
				TrackPoints[i].longDMS.min = result.min;
				TrackPoints[i].longDMS.sec = result.sec;
				result = getDMSFromDecimal(latitude);
				TrackPoints[i].latDMS.degree = result.degree;
				TrackPoints[i].latDMS.min = result.min;
				TrackPoints[i].latDMS.sec = result.sec;
				TrackPoints[i].coord = new Vector2(latitude, longitude);
				XmlNode xTrailJunction = segment.SelectSingleNode("a:extensions/a:trailjunction", namespaceManager);

				if (xTrailJunction != null)
				{
					GD.Print($"Extensions-Trailjunction");
					if (trailJunctions == null)
						trailJunctions = new List<GpxTrailJunction>();

					GpxTrailJunction trailJunction = new GpxTrailJunction();

					if (xTrailJunction.SelectNodes("name") != null)
						trailJunction.name = xTrailJunction["name"].InnerText;
					else
						trailJunction.name = "Elsewhere";
					GD.Print($"trailjunction.name {trailJunction.name}");

					XmlNodeList destinations = segment.SelectNodes("a:extensions/a:trailjunction/a:destination", namespaceManager);
					foreach (XmlNode destination in destinations)
					{
						GpxDestination dest = new GpxDestination();

						dest.name = destination.Attributes["name"].Value;
						dest.gpxFile = destination["gpx"].InnerText;
						dest.distance = float.Parse(destination["distance"].InnerText, CultureInfo.InvariantCulture.NumberFormat);
						dest.direction = strToDirection(destination["direction"].InnerText);
						dest.trail = destination["trail"].InnerText;

						GD.Print($"dest gpx: {destination["gpx"].InnerText} name: {destination.Attributes["name"].Value}");
						if (trailJunction.destinations == null)
							trailJunction.destinations = new List<GpxDestination>();
						trailJunction.destinations.Add(dest);
					}

					int newIndex = trailJunctions.Count;
					TrackPoints[i].trailJunctionIndex = newIndex;
					trailJunctions.Add(trailJunction);
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

