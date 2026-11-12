using Godot;

public enum SoldierOrder { Idle, Move, Build, ReturnToBase, Garrison }

public partial class Soldier : CharacterBody2D
{
	[Export] public UnitType Type = UnitType.Fusilero;
	[Export] public float StopDistance = 8f;

	private const float BuildReach = 26f;
	private const float LeaderAuraRadius = 240f;

	private UnitStats _stats;
	private Health _health;
	private Move _move;
	private Node2D _selectionRing;
	private Sprite2D _visualNode;
	private Line2D _moveLine;
	private ProgressBar _healthBar;
	private CollisionShape2D _collision;
	private Label _squadLabel;
	private Base _base;

	private Node2D _target;
	private float _attackTimer;
	private float _retargetTimer;

	private Vector2 _targetPosition;
	private Structure _buildTarget;
	private float _workingPower;
	private Structure _garrison;
	private bool _isSelected;

	public SoldierOrder Order { get; private set; } = SoldierOrder.Idle;
	public int SquadId { get; private set; }
	public bool IsLeader { get; private set; }
	public bool Demoralized { get; private set; }
	public bool IsSelected => _isSelected;
	public Health Health => _health;
	public Structure Garrison => _garrison;
	public UnitStats Stats => _stats;

	public float Damage
	{
		get
		{
			float value = _stats.Damage * GameManager.Instance.DamageMultiplier;
			if (Demoralized) value *= 0.75f;
			if (HasLeaderNearby()) value *= 1.1f;
			return value;
		}
	}

	public float Range => _stats.Range * GameManager.Instance.RangeMultiplier * (_garrison != null ? 1.4f : 1f);
	public float Cooldown => _stats.Cooldown * GameManager.Instance.CooldownMultiplier;
	public float BuildPower => _stats.BuildPower > 0 ? _stats.BuildPower : 1f;

	public void Setup(UnitType type)
	{
		Type = type;
	}

	public override void _Ready()
	{
		AddToGroup("soldiers");
		_stats = GameData.Units[Type];
		CollisionLayer = Combat.LayerSoldiers;
		CollisionMask = Combat.LayerWorld;

		_selectionRing = GetNode<Node2D>("SelectionRing");
		_selectionRing.Visible = false;
		_targetPosition = GlobalPosition;

		_health = GetNode<Health>("Health");
		_health.SetMaxHealth(_stats.MaxHealth * GameManager.Instance.UnitHealthMultiplier, true);
		_health.Died += OnDied;
		_health.HealthChanged += OnHealthChanged;

		var detectionArea = GetNode<Area2D>("DetectionArea");
		detectionArea.Monitoring = false;
		detectionArea.Monitorable = false;

		_collision = GetNode<CollisionShape2D>("CollisionShape2D");
		_visualNode = GetNode<Sprite2D>("Sprite2D");
		_visualNode.Texture = GD.Load<Texture2D>(_stats.Texture);

		_move = GetNode<Move>("Move");
		_move.Speed = _stats.Speed * GameManager.Instance.SpeedMultiplier;

		_healthBar = GetNode<ProgressBar>("HealthBar");
		_healthBar.MaxValue = _health.MaxHealth;
		_healthBar.Value = _health.CurrentHealth;
		_healthBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0.2f, 0.2f, 0.2f) });
		_healthBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = new Color(0.45f, 0.8f, 0.3f) });

		_moveLine = GetNode<Line2D>("MoveLine");
		_moveLine.Width = 2f;
		_moveLine.ClearPoints();

		_squadLabel = new Label { Position = new Vector2(-30, -48), Size = new Vector2(60, 18), HorizontalAlignment = HorizontalAlignment.Center };
		_squadLabel.AddThemeFontSizeOverride("font_size", 13);
		_squadLabel.AddThemeColorOverride("font_outline_color", Colors.Black);
		_squadLabel.AddThemeConstantOverride("outline_size", 4);
		AddChild(_squadLabel);
		UpdateSquadLabel();

		var bases = GetTree().GetNodesInGroup("base");
		if (bases.Count > 0) _base = bases[0] as Base;

		GameManager.Instance.PerksChanged += OnPerksChanged;
	}

	public override void _ExitTree()
	{
		if (GameManager.Instance != null)
			GameManager.Instance.PerksChanged -= OnPerksChanged;
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		_attackTimer -= dt;
		_retargetTimer -= dt;

		if (_retargetTimer <= 0f || !IsValidEnemy(_target) || GlobalPosition.DistanceTo(_target.GlobalPosition) > Range)
		{
			_target = FindClosestEnemy();
			_retargetTimer = 0.25f;
		}

		bool hasTargetInRange = _target != null;
		if (hasTargetInRange)
		{
			RotateTowards(_target.GlobalPosition, dt);
			if (_attackTimer <= 0f) Shoot();
		}

		HandleHealing(dt);

		switch (Order)
		{
			case SoldierOrder.Move:
			case SoldierOrder.ReturnToBase:
				HandleMovement(dt, hasTargetInRange);
				break;
			case SoldierOrder.Build:
				HandleBuildBehavior(dt, hasTargetInRange);
				break;
			case SoldierOrder.Garrison:
				HandleGarrison(dt, hasTargetInRange);
				break;
			default:
				_move.Stop();
				break;
		}
	}

	public void MoveTo(Vector2 worldPosition)
	{
		ClearTask();
		_targetPosition = ClampToMap(worldPosition);
		Order = SoldierOrder.Move;
	}

	public void ReturnToBase(Vector2 healPoint)
	{
		ClearTask();
		_targetPosition = ClampToMap(healPoint);
		Order = SoldierOrder.ReturnToBase;
	}

	public void AssignToBuild(Structure structure)
	{
		if (structure == null || !IsInstanceValid(structure)) return;
		ClearTask();
		_buildTarget = structure;
		Order = SoldierOrder.Build;
	}

	public void GarrisonIn(Structure tower)
	{
		if (tower == null || !IsInstanceValid(tower)) return;
		ClearTask();
		_buildTarget = tower;
		Order = SoldierOrder.Garrison;
	}

	public void Stop()
	{
		ClearTask();
		Order = SoldierOrder.Idle;
	}

	public void Select()
	{
		_isSelected = true;
		_selectionRing.Visible = true;
	}

	public void Deselect()
	{
		_isSelected = false;
		_selectionRing.Visible = false;
	}

	public void SetSquad(int squadId, bool isLeader)
	{
		SquadId = squadId;
		IsLeader = squadId > 0 && isLeader;
		UpdateSquadLabel();
	}

	public void SetDemoralized(bool value)
	{
		Demoralized = value;
		_visualNode.Modulate = value ? new Color(0.75f, 0.8f, 1f) : Colors.White;
		UpdateSquadLabel();
	}

	public void RestoreHealth(float value)
	{
		_health.SetCurrentHealth(value);
	}

	public void OnGarrisonLost()
	{
		_garrison = null;
		_collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		_health.DamageTakenMultiplier = 1f;
		_buildTarget = null;
		Order = SoldierOrder.Idle;
	}

	private void ClearTask()
	{
		if (_buildTarget != null && IsInstanceValid(_buildTarget) && _workingPower > 0f)
			_buildTarget.Buildable.RemoveWorker(_workingPower);
		_workingPower = 0f;

		if (_garrison != null)
		{
			Structure tower = _garrison;
			_garrison = null;
			if (IsInstanceValid(tower))
			{
				tower.ReleaseGarrison(this);
				GlobalPosition = tower.GlobalPosition + new Vector2(0, tower.Radius + 22f);
			}
			_collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
			_health.DamageTakenMultiplier = 1f;
		}

		_buildTarget = null;
		_moveLine.ClearPoints();
	}

	private void HandleMovement(float delta, bool hasTargetInRange)
	{
		float distanceToTarget = GlobalPosition.DistanceTo(_targetPosition);
		if (distanceToTarget <= StopDistance)
		{
			_move.Stop();
			_moveLine.ClearPoints();
			Order = SoldierOrder.Idle;
			return;
		}

		_move.MoveTowards(_targetPosition);
		if (!hasTargetInRange) RotateTowards(_targetPosition, delta);
		Color color = Order == SoldierOrder.ReturnToBase ? new Color(0.5f, 1f, 0.5f) : new Color(0.6f, 0.9f, 1f);
		UpdateMoveLine(_targetPosition, color);
	}

	private void HandleBuildBehavior(float delta, bool hasTargetInRange)
	{
		if (_buildTarget == null || !IsInstanceValid(_buildTarget) || _buildTarget.IsWorkDone)
		{
			Stop();
			_move.Stop();
			return;
		}

		float distance = Combat.EdgeDistance(this, _buildTarget);
		if (distance <= BuildReach)
		{
			if (_workingPower <= 0f)
			{
				_workingPower = BuildPower;
				_buildTarget.Buildable.AddWorker(_workingPower);
			}
			_move.Stop();
			_moveLine.ClearPoints();
		}
		else
		{
			_move.MoveTowards(_buildTarget.GlobalPosition);
			if (!hasTargetInRange) RotateTowards(_buildTarget.GlobalPosition, delta);
			UpdateMoveLine(_buildTarget.GlobalPosition, new Color(1f, 0.8f, 0.2f));
		}
	}

	private void HandleGarrison(float delta, bool hasTargetInRange)
	{
		if (_garrison != null)
		{
			Velocity = Vector2.Zero;
			return;
		}

		if (_buildTarget == null || !IsInstanceValid(_buildTarget) || !_buildTarget.IsCompleted)
		{
			Stop();
			return;
		}

		float distance = GlobalPosition.DistanceTo(_buildTarget.GlobalPosition);
		if (distance <= _buildTarget.Radius + 30f)
		{
			int slot = _buildTarget.TryGarrison(this);
			if (slot < 0)
			{
				GameManager.Instance.Radio($"Torre llena: {_stats.Name} se queda abajo.", RadioKind.Warning);
				Stop();
				return;
			}
			_garrison = _buildTarget;
			_collision.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
			_health.DamageTakenMultiplier = 0.5f;
			GlobalPosition = _garrison.GetSlotPosition(slot);
			Velocity = Vector2.Zero;
			_moveLine.ClearPoints();
			return;
		}

		_move.MoveTowards(_buildTarget.GlobalPosition);
		if (!hasTargetInRange) RotateTowards(_buildTarget.GlobalPosition, delta);
		UpdateMoveLine(_buildTarget.GlobalPosition, new Color(0.8f, 0.6f, 1f));
	}

	private void HandleHealing(float delta)
	{
		if (_base == null || !IsInstanceValid(_base) || !_base.IsInHealZone(GlobalPosition)) return;

		_health.Heal(_base.CurrentHealRate * delta);
		if (Demoralized)
		{
			SetDemoralized(false);
			GameManager.Instance.Radio($"{_stats.Name} recuperó la moral en la base.", RadioKind.Good);
		}
	}

	private void Shoot()
	{
		_attackTimer = Cooldown;
		float damage = Damage;
		Vector2 impact = _target.GlobalPosition;

		DealDamage(_target, damage);

		if (_stats.SplashRadius > 0f)
		{
			foreach (Node2D enemy in Combat.InRadius(GetTree(), "enemies", impact, _stats.SplashRadius))
			{
				if (enemy != _target) DealDamage(enemy, damage * 0.6f);
			}
			Effects.Blast(impact, _stats.SplashRadius, new Color(1f, 0.6f, 0.2f));
		}

		Effects.Shot(GlobalPosition, impact, _stats.ShotColor, _stats.ShotWidth);
	}

	private void DealDamage(Node2D enemy, float damage)
	{
		var health = Combat.GetHealth(enemy);
		if (health == null || health.IsDead) return;
		GameManager.Instance.Stats.DamageDealt += Mathf.Min(damage * health.DamageTakenMultiplier, health.CurrentHealth);
		bool killed = health.TakeDamage(damage);
		if (killed && GameManager.Instance.KillHeal > 0f)
			_health.Heal(GameManager.Instance.KillHeal);
	}

	private bool IsValidEnemy(Node2D enemy) => enemy != null && IsInstanceValid(enemy) && Combat.IsAlive(enemy);

	private Node2D FindClosestEnemy()
	{
		Node2D closest = null;
		float best = Range;
		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is not Node2D enemy || !Combat.IsAlive(enemy)) continue;
			float distance = GlobalPosition.DistanceTo(enemy.GlobalPosition);
			if (distance <= best)
			{
				best = distance;
				closest = enemy;
			}
		}
		return closest;
	}

	private bool HasLeaderNearby()
	{
		if (SquadId == 0 || IsLeader || SquadManager.Instance == null) return false;
		Soldier leader = SquadManager.Instance.GetLeader(SquadId);
		return leader != null && leader.GlobalPosition.DistanceTo(GlobalPosition) <= LeaderAuraRadius;
	}

	private void UpdateMoveLine(Vector2 targetPosition, Color color)
	{
		_moveLine.ClearPoints();
		_moveLine.DefaultColor = color;
		_moveLine.AddPoint(Vector2.Zero);
		_moveLine.AddPoint(targetPosition - GlobalPosition);
	}

	private void UpdateSquadLabel()
	{
		if (_squadLabel == null) return;
		string text = SquadId > 0 ? (IsLeader ? $"★{SquadId}" : SquadId.ToString()) : "";
		if (Demoralized) text += " ▼";
		_squadLabel.Text = text;
		_squadLabel.AddThemeColorOverride("font_color", IsLeader ? new Color(1f, 0.85f, 0.3f) : new Color(0.75f, 0.95f, 0.6f));
	}

	private void OnHealthChanged(float current, float max)
	{
		_healthBar.MaxValue = max;
		_healthBar.Value = current;
	}

	private void OnPerksChanged()
	{
		_health.SetMaxHealth(_stats.MaxHealth * GameManager.Instance.UnitHealthMultiplier, false);
		_move.Speed = _stats.Speed * GameManager.Instance.SpeedMultiplier;
	}

	private void RotateTowards(Vector2 worldPoint, float delta)
	{
		float targetAngle = (worldPoint - GlobalPosition).Angle() + Mathf.Pi / 2f;
		_visualNode.Rotation = Mathf.LerpAngle(_visualNode.Rotation, targetAngle, delta * 10f);
	}

	private static Vector2 ClampToMap(Vector2 position)
	{
		return new Vector2(Mathf.Clamp(position.X, 24, GameData.MapWidth - 24), Mathf.Clamp(position.Y, 24, GameData.MapHeight - 24));
	}

	private void OnDied()
	{
		ClearTask();
		GameManager.Instance.Stats.UnitsLost++;
		GameManager.Instance.Radio($"¡Baja! Perdimos un {_stats.Name}.", RadioKind.Danger);
		SquadManager.Instance?.OnSoldierDied(this);
		QueueFree();
	}
}
