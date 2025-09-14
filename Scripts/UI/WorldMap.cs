using Godot;
using System;

public partial class WorldMap : Control
{
	// flag storing that middle mouse button is currently pressed
	private bool m_middleButton = false;

	// Container which store the map
	private MarginContainer m_margin;
	// local position of mouse cursor when clicking on middle button
	Vector2 m_dragPosition;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		m_margin = GetNode<MarginContainer>("Margin");
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Middle)
			{
				GD.Print($"Middle {mouseEvent.Pressed}");
				m_middleButton = false;
				if (mouseEvent.Pressed)
				{
					Vector2 mousePosition = GetLocalMousePosition();
					if (m_margin.GetRect().HasPoint(mousePosition))
					{
						m_middleButton = true;
						m_dragPosition = new Vector2(mousePosition.X, mousePosition.Y);
						GD.Print($"decalage X {m_dragPosition.X} Y {m_dragPosition.Y}");
					}
				}
			}
		}
		else if (@event is InputEventMouseMotion mouseMotion)
		{
			if (m_middleButton)
			{
				Position = GetGlobalMousePosition() - m_dragPosition;
			}
		}
    }
}
