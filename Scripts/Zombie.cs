using Godot;

public partial class Zombie : CharacterBody2D
{
	[Export] public float Speed = 60f;
	[Export] public float AttackRange = 20f;
	[Export] public float AttackDamage = 10f;
	[Export] public float AttackCooldown = 1.5f;

	private float _attackTimer = 0f;
	private Node2D _target;
	private Health _health;

	// 🔄 NUEVO: referencia al target por defecto (la base)
	private Node2D _defaultTarget;

	[Signal] public delegate void DiedEventHandler();

	public override void _Ready()
	{
		AddToGroup("enemies");

		_health = GetNode<Health>("Health");
		_health.Died += OnHealthDepleted;

		var detectionArea = GetNode<Area2D>("DetectionArea");
		detectionArea.BodyEntered += OnBodyEnteredDetectionArea;
		detectionArea.BodyExited  += OnBodyExitedDetectionArea;

		// 🔄 NUEVO: buscamos la base en el grupo al iniciar
		var baseNodes = GetTree().GetNodesInGroup("base");
		if (baseNodes.Count > 0)
			_defaultTarget = baseNodes[0] as Node2D;
	}

	public override void _PhysicsProcess(double delta)
	{
		// 🔄 NUEVO: si no hay soldado cerca, usamos la base como target
		Node2D currentTarget = _target ?? _defaultTarget;

		if (currentTarget == null || !IsInstanceValid(currentTarget))
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		float distToTarget = GlobalPosition.DistanceTo(currentTarget.GlobalPosition);

		if (distToTarget <= AttackRange)
		{
			Velocity = Vector2.Zero;
			_attackTimer -= (float)delta;

			if (_attackTimer <= 0f)
			{
				Attack(currentTarget); // 🔄 le pasamos el target actual
				_attackTimer = AttackCooldown;
			}
		}
		else
		{
			Vector2 direction = (currentTarget.GlobalPosition - GlobalPosition).Normalized();
			Velocity = direction * Speed;
		}

		MoveAndSlide();
	}

	// ── Detección ─────────────────────────────────────────────────────

	private void OnBodyEnteredDetectionArea(Node2D body)
	{
		if (body.IsInGroup("soldiers") && _target == null)
			_target = body;
	}

	private void OnBodyExitedDetectionArea(Node2D body)
	{
		if (body == _target)
			_target = null;
		// Al soltar al soldado, automáticamente vuelve a perseguir la base
	}

	// ── Combate ───────────────────────────────────────────────────────

	// 🔄 CAMBIO: Attack ahora recibe el target como parámetro
	private void Attack(Node2D target)
	{
		GD.Print($"Zombie atacó a {target.Name}!");

		if (target.HasNode("Health"))
			target.GetNode<Health>("Health").TakeDamage(AttackDamage);
	}

	public void TakeDamage(float amount)
	{
		_health.TakeDamage(amount);
	}

	private void OnHealthDepleted()
	{
		EmitSignal(SignalName.Died);
		QueueFree();
	}
}
