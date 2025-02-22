using Godot;
using System;

public partial class MainMenu20 : Node2D
{
	[Export] public Label myLabel;
	[Export] public eSceneNames mySceneName;

	private int count = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		myLabel.Text = "Changed the label on Ready in C#!";
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void _on_button_1_button_up() {
		SceneManager.instance.ChangeScene(eSceneNames.Main);
		count++;
		myLabel.Text = "The button change the text. It has been clicked: " + count;
	}
}
