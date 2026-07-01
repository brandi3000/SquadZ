using Godot;

public partial class Health : Node
{
	[Export] public float MaxHealth = 100f;

	private float _currentHealth;

	// Señales para que otros scripts reaccionen sin acoplarse directamente
	[Signal] public delegate void HealthChangedEventHandler(float current, float max);
	[Signal] public delegate void DiedEventHandler();

	public override void _Ready()
	{
		_currentHealth = MaxHealth;
	}

	public void TakeDamage(float amount)
	{
		if (_currentHealth <= 0) return; // ya está muerto, ignoramos

		_currentHealth -= amount;
		_currentHealth = Mathf.Max(_currentHealth, 0);

		EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);

		if (_currentHealth <= 0)
		{
			EmitSignal(SignalName.Died);
		}
	}

	public void Heal(float amount)
	{
		_currentHealth = Mathf.Min(_currentHealth + amount, MaxHealth);
		EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
	}

	public float CurrentHealth => _currentHealth;
	public bool IsDead => _currentHealth <= 0;
}
