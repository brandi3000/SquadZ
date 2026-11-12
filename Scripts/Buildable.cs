using Godot;

public partial class Buildable : Node
{
	[Export] public float MaxBuildProgress = 100f;
	[Export] public float BuildRatePerWorker = 20f;

	private float _currentProgress = 0f;
	private float _workPower = 0f;
	private bool _completedEmitted;

	[Signal] public delegate void ProgressChangedEventHandler(float current, float max);
	[Signal] public delegate void ConstructionCompletedEventHandler();

	public bool IsCompleted => _currentProgress >= MaxBuildProgress;
	public float ProgressPercent => _currentProgress / MaxBuildProgress;
	public float CurrentProgress => _currentProgress;
	public float WorkPower => _workPower;

	public void AddWorker(float power = 1f) => _workPower += power;

	public void RemoveWorker(float power = 1f) => _workPower = Mathf.Max(0f, _workPower - power);

	public void SetProgress(float value)
	{
		_currentProgress = Mathf.Clamp(value, 0f, MaxBuildProgress);
		EmitSignal(SignalName.ProgressChanged, _currentProgress, MaxBuildProgress);
		CheckCompleted();
	}

	public override void _Process(double delta)
	{
		if (IsCompleted || _workPower <= 0f) return;

		_currentProgress += BuildRatePerWorker * _workPower * GameManager.Instance.BuildMultiplier * (float)delta;
		_currentProgress = Mathf.Min(_currentProgress, MaxBuildProgress);

		EmitSignal(SignalName.ProgressChanged, _currentProgress, MaxBuildProgress);
		CheckCompleted();
	}

	private void CheckCompleted()
	{
		if (IsCompleted && !_completedEmitted)
		{
			_completedEmitted = true;
			EmitSignal(SignalName.ConstructionCompleted);
		}
	}
}
