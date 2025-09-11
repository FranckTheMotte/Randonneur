using Godot;
using System;
using static Godot.GD;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;

public partial class Level1 : Node2D
{
	[Export] public Camera2D camera2d;
	[Export] public Player player;

	// godot directory to gpx files to define the level map
	[Export] string pathToMap;

	[Signal] public delegate void TrailJunctionChoiceEventHandler();
	[Signal] public delegate void TrailJunctionChoiceDoneEventHandler();
	private MapGenerator genLevel;
	private AnimationPlayer fadeAnimation;

	private Sol sol;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		/* Create the map */
		Node2D mapArea = GetNode<Node2D>("Map");
		genLevel = new MapGenerator();
		List<Area2D> trails = genLevel.generateMap(pathToMap);

		foreach (var trail in trails)
		{
			GD.Print($"Add {trail.GetType()} named {trail.Name} to mapArea");
			mapArea.AddChild(trail);
		}

		/* Setup mouse for collision methods */
		Input.MouseMode = Input.MouseModeEnum.Hidden;
		var customCursorCanvas = GD.Load<PackedScene>("res://Scenes/mouse_cursor.tscn").Instantiate();
		MouseCursor customCursor = customCursorCanvas.GetNode<MouseCursor>("MouseCursor");
		AddChild(customCursorCanvas);

		/* Scene transition */
		if (FindChild("SceneTransitionAnimation") != null)
		{
			Node2D sceneTransition = GetNode<Node2D>("SceneTransitionAnimation");
			fadeAnimation = sceneTransition.GetNode<AnimationPlayer>("FadeAnimation");
		}

		sol = GetNode<Sol>("Ground/Sol");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		genLevel._Process(delta);

		// Display the X,Y of the player in the label
		CharacterBody2D player = GetNode<CharacterBody2D>("Player");
		Label label = GetNode<Label>("Debug/Control/DebugLabel");
		string txt;
		txt = $"X {player.Position.X.ToString("N3")} Y {player.Position.Y.ToString("N3")}";
		label.Text = txt;
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
		TrailSignVisible(false);
		sol.generateGround("res://data/" + gpxFile);
		fadeAnimation.Play("fade_in");
		Sleep(500);
		fadeAnimation.Play("fade_out");
		Sleep(500);
	}

	private void _on_trail_junction_choice(int junctionIndex)
	{
		/* Update destinations list on sign */
		InGameUi gameUI = InGameUi.Instance;
		DestinationsList destinationsList = gameUI.GetNode<DestinationsList>("%DestinationsList");
		destinationsList.EmitSignal(DestinationsList.SignalName.DestinationsUpdate, sol.CurrentTrack.trailJunctions[junctionIndex]);
		TrailSignVisible(true);
	}


}	
