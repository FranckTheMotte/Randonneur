using Godot;
using System;
using System.Numerics;

public partial class Player : CharacterBody2D
{
	/* Coefficient of speed */
	[Export] public int Walk = 1;

	[Export] public Sol sol;

	[Signal] public delegate void CrossroadChoiceEventHandler();

	public const float Speed = 100.0f;
	public Godot.Vector2 worldLimit;

	public static Player Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;
	}

	public override void _PhysicsProcess(double delta)
	{
		Godot.Vector2 velocity = Velocity;

		// Don't fall
		if (this.Position.X >= 0 && this.Position.X < worldLimit.X)
		{
			/* Does the player reach a crossroad ? */
			int index = (int)this.Position.X;
			int crossroadIndex = sol.CurrentTrack.TrackPoints[index].crossroadIndex;
			if (index < sol.CurrentTrack.TrackPoints.Length &&
				crossroadIndex != -1 &&
				Walk != 0)
			{
				/* For the moment, just stop walking */
				Walk = 0;

				GD.Print($"crossroad index: {crossroadIndex}");
				InGameUi gameUI = InGameUi.Instance;
				DestinationsList destinationsList = gameUI.GetNode<DestinationsList>("%DestinationsList");
				destinationsList.EmitSignal(DestinationsList.SignalName.DestinationsUpdate, sol.CurrentTrack.crossRoads[crossroadIndex]);
				CrossroadSignVisible(true);
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

	private void CrossroadSignVisible(bool Visible)
	{
		CanvasLayer sign = GetNode<CanvasLayer>("TESTSign");
		sign.Visible = Visible;
	}

	private void _on_crossroad_choice()
	{
		this.Position = new Godot.Vector2(this.Position.X + 1, this.Position.Y);
		Walk = 1;
		CrossroadSignVisible(false);
	}
}
