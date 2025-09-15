using Godot;
using System;
using System.Numerics;

public partial class Player : CharacterBody2D
{
	/* Coefficient of speed */
	[Export] public int Walk = 1;

	[Export] public Sol sol;

	[Signal] public delegate void TrailJunctionChoiceEventHandler();

	[Export] public Level1 level;

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
			/* Does the player reach a trail junction ? */
			int index = (int)this.Position.X;
			int trailJunctionIndex = sol.CurrentTrack.m_trackPoints[index].trailJunctionIndex;
			if (index < sol.CurrentTrack.m_trackPoints.Length &&
				trailJunctionIndex != -1 &&
				Walk != 0)
			{
				/* For the moment, just stop walking */
				Walk = 0;

				/* Update destinations list on sign */
				GD.Print($"trail junction index: {trailJunctionIndex}");
				level.EmitSignal(Level1.SignalName.TrailJunctionChoice, trailJunctionIndex);
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

	private void _on_trail_junction_choice(string gpxFile)
	{
		level.EmitSignal(Level1.SignalName.TrailJunctionChoiceDone, gpxFile);
		this.Position = new Godot.Vector2(this.Position.X + 1, this.Position.Y);
		Walk = 1;
	}
}
