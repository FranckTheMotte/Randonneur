using Godot;
using System;

public partial class Player : CharacterBody2D
{
	public const float Speed = 100.0f;

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		velocity.X = 1 * Speed;

		Velocity = velocity;
		MoveAndSlide();
	}
}
