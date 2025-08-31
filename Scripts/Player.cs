using Godot;
using System;
using System.Numerics;

public partial class Player : CharacterBody2D
{
	/* Coefficient of speed */
	[Export] public int walk = 1;

	[Export] public Sol sol;

	public const float Speed = 100.0f;
	public Godot.Vector2 worldLimit;

	public override void _PhysicsProcess(double delta)
	{
		Godot.Vector2 velocity = Velocity;

		if (this.Position.X < worldLimit.X)
		{
			/* Does the player reach a crossroad ? */
			int index = (int)this.Position.X;
			if (index < sol.CurrentTrack.TrackPoints.Length && sol.CurrentTrack.TrackPoints[index].crossroads)
			{
				/* For the moment, just stop walking */
				walk = 0;
			}

			// Add the gravity.
			if (!IsOnFloor())
			{
				velocity += GetGravity() * (float)delta;
			}
			// Don't fall
			else
			{
				velocity.X = walk * Speed;
			}


			Velocity = velocity;
			MoveAndSlide();
		}
	}
}
