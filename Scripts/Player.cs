using Godot;
using System;
using System.Numerics;
using System.Threading.Tasks;

public partial class Player : CharacterBody2D
{
	/* Coefficient of speed */
	[Export] public int Walk = 1;

	[Export] public Sol sol;

	[Signal] public delegate void TrailJunctionChoiceEventHandler();

	public const float Speed = 100.0f;
	public Godot.Vector2 worldLimit;

	public static Player Instance { get; private set; }

	/* TODO move this in level scene */
	private AnimationPlayer fadeAnimation;

	public override void _Ready()
	{
		Instance = this;

		if (FindChild("SceneTransitionAnimation") != null)
		{
			Node2D sceneTransition = GetNode<Node2D>("SceneTransitionAnimation");
			fadeAnimation = sceneTransition.GetNode<AnimationPlayer>("FadeAnimation");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Godot.Vector2 velocity = Velocity;

		// Don't fall
		if (this.Position.X >= 0 && this.Position.X < worldLimit.X)
		{
			/* Does the player reach a trail junction ? */
			int index = (int)this.Position.X;
			int trailJunctionIndex = sol.CurrentTrack.TrackPoints[index].trailJunctionIndex;
			if (index < sol.CurrentTrack.TrackPoints.Length &&
				trailJunctionIndex != -1 &&
				Walk != 0)
			{
				/* For the moment, just stop walking */
				Walk = 0;

				/* TODO move this in level scene */
				GD.Print($"trail junction index: {trailJunctionIndex}");
				InGameUi gameUI = InGameUi.Instance;
				DestinationsList destinationsList = gameUI.GetNode<DestinationsList>("%DestinationsList");
				destinationsList.EmitSignal(DestinationsList.SignalName.DestinationsUpdate, sol.CurrentTrack.trailJunctions[trailJunctionIndex]);
				TrailSignVisible(true);
			}

			// Add the gravity.
			if (!IsOnFloor())
			{
				velocity += GetGravity() * (float)delta;
			}
			else
			{
				velocity.X = Walk * Speed;
			}

			Velocity = velocity;
			MoveAndSlide();
		}
	}


	/* TODO move this in level scene */
	private void TrailSignVisible(bool Visible)
	{
		CanvasLayer sign = GetNode<CanvasLayer>("TrailSign");
		sign.Visible = Visible;
	}

	/* TODO move this in global tools class */
	private async void Sleep(double value)
	{
		await Task.Delay(TimeSpan.FromMilliseconds(value));
	}

	/* TODO move this in level scene */
	private void _on_trail_junction_choice(string gpxFile)
	{
		this.Position = new Godot.Vector2(this.Position.X + 1, this.Position.Y);
		Walk = 1;
		TrailSignVisible(false);
		GD.Print($"player will go to {gpxFile}");
		sol.generateGround("res://data/" + gpxFile);
		fadeAnimation.Play("fade_in");
		Sleep(500);
		/* TODO : try to put fade out in Level1 ready */
		fadeAnimation.Play("fade_out");
		Sleep(500);
	}
}
