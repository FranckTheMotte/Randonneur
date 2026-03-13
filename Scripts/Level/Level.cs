using System.Collections.Generic;
using Godot;

namespace Randonneur.Scripts
{
    /// <summary>
    /// Class <c>Level</c> models a single trail and store the starting point.
    /// </summary>
    /// <param name="GpxFile">Full godot path to the gpx file.</param>
    public class Level(string GpxFile)
    {
        public const string REFERENCE_LEVEL_SCENE = "res://Scenes/20_Level1.tscn";

        /// <value>
        /// Property <c>Scene</c> the godot scene.
        /// </value>
        public Node? Scene { get; private set; }

        /// <value>
        /// Field <c>_gpxFile</c> stores full godot path to the gpx file.
        /// </value>
        private readonly string _gpxFile = GpxFile;

        public string TraceName = Path.GetFileName(GpxFile);

        /// <value>
        /// Field <c>_startpoint</c> stores a junction included in gpx file which defines the startpoint.
        /// </value>
        private Junction? _startpoint;

        /// <summary>
        /// The maximum distance from the origin in the level the player can go.
        /// </summary>
        public float LimitX { get; set; } = 10000.0f;

        /// <summary>
        /// Ref to sol.
        /// </summary>
        private Sol? _sol;

        private readonly Random _rand = new();

        /// <summary>
        /// Generate background with some sprite like clouds or birds.
        /// </summary>
        /// <param name="Sky">Node which regroups all sky nodes.</param>
        /// <param name="ViewportSize">Game display size.</param>
        /// <param name="RessourcePath">Full path to cloud image.</param>
        /// <param name="ParallaxName">Node name of the parallax2D.</param>
        /// <param name="Y">Heigh position.</param>
        private void SpawnCloud(
            Node2D Sky,
            Vector2 ViewportSize,
            string RessourcePath,
            string ParallaxName,
            float Y
        )
        {
            Parallax2D skyParallax = Sky.GetNodeOrNull<Parallax2D>(ParallaxName);
            // scale a cloud from 0.15 to 0.65 of original size
            float randScale = _rand.NextSingle() * 0.5f + 0.15f;
            int midX = (int)ViewportSize.X / 2;
            int midY = (int)ViewportSize.Y / 2;

            Sprite2D cloud = new()
            {
                Texture = GD.Load<Texture2D>(RessourcePath),
                Position = new Vector2(_rand.Next(0, midX), Y - midY),
                Scale = new Vector2(randScale, randScale),
            };
            skyParallax.AddChild(cloud);
        }

        /// <summary>
        /// Generate background with some sprite like clouds or birds.
        /// </summary>
        void GenerateBackground()
        {
            if (Scene == null)
            {
                return;
            }

            Node2D sky = Scene.GetNodeOrNull<Node2D>("Sky");

            // as the current scene is not attached to any scene, the viewport size is
            // retrieve from settings.
            int width = (int)ProjectSettings.GetSetting("display/window/size/viewport_width");
            int height = (int)ProjectSettings.GetSetting("display/window/size/viewport_height");
            GD.Print($"Viewport size {width} {height}");
            Vector2 vpSize = new(width, height);

            // Feed sky with clouds
            SpawnCloud(sky, vpSize, "res://Art/Background/nuage5.png", "SkyParallax1", 100);
            SpawnCloud(sky, vpSize, "res://Art/Background/nuage5.png", "SkyParallax1", 200);
            SpawnCloud(sky, vpSize, "res://Art/Background/nuage6.png", "SkyParallax2", 250);
            SpawnCloud(sky, vpSize, "res://Art/Background/nuage6.png", "SkyParallax3", 300);
        }

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
            PackedScene aPackedScene = GD.Load<PackedScene>(REFERENCE_LEVEL_SCENE);
            Scene = aPackedScene.Instantiate();

            TemplateLevel level1 = (TemplateLevel)Scene;

            _sol =
                Scene.GetNodeOrNull<Sol>("Ground/Sol")
                ?? throw new System.NullReferenceException("Sol node was not found");
            _sol.GenerateGround(_gpxFile);
            GenerateBackground();

            // TODO define start point in gpx file
            _startpoint = null;

            LimitX = _sol.LevelLimitX;

            // get unique connected traces to this gpx file
            List<Junction>? junctions = _sol.CurrentTrack?.TrailJunctions;
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
