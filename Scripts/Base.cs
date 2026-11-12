using Godot;

public partial class Base : StaticBody2D
{
	[Export] public float BaseMaxHealth = 600f;
	[Export] public float HealRadius = 230f;
	[Export] public float HealPerSecond = 6f;

	private Health _health;
	private Marker2D _spawnPoint;

	public Health Health => _health;
	public Vector2 SpawnPosition => _spawnPoint.GlobalPosition;

	public override void _Ready()
	{
		AddToGroup("base");
		_health = GetNode<Health>("Health");
		_spawnPoint = GetNode<Marker2D>("SoldierSpawnPoint");
		_health.SetMaxHealth(BaseMaxHealth * GameManager.Instance.BaseHealthMultiplier, true);
		ZIndex = -1;
	}

	public bool IsInHealZone(Vector2 worldPosition) => GlobalPosition.DistanceTo(worldPosition) <= HealRadius;

	public float CurrentHealRate => HealPerSecond * GameManager.Instance.HealMultiplier;

	public void ApplyFortalezaPerk()
	{
		_health.SetMaxHealth(BaseMaxHealth * GameManager.Instance.BaseHealthMultiplier, true);
	}

	public override void _Draw()
	{
		var fill = new Color(0.36f, 0.8f, 0.36f, 0.06f);
		DrawCircle(Vector2.Zero, HealRadius, fill);
		int segments = 48;
		for (int i = 0; i < segments; i += 2)
		{
			float a0 = Mathf.Tau * i / segments;
			float a1 = Mathf.Tau * (i + 1) / segments;
			DrawArc(Vector2.Zero, HealRadius, a0, a1, 4, new Color(0.6f, 0.95f, 0.45f, 0.35f), 2f);
		}
	}
}
