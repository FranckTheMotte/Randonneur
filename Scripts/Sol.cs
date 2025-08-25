using Godot;
using System;
using static Godot.GD;

public partial class Sol : StaticBody2D
{

	// Generate a 2D polygon (list of Vector2)
	private Vector2[] generateGround(int length)
	{
		Vector2[] result = new Vector2[length];
		int x = -50;
		int y = 200;

		for (int i = 0; i < length - 2; ++i)
		{
			result[i].X = x;
			result[i].Y = y;
			x += 10;
			y += i % 2 == 0 ? 5 : -5;

			Print($"X: {x} Y: {y}");
		}
		result[length - 2].X = x;
		result[length - 2].Y = 300;

		result[length - 1].X = -50;
		result[length - 1].Y = 300;

		return result;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Print("Copie des points du polygon vers le collision polygon");
		CollisionPolygon2D solCollision = GetNode<CollisionPolygon2D>("CollisionPolygon2D");
		Polygon2D sol = GetNode<Polygon2D>("Polygon2D");
		sol.Polygon = generateGround(400);
		solCollision.Polygon = sol.Polygon;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
