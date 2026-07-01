using Godot;
using System;

using Godot;

public partial class SpawnManager : Node
{
	// La escena del zombie para instanciarla 
	[Export] private PackedScene ZombieScene;

	// Puntos de spawn en el borde del mapa 
	[Export] private NodePath[] SpawnPointPaths;

	// Contenedor donde van los zombies instanciados
	[Export] private NodePath EnemiesContainerPath;

	// Configuración de oleadas
	[Export] public float TimeBetweenWaves = 30f;  // segundos entre oleadas
	[Export] public int ZombiesPerWave = 5;
	[Export] public float TimeBetweenSpawns = 0.5f; // tiempo entre cada zombie de la oleada

	private Node2D _enemiesContainer;
	private Node2D[] _spawnPoints;
	private float _waveTimer;
	private int _currentWave = 0;

	public override void _Ready()
	{
		_enemiesContainer = GetNode<Node2D>(EnemiesContainerPath);

		// Convertimos los paths a referencias de nodos
		_spawnPoints = new Node2D[SpawnPointPaths.Length];
		for (int i = 0; i < SpawnPointPaths.Length; i++)
			_spawnPoints[i] = GetNode<Node2D>(SpawnPointPaths[i]);

		_waveTimer = 3f; // Primera oleada a los 3 segundos de empezar
	}

	public override void _Process(double delta)
	{
		_waveTimer -= (float)delta;

		if (_waveTimer <= 0f)
		{
			StartNextWave();
			_waveTimer = TimeBetweenWaves;
		}
	}

	private async void StartNextWave()
	{
		_currentWave++;
		GD.Print($"=== OLEADA {_currentWave} ===");

		// Cantidad de zombies aumenta con cada oleada
		int zombiesThisWave = ZombiesPerWave + (_currentWave - 1) * 2;

		for (int i = 0; i < zombiesThisWave; i++)
		{
			SpawnZombie();

			// Esperamos un poco entre cada zombie para que no aparezcan todos juntos
			// SceneTreeTimer es la forma de Godot de hacer un "esperar X segundos"
			await ToSignal(GetTree().CreateTimer(TimeBetweenSpawns), "timeout");
		}
	}

	private void SpawnZombie()
	{
		if (ZombieScene == null || _spawnPoints.Length == 0) return;

		//spawn al azar
		int randomIndex = GD.RandRange(0, _spawnPoints.Length - 1);
		Vector2 spawnPosition = _spawnPoints[randomIndex].GlobalPosition;

		// Instanciamos la escena del zimbie
		Zombie zombie = ZombieScene.Instantiate<Zombie>();
		_enemiesContainer.AddChild(zombie);
		zombie.GlobalPosition = spawnPosition;
	}
}
