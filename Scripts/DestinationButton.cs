using Godot;
using System;

public partial class DestinationButton : Button
{
	private void _on_pressed()
	{
		GD.Print("Button pressed");
		Player player = Player.Instance;
		player.EmitSignal(Player.SignalName.CrossroadChoice);
	}
}
