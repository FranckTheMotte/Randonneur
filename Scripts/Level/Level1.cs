using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
/* Because of System.Numerics */
using Vector2 = Godot.Vector2;

public partial class Level1 : Node2D
{
    [Export]
    public Camera2D? camera2d;

    [Export]
    public Player? player;

    // godot directory to gpx files to define the level map
    [Export]
    string? pathToMap;

    [Signal]
    public delegate void TrailJunctionChoiceEventHandler();

    [Signal]
    public delegate void TrailJunctionChoiceDoneEventHandler();
    private MapGenerator? mapGenerator;
    private AnimationPlayer? fadeAnimation;

    private Sol? sol;

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
        mapGenerator = new MapGenerator(mapWidth, mapHeight);

        List<Area2D>? trails = mapGenerator.generateMap(pathToMap);

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
            fadeAnimation = sceneTransition.GetNode<AnimationPlayer>("FadeAnimation");
        }

        sol = GetNode<Sol>("Ground/Sol");

        // Map is not visible at start
        MapVisible(false);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        // Sanity checks
        if (mapGenerator == null)
        {
            GD.PushWarning($"${nameof(_Process)}: sanity checks failed");
            return;
        }

        mapGenerator._Process(delta);

        // Display the X,Y of the player in the label
        CharacterBody2D player = GetNode<CharacterBody2D>("Player");
        Label label = GetNode<Label>("Debug/Control/DebugLabel");
        string txt;
        txt = $"X {player.Position.X.ToString("N3")} Y {player.Position.Y.ToString("N3")}";
        label.Text = txt;
    }

    public void MapVisible(bool Visible)
    {
        WorldMap map = GetNode<WorldMap>("WorldMap");
        map.Disable(!Visible);
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
        map.Position = new Godot.Vector2(player.Position.X - 200, player.Position.Y - 50);
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
        if (sol == null || fadeAnimation == null)
        {
            GD.PushWarning($"${nameof(_on_trail_junction_choice_done)}: sanity checks failed");
            return;
        }

        MapVisible(false);
        // TODO gpxFile must contains full path
        sol.generateGround("res://data/Map1/" + gpxFile);
        fadeAnimation.Play("fade_in");
        Sleep(500);
        fadeAnimation.Play("fade_out");
        Sleep(500);
    }

    private void _on_trail_junction_choice(int junctionIndex)
    {
        // Sanity checks
        if (
            sol == null
            || sol.CurrentTrack == null
            || sol.CurrentTrack.m_trailJunctions == null
            || InGameUi.Instance == null
        )
        {
            GD.PushWarning($"${nameof(_on_trail_junction_choice)}: sanity checks failed");
            return;
        }

        /* Update destinations list on sign */
        DestinationsList destinationsList = InGameUi.Instance.GetNode<DestinationsList>(
            "%DestinationsList"
        );
        destinationsList.EmitSignal(
            DestinationsList.SignalName.DestinationsUpdate,
            sol.CurrentTrack.m_trailJunctions[junctionIndex]
        );
        MapPositionUpdate();
        MapVisible(true);
    }
}
