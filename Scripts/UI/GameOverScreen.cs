using Godot;

public partial class GameOverScreen : Control
{
	private GridContainer _statsGrid;
	private VBoxContainer _perkList;
	private Label _records;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		MouseFilter = MouseFilterEnum.Stop;
		UiTheme.FullRect(this);
		Visible = false;

		AddChild(UiTheme.FullRect(new ColorRect { Color = new Color(0.05f, 0, 0, 0.8f), MouseFilter = MouseFilterEnum.Ignore }));
		var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		AddChild(UiTheme.FullRect(center));

		var device = UiTheme.MakePanel(UiTheme.Device());
		center.AddChild(device);
		var layout = new VBoxContainer();
		layout.AddThemeConstantOverride("separation", 12);
		device.AddChild(layout);

		var titleScreen = UiTheme.MakePanel(UiTheme.Screen());
		layout.AddChild(titleScreen);
		var titleBox = new VBoxContainer();
		titleScreen.AddChild(titleBox);
		var title = UiTheme.MakeLabel("LA BASE HA CAÍDO", 40, UiTheme.Danger);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		titleBox.AddChild(title);
		_records = UiTheme.MakeLabel("", 15, UiTheme.LcdDim);
		_records.HorizontalAlignment = HorizontalAlignment.Center;
		titleBox.AddChild(_records);

		var columns = new HBoxContainer();
		columns.AddThemeConstantOverride("separation", 12);
		layout.AddChild(columns);

		var statsScreen = UiTheme.MakePanel(UiTheme.Screen());
		statsScreen.CustomMinimumSize = new Vector2(440, 0);
		columns.AddChild(statsScreen);
		var statsBox = new VBoxContainer();
		statsScreen.AddChild(statsBox);
		statsBox.AddChild(UiTheme.MakeLabel("ESTADÍSTICAS", 22));
		_statsGrid = new GridContainer { Columns = 2 };
		_statsGrid.AddThemeConstantOverride("h_separation", 24);
		statsBox.AddChild(_statsGrid);

		var perksScreen = UiTheme.MakePanel(UiTheme.Screen());
		perksScreen.CustomMinimumSize = new Vector2(300, 0);
		columns.AddChild(perksScreen);
		_perkList = new VBoxContainer();
		perksScreen.AddChild(_perkList);

		var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
		buttons.AddThemeConstantOverride("separation", 16);
		layout.AddChild(buttons);

		var retry = UiTheme.MakeButton("NUEVA PARTIDA", 20);
		retry.Pressed += () =>
		{
			GameManager.Instance.StartNewGame();
			GetTree().Paused = false;
			GetTree().ReloadCurrentScene();
		};
		buttons.AddChild(retry);

		var menu = UiTheme.MakeButton("MENÚ PRINCIPAL", 20);
		menu.Pressed += () =>
		{
			GetTree().Paused = false;
			GetTree().ChangeSceneToFile(PauseMenu.MenuScenePath);
		};
		buttons.AddChild(menu);
	}

	public void ShowResults()
	{
		var gm = GameManager.Instance;
		var stats = gm.Stats;

		foreach (Node child in _statsGrid.GetChildren()) child.QueueFree();
		AddStat("Días sobrevividos", stats.DaysSurvived.ToString());
		AddStat("Día alcanzado", gm.CurrentDay.ToString());
		AddStat("Tiempo de juego", FormatTime(stats.TimePlayed));
		AddStat("Enemigos abatidos", stats.TotalKills.ToString());
		foreach (EnemyType type in System.Enum.GetValues<EnemyType>())
		{
			int kills = stats.GetKills(type);
			if (kills > 0) AddStat($"  · {GameData.Enemies[type].Name}", kills.ToString());
		}
		AddStat("Jefes derrotados", stats.BossesKilled.ToString());
		AddStat("Daño causado", Mathf.RoundToInt(stats.DamageDealt).ToString());
		AddStat("Chatarra ganada", stats.ScrapEarned.ToString());
		AddStat("Chatarra gastada", stats.ScrapSpent.ToString());
		AddStat("Chatarra robada", stats.ScrapStolen.ToString());
		AddStat("Unidades reclutadas", stats.UnitsRecruited.ToString());
		AddStat("Unidades perdidas", stats.UnitsLost.ToString());
		AddStat("Estructuras construidas", stats.StructuresBuilt.ToString());
		AddStat("Estructuras perdidas", stats.StructuresLost.ToString());

		foreach (Node child in _perkList.GetChildren()) child.QueueFree();
		_perkList.AddChild(UiTheme.MakeLabel("PERKS CONSEGUIDOS", 22));
		if (gm.Perks.Count == 0)
		{
			_perkList.AddChild(UiTheme.MakeLabel("Ninguno. Derrotá a un jefe", 15, UiTheme.LcdDim));
			_perkList.AddChild(UiTheme.MakeLabel($"(aparecen cada 5 días).", 15, UiTheme.LcdDim));
		}
		foreach (var pair in gm.Perks)
		{
			_perkList.AddChild(UiTheme.MakeLabel($"{GameData.Perks[pair.Key].Name}  x{pair.Value}", 17, UiTheme.Warning));
			var description = UiTheme.MakeLabel(GameData.Perks[pair.Key].Description, 13, UiTheme.LcdDim);
			description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			description.CustomMinimumSize = new Vector2(270, 0);
			_perkList.AddChild(description);
		}

		var records = SaveManager.LoadRecords();
		_records.Text = $"Récord: {records.BestDay} días · Partidas jugadas: {records.GamesPlayed} · Bajas totales: {records.TotalKills}";

		Visible = true;
		GetTree().Paused = true;
	}

	private void AddStat(string name, string value)
	{
		_statsGrid.AddChild(UiTheme.MakeLabel(name, 16));
		var valueLabel = UiTheme.MakeLabel(value, 16, UiTheme.Sand);
		valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
		_statsGrid.AddChild(valueLabel);
	}

	public static string FormatTime(float seconds)
	{
		int total = Mathf.FloorToInt(seconds);
		return $"{total / 60:00}:{total % 60:00}";
	}
}
