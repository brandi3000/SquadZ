using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Soldier : CharacterBody2D
{
	[Export] public float Speed = 150f;
	[Export] public float StopDistance = 8f;

	[Export] public float AttackDamage = 50f;
	[Export] public float AttackRange = 200f;
	[Export] public float AttackCooldown = 1f;

	private float _attackTimer = 0f;
	private Node2D _target;
	private List<Node2D> _visibleEnemies = new List<Node2D>();
	private Area2D _detectionArea;
	private Health _health;

	private Vector2 _targetPosition;
	private bool _isMoving = false;
	private bool _isSelected = false;
	private Node2D _selectionRing;

	private Node2D _visualNode;
	private Line2D _moveLine;
	private ProgressBar _healthBar;

	public override void _Ready()
	{
		_selectionRing = GetNode<Node2D>("SelectionRing");
		_selectionRing.Visible = false;
		_targetPosition = GlobalPosition;

		_health = GetNode<Health>("Health");
		_health.Died += OnDied;

		_detectionArea = GetNode<Area2D>("DetectionArea");
		_detectionArea.BodyEntered += OnBodyEntered;
		_detectionArea.BodyExited += OnBodyExited;

        _visualNode = GetNode<Node2D>("Sprite2D");

        _healthBar = GetNode<ProgressBar>("HealthBar");
        _healthBar.MaxValue = _health.MaxHealth;
        _healthBar.Value = _health.CurrentHealth;

        _health.HealthChanged += OnHealthChanged;

        _moveLine = GetNode<Line2D>("MoveLine");
        _moveLine.ClearPoints();

        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = new Color(0.2f, 0.2f, 0.2f); // gris oscuro

        var fillStyle = new StyleBoxFlat();
        fillStyle.BgColor = new Color(0.8f, 0.1f, 0.1f); // rojo

        _healthBar.AddThemeStyleboxOverride("background", bgStyle);
        _healthBar.AddThemeStyleboxOverride("fill", fillStyle);
    }

	public override void _PhysicsProcess(double delta)
	{
		if (_target == null || !IsInstanceValid(_target))
		{
			_target = FindClosestEnemy();
		}

		if (_target != null)
		{
			float distance = GlobalPosition.DistanceTo(_target.GlobalPosition);

			if (distance <= AttackRange)
			{
				Velocity = Vector2.Zero;
				MoveAndSlide();
                RotateTowards(_target.GlobalPosition, (float)delta);
                TryAttack((float)delta);
				return;
			}
		}

		if (!_isMoving)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		float distanceToTarget = GlobalPosition.DistanceTo(_targetPosition);
		if (distanceToTarget <= StopDistance)
		{
			Velocity = Vector2.Zero;
			_isMoving = false;
            _moveLine.ClearPoints();
        }
		else
		{
			Vector2 direction = (_targetPosition - GlobalPosition).Normalized();
			Velocity = direction * Speed;
            RotateTowards(_targetPosition, (float)delta);
            UpdateMoveLine();
        }

		MoveAndSlide();
	}

    private void DrawShootLine(Vector2 from, Vector2 to)
    {
        Line2D line = new Line2D();
        line.AddPoint(from);
        line.AddPoint(to);
        line.Width = 2f;
        line.DefaultColor = new Color(1f, 0.9f, 0.2f);

        GetTree().CurrentScene.AddChild(line);

        GetTree().CreateTimer(0.3f).Timeout += () => line.QueueFree();
    }

    private void TryAttack(float delta)
	{
		_attackTimer -= delta;
		if (_attackTimer <= 0f)
		{
			_attackTimer = AttackCooldown;

			if (_target.HasNode("Health"))
			{
				Health targetHealth = _target.GetNode<Health>("Health");
				targetHealth.TakeDamage(AttackDamage);

                DrawShootLine(GlobalPosition, _target.GlobalPosition);

                GD.Print($"{Name} ataca a {_target.Name} por {AttackDamage} de daño");
			}
		}
	}

    private void UpdateMoveLine()
    {
        _moveLine.ClearPoints();
        _moveLine.AddPoint(Vector2.Zero);
        _moveLine.AddPoint(_targetPosition - GlobalPosition);
    }

    private void OnHealthChanged(float current, float max)
    {
        _healthBar.Value = current;
    }

    private void RotateTowards(Vector2 worldPoint, float delta)
    {
        float targetAngle = (worldPoint - GlobalPosition).Angle() + Mathf.Pi / 2f;
        _visualNode.Rotation = Mathf.LerpAngle(_visualNode.Rotation, targetAngle, delta * 10f);
    }

    private Node2D FindClosestEnemy()
	{
		if (_visibleEnemies.Count == 0) return null;
		return _visibleEnemies
			.Where(e => IsInstanceValid(e))
			.OrderBy(e => GlobalPosition.DistanceTo(e.GlobalPosition))
			.FirstOrDefault();
	}

	private void OnBodyEntered(Node2D body)
{
    if (body is Zombie)
    {
        _visibleEnemies.Add(body);
    }
}

private void OnBodyExited(Node2D body)
{
    if (body is Zombie)
    {
        _visibleEnemies.Remove(body);
    }
}

	private void OnDied()
	{
		GD.Print($"{Name} murió");
		QueueFree();
	}

	public void MoveTo(Vector2 worldPosition)
	{
		_targetPosition = worldPosition;
		_isMoving = true;
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

	public bool IsSelected => _isSelected;
}
