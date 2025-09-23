using Godot;
using System;

public partial class MapArea : Area2D
{

	private Line2D trailLine;
	[Signal] public delegate void TrailSelectionEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Trigger actions when mouse go over/out a trail
		Connect("TrailSelection", new Callable(this, nameof(_on_trail_selection)));
		InputEvent += OnInputEvent;
		InputPickable = true;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	/**
		Called with Signal "TrailSelection".

		@param area Properties of the un/selected area
		@param selected True if selected, False otherwise

	*/
	private void _on_trail_selection(Area2D area, bool selected)
	{
		WorldMap worldMap = WorldMap.Instance;

		// Retrieve the line
		trailLine = area.GetNode<Line2D>("TrailLine2D");
		GD.Print($"trail selection {area.Name}");

		if (selected)
		{
			setColorTrail(Colors.Red);
			worldMap.m_selectedTrail = area.Name;
		}
		else
		{
			setColorTrail(Colors.Orange);
			worldMap.m_selectedTrail = null;
		}
	}

	private void setColorTrail(Color color)
	{
		trailLine.DefaultColor = color;
	}

	/**
		Emitted when an input event occurs.
		Here, only the MouseButton event is catched

		@param viewport current viewport
		@param event input event properties
		@param shapeIdx index to the collision2D object
	*/
	private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
	{
		if (@event is InputEventMouseButton mouseButton)
		{
			if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
			{
				GD.Print("Clic gauche détecté sur l'Area2D !");
			}
		}
	}
}
