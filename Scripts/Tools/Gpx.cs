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

public struct GpxProperties
{
	public Vector2 Elevation { get; set; }
	public int crossroadIndex;
}

public struct GpxDestination
{
	public string gpxFile;
	public string name;
	public float distance;
	public Direction direction;
	public string trail;
}

public partial class GpxCrossRoad : GodotObject
{
	public string name;
	public List<GpxDestination> destinations;
}

public class Gpx
{
	// x : index, y : value	
	public GpxProperties[] TrackPoints { get; set; }
	public List<GpxCrossRoad> crossRoads;

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
				//Print($"Segment[{i}]: {segment["ele"].InnerText}");
				// TODO: don't use 2000.00f
				TrackPoints[i].Elevation = new Vector2(i, 2000.00f + (float.Parse(segment["ele"].InnerText, CultureInfo.InvariantCulture.NumberFormat) * -1.00f));
				TrackPoints[i].crossroadIndex = -1;
				XmlNode xCrossroad = segment.SelectSingleNode("a:extensions/a:crossroad", namespaceManager);

				if (xCrossroad != null)
				{
					GD.Print($"Extensions-Crossroad");
					if (crossRoads == null)
						crossRoads = new List<GpxCrossRoad>();

					GpxCrossRoad crossroad = new GpxCrossRoad();

					if (xCrossroad.SelectNodes("name") != null)
						crossroad.name = xCrossroad["name"].InnerText;
					else
						crossroad.name = "Elsewhere";
					GD.Print($"crossroad.name {crossroad.name}");

					XmlNodeList destinations = segment.SelectNodes("a:extensions/a:crossroad/a:destination", namespaceManager);
					foreach (XmlNode destination in destinations)
					{
						GpxDestination dest = new GpxDestination();

						dest.name = destination.Attributes["name"].Value;
						dest.gpxFile = destination["gpx"].InnerText;
						dest.distance = float.Parse(destination["distance"].InnerText, CultureInfo.InvariantCulture.NumberFormat);
						dest.direction = strToDirection(destination["direction"].InnerText);
						dest.trail = destination["trail"].InnerText;

						GD.Print($"dest gpx: {destination["gpx"].InnerText} name: {destination.Attributes["name"].Value}");
						if (crossroad.destinations == null)
							crossroad.destinations = new List<GpxDestination>();
						crossroad.destinations.Add(dest);
					}

					int newIndex = crossRoads.Count;
					TrackPoints[i].crossroadIndex = newIndex;
					crossRoads.Add(crossroad);
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

