using Godot;

public partial class Jugar : Button
{
	public const string GameScenePath = "res://Scenes/main.tscn";

	public override void _Ready()
	{
		Pressed += jugar;
	}

	public void jugar()
	{
		GameManager.Instance.StartNewGame();
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile(GameScenePath);
	}
}
