using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Randonneur;
/* Because of System.Numerics */
using Vector2 = Godot.Vector2;

public partial class TemplateLevel : Node2D
{
    // godot directory to gpx files to define the level map
    [Export]
    string? pathToMap;

    private AnimationPlayer? _fadeAnimation;

    public Dictionary<string, Gpx>? Trails { get; private set; }

    /// <summary>
    /// Last waypoint reached by the player when the map is displayed.
    /// </summary>
    public Waypoint? CurrentWaypoint { get; set; }

    /// <summary>
    /// Reference to the world map.
    /// </summary>
    internal WorldMap? Map;
    public string? CurrentTraceName;

    /// <summary>
    /// The maximum distance from the origin in the level the player can go.
    /// </summary>
    public float LimitX { get; set; } = 10000.0f;

    public override void _Ready()
    {
        // Sanity checks
        if (pathToMap == null || CurrentWaypoint == null || CurrentTraceName == null)
        {
            GD.PushWarning($"${nameof(_Ready)}: sanity checks failed");
            return;
        }

        // game start ? create player
        if (Player.Instance == null)
        {
            PackedScene playerScene = GD.Load<PackedScene>("res://Scenes/player.tscn");
            Player player = playerScene.Instantiate<Player>();
            // Add to scene in order to no be free quickly
            AddChild(player);
        }

        if (Player.Instance == null)
        {
            GD.PushError($"${nameof(_Ready)}: failed to instanciate player");
            return;
        }

        // Player
        UpdatePlayerInfos();

        // World map
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
        Player? player = Player.Instance;
        if (player == null)
        {
            return;
        }
        // Display the X,Y of the player in the label
        Label label = GetNode<Label>("Debug/Control/DebugLabel");
        label.Text = $"X {player.Position.X.ToString("N3")} Y {player.Position.Y.ToString("N3")} MAX: {player.LevelLimitX.X}";
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
        if (Map == null)
        {
            GD.PushError("Map is not instancied.");
            return;
        }
        GD.Print($"MAP ${Map.Name} VISIBLE {Map.Visible} (set to {Visible})");
        Waypoints.Instance.DisplayLinks();
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

        Waypoints waypoints = (Waypoints)Waypoints.Instance;

        // by default nothing can be selected
        Map?.DisableCollision();

        // Put White on  the reachable waypoints.
        if (Waypoint != null && waypoints.Links != null)
        {
            string key = Waypoint.Name;
            if (waypoints.Links.TryGetValue(key, out WaypointsLinks? links))
            {
                foreach (
                    KeyValuePair<
                        string,
                        ConnectedWaypoint
                    > connectedWaypoint in links.ConnectedWaypoints
                )
                {
                    GfxWaypoint gfxWaypoint = (GfxWaypoint)connectedWaypoint.Value.Waypoint;
                    MapJunctionArea junctionArea = gfxWaypoint.MapJunctionGfx;
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
        if (Player.Instance == null || Map == null)
        {
            GD.PushWarning($"${nameof(_PhysicsProcess)}: sanity checks failed");
            return;
        }

        Map.Position = new Godot.Vector2(
            Player.Instance.Position.X - 200,
            Player.Instance.Position.Y - 100
        );
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

    public void TrailJunctionChoiceDone(string destWaypointName)
    {
        Waypoints waypoints = (Waypoints)Waypoints.Instance;

        // Sanity checks
        if (
            _fadeAnimation == null
            || waypoints == null
            || CurrentWaypoint == null
            || waypoints.Links == null
        )
        {
            GD.PushWarning($"${nameof(TrailJunctionChoiceDone)}: sanity checks failed");
            return;
        }

        if (waypoints.Links.TryGetValue(CurrentWaypoint.Name, out WaypointsLinks? links))
        {
            //Waypoint destinationWaypoint = links.ConnectedWaypoints[destWaypointName].Waypoint;
            MapVisible(false, CurrentWaypoint);

            string traceName = links.ConnectedWaypoints[destWaypointName].TraceName;
            string gpxFile = Global.DefautlMapDirectory + traceName;

            // need to change the scene?
            CurrentTraceName = CurrentWaypoint.TraceName;
            if (traceName != CurrentWaypoint.TraceName)
            {
                SceneManager.Instance?.ChangeLevel(gpxFile, CurrentWaypoint.Name);

                _fadeAnimation.Play("fade_in");
                Sleep(500);
                _fadeAnimation.Play("fade_out");
                Sleep(500);

                /* Save the next trace */
                CurrentTraceName = traceName;
            }
        }
    }

    ///  <summary>
    ///  Display the junction choice through the map.
    ///  </summary>
    ///  <param name="TraceName">Contains the name of the gpx file.</param>
    ///  <param name="WaypointName">Name of the triggered waypoint.</param>
    public void JunctionChoice(string TraceName, string WaypointName)
    {
        Waypoint? waypoint = null;

        Waypoints waypoints = (Waypoints)Waypoints.Instance;
        waypoint = waypoints.GetWaypoint(WaypointName);
        // sanity checks
        if (waypoint == null)
        {
            GD.PushError($"Waypoint {WaypointName} not found");
            return;
        }

        if (Player.Instance is not null)
            Player.Instance.CurrentWaypoint = waypoint;

        MapPositionUpdate();
        MapVisible(true, waypoint);

        // keep the location where player comes from.
        CurrentWaypoint = waypoint;
        // update trace name with the last used
        // otherwise the current and dest transition can fail
        waypoint.TraceName = TraceName;
    }

    internal void UpdatePlayerInfos()
    {
        // Sanity checks
        if (Player.Instance == null || CurrentWaypoint == null || CurrentTraceName == null)
        {
            GD.PushWarning($"${nameof(UpdatePlayerInfos)}: sanity checks failed");
            return;
        }

        if (Player.Instance.Level == this)
        {
            GD.Print("Player infos already updated.");
            return;
        }
        Player.Instance.MoveTo(CurrentWaypoint.LevelCoord[CurrentTraceName]);
        Player.Instance.Move = true;
        Player.Instance.Walk = CurrentWaypoint.PlayerDirection;
        Player.Instance.LevelLimitX.X = LimitX;
        Player.Instance.Level = this;
        Player.Instance.Reparent(this);
    }
}
