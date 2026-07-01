using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Soldier : CharacterBody2D
{
	[Export] public float Speed = 150f;
	[Export] public float StopDistance = 8f;

	// --- Combate ---
	[Export] public float AttackDamage = 50f;
	[Export] public float AttackRange = 200f; // los soldados disparan, tienen más rango
	[Export] public float AttackCooldown = 1f; // segundos entre disparos

	private float _attackTimer = 0f;
	private Node2D _target;
	private List<Node2D> _visibleEnemies = new List<Node2D>();
	private Area2D _detectionArea;
	private Health _health;

	private Vector2 _targetPosition;
	private bool _isMoving = false;
	private bool _isSelected = false;
	private Node2D _selectionRing;

	public override void _Ready()
	{
		_selectionRing = GetNode<Node2D>("SelectionRing");
		_selectionRing.Visible = false;
		_targetPosition = GlobalPosition;

		_health = GetNode<Health>("Health");
		_health.Died += OnDied;

		// Necesitamos un Area2D de detección, igual que el zombie.
		_detectionArea = GetNode<Area2D>("DetectionArea");
		_detectionArea.BodyEntered += OnBodyEntered;
		_detectionArea.BodyExited += OnBodyExited;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Buscamos objetivo de ataque
		if (_target == null || !IsInstanceValid(_target))
		{
			_target = FindClosestEnemy();
		}

		// Si hay un enemigo en rango, priorizamos atacar por sobre moverse a destino manual
		if (_target != null)
		{
			float distance = GlobalPosition.DistanceTo(_target.GlobalPosition);

			if (distance <= AttackRange)
			{
				Velocity = Vector2.Zero;
				MoveAndSlide();
				TryAttack((float)delta);
				return;
			}
		}

		// Si no hay enemigo en rango, movimiento normal hacia el punto ordenado
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
		}
		else
		{
			Vector2 direction = (_targetPosition - GlobalPosition).Normalized();
			Velocity = direction * Speed;
		}

		MoveAndSlide();
	}

	private void TryAttack(float delta)
	{
		_attackTimer -= delta;
		if (_attackTimer <= 0f)
		{
			_attackTimer = AttackCooldown;

			// Buscamos el componente Health del objetivo y le hacemos daño
			if (_target.HasNode("Health"))
			{
				Health targetHealth = _target.GetNode<Health>("Health");
				targetHealth.TakeDamage(AttackDamage);
				GD.Print($"{Name} ataca a {_target.Name} por {AttackDamage} de daño");
			}
		}
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
		// Detectamos cualquier cosa que tenga un nodo "Health" y no sea otro Soldier
		if (body.HasNode("Health") && body is not Soldier)
		{
			_visibleEnemies.Add(body);
		}
	}

	private void OnBodyExited(Node2D body)
	{
		_visibleEnemies.Remove(body);
	}

	private void OnDied()
	{
		GD.Print($"{Name} murió");
		QueueFree(); // elimina el nodo de la escena
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
