using Godot;

public partial class Base : StaticBody2D
{
	public override void _Ready()
	{
		// La base también tiene vida, así que le ponemos el nodo Health
		// (lo conectaremos más adelante)
		AddToGroup("base");
	}
}
