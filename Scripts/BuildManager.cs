using Godot;

public partial class BuildManager : Node2D
{
	[Export] private PackedScene _wallScene;
	[Export] private Node2D _structuresContainer;
	[Export] public int WallCost = 30;
	[Export] public int GridSize = 32; // 👈 nuevo

	private bool _isPlacingBuilding = false;
	private PackedScene _currentBuildingScene;
	private Structure _previewStructure;
	private int _currentCost;

	public void StartPlacingWall()
	{
		if (_isPlacingBuilding) return;

		_isPlacingBuilding = true;
		_currentBuildingScene = _wallScene;
		_currentCost = WallCost;

		_previewStructure = _currentBuildingScene.Instantiate<Structure>();
		_structuresContainer.AddChild(_previewStructure);
		_previewStructure.SetPreviewMode(true);

		GD.Print("Modo colocación activado: click para confirmar, click derecho para cancelar");
	}

	public override void _Process(double delta)
	{
		if (!_isPlacingBuilding || _previewStructure == null) return;

		Vector2 mousePos = GetGlobalMousePosition();
		_previewStructure.GlobalPosition = SnapToGrid(mousePos); // 👈 nuevo
	}

	private Vector2 SnapToGrid(Vector2 position)
	{
		float x = Mathf.Round(position.X / GridSize) * GridSize;
		float y = Mathf.Round(position.Y / GridSize) * GridSize;
		return new Vector2(x, y);
	}

	public override void _Input(InputEvent @event)
	{
		if (!_isPlacingBuilding) return;

		if (@event is InputEventMouseButton mouseBtn && mouseBtn.ButtonIndex == MouseButton.Left && mouseBtn.Pressed)
		{
			ConfirmPlacement();
		}

		if (@event is InputEventMouseButton rightBtn && rightBtn.ButtonIndex == MouseButton.Right && rightBtn.Pressed)
		{
			CancelPlacement();
		}
	}

	private void ConfirmPlacement()
	{
		if (!GameManager.Instance.SpendScrap(_currentCost))
		{
			GD.Print("No hay suficiente chatarra para construir");
			return;
		}

		_previewStructure.SetPreviewMode(false);
		_previewStructure = null;
		_isPlacingBuilding = false;
	}

	private void CancelPlacement()
	{
		if (_previewStructure != null)
		{
			_previewStructure.QueueFree();
			_previewStructure = null;
		}

		_isPlacingBuilding = false;
		GD.Print("Colocación cancelada");
	}
}
