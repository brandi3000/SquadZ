using Godot;
using System.Collections.Generic;

public partial class SpawnManager : Node
{
	[Export] private PackedScene ZombieScene;
	[Export] private PackedScene RaiderScene;
	[Export] private NodePath[] SpawnPointPaths;
	[Export] private NodePath EnemiesContainerPath;

	[Export] public float TimeBetweenRounds = 20f;
	[Export] public int ZombiesPerWave = 6;
	[Export] public float TimeBetweenSpawns = 0.6f;
	[Export] public int BossEveryDays = 5;
	[Export] public int RaidersFromDay = 10;

	private Node2D _enemiesContainer;
	private Node2D[] _spawnPoints;
	private readonly Queue<EnemyType> _spawnQueue = new();
	private readonly RandomNumberGenerator _rng = new();

	private int _currentDay = 0;
	private int _currentRound = 0;
	private int _enemiesAlive = 0;
	private bool _roundInProgress = false;
	private float _roundTransitionTimer = 0f;
	private bool _waitingForNextRound = false;
	private float _spawnTimer = 0f;
	private bool _shuttingDown;

	public int CurrentDay => _currentDay;
	public int CurrentRound => _currentRound;
	public int EnemiesRemaining => _enemiesAlive + _spawnQueue.Count;
	public bool WaitingForNextRound => _waitingForNextRound;
	public float NextRoundCountdown => _roundTransitionTimer;

	public override void _Ready()
	{
		_rng.Randomize();
		_enemiesContainer = GetNode<Node2D>(EnemiesContainerPath);
		_spawnPoints = new Node2D[SpawnPointPaths.Length];
		for (int i = 0; i < SpawnPointPaths.Length; i++)
			_spawnPoints[i] = GetNode<Node2D>(SpawnPointPaths[i]);
	}

	public override void _ExitTree()
	{
		_shuttingDown = true;
	}

	public override void _Process(double delta)
	{
		if (GameManager.Instance.CurrentState == GameState.GameOver) return;
		float dt = (float)delta;

		if (_waitingForNextRound)
		{
			_roundTransitionTimer -= dt;
			if (_roundTransitionTimer <= 0f)
			{
				_waitingForNextRound = false;
				StartRound(2);
			}
			return;
		}

		if (!_roundInProgress) return;

		if (_spawnQueue.Count > 0)
		{
			_spawnTimer -= dt;
			if (_spawnTimer <= 0f)
			{
				SpawnEnemy(_spawnQueue.Dequeue());
				_spawnTimer = TimeBetweenSpawns;
			}
		}
		else if (_enemiesAlive <= 0)
		{
			OnRoundCompleted();
		}
	}

	public void StartNewDay()
	{
		if (GameManager.Instance.CurrentState == GameState.GameOver) return;

		_currentDay++;
		GameManager.Instance.SetDay(_currentDay, 1);
		GameManager.Instance.SetState(GameState.Combat);
		GameManager.Instance.Radio($"=== DÍA {_currentDay} ===", RadioKind.Info);

		if (_currentDay == RaidersFromDay)
			GameManager.Instance.Radio("Se ven humanos armados en el perímetro. Los saqueadores van por nuestra chatarra.", RadioKind.Danger);
		if (IsBossDay(_currentDay))
			GameManager.Instance.Radio("Algo enorme se acerca... prepárense para la segunda oleada.", RadioKind.Danger);

		StartRound(1);
	}

	public void SkipWait()
	{
		if (_waitingForNextRound) _roundTransitionTimer = 0f;
	}

	public void ResumeFromSave(int day)
	{
		_currentDay = day;
		_currentRound = 2;
		_roundInProgress = false;
		_waitingForNextRound = false;
		GameManager.Instance.SetDay(day, 2);
		GameManager.Instance.SetState(GameState.Intermission);
		GameManager.Instance.Radio($"Partida cargada: día {day} superado. Preparen la defensa.", RadioKind.Good);
	}

	private bool IsBossDay(int day) => BossEveryDays > 0 && day % BossEveryDays == 0;

	private void StartRound(int roundNumber)
	{
		_currentRound = roundNumber;
		_roundInProgress = true;
		_spawnTimer = 0.5f;
		GameManager.Instance.SetDay(_currentDay, _currentRound);

		_spawnQueue.Clear();
		foreach (EnemyType type in ComposeWave(_currentDay, roundNumber))
			_spawnQueue.Enqueue(type);

		GameManager.Instance.Radio($"Oleada {roundNumber}/2: {_spawnQueue.Count} hostiles en camino.", RadioKind.Warning);
	}

	public List<EnemyType> ComposeWave(int day, int round)
	{
		var wave = new List<EnemyType>();

		if (round == 2 && IsBossDay(day))
			wave.Add(EnemyType.Jefe);

		int zombies = ZombiesPerWave + (day - 1) * 2 + (round == 2 ? 1 : 0);
		float runnerChance = day >= 2 ? Mathf.Min(0.10f + 0.03f * (day - 2), 0.30f) : 0f;
		float boomerChance = day >= 3 ? Mathf.Min(0.08f + 0.02f * (day - 3), 0.20f) : 0f;
		float tankChance = day >= 4 ? Mathf.Min(0.05f + 0.02f * (day - 4), 0.15f) : 0f;

		for (int i = 0; i < zombies; i++)
		{
			float roll = _rng.Randf();
			if (roll < tankChance) wave.Add(EnemyType.Tanque);
			else if (roll < tankChance + boomerChance) wave.Add(EnemyType.Boomer);
			else if (roll < tankChance + boomerChance + runnerChance) wave.Add(EnemyType.Corredor);
			else wave.Add(EnemyType.Normal);
		}

		if (day >= RaidersFromDay && (round == 2 || day >= RaidersFromDay + 3))
		{
			int raiders = Mathf.Min(1 + (day - RaidersFromDay) / 2, 6);
			for (int i = 0; i < raiders; i++)
				wave.Insert(_rng.RandiRange(0, wave.Count), EnemyType.Saqueador);
		}

		return wave;
	}

	private void SpawnEnemy(EnemyType type)
	{
		if (_spawnPoints.Length == 0) return;

		Node2D spawnPoint = _spawnPoints[_rng.RandiRange(0, _spawnPoints.Length - 1)];
		Vector2 position = spawnPoint.GlobalPosition + new Vector2(_rng.RandfRange(-40, 40), _rng.RandfRange(-40, 40));

		Node2D enemy;
		if (type == EnemyType.Saqueador)
		{
			if (RaiderScene == null) return;
			var raider = RaiderScene.Instantiate<Raider>();
			raider.Setup(_currentDay);
			enemy = raider;
		}
		else
		{
			if (ZombieScene == null) return;
			var zombie = ZombieScene.Instantiate<Zombie>();
			zombie.Setup(type, _currentDay);
			enemy = zombie;
		}

		enemy.Position = position;
		_enemiesAlive++;
		enemy.TreeExiting += OnEnemyRemoved;
		_enemiesContainer.AddChild(enemy);

		if (type == EnemyType.Jefe)
			GameManager.Instance.Radio("¡CONTACTO! Una Abominación entró al mapa.", RadioKind.Danger);
	}

	private void OnEnemyRemoved()
	{
		if (_shuttingDown) return;
		_enemiesAlive = Mathf.Max(0, _enemiesAlive - 1);
	}

	private void OnRoundCompleted()
	{
		_roundInProgress = false;

		if (_currentRound == 1)
		{
			_roundTransitionTimer = TimeBetweenRounds;
			_waitingForNextRound = true;
			GameManager.Instance.Radio($"Oleada 1 superada. La siguiente llega en {TimeBetweenRounds:0} s.", RadioKind.Good);
		}
		else
		{
			GameManager.Instance.SetState(GameState.Intermission);
			GameManager.Instance.Radio($"Día {_currentDay} superado. Reparen, recluten y pasen al día siguiente.", RadioKind.Good);
			var main = GetParent() as Main;
			if (main != null && SaveManager.Save(main))
				GameManager.Instance.Radio("Partida guardada automáticamente.", RadioKind.Info);
		}
	}
}
