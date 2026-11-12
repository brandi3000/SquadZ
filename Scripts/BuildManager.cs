using Godot;

public partial class BuildManager : Node2D
{
	[Export] private PackedScene _wallScene;
	[Export] private PackedScene _towerScene;
	[Export] private Node2D _structuresContainer;
	[Export] public int GridSize = 32;

	private bool _isPlacingBuilding = false;
	private StructureType _currentType;
	private Structure _previewStructure;
	private bool _previewValid;

	public bool IsPlacing => _isPlacingBuilding;
	public StructureType CurrentType => _currentType;

	public void StartPlacingWall() => StartPlacing(StructureType.Muro);

	public void StartPlacing(StructureType type)
	{
		if (GameManager.Instance.CurrentState == GameState.GameOver) return;
		if (_isPlacingBuilding) CancelPlacement();

		_isPlacingBuilding = true;
		_currentType = type;
		_previewStructure = CreateStructure(type);
		_previewStructure.GlobalPosition = SnapToGrid(GetGlobalMousePosition(), type);

		var stats = GameData.Structures[type];
		if (GameManager.Instance.CurrentScrap < stats.Cost)
			GameManager.Instance.Radio($"Falta chatarra para {stats.Name} ({stats.Cost}).", RadioKind.Warning);
	}

	public Structure CreateStructure(StructureType type)
	{
		PackedScene scene = type == StructureType.Torre ? _towerScene : _wallScene;
		var structure = scene.Instantiate<Structure>();
		structure.Type = type;
		_structuresContainer.AddChild(structure);
		return structure;
	}

	public override void _Process(double delta)
	{
		if (!_isPlacingBuilding || _previewStructure == null) return;

		Vector2 position = SnapToGrid(GetGlobalMousePosition(), _currentType);
		_previewStructure.GlobalPosition = position;
		_previewValid = IsValidPlacement(position, _currentType);
		_previewStructure.SetPlacementValid(_previewValid);
	}

	private Vector2 SnapToGrid(Vector2 position, StructureType type)
	{
		Vector2 size = GameData.Structures[type].Size;
		float offsetX = Mathf.PosMod(size.X / GridSize, 2f) >= 1f ? GridSize / 2f : 0f;
		float offsetY = Mathf.PosMod(size.Y / GridSize, 2f) >= 1f ? GridSize / 2f : 0f;
		float x = Mathf.Round((position.X - offsetX) / GridSize) * GridSize + offsetX;
		float y = Mathf.Round((position.Y - offsetY) / GridSize) * GridSize + offsetY;
		return new Vector2(x, y);
	}

	private bool IsValidPlacement(Vector2 position, StructureType type)
	{
		Vector2 half = GameData.Structures[type].Size / 2f;
		if (position.X - half.X < 16 || position.Y - half.Y < 16 || position.X + half.X > GameData.MapWidth - 16 || position.Y + half.Y > GameData.MapHeight - 16)
			return false;

		var bases = GetTree().GetNodesInGroup("base");
		if (bases.Count > 0 && bases[0] is Base baseNode)
		{
			Vector2 baseHalf = new Vector2(96, 80) + half + new Vector2(8, 8);
			Vector2 diff = (position - baseNode.GlobalPosition).Abs();
			if (diff.X < baseHalf.X && diff.Y < baseHalf.Y) return false;
		}

		foreach (Node node in GetTree().GetNodesInGroup("structures"))
		{
			if (node is not Structure other || other == _previewStructure) continue;
			Vector2 otherHalf = other.Stats.Size / 2f;
			Vector2 diff = (position - other.GlobalPosition).Abs();
			if (diff.X < half.X + otherHalf.X - 0.5f && diff.Y < half.Y + otherHalf.Y - 0.5f) return false;
		}

		foreach (Node node in GetTree().GetNodesInGroup("enemies"))
		{
			if (node is Node2D enemy && enemy.GlobalPosition.DistanceTo(position) < half.Length() + 16f) return false;
		}

		return true;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_isPlacingBuilding) return;

		if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed)
		{
			if (mouseBtn.ButtonIndex == MouseButton.Left)
			{
				ConfirmPlacement(mouseBtn.ShiftPressed);
				GetViewport().SetInputAsHandled();
			}
			else if (mouseBtn.ButtonIndex == MouseButton.Right)
			{
				CancelPlacement();
				GetViewport().SetInputAsHandled();
			}
		}
		else if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
		{
			CancelPlacement();
			GetViewport().SetInputAsHandled();
		}
	}

	private void ConfirmPlacement(bool keepPlacing)
	{
		var stats = GameData.Structures[_currentType];
		if (!_previewValid)
		{
			GameManager.Instance.Radio("No se puede construir ahí.", RadioKind.Warning);
			return;
		}

		if (!GameManager.Instance.SpendScrap(stats.Cost))
		{
			GameManager.Instance.Radio($"No hay suficiente chatarra para {stats.Name} ({stats.Cost}).", RadioKind.Warning);
			return;
		}

		Structure placed = _previewStructure;
		placed.SetPreviewMode(false);
		_previewStructure = null;
		_isPlacingBuilding = false;

		SelectionManager.Instance?.AssignSelectedToBuild(placed);
		GameManager.Instance.Radio($"Plano de {stats.Name} colocado. Click derecho con unidades seleccionadas para construir.", RadioKind.Info);

		if (keepPlacing)
			StartPlacing(_currentType);
	}

	public void CancelPlacement()
	{
		if (_previewStructure != null)
		{
			_previewStructure.QueueFree();
			_previewStructure = null;
		}
		_isPlacingBuilding = false;
	}
}
