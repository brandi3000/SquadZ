using Godot;

public partial class Buildable : Node
{
    [Export] public float MaxBuildProgress = 100f;
    [Export] public float BuildRatePerWorker = 20f; // por segundo, por cada unidad ayudando

    private float _currentProgress = 0f;
    private int _workersAssigned = 0;

    [Signal] public delegate void ProgressChangedEventHandler(float current, float max);
    [Signal] public delegate void ConstructionCompletedEventHandler();

    public bool IsCompleted => _currentProgress >= MaxBuildProgress;
    public float ProgressPercent => _currentProgress / MaxBuildProgress;

    public void AddWorker() => _workersAssigned++;
    public void RemoveWorker() => _workersAssigned = Mathf.Max(0, _workersAssigned - 1);

    public override void _Process(double delta)
    {
        if (IsCompleted || _workersAssigned == 0) return;

        _currentProgress += BuildRatePerWorker * _workersAssigned * (float)delta;
        _currentProgress = Mathf.Min(_currentProgress, MaxBuildProgress);

        EmitSignal(SignalName.ProgressChanged, _currentProgress, MaxBuildProgress);

        if (IsCompleted)
        {
            EmitSignal(SignalName.ConstructionCompleted);
        }
    }
}