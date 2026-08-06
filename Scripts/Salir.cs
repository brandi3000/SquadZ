using Godot;
using System;

public partial class Salir : Button
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pressed += salir;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public void salir()
	{
		GetTree().Quit();
	}
}
