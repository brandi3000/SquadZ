using Godot;

public enum RaiderState { Advance, Fight, Flee }

public partial class Raider : CharacterBody2D
{
	[Export] public int Day = 10;

	[Signal] public delegate void DiedEventHandler();

	private const float CoverSearchRadius = 240f;
	private const float CoverOffset = 30f;

	private EnemyStats _stats;
	private float _damage;
	private float _attackTimer;
	private float _retargetTimer;
	private Health _health;
	private Move _mover;
	private Sprite2D _visualNode;
	private ProgressBar _healthBar;
	private Base _base;
	private Node2D _targetSoldier;
	private Node2D _cover;
	private Vector2 _exitPoint;
	private int _stolenScrap;
	private bool _dead;

	public RaiderState State { get; private set; } = RaiderState.Advance;
	public bool InCover { get; private set; }

	public void Setup(int day)
	{
		Day = Mathf.Max(1, day);
	}

	public override void _Ready()
	{
		_stats = GameData.Enemies[EnemyType.Saqueador];
		AddToGroup("enemies");
		AddToGroup("raiders");
		CollisionLayer = Combat.LayerEnemies;
		CollisionMask = Combat.LayerWorld | Combat.LayerSoldiers;

		_damage = _stats.Damage * GameData.DayDamageMultiplier(Day - 9);
		_exitPoint = GlobalPosition;

		_health = GetNode<Health>("Health");
		_health.SetMaxHealth(_stats.MaxHealth * GameData.DayHealthMultiplier(Day - 9), true);
		_health.Died += OnDied;
		_health.HealthChanged += (current, max) => { _healthBar.MaxValue = max; _healthBar.Value = current; };

		_visualNode = GetNode<Sprite2D>("Sprite2D");
		_mover = GetNode<Move>("Move");
		_mover.Speed = _stats.Speed;

		_healthBar = GetNode<ProgressBar>("HealthBar");
		_healthBar.MaxValue = _health.MaxHealth;
		_healthBar.Value = _health.CurrentHealth;
		_healthBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0.2f, 0.2f, 0.2f) });
		_healthBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color(0.9f, 0.55f, 0.1f) });

		var bases = GetTree().GetNodesInGroup("base");
		if (bases.Count > 0) _base = bases[0] as Base;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_dead) return;
		float dt = (float)delta;
		_attackTimer -= dt;
		_retargetTimer -= dt;

		if (State == RaiderState.Flee)
		{
			Flee(dt);
			return;
		}

		if (_retargetTimer <= 0f || !Combat.IsAlive(_targetSoldier))
		{
			_targetSoldier = FindClosestSoldier();
			_retargetTimer = 0.4f;
		}

		if (_targetSoldier != null)
			Fight(dt);
		else
			Advance(dt);
	}

	private void Advance(float delta)
	{
		State = RaiderState.Advance;
		_cover = null;
		SetInCover(false);
		if (_base == null || !IsInstanceValid(_base)) return;

		RotateTowards(_base.GlobalPosition, delta);
		if (Combat.EdgeDistance(this, _base) <= 16f)
		{
			StealScrap();
			return;
		}

		_mover.MoveTowards(_base.GlobalPosition);
		ShootBlockingStructure();
	}

	private void Fight(float delta)
	{
		State = RaiderState.Fight;

		if (_cover == null || !IsInstanceValid(_cover))
			_cover = FindCover();

		if (_cover != null)
		{
			Vector2 away = (_cover.GlobalPosition - _targetSoldier.GlobalPosition).Normalized();
			Vector2 coverPoint = _cover.GlobalPosition + away * CoverOffset;
			if (GlobalPosition.DistanceTo(coverPoint) > 8f)
			{
				_mover.MoveTowards(coverPoint);
				SetInCover(false);
			}
			else
			{
				_mover.Stop();
				SetInCover(true);
			}
		}
		else
		{
			SetInCover(false);
			if (GlobalPosition.DistanceTo(_targetSoldier.GlobalPosition) > _stats.Range * 0.9f)
				_mover.MoveTowards(_targetSoldier.GlobalPosition);
			else
				_mover.Stop();
		}

		RotateTowards(_targetSoldier.GlobalPosition, delta);
		if (GlobalPosition.DistanceTo(_targetSoldier.GlobalPosition) <= _stats.Range && _attackTimer <= 0f)
		{
			_attackTimer = _stats.Cooldown;
			Combat.Damage(_targetSoldier, _damage);
			Effects.Shot(GlobalPosition, _targetSoldier.GlobalPosition, new Color(1f, 0.4f, 0.3f), 1.5f);
		}
	}

	private void Flee(float delta)
	{
		SetInCover(false);
		RotateTowards(_exitPoint, delta);
		if (GlobalPosition.DistanceTo(_exitPoint) <= 30f)
		{
			if (_stolenScrap > 0)
				GameManager.Instance.Radio($"Un saqueador escapó con {_stolenScrap} de chatarra.", RadioKind.Danger);
			_dead = true;
			QueueFree();
			return;
		}
		_mover.MoveTowards(_exitPoint);
	}

	private void StealScrap()
	{
		_stolenScrap = GameManager.Instance.StealAllScrap();
		State = RaiderState.Flee;
		_mover.Speed = _stats.Speed * 1.15f;
		if (_stolenScrap > 0)
		{
			Effects.FloatingText(GlobalPosition, $"-{_stolenScrap}", new Color(1f, 0.4f, 0.3f));
			GameManager.Instance.Radio($"¡Saqueador en la base! Robó {_stolenScrap} de chatarra. ¡Elimínenlo antes de que escape!", RadioKind.Danger);
		}
		else
		{
			GameManager.Instance.Radio("Un saqueador entró a la base pero no encontró chatarra.", RadioKind.Warning);
		}
	}

	private void ShootBlockingStructure()
	{
		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			if (GetSlideCollision(i).GetCollider() is Structure structure && structure.IsCompleted && _attackTimer <= 0f)
			{
				_attackTimer = _stats.Cooldown;
				Combat.Damage(structure, _damage);
				Effects.Shot(GlobalPosition, structure.GlobalPosition, new Color(1f, 0.4f, 0.3f), 1.5f);
				return;
			}
		}
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

	private Node2D FindCover()
	{
		Node2D best = null;
		float bestDistance = CoverSearchRadius;
		foreach (Node node in GetTree().GetNodesInGroup("cover"))
		{
			if (node is not Node2D cover) continue;
			float distance = GlobalPosition.DistanceTo(cover.GlobalPosition);
			if (distance < bestDistance)
			{
				bestDistance = distance;
				best = cover;
			}
		}
		return best;
	}

	private void SetInCover(bool value)
	{
		InCover = value;
		_health.DamageTakenMultiplier = value ? 0.5f : 1f;
		_visualNode.Modulate = value ? new Color(0.8f, 0.8f, 0.8f) : Colors.White;
	}

	private void RotateTowards(Vector2 worldPoint, float delta)
	{
		float targetAngle = (worldPoint - GlobalPosition).Angle() + Mathf.Pi / 2f;
		_visualNode.Rotation = Mathf.LerpAngle(_visualNode.Rotation, targetAngle, delta * 10f);
	}

	private void OnDied()
	{
		if (_dead) return;
		_dead = true;

		if (_stolenScrap > 0)
		{
			GameManager.Instance.ReturnScrap(_stolenScrap);
			GameManager.Instance.Radio($"Saqueador abatido: recuperamos {_stolenScrap} de chatarra.", RadioKind.Good);
		}

		int scrap = GameManager.Instance.AddScrapFromKill(_stats.Scrap);
		Effects.FloatingText(GlobalPosition, $"+{scrap}", new Color(0.85f, 0.95f, 0.4f));
		GameManager.Instance.Stats.AddKill(EnemyType.Saqueador);
		Effects.Decal(GlobalPosition, 0.8f);
		EmitSignal(SignalName.Died);
		QueueFree();
	}
}
