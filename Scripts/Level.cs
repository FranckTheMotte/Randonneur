using System.Collections.Generic;
using Godot;

namespace Randonneur.Scripts
{
    /// <summary>
    /// Class <c>Level</c> models a single trail and store the starting point.
    /// </summary>
    /// <param name="GpxFile">Full godot path to the gpx file.</param>
    public partial class Level(string GpxFile)
    {
        public const string LevelTextScene = "res://Scenes/20_Level1.tscn";

        /// <value>
        /// Property <c>Scene</c> the godot scene.
        /// </value>
        public Node? Scene { get; private set; }

        /// <value>
        /// Field <c>_gpxFile</c> stores full godot path to the gpx file.
        /// </value>
        private readonly string _gpxFile = GpxFile;

        /// <value>
        /// Field <c>_startpoint</c> stores a junction included in gpx file which defines the startpoint.
        /// </value>
        private Junction? _startpoint;

        /// <summary>
        /// Create the level by loading the gpx file.
        /// The list of junctions will be returned.
        /// </summary>
        /// <returns>
        /// A dictionary of linked traces of the current gpx file.
        /// </returns>
        public Dictionary<string, string> Create()
        {
            Dictionary<string, string> connectedTraces = [];

            // create the scene to store the level
            PackedScene aPackedScene = GD.Load<PackedScene>(LevelTextScene);
            Scene = aPackedScene.Instantiate();

            TemplateLevel level1 = (TemplateLevel)Scene;

            Sol land =
                Scene.GetNodeOrNull<Sol>("Ground/Sol")
                ?? throw new System.NullReferenceException("Sol node was not found");

            land.generateGround(_gpxFile);

            // TODO define start point in gpx file
            _startpoint = null;

            // get unique connected traces to this gpx file
            List<Junction>? junctions = land.CurrentTrack?.TrailJunctions;
            if (junctions != null)
            {
                foreach (var junction in junctions)
                {
                    if (junction.Destinations != null)
                    {
                        foreach (var destination in junction.Destinations)
                        {
                            connectedTraces[destination.GpxFile] = "";
                        }
                    }
                }
            }

            return connectedTraces;
        }
    }
}
