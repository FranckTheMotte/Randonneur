using Godot;
using System;

public partial class Actions : CanvasLayer
{
	// Called when the node enters the scene tree for the first time.
	[Export] Player player;
	private CheckButton checkButton;

	public override void _Ready()
	{
		checkButton = GetNode<CheckButton>("Control/CheckButton");
		player.walk = 0;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void _on_check_button_toggled(bool isToggled)
	{
		/* Test to start or stop the auto-walk */
		if (isToggled)
		{
			player.walk = 1;
		}
		else
		{
			player.walk = 0;
		}
	}

}
