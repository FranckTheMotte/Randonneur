using Godot;
using System;
using static Godot.GD;

public partial class Sol : StaticBody2D
{
	[Export] Player player;

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

			//Print($"X: {x} Y: {y}");
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


		/* Generate a profil from a gpx file */
		Gpx track = new Gpx();
		track.Load("res://data/Test1.gpx");

		/* Add 2 points in order to display a solid ground */
		Vector2[] ground = new Vector2[track.Elevation.Length + 2];
		track.Elevation.CopyTo(ground, 0);
		ground[track.Elevation.Length].X = track.Elevation.Length;
		ground[track.Elevation.Length].Y = 10000;
		ground[track.Elevation.Length + 1].X = 0;
		ground[track.Elevation.Length + 1].Y = 10000;

		sol.Polygon = ground;
		solCollision.Polygon = sol.Polygon;

		/* player start position */
		Vector2 position = player.Position;
		position.X = 0.00f;

		CollisionShape2D playerCollisionShape = player.GetNode<CollisionShape2D>("PlayerCollisionShape2D");

		GD.Print($"ground[0].Y : {ground[0].Y} shape height {playerCollisionShape.Shape.GetRect().Size.Y}");
		position.Y = ground[0].Y - (playerCollisionShape.Shape.GetRect().Size.Y / 2 * player.Scale.Y);
		player.Position = position;


		/* player limit */
		Vector2 limit = new Vector2(track.Elevation.Length, 0);
		player.worldLimit = limit;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
