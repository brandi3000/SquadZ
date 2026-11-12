using Godot;

public partial class Health : Node
{
	[Export] public float MaxHealth = 100f;

	private float _currentHealth;
	private bool _initialized;

	[Signal] public delegate void HealthChangedEventHandler(float current, float max);
	[Signal] public delegate void DiedEventHandler();

	public float DamageTakenMultiplier { get; set; } = 1f;
	public float CurrentHealth => _currentHealth;
	public bool IsDead => _initialized && _currentHealth <= 0;
	public bool IsFull => _currentHealth >= MaxHealth;
	public float Percent => MaxHealth > 0 ? _currentHealth / MaxHealth : 0f;

	public override void _Ready()
	{
		if (!_initialized)
		{
			_currentHealth = MaxHealth;
			_initialized = true;
		}
	}

	public bool TakeDamage(float amount)
	{
		if (IsDead || amount <= 0) return false;

		_currentHealth = Mathf.Max(_currentHealth - amount * DamageTakenMultiplier, 0);
		EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);

		if (_currentHealth <= 0)
		{
			EmitSignal(SignalName.Died);
			return true;
		}
		return false;
	}

	public void Heal(float amount)
	{
		if (IsDead || amount <= 0 || IsFull) return;
		_currentHealth = Mathf.Min(_currentHealth + amount, MaxHealth);
		EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
	}

	public void SetMaxHealth(float newMax, bool refill)
	{
		float ratio = _initialized && MaxHealth > 0 ? _currentHealth / MaxHealth : 1f;
		MaxHealth = newMax;
		_currentHealth = refill ? newMax : newMax * ratio;
		_initialized = true;
		EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
	}

	public void SetCurrentHealth(float value)
	{
		_currentHealth = Mathf.Clamp(value, 1f, MaxHealth);
		_initialized = true;
		EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
	}
}
