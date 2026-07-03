using Godot;

public partial class SpawnManager : Node
{
	[Export] private PackedScene ZombieScene;
	[Export] private NodePath[] SpawnPointPaths;
	[Export] private NodePath EnemiesContainerPath;

	[Export] public float TimeBetweenRounds = 15f; // entre ronda 1 y ronda 2 del mismo día
	[Export] public int ZombiesPerWave = 5;
	[Export] public float TimeBetweenSpawns = 0.5f;

	private Node2D _enemiesContainer;
	private Node2D[] _spawnPoints;

	private int _currentDay = 0;
	private int _currentRound = 0; // 1 o 2, dentro del día actual

	private int _zombiesAlive = 0;
	private bool _roundInProgress = false;
	private float _roundTransitionTimer = 0f;
	private bool _waitingForNextRound = false;

	public override void _Ready()
	{
		_enemiesContainer = GetNode<Node2D>(EnemiesContainerPath);

		_spawnPoints = new Node2D[SpawnPointPaths.Length];
		for (int i = 0; i < SpawnPointPaths.Length; i++)
			_spawnPoints[i] = GetNode<Node2D>(SpawnPointPaths[i]);

		StartNewDay(); // arranca el día 1 apenas empieza el juego
	}

	public override void _Process(double delta)
	{
		if (!_waitingForNextRound) return;

		_roundTransitionTimer -= (float)delta;

		if (_roundTransitionTimer <= 0f)
		{
			_waitingForNextRound = false;
			StartRound(2); // la ronda 2 siempre arranca sola, por tiempo
		}
	}

	// Se llama al arrancar el juego, y también cuando el jugador aprieta "Pasar al día siguiente"
	public void StartNewDay()
	{
		_currentDay++;
		GameManager.Instance.NextWave(); // reusamos el label existente, ahora representa "día"
		GD.Print($"=== DÍA {_currentDay} ===");

		GameManager.Instance.SetState(GameState.Combat);
		StartRound(1);
	}

	private async void StartRound(int roundNumber)
	{
		_currentRound = roundNumber;
		_roundInProgress = true;
		GD.Print($"--- Día {_currentDay}, Ronda {_currentRound} ---");

		// La dificultad depende del DÍA, no de la ronda
		int zombiesThisRound = ZombiesPerWave + (_currentDay - 1) * 2;
		_zombiesAlive = zombiesThisRound;

		for (int i = 0; i < zombiesThisRound; i++)
		{
			SpawnZombie();
			await ToSignal(GetTree().CreateTimer(TimeBetweenSpawns), "timeout");
		}
	}

	private void SpawnZombie()
	{
		if (ZombieScene == null || _spawnPoints.Length == 0) return;

		int randomIndex = GD.RandRange(0, _spawnPoints.Length - 1);
		Vector2 spawnPosition = _spawnPoints[randomIndex].GlobalPosition;

		Zombie zombie = ZombieScene.Instantiate<Zombie>();
		_enemiesContainer.AddChild(zombie);
		zombie.GlobalPosition = spawnPosition;

		zombie.Died += OnZombieDied;
	}

	private void OnZombieDied()
	{
		_zombiesAlive--;

		if (_zombiesAlive <= 0)
		{
			_roundInProgress = false;
			OnRoundCompleted();
		}
	}

	private void OnRoundCompleted()
	{
		if (_currentRound == 1)
		{
			GD.Print("Ronda 1 completada, preparando ronda 2...");
			_roundTransitionTimer = TimeBetweenRounds;
			_waitingForNextRound = true; // la ronda 2 arranca sola por tiempo, en _Process
		}
		else
		{
			GD.Print("Día completado. Esperando que el jugador confirme el día siguiente.");
			GameManager.Instance.SetState(GameState.Intermission); // 👈 acá aparece el botón
		}
	}
}