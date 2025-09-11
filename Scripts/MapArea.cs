using Godot;
using System;

public partial class MapArea : Area2D
{

	private Line2D trailLine;
	[Signal] public delegate void TrailSelectionEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        Connect("TrailSelection", new Callable(this, nameof(_on_trail_selection)));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void _on_trail_selection(Area2D area, bool selected)
	{
		// Retrieve the line
		trailLine = area.GetNode<Line2D>("TrailLine2D");

		if (selected)
		{
			setColorTrail(Colors.Red);
		}
		else
		{
			setColorTrail(Colors.Orange);
		}
	}

	private void setColorTrail(Color color)
	{
		trailLine.DefaultColor = color;
    }
}
