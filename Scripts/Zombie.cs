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

	private Node2D _defaultTarget;

	private Node2D _visualNode;
	private ProgressBar _healthBar;

	[Signal] public delegate void DiedEventHandler();

	public override void _Ready()
	{
		AddToGroup("enemies");

		_health = GetNode<Health>("Health");
		_health.Died += OnHealthDepleted;

        _visualNode = GetNode<Node2D>("Sprite2D");

        var detectionArea = GetNode<Area2D>("DetectionArea");
		detectionArea.BodyEntered += OnBodyEnteredDetectionArea;
		detectionArea.BodyExited  += OnBodyExitedDetectionArea;

		var baseNodes = GetTree().GetNodesInGroup("base");
		if (baseNodes.Count > 0)
			_defaultTarget = baseNodes[0] as Node2D;

        _healthBar = GetNode<ProgressBar>("HealthBar");
        _healthBar.MaxValue = _health.MaxHealth;
        _healthBar.Value = _health.CurrentHealth;

        _health.HealthChanged += OnHealthChanged;

        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = new Color(0.2f, 0.2f, 0.2f); // gris oscuro

        var fillStyle = new StyleBoxFlat();
        fillStyle.BgColor = new Color(0.8f, 0.1f, 0.1f); // rojo

        _healthBar.AddThemeStyleboxOverride("background", bgStyle);
        _healthBar.AddThemeStyleboxOverride("fill", fillStyle);

    }

	public override void _PhysicsProcess(double delta)
	{
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
            RotateTowards(_target.GlobalPosition, (float)delta);
            if (_attackTimer <= 0f)
			{
				Attack(currentTarget);
				_attackTimer = AttackCooldown;
			}
		}
		else
		{
			Vector2 direction = (currentTarget.GlobalPosition - GlobalPosition).Normalized();
			Velocity = direction * Speed;
            RotateTowards(currentTarget.GlobalPosition, (float)delta);
        }

		MoveAndSlide();
	}

    private void OnHealthChanged(float current, float max)
    {
        _healthBar.Value = current;
    }

    private void OnBodyEnteredDetectionArea(Node2D body)
	{
		if (body.IsInGroup("soldiers") && _target == null)
			_target = body;
	}

	private void OnBodyExitedDetectionArea(Node2D body)
	{
		if (body == _target)
			_target = null;
	}

	private void Attack(Node2D target)
	{
		GD.Print($"Zombie atacó a {target.Name}!");

		if (target.HasNode("Health"))
			target.GetNode<Health>("Health").TakeDamage(AttackDamage);
	}

    private void RotateTowards(Vector2 worldPoint, float delta)
    {
        float targetAngle = (worldPoint - GlobalPosition).Angle() + Mathf.Pi / 2f;
        _visualNode.Rotation = Mathf.LerpAngle(_visualNode.Rotation, targetAngle, delta * 10f);
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
