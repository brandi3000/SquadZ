using Godot;

public partial class Mejoras : Button
{
	private Control _panel;

	public override void _Ready()
	{
		Pressed += mejoras;
	}

	public void mejoras()
	{
		if (_panel == null) BuildPanel();
		_panel.Visible = true;
	}

	private void BuildPanel()
	{
		_panel = new Control { Theme = UiTheme.Build(), MouseFilter = MouseFilterEnum.Stop };
		UiTheme.FullRect(_panel);
		var root = GetTree().CurrentScene as Control;
		(root ?? (Node)GetParent()).AddChild(_panel);

		_panel.AddChild(UiTheme.FullRect(new ColorRect { Color = new Color(0, 0, 0, 0.75f), MouseFilter = MouseFilterEnum.Ignore }));
		var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		_panel.AddChild(UiTheme.FullRect(center));

		var device = UiTheme.MakePanel(UiTheme.Device());
		center.AddChild(device);
		var layout = new VBoxContainer();
		layout.AddThemeConstantOverride("separation", 10);
		device.AddChild(layout);

		var records = SaveManager.LoadRecords();
		var recordScreen = UiTheme.MakePanel(UiTheme.Screen());
		layout.AddChild(recordScreen);
		var recordBox = new VBoxContainer();
		recordScreen.AddChild(recordBox);
		recordBox.AddChild(UiTheme.MakeLabel("RÉCORDS", 28));
		recordBox.AddChild(UiTheme.MakeLabel($"Mejor marca: {records.BestDay} días   ·   Más bajas en una partida: {records.MostKills}", 16));
		recordBox.AddChild(UiTheme.MakeLabel($"Partidas jugadas: {records.GamesPlayed}   ·   Bajas totales: {records.TotalKills}", 16));

		var perkScreen = UiTheme.MakePanel(UiTheme.Screen());
		layout.AddChild(perkScreen);
		var perkBox = new VBoxContainer();
		perkScreen.AddChild(perkBox);
		perkBox.AddChild(UiTheme.MakeLabel("PERKS DISPONIBLES", 24));
		perkBox.AddChild(UiTheme.MakeLabel("Cada 5 días aparece un jefe. Al derrotarlo elegís uno de 3 perks (permanentes y acumulables).", 14, UiTheme.LcdDim));
		var grid = new GridContainer { Columns = 2 };
		grid.AddThemeConstantOverride("h_separation", 24);
		perkBox.AddChild(grid);
		foreach (var pair in GameData.Perks)
		{
			grid.AddChild(UiTheme.MakeLabel(pair.Value.Name, 16, UiTheme.Warning));
			grid.AddChild(UiTheme.MakeLabel(pair.Value.Description, 15));
		}

		var close = UiTheme.MakeButton("VOLVER", 20);
		close.Pressed += () => _panel.Visible = false;
		layout.AddChild(close);
	}
}
