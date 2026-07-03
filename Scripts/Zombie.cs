using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Zombie : CharacterBody2D
{
	[Export] public float Speed = 30f;
	[Export] public float AttackRange = 40f;
	[Export] public float AttackDamage = 10f;
	[Export] public float AttackCooldown = 1.5f;

	private float _attackTimer = 0f;
	private Node2D _target;
	private Health _health;

	private Node2D _defaultTarget;

	private Node2D _visualNode;
	private ProgressBar _healthBar;

	private Node2D _base;

	private Move _mover;

	private List<Node2D> _visibleSoldiers = new List<Node2D>();

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
		
		_mover = GetNode<Move>("Move");

		_base = GetNode<Node2D>("/root/Main/Base");

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
		if (_target == null || !IsInstanceValid(_target))
		{
			_target = FindClosestSoldier();
			if (_target == null) _target = _base;
		}

		if (_target == null)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		float distance = GetEdgeDistance(_target);
		bool inAttackRange = distance <= AttackRange;

		RotateTowards(_target.GlobalPosition, (float)delta);

		if (inAttackRange)
		{
			TryAttack((float)delta);
		}

		if (distance > 0)
		{
			_mover.MoveTowards(_target.GlobalPosition); // 👈 usa el componente
		}
		else
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
		}
	}


	private void TryAttack(float delta)
	{
		_attackTimer -= delta;
		if (_attackTimer <= 0f)
		{
			_attackTimer = AttackCooldown;
			Attack(_target); // reutiliza el método que ya tenía
		}
	}

    private void OnHealthChanged(float current, float max)
    {
        _healthBar.Value = current;
    }
	private Node2D FindClosestSoldier()
    {
        // Limpiamos por las dudas algún soldado que ya no exista más (murió, etc.)
        _visibleSoldiers.RemoveAll(s => !IsInstanceValid(s) || !s.IsInGroup("soldiers"));

        if (_visibleSoldiers.Count == 0) return null;

        return _visibleSoldiers
            .OrderBy(s => GlobalPosition.DistanceTo(s.GlobalPosition))
            .FirstOrDefault();
    }
	private float GetEdgeDistance(Node2D target)
    {
        float centerDistance = GlobalPosition.DistanceTo(target.GlobalPosition);
        float myRadius = GetTargetRadius(this);
        float targetRadius = GetTargetRadius(target);
        
        return centerDistance - myRadius - targetRadius;
    }
    private float GetTargetRadius(Node2D target)
    {
        if (target is CollisionObject2D collisionObject)
        {
            var shapeOwners = collisionObject.GetShapeOwners();
            foreach (uint ownerId in shapeOwners)
            {
                int shapeCount = collisionObject.ShapeOwnerGetShapeCount(ownerId);
                for (int i = 0; i < shapeCount; i++)
                {
                    var shape = collisionObject.ShapeOwnerGetShape(ownerId, i);

                    if (shape is CircleShape2D circle)
                        return circle.Radius;

                    if (shape is RectangleShape2D rect)
                        return Mathf.Min(rect.Size.X, rect.Size.Y) / 2f;
                }
            }
        }
        return 0f;
    }
	private Vector2 GetClosestEdgePoint(Node2D target)
	{
		float targetRadius = GetTargetRadius(target);
		
		Vector2 directionToTarget = (target.GlobalPosition - GlobalPosition).Normalized();
		
		// Retrocedemos desde el centro del target, en dirección hacia nosotros,
		// la distancia de su radio -> nos da el punto del borde más cercano
		return target.GlobalPosition - directionToTarget * targetRadius;
	}
    private void OnBodyEnteredDetectionArea(Node2D body)
	{
		if (body == this) return;
		if (body.IsInGroup("soldiers"))
		{
			_visibleSoldiers.Add(body);
			_target = body;
		}
	}
	private void OnBodyExitedDetectionArea(Node2D body)
	{
		_visibleSoldiers.Remove(body);
		if (body == _target)
			_target = null;
	}
	private void Attack(Node2D target)
{

    if (target.HasNode("Health"))
        target.GetNode<Health>("Health").TakeDamage(AttackDamage);
    else
        GD.Print($"⚠️ {target.Name} no tiene nodo Health!"); // 👈 agregá esto
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
		GD.Print("Zombie murió, agregando chatarra");
		GameManager.Instance.AddScrap(10);
		EmitSignal(SignalName.Died);
		QueueFree();
	}
}
