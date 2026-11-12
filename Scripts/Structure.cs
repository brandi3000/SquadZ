using Godot;
using System.Collections.Generic;

public partial class Structure : StaticBody2D
{
	[Export] public StructureType Type = StructureType.Muro;
	[Export] public float RepairPerSecond = 12f;

	private StructureStats _stats;
	private Buildable _buildable;
	private Health _health;
	private CollisionShape2D _collisionShape;
	private Sprite2D _sprite;
	private bool _isPreview = true;
	private bool _placementValid = true;
	private bool _destroyed;
	private bool _loading;
	private readonly List<Soldier> _garrison = new();

	public Buildable Buildable => _buildable;
	public Health Health => _health;
	public StructureStats Stats => _stats;
	public bool IsPreview => _isPreview;
	public bool IsCompleted => !_isPreview && _buildable.IsCompleted;
	public bool IsWorkDone => IsCompleted && _health.IsFull;
	public bool NeedsWork => !_isPreview && !IsWorkDone;
	public float Radius => Mathf.Max(_stats.Size.X, _stats.Size.Y) / 2f;
	public int GarrisonCount => _garrison.Count;
	public bool CanGarrison => IsCompleted && _stats.GarrisonSlots > 0 && _garrison.Count < _stats.GarrisonSlots;

	public override void _Ready()
	{
		AddToGroup("structures");
		_stats = GameData.Structures[Type];

		_buildable = GetNode<Buildable>("Buildable");
		_buildable.MaxBuildProgress = _stats.BuildWork;
		_health = GetNode<Health>("Health");
		_health.SetMaxHealth(_stats.MaxHealth * GameManager.Instance.StructureHealthMultiplier, true);
		_collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
		_collisionShape.Shape = new RectangleShape2D { Size = _stats.Size };
		_sprite = GetNode<Sprite2D>("Sprite");
		_sprite.Texture = GD.Load<Texture2D>(_stats.Texture);
		_sprite.Scale = _stats.Size / _sprite.Texture.GetSize();

		_collisionShape.Disabled = true;
		UpdateVisual();

		_buildable.ProgressChanged += OnProgressChanged;
		_buildable.ConstructionCompleted += OnConstructionCompleted;
		_buildable.SetProcess(false);
		_health.Died += OnDestroyed;
		_health.HealthChanged += (current, max) => QueueRedraw();
		GameManager.Instance.PerksChanged += OnPerksChanged;
	}

	public override void _ExitTree()
	{
		if (GameManager.Instance != null)
			GameManager.Instance.PerksChanged -= OnPerksChanged;
	}

	public override void _Process(double delta)
	{
		if (!IsCompleted || _health.IsFull || _buildable.WorkPower <= 0f) return;
		_health.Heal(RepairPerSecond * _buildable.WorkPower * GameManager.Instance.BuildMultiplier * (float)delta);
	}

	public void SetPreviewMode(bool isPreview)
	{
		_isPreview = isPreview;
		UpdateVisual();
		if (!isPreview)
			_buildable.SetProcess(true);
	}

	public void SetPlacementValid(bool valid)
	{
		_placementValid = valid;
		UpdateVisual();
	}

	public void LoadState(float progress, float health)
	{
		_loading = true;
		SetPreviewMode(false);
		_buildable.SetProgress(progress);
		_loading = false;
		if (health > 0f) _health.SetCurrentHealth(health);
		QueueRedraw();
	}

	public bool ContainsPoint(Vector2 worldPosition, float margin = 6f)
	{
		Vector2 local = worldPosition - GlobalPosition;
		return Mathf.Abs(local.X) <= _stats.Size.X / 2f + margin && Mathf.Abs(local.Y) <= _stats.Size.Y / 2f + margin;
	}

	public int TryGarrison(Soldier soldier)
	{
		if (_garrison.Contains(soldier)) return _garrison.IndexOf(soldier);
		if (!CanGarrison) return -1;
		_garrison.Add(soldier);
		return _garrison.Count - 1;
	}

	public void ReleaseGarrison(Soldier soldier)
	{
		_garrison.Remove(soldier);
		for (int i = 0; i < _garrison.Count; i++)
		{
			if (IsInstanceValid(_garrison[i]))
				_garrison[i].GlobalPosition = GetSlotPosition(i);
		}
	}

	public Vector2 GetSlotPosition(int slot)
	{
		float offset = _stats.GarrisonSlots > 1 ? (slot == 0 ? -14f : 14f) : 0f;
		return GlobalPosition + new Vector2(offset, -4f);
	}

	private void UpdateVisual()
	{
		if (_sprite == null) return;
		if (_isPreview)
			_sprite.Modulate = _placementValid ? new Color(0.5f, 1f, 0.5f, 0.55f) : new Color(1f, 0.35f, 0.35f, 0.55f);
		else if (!_buildable.IsCompleted)
			_sprite.Modulate = new Color(1, 1, 1, 0.35f + 0.65f * _buildable.ProgressPercent);
		else
			_sprite.Modulate = Colors.White;
		QueueRedraw();
	}

	private void OnProgressChanged(float current, float max)
	{
		UpdateVisual();
	}

	private void OnConstructionCompleted()
	{
		_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		UpdateVisual();
		if (!_isPreview && !_loading)
		{
			GameManager.Instance.Stats.StructuresBuilt++;
			GameManager.Instance.Radio($"Construcción terminada: {_stats.Name}.", RadioKind.Good);
		}
	}

	private void OnPerksChanged()
	{
		_health.SetMaxHealth(_stats.MaxHealth * GameManager.Instance.StructureHealthMultiplier, false);
	}

	private void OnDestroyed()
	{
		if (_destroyed) return;
		_destroyed = true;
		GameManager.Instance.Stats.StructuresLost++;
		GameManager.Instance.Radio($"¡Estructura destruida: {_stats.Name}!", RadioKind.Danger);
		foreach (Soldier soldier in _garrison.ToArray())
		{
			if (IsInstanceValid(soldier)) soldier.OnGarrisonLost();
		}
		_garrison.Clear();
		Effects.Blast(GlobalPosition, Radius + 10f, new Color(0.7f, 0.7f, 0.6f));
		QueueFree();
	}

	public override void _Draw()
	{
		if (_isPreview || _stats == null) return;
		float width = _stats.Size.X;
		Vector2 origin = new Vector2(-width / 2f, -_stats.Size.Y / 2f - 9f);

		if (!_buildable.IsCompleted)
		{
			DrawRect(new Rect2(origin, new Vector2(width, 5)), new Color(0.1f, 0.1f, 0.1f, 0.8f));
			DrawRect(new Rect2(origin, new Vector2(width * _buildable.ProgressPercent, 5)), new Color(1f, 0.8f, 0.2f));
		}
		else if (!_health.IsFull)
		{
			DrawRect(new Rect2(origin, new Vector2(width, 5)), new Color(0.1f, 0.1f, 0.1f, 0.8f));
			DrawRect(new Rect2(origin, new Vector2(width * _health.Percent, 5)), new Color(0.4f, 0.75f, 1f));
		}
	}
}
