using Godot;

public partial class Salir : Button
{
	public override void _Ready()
	{
		Pressed += salir;
	}

	public void salir()
	{
		GetTree().Quit();
	}
}
