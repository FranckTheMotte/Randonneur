using Godot;
using System;

public partial class DestinationButton : Button
{

	public string GpxFile;

	private void _on_pressed()
	{
		GD.Print($"Button pressed {GpxFile}");
		Player player = Player.Instance;
		player.EmitSignal(Player.SignalName.TrailJunctionChoice, GpxFile);
	}
}
