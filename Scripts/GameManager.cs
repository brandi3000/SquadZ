using Godot;

public enum GameState
{
    Combat,
    Intermission
}

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    [Signal] public delegate void ScrapChangedEventHandler(int newAmount);
    [Signal] public delegate void WaveChangedEventHandler(int newWave);
    [Signal] public delegate void GameStateChangedEventHandler(int newState);

    private int _scrap = 0;
    private int _currentWave = 0;
    private GameState _currentState = GameState.Combat;

    public override void _Ready()
    {
        Instance = this;
    }

    public void AddScrap(int amount)
    {
        _scrap += amount;
        EmitSignal(SignalName.ScrapChanged, _scrap);
    }

    public bool SpendScrap(int amount)
    {
        if (_scrap < amount) return false;
        _scrap -= amount;
        EmitSignal(SignalName.ScrapChanged, _scrap);
        return true;
    }

    public void NextWave()
    {
        _currentWave++;
        EmitSignal(SignalName.WaveChanged, _currentWave);
    }

    public void SetState(GameState newState)
    {
        _currentState = newState;
        EmitSignal(SignalName.GameStateChanged, (int)newState);
    }

    public int CurrentScrap => _scrap;
    public int CurrentWave => _currentWave;
    public GameState CurrentState => _currentState;
}