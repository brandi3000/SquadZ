using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Main : Node2D
{
	[Export] private PackedScene _soldierScene;

	private Node2D _unitsContainer;
	private Node2D _structuresContainer;
	private Base _base;
	private SpawnManager _spawnManager;
	private BuildManager _buildManager;
	private int _fortalezaStacks;

	public Health BaseHealth => _base.Health;
	public Base Base => _base;
	public SpawnManager SpawnManager => _spawnManager;
	public BuildManager BuildManager => _buildManager;

	public override void _Ready()
	{
		GetTree().Paused = false;
		AddChild(new SquadManager { Name = "SquadManager" });
		AddChild(new Effects { Name = "Effects" });

		_unitsContainer = GetNode<Node2D>("Units");
		_structuresContainer = GetNode<Node2D>("Structures");
		_base = GetNode<Base>("Base");
		_spawnManager = GetNode<SpawnManager>("SpawnManager");
		_buildManager = GetNode<BuildManager>("BuildManager");

		_base.Health.Died += OnBaseDied;
		GameManager.Instance.PerksChanged += OnPerksChanged;

		var gm = GameManager.Instance;
		if (gm.PendingLoad != null)
		{
			ApplyLoad(gm.PendingLoad);
			gm.PendingLoad = null;
		}
		else
		{
			if (gm.CurrentDay > 0 || gm.CurrentState == GameState.GameOver)
				gm.StartNewGame();
			gm.Radio("Aquí base. Extracción fuera de servicio: hay que resistir.", RadioKind.Info);
			gm.Radio("Cámara: WASD o bordes de pantalla. Esc: menú y controles.", RadioKind.Info);
			_spawnManager.StartNewDay();
		}
		_fortalezaStacks = gm.GetPerkStacks(PerkType.Fortaleza);
	}

	public override void _ExitTree()
	{
		if (GameManager.Instance != null)
			GameManager.Instance.PerksChanged -= OnPerksChanged;
	}

	public bool TrySpawnUnit(UnitType type)
	{
		if (GameManager.Instance.CurrentState == GameState.GameOver) return false;
		int cost = GameManager.Instance.GetUnitCost(type);
		if (!GameManager.Instance.SpendScrap(cost))
		{
			GameManager.Instance.Radio($"No hay suficiente chatarra para un {GameData.Units[type].Name} ({cost}).", RadioKind.Warning);
			return false;
		}

		Vector2 spawnOffset = new Vector2((float)GD.RandRange(-14, 14), (float)GD.RandRange(-14, 14));
		SpawnUnit(type, _base.SpawnPosition + spawnOffset);
		GameManager.Instance.Stats.UnitsRecruited++;
		return true;
	}

	public void TrySpawnSoldier() => TrySpawnUnit(UnitType.Fusilero);

	public Soldier SpawnUnit(UnitType type, Vector2 position)
	{
		Soldier soldier = _soldierScene.Instantiate<Soldier>();
		soldier.Setup(type);
		soldier.Position = position;
		_unitsContainer.AddChild(soldier);
		return soldier;
	}

	public IEnumerable<Soldier> GetSoldiers() => _unitsContainer.GetChildren().OfType<Soldier>().Where(s => !s.IsQueuedForDeletion());

	public IEnumerable<Structure> GetStructures() => _structuresContainer.GetChildren().OfType<Structure>().Where(s => !s.IsQueuedForDeletion());

	public bool SaveGame()
	{
		if (GameManager.Instance.CurrentState != GameState.Intermission)
		{
			GameManager.Instance.Radio("Solo se puede guardar entre días.", RadioKind.Warning);
			return false;
		}
		bool ok = SaveManager.Save(this);
		GameManager.Instance.Radio(ok ? "Partida guardada." : "Error al guardar la partida.", ok ? RadioKind.Good : RadioKind.Danger);
		return ok;
	}

	private void ApplyLoad(SaveData data)
	{
		foreach (Node child in _unitsContainer.GetChildren())
		{
			_unitsContainer.RemoveChild(child);
			child.QueueFree();
		}

		_base.ApplyFortalezaPerk();
		if (data.BaseHealth > 0) _base.Health.SetCurrentHealth(data.BaseHealth);

		foreach (UnitSave unit in data.Units)
		{
			if (!System.Enum.TryParse(unit.Type, out UnitType type)) continue;
			Soldier soldier = SpawnUnit(type, new Vector2(unit.X, unit.Y));
			if (unit.Health > 0) soldier.RestoreHealth(unit.Health);
			if (unit.Squad > 0) SquadManager.Instance.RestoreMember(soldier, unit.Squad, unit.Leader);
		}

		foreach (StructureSave saved in data.Structures)
		{
			if (!System.Enum.TryParse(saved.Type, out StructureType type)) continue;
			Structure structure = _buildManager.CreateStructure(type);
			structure.GlobalPosition = new Vector2(saved.X, saved.Y);
			structure.LoadState(saved.Progress, saved.Health);
		}

		_spawnManager.ResumeFromSave(data.Day);
	}

	private void OnPerksChanged()
	{
		int stacks = GameManager.Instance.GetPerkStacks(PerkType.Fortaleza);
		if (stacks != _fortalezaStacks)
		{
			_fortalezaStacks = stacks;
			_base.ApplyFortalezaPerk();
		}
	}

	private void OnBaseDied()
	{
		_buildManager.CancelPlacement();
		GameManager.Instance.Radio("¡La base fue destruida!", RadioKind.Danger);
		GameManager.Instance.TriggerGameOver();
	}
}
