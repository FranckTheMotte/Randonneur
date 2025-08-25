using Godot;
using System;
using System.Numerics;

public partial class Player : CharacterBody2D
{
	public const float Speed = 100.0f;
	public Godot.Vector2 worldLimit;

	public override void _PhysicsProcess(double delta)
	{
		Godot.Vector2 velocity = Velocity;

		if (this.Position.X < worldLimit.X)
		{
			// Add the gravity.
			if (!IsOnFloor())
			{
				velocity += GetGravity() * (float)delta;
			}
			// Don't fall
			else
			{
				velocity.X = 1 * Speed;
			}


			Velocity = velocity;
			MoveAndSlide();
		}
	}
}
