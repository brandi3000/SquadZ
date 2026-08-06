using Godot;
using System;


public partial class Jugar : Button
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pressed +=jugar;
	}

	public void jugar()
	{
		GetTree().ChangeSceneToFile("res://Scenes/main.tscn");
	}
}
