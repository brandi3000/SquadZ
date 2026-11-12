using Godot;

public partial class PauseMenu : Control
{
	public const string MenuScenePath = "res://Scenes/MenuPrincipal.tscn";

	private Button _saveButton;
	private Label _status;
	private Label _perks;

	public bool IsOpen => Visible;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		MouseFilter = MouseFilterEnum.Stop;
		UiTheme.FullRect(this);
		Visible = false;

		AddChild(UiTheme.FullRect(new ColorRect { Color = new Color(0, 0, 0, 0.6f), MouseFilter = MouseFilterEnum.Ignore }));
		var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		AddChild(UiTheme.FullRect(center));

		var device = UiTheme.MakePanel(UiTheme.Device());
		center.AddChild(device);
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 16);
		device.AddChild(row);

		var menuScreen = UiTheme.MakePanel(UiTheme.Screen());
		menuScreen.CustomMinimumSize = new Vector2(280, 0);
		row.AddChild(menuScreen);
		var menu = new VBoxContainer();
		menu.AddThemeConstantOverride("separation", 10);
		menuScreen.AddChild(menu);
		menu.AddChild(UiTheme.MakeLabel("PAUSA", 34));

		var resume = UiTheme.MakeButton("CONTINUAR", 20);
		resume.Pressed += Close;
		menu.AddChild(resume);

		_saveButton = UiTheme.MakeButton("GUARDAR PARTIDA", 20);
		_saveButton.Pressed += OnSavePressed;
		menu.AddChild(_saveButton);

		var exit = UiTheme.MakeButton("MENÚ PRINCIPAL", 20);
		exit.Pressed += OnExitPressed;
		menu.AddChild(exit);

		_status = UiTheme.MakeLabel("", 14, UiTheme.LcdDim);
		_status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_status.CustomMinimumSize = new Vector2(250, 40);
		menu.AddChild(_status);

		var helpScreen = UiTheme.MakePanel(UiTheme.Screen());
		row.AddChild(helpScreen);
		var help = new VBoxContainer();
		helpScreen.AddChild(help);
		help.AddChild(UiTheme.MakeLabel("CONTROLES", 22));
		string[] lines =
		{
			"CÁMARA: WASD o flechas · bordes de pantalla",
			"   rueda: zoom · botón medio: arrastrar · Espacio: base · M: minimapa",
			"UNIDADES: click o arrastrar para seleccionar (Shift suma)",
			"   click der.: mover / construir / reparar / subir a torre",
			"   H: volver a la base · X: detener · Ctrl+A: todas",
			"   Ctrl+1-4: formar escuadra · 1-4: seleccionarla",
			"RECLUTAR: F1-F5 · CONSTRUIR: B muro, T torre (Shift: varios)",
			"DÍAS: Enter pasa de día · N adelanta la oleada"
		};
		foreach (string line in lines)
			help.AddChild(UiTheme.MakeLabel(line, 14));

		help.AddChild(new HSeparator());
		help.AddChild(UiTheme.MakeLabel("PERKS", 20));
		_perks = UiTheme.MakeLabel("", 14, UiTheme.Warning);
		_perks.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		_perks.CustomMinimumSize = new Vector2(420, 0);
		help.AddChild(_perks);
	}

	public void Open()
	{
		bool canSave = GameManager.Instance.CurrentState == GameState.Intermission;
		_saveButton.Disabled = !canSave;
		_saveButton.TooltipText = canSave ? "Guarda el progreso actual." : "Solo se puede guardar entre días.";
		_status.Text = canSave ? "" : "Se guarda automáticamente al terminar cada día.";
		var perks = GameManager.Instance.Perks;
		_perks.Text = perks.Count == 0
			? "Ninguno todavía. Cada 5 días aparece un jefe: al derrotarlo elegís uno."
			: string.Join("\n", System.Linq.Enumerable.Select(perks, p => $"{GameData.Perks[p.Key].Name} x{p.Value}: {GameData.Perks[p.Key].Description}"));
		Visible = true;
		GetTree().Paused = true;
	}

	public void Close()
	{
		Visible = false;
		GetTree().Paused = false;
	}

	private void OnSavePressed()
	{
		if (GetTree().CurrentScene is Main main)
			_status.Text = main.SaveGame() ? "Partida guardada." : "No se pudo guardar.";
	}

	private void OnExitPressed()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile(MenuScenePath);
	}
}
