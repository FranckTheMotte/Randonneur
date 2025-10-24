using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Randonneur;
using XmlGpx;
/* Because of System.Numerics */
using Vector2 = Godot.Vector2;

public partial class TemplateLevel : Node2D
{
    [Export]
    public Camera2D? camera2d;

    [Export]
    public Player? player;

    // godot directory to gpx files to define the level map
    [Export]
    string? pathToMap;

    [Signal]
    public delegate void TrailJunctionChoiceDoneEventHandler();

    private AnimationPlayer? _fadeAnimation;

    public Dictionary<string, Gpx>? Trails { get; private set; }

    /// <summary>
    /// Last waypoint reached by the player when the map is displayed.
    /// </summary>
    public Waypoint? LastReachedWaypoint { get; private set; }

    /// <summary>
    /// Reference to the world map.
    /// </summary>
    internal WorldMap? Map;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Sanity checks
        if (pathToMap == null || player == null)
        {
            GD.PushWarning($"${nameof(_Ready)}: sanity checks failed");
            return;
        }

        Viewport root = GetTree().Root;
        Map = root.GetNode<WorldMap>("worldMapControl");
        if (Map is null)
        {
            GD.PushError($"${nameof(_Ready)}: fail to find world map");
            return;
        }

        /* Set world map size */
        MarginContainer bgMargin = Map.GetNode<MarginContainer>("BgMargin");
        MarginContainer mapMargin = bgMargin.GetNode<MarginContainer>("BgNinePathRect/MapMargin");
        ColorRect mapRect = mapMargin.GetNode<ColorRect>("MapRect");
        MapPositionUpdate();

        /* TODO: compute with margins */
        float mapWidth =
            bgMargin.CustomMinimumSize.X
            - bgMargin.GetThemeConstant("margin_left")
            - bgMargin.GetThemeConstant("margin_right")
            - mapMargin.GetThemeConstant("margin_left")
            - mapMargin.GetThemeConstant("margin_right");
        float mapHeight =
            bgMargin.CustomMinimumSize.Y
            - bgMargin.GetThemeConstant("margin_top")
            - bgMargin.GetThemeConstant("margin_bottom")
            - mapMargin.GetThemeConstant("margin_top")
            - mapMargin.GetThemeConstant("margin_bottom");
        GD.Print($"map W H {mapWidth} {mapHeight}");
        MapGenerator mapGenerator = new(mapWidth, mapHeight);

        var (TrailsNodes, TrailsGpx) = mapGenerator.GenerateMap(pathToMap);
        List<Area2D>? trails = TrailsNodes;
        Trails = TrailsGpx;

        if (trails is null)
        {
            GD.PushError($"${nameof(_Ready)}: map not generated");
            return;
        }
        foreach (var trail in trails)
        {
            GD.Print($"Add {trail.GetType()} named {trail.Name} to mapArea");
            mapRect.AddChild(trail);
        }

        /* Setup mouse for collision methods */
        Input.MouseMode = Input.MouseModeEnum.Hidden;
        var customCursorCanvas = GD.Load<PackedScene>("res://Scenes/mouse_cursor.tscn")
            .Instantiate();
        MouseCursor customCursor = customCursorCanvas.GetNode<MouseCursor>("MouseCursor");
        AddChild(customCursorCanvas);

        /* Scene transition */
        if (FindChild("SceneTransitionAnimation") != null)
        {
            Node2D sceneTransition = GetNode<Node2D>("SceneTransitionAnimation");
            _fadeAnimation = sceneTransition.GetNode<AnimationPlayer>("FadeAnimation");
        }

        // Map is not visible at start
        MapVisible(false);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        // Display the X,Y of the player in the label
        CharacterBody2D player = GetNode<CharacterBody2D>("Player");
        Label label = GetNode<Label>("Debug/Control/DebugLabel");
        string txt;
        txt = $"X {player.Position.X.ToString("N3")} Y {player.Position.Y.ToString("N3")}";
        label.Text = txt;
    }

    /// <summary>
    /// Show or hide the map.
    /// </summary>
    /// <param name="Visible">true to show the map, false to hide.</param>
    /// <remarks>
    /// </summary>
    /// <param name="Waypoint">optional, indice the player location.</param>
    /// <remarks>
    /// When the map is visible, all landmarks are hidden.
    /// </remarks>
    public void MapVisible(bool Visible, Waypoint? Waypoint = null)
    {
        if (Map is null)
        {
            GD.PushError("Map is not instancied.");
            return;
        }
        GD.Print($"MAP ${Map.Name} VISIBLE {Map.Visible} (set to {Visible})");

        Map.Visible = Visible;
        Map?.CollisionStatus(Visible);

        if (Visible)
        {
            // Hide all landmark when the map is displayed.
            if (Trails != null)
            {
                foreach (KeyValuePair<string, Gpx> trail in Trails)
                {
                    if (trail.Value.TrailJunctions != null)
                    {
                        trail.Value.XWaypoints.SetAllLandmarkVisibility(false);
                    }
                }
            }
        }

        Waypoints waypoints = Waypoints.Instance;

        // by default nothing can be selected
        waypoints.DisableCollision();

        // Put White on  the reachable waypoints.
        if (Waypoint != null)
        {
            string key = Waypoint.Name;
            if (waypoints.Links.TryGetValue(key, out WaypointsLinks? links))
            {
                foreach (
                    KeyValuePair<string, Waypoint> connectedWaypoint in links.ConnectedWaypoints
                )
                {
                    Waypoint waypoint = connectedWaypoint.Value;
                    MapJunctionArea junctionArea = waypoint.MapJunctionGfx;
                    if (junctionArea != null)
                    {
                        if (Visible)
                        {
                            junctionArea.Reachable = true;
                            junctionArea.SetupCollision(true);
                            junctionArea.SetColor(Colors.AntiqueWhite);
                        }
                        else
                        {
                            junctionArea.Reachable = false;
                            junctionArea.SetColor(Colors.CadetBlue);
                        }
                    }
                }
            }
        }
    }

    /**
     Update the map position based on the player's position.
     */
    private void MapPositionUpdate()
    {
        // Sanity checks
        if (player == null || Map == null)
        {
            GD.PushWarning($"${nameof(_PhysicsProcess)}: sanity checks failed");
            return;
        }

        Map.Position = new Godot.Vector2(player.Position.X - 200, player.Position.Y - 100);
    }

    public void TrailSignVisible(bool Visible)
    {
        CanvasLayer sign = GetNode<CanvasLayer>("TrailSignNode/TrailSign");
        sign.Visible = Visible;
    }

    /* TODO move this in global tools class */
    private async void Sleep(double value)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(value));
    }

    private void _on_trail_junction_choice_done(string waypointName)
    {
        Waypoints waypoints = Waypoints.Instance;

        // Sanity checks
        if (_fadeAnimation == null || waypoints == null)
        {
            GD.PushWarning($"${nameof(_on_trail_junction_choice_done)}: sanity checks failed");
            return;
        }

        if (waypoints.Links.TryGetValue(waypointName, out WaypointsLinks? links))
        {
            Waypoint waypoint = links.Waypoint;
            MapVisible(false, LastReachedWaypoint);

            // left the current waypoint
            if (Player.Instance is not null)
                Player.Instance.CurrentWaypoint = null;

            string gpxFile = Global.DefautlMapDirectory + waypoint.TraceName;
            SceneManager.instance?.ChangeLevel(gpxFile);

            _fadeAnimation.Play("fade_in");
            Sleep(500);
            _fadeAnimation.Play("fade_out");
            Sleep(500);
        }
    }

    ///  <summary>
    ///  Display the junction choice through the map.
    ///  </summary>
    ///  <param name="TrackName">Contains the name of the gpx file.</param>
    ///  <param name="Coord">Coordinate of the triggered waypoint.</param>
    public void JunctionChoice(string TrackName, Vector2 Coord)
    {
        Waypoint? waypoint = null;
        // if possible put a marker on the map to display the player position
        // all landmarks are not visible by default
        Gpx? trail = (Gpx?)Trails?[TrackName];
        if (trail != null)
        {
            waypoint = trail.XWaypoints.GetWaypoint(Coord);
            if (waypoint is not null)
            {
                waypoint.FocusLandmark(true);
            }
        }

        if (Player.Instance is not null)
            Player.Instance.CurrentWaypoint = waypoint;

        MapPositionUpdate();
        MapVisible(true, waypoint);

        // keep the location where player comes from.
        LastReachedWaypoint = waypoint;
    }
}
