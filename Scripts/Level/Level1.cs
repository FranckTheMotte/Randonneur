using Godot;
using System;
using static Godot.GD;

public partial class Level1 : Node2D
{
	[Export] public Camera2D camera2d;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Vector2 inc = new Vector2(camera2d.Offset.X + (100 * (float) delta), camera2d.Offset.Y);
		// camera2d.Offset = inc;
		// Display the X,Y of the player in the label
		CharacterBody2D player = GetNode<CharacterBody2D>("Player");
		Label label = GetNode<Label>("CanvasLayer/Control/DebugLabel");
		string txt;
		txt = $"X {player.Position.X.ToString("N3")} Y {player.Position.Y.ToString("N3")}";
		label.Text = txt;
	}
}	
