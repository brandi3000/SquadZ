using Godot;

public partial class Cargar : Button
{
	private SaveData _save;

	public override void _Ready()
	{
		Pressed += cargar;
		_save = SaveManager.Load();
		Disabled = _save == null;
		Text = _save == null ? "Cargar" : $"Cargar (día {_save.Day})";
		TooltipText = _save == null ? "No hay partida guardada. Se guarda sola al terminar cada día." : $"Guardada: {_save.SavedAt}";
	}

	public void cargar()
	{
		if (_save == null) return;
		GameManager.Instance.RestoreFromSave(_save);
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile(Jugar.GameScenePath);
	}
}
