using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
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

    public Hashtable? Trails { get; private set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Sanity checks
        if (pathToMap == null)
        {
            GD.PushWarning($"${nameof(_Ready)}: sanity checks failed");
            return;
        }

        /* Create the map */
        Control worldMapSign = GetNode<Control>("WorldMap");

        /* Set world map size */
        MarginContainer bgMargin = worldMapSign.GetNode<MarginContainer>("BgMargin");
        MarginContainer mapMargin = bgMargin.GetNode<MarginContainer>("BgNinePathRect/MapMargin");
        ColorRect mapRect = mapMargin.GetNode<ColorRect>("MapRect");
        worldMapSign.Position = new Vector2(50, 500);

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

        var (TrailsNodes, TrailsGpx) = mapGenerator.generateMap(pathToMap);
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

    public void MapVisible(bool Visible)
    {
        GD.Print($"MAP VISIBLE {Visible}");
        WorldMap map = GetNodeOrNull<WorldMap>("WorldMap");
        map?.Disable(!Visible);
    }

    /**
     Update the map position based on the player's position.
     */
    private void MapPositionUpdate()
    {
        // Sanity checks
        if (player == null)
        {
            GD.PushWarning($"${nameof(_PhysicsProcess)}: sanity checks failed");
            return;
        }

        Control map = GetNode<Control>("WorldMap");
        map.Position = new Godot.Vector2(player.Position.X - 200, player.Position.Y - 100);
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

    private void _on_trail_junction_choice_done(string gpxFile)
    {
        // Sanity checks
        if (_fadeAnimation == null)
        {
            GD.PushWarning($"${nameof(_on_trail_junction_choice_done)}: sanity checks failed");
            return;
        }

        MapVisible(false);
        // TODO: POC: force a trace
        gpxFile = "res://data/Map1/TraceG.gpx";
        SceneManager.instance?.ChangeLevel(gpxFile);

        _fadeAnimation.Play("fade_in");
        Sleep(500);
        _fadeAnimation.Play("fade_out");
        Sleep(500);
    }

    /**
      Display the junction choice through the map.

      @param TrackName Contains the name of the gpx file.
      @param Coord     Coordinate of the triggered waypoint.
    */
    public void JunctionChoice(string TrackName, Vector2 Coord)
    {
        MapPositionUpdate();
        MapVisible(true);

        /* TODO highlight the matched waypoint, unlight others */
        Gpx? trail = (Gpx?)Trails?[TrackName];
        if (trail != null)
        {
            GpxWaypoint? waypoint = trail.Waypoints.GetWaypoint(Coord);
            if (waypoint is not null)
            {
                ColorRect? landmark = waypoint.Landmark;
                if (landmark is not null)
                {
                    landmark.Color = Colors.Black;
                }
            }
        }
    }
}
