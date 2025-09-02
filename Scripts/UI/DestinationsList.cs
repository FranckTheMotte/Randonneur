using Godot;
using System;

public partial class DestinationsList : VBoxContainer
{
	[Export] public HBoxContainer Destination;

	[Signal] public delegate void DestinationsUpdateEventHandler();

	// Called when the node enters the scene tree for the first time.
	public void populateDestination()
	{
		int nbDest = 4;
		for (int i = 0; i < nbDest; i++)
		{
			HBoxContainer newDest = (HBoxContainer)Destination.Duplicate();
			Button destButton = (Button)newDest.GetChild(0);
			destButton.Text = $"dest {i}";
			this.AddChild(newDest);
		}
		// Hide the template
		Destination.Hide();

		// Adapt the sign size
		NinePatchRect backgroundSign = (NinePatchRect)this.GetParent().GetParent();
		HBoxContainer dest = (HBoxContainer)this.GetChild(1);
		// margin H up + margin H low + title Height + nb dest * Height
		float height = (15 * 2) + 32 + (nbDest + 1) * dest.CustomMinimumSize.Y;
		backgroundSign.CustomMinimumSize = new Vector2(256, height);
	}

	private void _on_ready()
	{
		populateDestination();
	}

	private void _on_destinations_update()
	{
		GD.Print("_on_destinations_update");
	}
}

