using Godot;

public partial class Zombie : CharacterBody2D
{
	[Export] public EnemyType Type = EnemyType.Normal;
	[Export] public int Day = 1;

	[Signal] public delegate void DiedEventHandler();

	private const float SpriteBaseSize = 108f;

	private EnemyStats _stats;
	private float _damage;
	private float _attackTimer;
	private float _retargetTimer;
	private Node2D _target;
	private Node2D _blocker;
	private float _blockerTimer;
	private Health _health;
	private Sprite2D _visualNode;
	private ProgressBar _healthBar;
	private Base _base;
	private Move _mover;
	private bool _dead;

	public EnemyStats Stats => _stats;
	public Health Health => _health;
	public bool IsBoss => Type == EnemyType.Jefe;

	public void Setup(EnemyType type, int day)
	{
		Type = type;
		Day = Mathf.Max(1, day);
	}

	public override void _Ready()
	{
		_stats = GameData.Enemies[Type];
		AddToGroup("enemies");
		if (IsBoss) AddToGroup("boss");
		CollisionLayer = Combat.LayerEnemies;
		CollisionMask = Combat.LayerWorld | Combat.LayerSoldiers;

		_damage = _stats.Damage * GameData.DayDamageMultiplier(Day);

		_health = GetNode<Health>("Health");
		_health.SetMaxHealth(_stats.MaxHealth * GameData.DayHealthMultiplier(Day), true);
		_health.Died += OnHealthDepleted;
		_health.HealthChanged += OnHealthChanged;

		var detectionArea = GetNode<Area2D>("DetectionArea");
		detectionArea.Monitoring = false;
		detectionArea.Monitorable = false;

		var collision = GetNode<CollisionShape2D>("CollisionShape2D");
		collision.Shape = new CircleShape2D { Radius = _stats.CollisionRadius };

		_visualNode = GetNode<Sprite2D>("Sprite2D");
		var texture = GD.Load<Texture2D>(_stats.Texture);
		_visualNode.Texture = texture;
		float scale = SpriteBaseSize * _stats.Scale / texture.GetWidth();
		_visualNode.Scale = new Vector2(scale, scale);

		_mover = GetNode<Move>("Move");
		_mover.Speed = _stats.Speed * Mathf.Min(1f + 0.02f * (Day - 1), 1.4f);

		var bases = GetTree().GetNodesInGroup("base");
		if (bases.Count > 0) _base = bases[0] as Base;

		_healthBar = GetNode<ProgressBar>("HealthBar");
		_healthBar.MaxValue = _health.MaxHealth;
		_healthBar.Value = _health.CurrentHealth;
		_healthBar.Position = new Vector2(-_stats.CollisionRadius - 4, _stats.CollisionRadius + 8);
		_healthBar.Size = new Vector2(4, _stats.CollisionRadius * 2 + 8);
		_healthBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0.2f, 0.2f, 0.2f) });
		_healthBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color(0.8f, 0.1f, 0.1f) });
		if (IsBoss) _healthBar.Visible = false;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_dead) return;
		float dt = (float)delta;
		_attackTimer -= dt;
		_retargetTimer -= dt;
		_blockerTimer -= dt;

		if (_retargetTimer <= 0f || !Combat.IsAlive(_target))
		{
			_target = FindClosestSoldier() ?? _base;
			_retargetTimer = 0.4f;
		}

		if (_blockerTimer <= 0f || !Combat.IsAlive(_blocker))
			_blocker = null;

		Node2D attackTarget = _blocker ?? _target;
		if (attackTarget is Soldier soldier && soldier.Garrison != null)
			attackTarget = soldier.Garrison;

		if (attackTarget == null || !Combat.IsAlive(attackTarget))
		{
			_mover.Stop();
			return;
		}

		RotateTowards(attackTarget.GlobalPosition, dt);
		float distance = Combat.EdgeDistance(this, attackTarget);

		if (distance <= _stats.Range)
		{
			if (Type == EnemyType.Boomer)
			{
				Explode(1f);
				Die(false);
				return;
			}
			_mover.Stop();
			TryAttack(attackTarget);
		}
		else
		{
			_mover.MoveTowards(attackTarget.GlobalPosition);
			DetectBlockers();
		}
	}

	private void DetectBlockers()
	{
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collider = GetSlideCollision(i).GetCollider();
			if (collider is Structure structure && structure.IsCompleted)
			{
				_blocker = structure;
				_blockerTimer = 2.5f;
				return;
			}
			if (collider is Base baseNode)
			{
				_blocker = baseNode;
				_blockerTimer = 2.5f;
				return;
			}
		}
	}

	private void TryAttack(Node2D target)
	{
		if (_attackTimer > 0f) return;
		_attackTimer = _stats.Cooldown;

		if (IsBoss)
		{
			Effects.Blast(GlobalPosition, _stats.SplashRadius, new Color(0.8f, 0.2f, 0.3f));
			Combat.AreaDamage(GetTree(), GlobalPosition, _stats.SplashRadius, _damage, true);
			return;
		}

		Combat.Damage(target, _damage);
	}

	private void Explode(float power)
	{
		Effects.Blast(GlobalPosition, _stats.SplashRadius, new Color(0.7f, 0.9f, 0.2f));
		Combat.AreaDamage(GetTree(), GlobalPosition, _stats.SplashRadius, _damage * power, true);
	}

	private Node2D FindClosestSoldier()
	{
		Node2D closest = null;
		float best = _stats.AggroRadius;
		foreach (Node node in GetTree().GetNodesInGroup("soldiers"))
		{
			if (node is not Soldier soldier || !Combat.IsAlive(soldier)) continue;
			float distance = GlobalPosition.DistanceTo(soldier.GlobalPosition);
			if (distance < best)
			{
				best = distance;
				closest = soldier;
			}
		}
		return closest;
	}

	private void RotateTowards(Vector2 worldPoint, float delta)
	{
		float targetAngle = (worldPoint - GlobalPosition).Angle() + Mathf.Pi / 2f;
		_visualNode.Rotation = Mathf.LerpAngle(_visualNode.Rotation, targetAngle, delta * 10f);
	}

	private void OnHealthChanged(float current, float max)
	{
		_healthBar.MaxValue = max;
		_healthBar.Value = current;
	}

	private void OnHealthDepleted()
	{
		if (Type == EnemyType.Boomer)
			Explode(0.6f);
		Die(true);
	}

	private void Die(bool reward)
	{
		if (_dead) return;
		_dead = true;

		if (reward)
		{
			int scrap = GameManager.Instance.AddScrapFromKill(_stats.Scrap);
			Effects.FloatingText(GlobalPosition, $"+{scrap}", new Color(0.85f, 0.95f, 0.4f));
			if (IsBoss)
			{
				GameManager.Instance.Radio("¡La Abominación cayó! Elegí una mejora.", RadioKind.Good);
				GameManager.Instance.NotifyBossDefeated();
			}
			else
			{
				GameManager.Instance.Stats.AddKill(Type);
			}
		}

		Effects.Decal(GlobalPosition, _stats.Scale);
		EmitSignal(SignalName.Died);
		QueueFree();
	}
}
