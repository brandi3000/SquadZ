using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class PerkSelectionPanel : Control
{
	[Signal] public delegate void PerkChosenEventHandler();

	private HBoxContainer _cards;
	private Label _subtitle;
	private int _pending;
	private readonly RandomNumberGenerator _rng = new();

	public bool IsOpen => Visible;

	public override void _Ready()
	{
		_rng.Randomize();
		ProcessMode = ProcessModeEnum.Always;
		MouseFilter = MouseFilterEnum.Stop;
		UiTheme.FullRect(this);
		Visible = false;

		var dim = new ColorRect { Color = new Color(0, 0, 0, 0.7f), MouseFilter = MouseFilterEnum.Ignore };
		AddChild(UiTheme.FullRect(dim));

		var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		AddChild(UiTheme.FullRect(center));

		var device = UiTheme.MakePanel(UiTheme.Device());
		center.AddChild(device);

		var layout = new VBoxContainer();
		layout.AddThemeConstantOverride("separation", 14);
		device.AddChild(layout);

		var screen = UiTheme.MakePanel(UiTheme.Screen());
		layout.AddChild(screen);
		var header = new VBoxContainer();
		screen.AddChild(header);
		var title = UiTheme.MakeLabel("JEFE ABATIDO — ELEGÍ UN PERK", 30);
		title.HorizontalAlignment = HorizontalAlignment.Center;
		header.AddChild(title);
		_subtitle = UiTheme.MakeLabel("Las mejoras son permanentes y acumulables.", 15, UiTheme.LcdDim);
		_subtitle.HorizontalAlignment = HorizontalAlignment.Center;
		header.AddChild(_subtitle);

		_cards = new HBoxContainer();
		_cards.AddThemeConstantOverride("separation", 14);
		layout.AddChild(_cards);
	}

	public void Open()
	{
		_pending++;
		if (Visible) return;
		ShowChoices();
	}

	private void ShowChoices()
	{
		foreach (Node child in _cards.GetChildren())
			child.QueueFree();

		var all = GameData.Perks.Keys.ToList();
		var choices = new List<PerkType>();
		while (choices.Count < 3 && all.Count > 0)
		{
			int index = _rng.RandiRange(0, all.Count - 1);
			choices.Add(all[index]);
			all.RemoveAt(index);
		}

		foreach (PerkType perk in choices)
			_cards.AddChild(MakeCard(perk));

		_subtitle.Text = _pending > 1 ? $"Las mejoras son permanentes y acumulables. Elecciones pendientes: {_pending}" : "Las mejoras son permanentes y acumulables.";
		Visible = true;
		GetTree().Paused = true;
	}

	private Control MakeCard(PerkType perk)
	{
		var info = GameData.Perks[perk];
		int stacks = GameManager.Instance.GetPerkStacks(perk);

		var card = UiTheme.MakePanel(UiTheme.Screen());
		card.CustomMinimumSize = new Vector2(250, 210);
		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 8);
		card.AddChild(box);

		box.AddChild(UiTheme.MakeLabel(info.Name.ToUpper(), 20, UiTheme.Warning));
		var description = UiTheme.MakeLabel(info.Description, 15);
		description.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		description.CustomMinimumSize = new Vector2(230, 70);
		description.SizeFlagsVertical = SizeFlags.ExpandFill;
		box.AddChild(description);
		box.AddChild(UiTheme.MakeLabel(stacks > 0 ? $"Nivel actual: {stacks} → {stacks + 1}" : "Nuevo", 14, UiTheme.LcdDim));

		var choose = UiTheme.MakeButton("ELEGIR", 17);
		choose.Pressed += () => Choose(perk);
		box.AddChild(choose);
		return card;
	}

	private void Choose(PerkType perk)
	{
		GameManager.Instance.AddPerk(perk);
		_pending = Mathf.Max(0, _pending - 1);
		if (_pending > 0)
		{
			ShowChoices();
			return;
		}
		Visible = false;
		GetTree().Paused = false;
		EmitSignal(SignalName.PerkChosen);
	}
}
