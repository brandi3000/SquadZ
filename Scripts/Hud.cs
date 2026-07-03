using Godot;

public partial class Hud : Control
{
    private Label _waveLabel;
    private Label _scrapLabel;
    private Button _spawnSoldierButton;
    private ProgressBar _baseHealthBar;

	private Button _buildWallButton;

    private Health _baseHealth;

	private Button _nextDayButton;

    public override void _Ready()
    {
        _waveLabel = GetNode<Label>("WaveLabel");
        _scrapLabel = GetNode<Label>("ScrapLabel");
        _spawnSoldierButton = GetNode<Button>("SpawnSoldierButton");
        _baseHealthBar = GetNode<ProgressBar>("BaseHealthBar"); // ajustá el path según tu árbol

        GameManager.Instance.ScrapChanged += OnScrapChanged;
        GameManager.Instance.WaveChanged += OnWaveChanged;


        _spawnSoldierButton.Pressed += OnSpawnSoldierPressed;

        UpdateWaveLabel(GameManager.Instance.CurrentWave);
        UpdateScrapLabel(GameManager.Instance.CurrentScrap);

        // Configuración de la barra de vida de la base
        _baseHealth = GetNode<Health>("/root/Main/Base/Health");

		_buildWallButton = GetNode<Button>("BuildWallButton");
		_buildWallButton.Pressed += OnBuildWallPressed;

        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = new Color(0.2f, 0.2f, 0.2f);

        var fillStyle = new StyleBoxFlat();
        fillStyle.BgColor = new Color(0.1f, 0.6f, 0.9f);

        _baseHealthBar.AddThemeStyleboxOverride("background", bgStyle);
        _baseHealthBar.AddThemeStyleboxOverride("fill", fillStyle);

        _baseHealthBar.MaxValue = _baseHealth.MaxHealth;
        _baseHealthBar.Value = _baseHealth.MaxHealth;

        _baseHealth.HealthChanged += OnBaseHealthChanged;
		
		_nextDayButton = GetNode<Button>("NextDayButton");   // 👈 agregar
		_nextDayButton.Visible = false;                       // 👈 agregar
		_nextDayButton.Pressed += OnNextDayPressed;            // 👈 agregar

    	GameManager.Instance.GameStateChanged += OnGameStateChanged;
    }

    private void OnScrapChanged(int newAmount) => UpdateScrapLabel(newAmount);
    private void OnWaveChanged(int newWave) => UpdateWaveLabel(newWave);
    private void OnBaseHealthChanged(float current, float max) => _baseHealthBar.Value = current;
    private void UpdateScrapLabel(int amount) => _scrapLabel.Text = $"Chatarra: {amount}";
    private void UpdateWaveLabel(int wave) => _waveLabel.Text = $"Oleada: {wave}";
    private void OnSpawnSoldierPressed()
    {
        var main = GetNode<Main>("/root/Main");
        main.TrySpawnSoldier();
    }
	private void OnGameStateChanged(int newState)
	{
		GameState state = (GameState)newState;
		_nextDayButton.Visible = (state == GameState.Intermission);
	}
	private void OnNextDayPressed()
	{
		_nextDayButton.Visible = false;

		var spawnManager = GetNode<SpawnManager>("/root/Main/SpawnManager");
		spawnManager.StartNewDay(); // 👈 esto ya internamente hace SetState(Combat) y arranca la ronda 1
	}
	private void OnBuildWallPressed()
	{
		var buildManager = GetNode<BuildManager>("/root/Main/BuildManager");
		buildManager.StartPlacingWall();
	}
}
