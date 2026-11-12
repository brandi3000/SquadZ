using Godot;
using System.Collections.Generic;

public enum GameState
{
	Combat,
	Intermission,
	GameOver
}

public enum RadioKind
{
	Info,
	Warning,
	Danger,
	Good
}

public partial class GameManager : Node
{
	public const int StartingScrap = 40;

	public static GameManager Instance { get; private set; }

	[Signal] public delegate void ScrapChangedEventHandler(int newAmount);
	[Signal] public delegate void WaveChangedEventHandler(int newWave);
	[Signal] public delegate void DayChangedEventHandler(int day, int round);
	[Signal] public delegate void GameStateChangedEventHandler(int newState);
	[Signal] public delegate void RadioMessageEventHandler(string text, int kind);
	[Signal] public delegate void PerksChangedEventHandler();
	[Signal] public delegate void BossDefeatedEventHandler();
	[Signal] public delegate void GameOverEventHandler();

	private int _scrap = StartingScrap;
	private int _currentDay = 0;
	private int _currentRound = 0;
	private GameState _currentState = GameState.Combat;
	private readonly Dictionary<PerkType, int> _perks = new();

	public GameStats Stats { get; private set; } = new GameStats();
	public SaveData PendingLoad { get; set; }

	public int CurrentScrap => _scrap;
	public int CurrentWave => _currentDay;
	public int CurrentDay => _currentDay;
	public int CurrentRound => _currentRound;
	public GameState CurrentState => _currentState;
	public IReadOnlyDictionary<PerkType, int> Perks => _perks;

	public override void _Ready()
	{
		Instance = this;
	}

	public override void _Process(double delta)
	{
		if (_currentState != GameState.GameOver && _currentDay > 0)
			Stats.TimePlayed += (float)delta;
	}

	public void StartNewGame()
	{
		_scrap = StartingScrap;
		_currentDay = 0;
		_currentRound = 0;
		_currentState = GameState.Combat;
		_perks.Clear();
		Stats = new GameStats();
		PendingLoad = null;
	}

	public void RestoreFromSave(SaveData data)
	{
		StartNewGame();
		_scrap = data.Scrap;
		_currentDay = data.Day;
		_currentRound = 2;
		Stats = data.Stats ?? new GameStats();
		foreach (var pair in data.Perks)
		{
			if (System.Enum.TryParse(pair.Key, out PerkType perk))
				_perks[perk] = pair.Value;
		}
		PendingLoad = data;
	}

	public void AddScrap(int amount)
	{
		_scrap += amount;
		Stats.ScrapEarned += amount;
		EmitSignal(SignalName.ScrapChanged, _scrap);
	}

	public void ReturnScrap(int amount)
	{
		_scrap += amount;
		Stats.ScrapStolen -= amount;
		EmitSignal(SignalName.ScrapChanged, _scrap);
	}

	public int AddScrapFromKill(int baseAmount)
	{
		int amount = Mathf.RoundToInt(baseAmount * ScrapMultiplier);
		AddScrap(amount);
		return amount;
	}

	public bool SpendScrap(int amount)
	{
		if (_scrap < amount) return false;
		_scrap -= amount;
		Stats.ScrapSpent += amount;
		EmitSignal(SignalName.ScrapChanged, _scrap);
		return true;
	}

	public int StealAllScrap()
	{
		int stolen = _scrap;
		_scrap = 0;
		Stats.ScrapStolen += stolen;
		EmitSignal(SignalName.ScrapChanged, _scrap);
		return stolen;
	}

	public void NextWave()
	{
		SetDay(_currentDay + 1, 1);
	}

	public void SetDay(int day, int round)
	{
		_currentDay = day;
		_currentRound = round;
		Stats.DaysSurvived = Mathf.Max(0, day - 1);
		EmitSignal(SignalName.WaveChanged, _currentDay);
		EmitSignal(SignalName.DayChanged, _currentDay, _currentRound);
	}

	public void SetState(GameState newState)
	{
		_currentState = newState;
		if (newState == GameState.Intermission)
			Stats.DaysSurvived = _currentDay;
		EmitSignal(SignalName.GameStateChanged, (int)newState);
	}

	public void Radio(string text, RadioKind kind = RadioKind.Info)
	{
		EmitSignal(SignalName.RadioMessage, text, (int)kind);
	}

	public void AddPerk(PerkType perk)
	{
		_perks[perk] = GetPerkStacks(perk) + 1;
		Radio($"Perk obtenido: {GameData.Perks[perk].Name} (x{_perks[perk]})", RadioKind.Good);
		EmitSignal(SignalName.PerksChanged);
	}

	public int GetPerkStacks(PerkType perk) => _perks.TryGetValue(perk, out int stacks) ? stacks : 0;

	public void NotifyBossDefeated()
	{
		Stats.AddKill(EnemyType.Jefe);
		EmitSignal(SignalName.BossDefeated);
	}

	public void TriggerGameOver()
	{
		if (_currentState == GameState.GameOver) return;
		Stats.DaysSurvived = Mathf.Max(0, _currentDay - 1);
		SetState(GameState.GameOver);
		SaveManager.DeleteSave();
		SaveManager.UpdateRecords(Stats);
		EmitSignal(SignalName.GameOver);
	}

	public int GetUnitCost(UnitType type) => Mathf.RoundToInt(GameData.Units[type].Cost * CostMultiplier);

	public float UnitHealthMultiplier => 1f + 0.2f * GetPerkStacks(PerkType.Blindaje);
	public float DamageMultiplier => 1f + 0.2f * GetPerkStacks(PerkType.MunicionPerforante);
	public float CooldownMultiplier => Mathf.Pow(0.88f, GetPerkStacks(PerkType.GatilloFacil));
	public float ScrapMultiplier => 1f + 0.25f * GetPerkStacks(PerkType.Chatarrero);
	public float SpeedMultiplier => 1f + 0.15f * GetPerkStacks(PerkType.BotasDeCombate);
	public float RangeMultiplier => 1f + 0.15f * GetPerkStacks(PerkType.OjoDeHalcon);
	public float StructureHealthMultiplier => 1f + 0.5f * GetPerkStacks(PerkType.MurosReforzados);
	public float BuildMultiplier => 1f + 0.35f * GetPerkStacks(PerkType.IngenieriaRapida);
	public float HealMultiplier => 1f + 1f * GetPerkStacks(PerkType.Medico);
	public float BaseHealthMultiplier => 1f + 0.3f * GetPerkStacks(PerkType.Fortaleza);
	public float CostMultiplier => Mathf.Pow(0.85f, GetPerkStacks(PerkType.Reclutamiento));
	public float KillHeal => 8f * GetPerkStacks(PerkType.Adrenalina);
}
