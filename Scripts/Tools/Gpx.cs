using Godot;
using static Godot.GD;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Xml;
using System.ComponentModel;

// This class load a gpx file to:
// - convert lat and lon to coord (x,y) for a map (top view)
// - convert elevation to coord (x,y) for an elevation profile

// A gpx file can contains reference to another gpx files.

public struct GpxProperties
{
	public Vector2 Elevation { get; set; }
	public bool crossroads;
}

public class Gpx
{
	// x : index, y : value	
	public GpxProperties[] TrackPoints { get; set; }

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
			//Change the price on the books.
			int i = 0;
			foreach (XmlNode segment in segments)
			{
				//Print($"Segment[{i}]: {segment["ele"].InnerText}");
				TrackPoints[i].Elevation = new Vector2(i, 2000.00f + (float.Parse(segment["ele"].InnerText, CultureInfo.InvariantCulture.NumberFormat) * -1.00f));
				TrackPoints[i].crossroads = false;
				if (segment.SelectSingleNode("a:link", namespaceManager) != null)
				{
					TrackPoints[i].crossroads = true;
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

