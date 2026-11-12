using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Hud : Control
{
	private const int MaxRadioLines = 3;
	private const float RadioLifetime = 7f;

	private Main _main;
	private SpawnManager _spawnManager;
	private BuildManager _buildManager;
	private Base _base;

	private Label _dayLabel;
	private Label _statusLabel;
	private Label _scrapLabel;
	private ProgressBar _baseHealthBar;
	private Label _baseLabel;
	private VBoxContainer _radioLines;
	private Label _selectionLabel;
	private Control _selectionPanel;
	private Button _nextDayButton;
	private Button _skipWaitButton;
	private Control _bossPanel;
	private ProgressBar _bossBar;
	private Label _bossLabel;
	private Minimap _minimap;
	private readonly Dictionary<UnitType, Button> _recruitButtons = new();
	private readonly Dictionary<StructureType, Button> _buildButtons = new();

	private PerkSelectionPanel _perkPanel;
	private PauseMenu _pauseMenu;
	private GameOverScreen _gameOver;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Theme = UiTheme.Build();
		UiTheme.FullRect(this);
		MouseFilter = MouseFilterEnum.Ignore;

		foreach (Node child in GetChildren())
			child.QueueFree();

		_main = GetParent()?.GetParent() as Main ?? GetTree().CurrentScene as Main;
		_spawnManager = _main?.GetNodeOrNull<SpawnManager>("SpawnManager");
		_buildManager = _main?.GetNodeOrNull<BuildManager>("BuildManager");
		_base = _main?.GetNodeOrNull<Base>("Base");

		BuildTopBar();
		BuildBossBar();
		BuildRadio();
		BuildMinimap();
		BuildCommandBar();
		BuildDayButtons();

		_perkPanel = new PerkSelectionPanel();
		AddChild(_perkPanel);
		_pauseMenu = new PauseMenu();
		AddChild(_pauseMenu);
		_gameOver = new GameOverScreen();
		AddChild(_gameOver);

		var gm = GameManager.Instance;
		gm.ScrapChanged += OnScrapChanged;
		gm.DayChanged += OnDayChanged;
		gm.GameStateChanged += OnGameStateChanged;
		gm.RadioMessage += OnRadioMessage;
		gm.PerksChanged += UpdateRecruitLabels;
		gm.BossDefeated += OnBossDefeated;
		gm.GameOver += OnGameOver;

		if (_base != null)
		{
			_base.Health.HealthChanged += OnBaseHealthChanged;
			CallDeferred(MethodName.RefreshBaseHealth);
		}

		CallDeferred(MethodName.ConnectSelection);

		UpdateScrapLabel(gm.CurrentScrap);
		OnDayChanged(gm.CurrentDay, gm.CurrentRound);
		UpdateRecruitLabels();
		OnGameStateChanged((int)gm.CurrentState);
	}

	public override void _ExitTree()
	{
		var gm = GameManager.Instance;
		if (gm == null) return;
		gm.ScrapChanged -= OnScrapChanged;
		gm.DayChanged -= OnDayChanged;
		gm.GameStateChanged -= OnGameStateChanged;
		gm.RadioMessage -= OnRadioMessage;
		gm.PerksChanged -= UpdateRecruitLabels;
		gm.BossDefeated -= OnBossDefeated;
		gm.GameOver -= OnGameOver;
	}

	public override void _Process(double delta)
	{
		var gm = GameManager.Instance;
		if (_spawnManager != null)
		{
			string status;
			if (gm.CurrentState == GameState.Intermission) status = "Zona segura";
			else if (_spawnManager.WaitingForNextRound) status = $"Próxima oleada en {Mathf.CeilToInt(_spawnManager.NextRoundCountdown)} s";
			else status = $"{_spawnManager.EnemiesRemaining} enemigos";
			_statusLabel.Text = status;
			_skipWaitButton.Visible = _spawnManager.WaitingForNextRound && gm.CurrentState == GameState.Combat;
		}

		foreach (var pair in _recruitButtons)
			pair.Value.Disabled = gm.CurrentScrap < gm.GetUnitCost(pair.Key) || gm.CurrentState == GameState.GameOver;
		foreach (var pair in _buildButtons)
			pair.Value.Disabled = gm.CurrentScrap < GameData.Structures[pair.Key].Cost || gm.CurrentState == GameState.GameOver;

		UpdateBossBar();
		FadeRadio((float)delta);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;
		if (_gameOver.Visible || _perkPanel.IsOpen) return;

		if (key.Keycode == Key.Escape)
		{
			if (_pauseMenu.IsOpen) _pauseMenu.Close();
			else _pauseMenu.Open();
			GetViewport().SetInputAsHandled();
			return;
		}
		if (_pauseMenu.IsOpen) return;

		foreach (var pair in GameData.Units)
		{
			if (key.Keycode == pair.Value.Hotkey)
			{
				_main?.TrySpawnUnit(pair.Key);
				GetViewport().SetInputAsHandled();
				return;
			}
		}
		foreach (var pair in GameData.Structures)
		{
			if (key.Keycode == pair.Value.Hotkey)
			{
				_buildManager?.StartPlacing(pair.Key);
				GetViewport().SetInputAsHandled();
				return;
			}
		}
		if (key.Keycode == Key.M)
		{
			_minimap.Visible = !_minimap.Visible;
			GetViewport().SetInputAsHandled();
		}
		else if ((key.Keycode == Key.Enter || key.Keycode == Key.KpEnter) && _nextDayButton.Visible)
		{
			OnNextDayPressed();
			GetViewport().SetInputAsHandled();
		}
		else if (key.Keycode == Key.N && _skipWaitButton.Visible)
		{
			_spawnManager.SkipWait();
			GetViewport().SetInputAsHandled();
		}
	}

	private void BuildTopBar()
	{
		var bar = UiTheme.MakePanel(UiTheme.Box(new Color(UiTheme.LcdBack, 0.85f), UiTheme.Black, 2, 4, 4));
		bar.Position = new Vector2(8, 8);
		AddChild(bar);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 14);
		bar.AddChild(row);

		_dayLabel = UiTheme.MakeLabel("DÍA 1 · 1/2", 15);
		row.AddChild(_dayLabel);
		_statusLabel = UiTheme.MakeLabel("", 13, UiTheme.Sand);
		_statusLabel.CustomMinimumSize = new Vector2(150, 0);
		row.AddChild(_statusLabel);

		row.AddChild(new VSeparator());
		_scrapLabel = UiTheme.MakeLabel("⚙ 0", 15, UiTheme.Warning);
		_scrapLabel.TooltipText = "Chatarra";
		_scrapLabel.MouseFilter = MouseFilterEnum.Pass;
		_scrapLabel.CustomMinimumSize = new Vector2(60, 0);
		row.AddChild(_scrapLabel);

		row.AddChild(new VSeparator());
		row.AddChild(UiTheme.MakeLabel("BASE", 13, UiTheme.Sand));
		_baseHealthBar = new ProgressBar { ShowPercentage = false, CustomMinimumSize = new Vector2(110, 10), SizeFlagsVertical = SizeFlags.ShrinkCenter };
		row.AddChild(_baseHealthBar);
		_baseLabel = UiTheme.MakeLabel("600", 13);
		row.AddChild(_baseLabel);

		row.AddChild(new VSeparator());
		row.AddChild(UiTheme.MakeLabel("Esc: menú", 12, UiTheme.LcdDim));
	}

	private void BuildBossBar()
	{
		var panel = UiTheme.MakePanel(UiTheme.Box(new Color(UiTheme.LcdBack, 0.85f), UiTheme.Black, 2, 4, 4));
		panel.SetAnchorsPreset(LayoutPreset.CenterTop);
		panel.GrowHorizontal = GrowDirection.Both;
		panel.Position = new Vector2(0, 52);
		AddChild(panel);
		_bossPanel = panel;

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 2);
		panel.AddChild(box);
		_bossLabel = UiTheme.MakeLabel("ABOMINACIÓN", 13, UiTheme.Danger);
		_bossLabel.HorizontalAlignment = HorizontalAlignment.Center;
		box.AddChild(_bossLabel);
		_bossBar = new ProgressBar { ShowPercentage = false, CustomMinimumSize = new Vector2(280, 8) };
		_bossBar.AddThemeStyleboxOverride("fill", UiTheme.Box(UiTheme.Danger, UiTheme.Danger, 0, 2, 0));
		box.AddChild(_bossBar);
		_bossPanel.Visible = false;
	}

	private void BuildRadio()
	{
		_radioLines = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		_radioLines.SetAnchorsPreset(LayoutPreset.BottomLeft);
		_radioLines.GrowVertical = GrowDirection.Begin;
		_radioLines.Position = new Vector2(12, -84);
		_radioLines.CustomMinimumSize = new Vector2(360, 0);
		_radioLines.AddThemeConstantOverride("separation", 2);
		AddChild(_radioLines);
	}

	private void BuildMinimap()
	{
		var frame = UiTheme.MakePanel(UiTheme.Box(UiTheme.Charcoal, UiTheme.Black, 2, 4, 3));
		frame.SetAnchorsPreset(LayoutPreset.BottomRight);
		frame.GrowHorizontal = GrowDirection.Begin;
		frame.GrowVertical = GrowDirection.Begin;
		frame.Position = new Vector2(-12, -12);
		AddChild(frame);
		_minimap = new Minimap { CustomMinimumSize = new Vector2(160, 100) };
		frame.AddChild(_minimap);
		_minimap.VisibilityChanged += () => frame.Visible = _minimap.Visible;
	}

	private void BuildCommandBar()
	{
		var bar = UiTheme.MakePanel(UiTheme.Box(new Color(UiTheme.Charcoal, 0.92f), UiTheme.Black, 2, 4, 4));
		bar.SetAnchorsPreset(LayoutPreset.CenterBottom);
		bar.GrowHorizontal = GrowDirection.Both;
		bar.GrowVertical = GrowDirection.Begin;
		bar.Position = new Vector2(0, -8);
		AddChild(bar);

		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 4);
		bar.AddChild(row);

		foreach (var pair in GameData.Units)
		{
			UnitType type = pair.Key;
			var button = MakeIconButton(pair.Value.Texture);
			button.TooltipText = $"{pair.Value.Name} ({pair.Value.Hotkey})\n{pair.Value.Role}";
			button.Pressed += () => _main?.TrySpawnUnit(type);
			row.AddChild(button);
			_recruitButtons[type] = button;
		}

		row.AddChild(new VSeparator());
		foreach (var pair in GameData.Structures)
		{
			StructureType type = pair.Key;
			var button = MakeIconButton(pair.Value.Texture);
			button.Text = pair.Value.Cost.ToString();
			button.TooltipText = type == StructureType.Torre
				? $"Torre ({pair.Value.Hotkey})\nHasta 2 soldados arriba: más alcance y menos daño recibido."
				: $"Muro ({pair.Value.Hotkey})\nBloquea a los zombies. Shift + click para poner varios.";
			button.Pressed += () => _buildManager?.StartPlacing(type);
			row.AddChild(button);
			_buildButtons[type] = button;
		}

		row.AddChild(new VSeparator());
		var home = UiTheme.MakeButton("⌂", 20);
		home.CustomMinimumSize = new Vector2(40, 40);
		home.TooltipText = "Volver a la base (H)\nCura a las unidades y les devuelve la moral.";
		home.Pressed += () => SelectionManager.Instance?.OrderReturnToBase();
		row.AddChild(home);

		_selectionPanel = UiTheme.MakePanel(UiTheme.Box(new Color(UiTheme.LcdBack, 0.85f), UiTheme.Black, 1, 3, 4));
		_selectionPanel.SetAnchorsPreset(LayoutPreset.CenterBottom);
		_selectionPanel.GrowHorizontal = GrowDirection.Both;
		_selectionPanel.GrowVertical = GrowDirection.Begin;
		_selectionPanel.Position = new Vector2(0, -62);
		AddChild(_selectionPanel);
		_selectionLabel = UiTheme.MakeLabel("", 13);
		_selectionPanel.AddChild(_selectionLabel);
		_selectionPanel.Visible = false;
	}

	private static Button MakeIconButton(string texturePath)
	{
		var button = UiTheme.MakeButton("", 12);
		button.Icon = GD.Load<Texture2D>(texturePath);
		button.ExpandIcon = true;
		button.IconAlignment = HorizontalAlignment.Left;
		button.CustomMinimumSize = new Vector2(64, 40);
		button.AddThemeConstantOverride("icon_max_width", 28);
		button.AddThemeStyleboxOverride("normal", UiTheme.Box(UiTheme.OliveDark, UiTheme.Black, 1, 3, 3));
		button.AddThemeStyleboxOverride("hover", UiTheme.Box(UiTheme.Olive, UiTheme.LcdText, 1, 3, 3));
		button.AddThemeStyleboxOverride("pressed", UiTheme.Box(UiTheme.LcdBack, UiTheme.LcdText, 1, 3, 3));
		button.AddThemeStyleboxOverride("disabled", UiTheme.Box(new Color(0.15f, 0.17f, 0.15f), UiTheme.Black, 1, 3, 3));
		return button;
	}

	private void BuildDayButtons()
	{
		_nextDayButton = UiTheme.MakeButton("▶ Día 2", 18);
		_nextDayButton.SetAnchorsPreset(LayoutPreset.CenterBottom);
		_nextDayButton.GrowHorizontal = GrowDirection.Both;
		_nextDayButton.GrowVertical = GrowDirection.Begin;
		_nextDayButton.CustomMinimumSize = new Vector2(200, 38);
		_nextDayButton.Position = new Vector2(-100, -110);
		_nextDayButton.TooltipText = "Empezar el día siguiente (Enter)";
		_nextDayButton.Pressed += OnNextDayPressed;
		_nextDayButton.Visible = false;
		AddChild(_nextDayButton);

		_skipWaitButton = UiTheme.MakeButton("Adelantar oleada (N)", 13);
		_skipWaitButton.Position = new Vector2(8, 46);
		_skipWaitButton.Pressed += () => _spawnManager?.SkipWait();
		_skipWaitButton.Visible = false;
		AddChild(_skipWaitButton);
	}

	private void UpdateRecruitLabels()
	{
		foreach (var pair in _recruitButtons)
			pair.Value.Text = GameManager.Instance.GetUnitCost(pair.Key).ToString();
	}

	private void UpdateSelection()
	{
		var selection = SelectionManager.Instance?.Selected;
		var valid = selection?.Where(IsInstanceValid).ToList();
		if (valid == null || valid.Count == 0)
		{
			_selectionPanel.Visible = false;
			return;
		}

		var counts = valid.GroupBy(s => s.Type).Select(g => g.Count() > 1 ? $"{GameData.Units[g.Key].Name} x{g.Count()}" : GameData.Units[g.Key].Name);
		string text = $"{valid.Count} seleccionados: {string.Join(", ", counts)}";
		var squads = valid.Where(s => s.SquadId > 0).Select(s => s.SquadId).Distinct().OrderBy(id => id).ToList();
		if (squads.Count > 0) text += $"  ·  Escuadra {string.Join(",", squads)}";
		if (valid.Any(s => s.Demoralized)) text += "  ·  ▼ sin moral: H";
		_selectionLabel.Text = text;
		_selectionPanel.Visible = true;
	}

	private void ConnectSelection()
	{
		if (SelectionManager.Instance != null)
			SelectionManager.Instance.SelectionChanged += UpdateSelection;
	}

	private void RefreshBaseHealth()
	{
		var health = _base?.Health;
		if (health != null) OnBaseHealthChanged(health.CurrentHealth, health.MaxHealth);
	}

	private void UpdateBossBar()
	{
		var bosses = GetTree().GetNodesInGroup("boss");
		Zombie boss = bosses.Count > 0 ? bosses[0] as Zombie : null;
		bool show = boss != null && IsInstanceValid(boss) && boss.Health != null;
		_bossPanel.Visible = show;
		if (!show) return;
		_bossBar.MaxValue = boss.Health.MaxHealth;
		_bossBar.Value = boss.Health.CurrentHealth;
		_bossLabel.Text = boss.Stats.Name.ToUpper();
	}

	private void OnScrapChanged(int newAmount) => UpdateScrapLabel(newAmount);

	private void UpdateScrapLabel(int amount) => _scrapLabel.Text = $"⚙ {amount}";

	private void OnDayChanged(int day, int round)
	{
		_dayLabel.Text = $"DÍA {Mathf.Max(day, 1)} · {Mathf.Max(round, 1)}/2";
		_nextDayButton.Text = $"▶ Día {day + 1}";
	}

	private void OnBaseHealthChanged(float current, float max)
	{
		_baseHealthBar.MaxValue = max;
		_baseHealthBar.Value = current;
		_baseLabel.Text = Mathf.CeilToInt(current).ToString();
		_baseLabel.AddThemeColorOverride("font_color", current / max < 0.3f ? UiTheme.Danger : UiTheme.LcdText);
	}

	private void OnGameStateChanged(int newState)
	{
		_nextDayButton.Visible = (GameState)newState == GameState.Intermission;
	}

	private void OnRadioMessage(string text, int kind)
	{
		var line = UiTheme.MakeLabel(text, 13, UiTheme.ForRadio((RadioKind)kind));
		line.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		line.CustomMinimumSize = new Vector2(360, 0);
		line.AddThemeColorOverride("font_outline_color", UiTheme.Black);
		line.AddThemeConstantOverride("outline_size", 4);
		line.SetMeta("age", 0f);
		_radioLines.AddChild(line);

		while (_radioLines.GetChildCount() > MaxRadioLines)
		{
			var oldest = _radioLines.GetChild(0);
			_radioLines.RemoveChild(oldest);
			oldest.QueueFree();
		}
	}

	private void FadeRadio(float delta)
	{
		foreach (Node child in _radioLines.GetChildren())
		{
			if (child is not Label label) continue;
			float age = (float)label.GetMeta("age") + delta;
			label.SetMeta("age", age);
			label.Modulate = new Color(1, 1, 1, Mathf.Clamp((RadioLifetime - age) / 1.5f, 0f, 1f));
			if (age >= RadioLifetime) label.QueueFree();
		}
	}

	private void OnBossDefeated() => _perkPanel.Open();

	private void OnGameOver() => _gameOver.ShowResults();

	private void OnNextDayPressed()
	{
		_nextDayButton.Visible = false;
		_spawnManager?.StartNewDay();
	}
}
