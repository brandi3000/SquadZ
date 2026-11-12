using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class SelectionManager : Node2D
{
	[Export] private NodePath UnitsContainerPath;

	[Signal] public delegate void SelectionChangedEventHandler();

	public static SelectionManager Instance { get; private set; }

	private const float ClickRadius = 26f;
	private const float DragThreshold = 10f;

	private Node2D _unitsContainer;
	private BuildManager _buildManager;
	private readonly List<Soldier> _selectedSoldiers = new();

	private bool _mouseDown;
	private bool _isDragging;
	private Vector2 _dragStart;
	private Vector2 _dragCurrent;

	public IReadOnlyList<Soldier> Selected => _selectedSoldiers;

	public override void _Ready()
	{
		Instance = this;
		ZIndex = 30;
		_unitsContainer = GetNode<Node2D>(UnitsContainerPath);
		_buildManager = GetNodeOrNull<BuildManager>("../BuildManager");
	}

	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;
	}

	public override void _Process(double delta)
	{
		int removed = _selectedSoldiers.RemoveAll(s => !IsInstanceValid(s) || s.IsQueuedForDeletion());
		if (removed > 0)
			EmitSignal(SignalName.SelectionChanged);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (GameManager.Instance.CurrentState == GameState.GameOver) return;
		if (_buildManager != null && _buildManager.IsPlacing) return;

		if (@event is InputEventMouseButton mouseBtn)
		{
			if (mouseBtn.ButtonIndex == MouseButton.Left)
			{
				if (mouseBtn.Pressed)
				{
					_mouseDown = true;
					_dragStart = GetGlobalMousePosition();
					_dragCurrent = _dragStart;
				}
				else if (_mouseDown)
				{
					_mouseDown = false;
					if (_isDragging) BoxSelect(new Rect2(_dragStart, _dragCurrent - _dragStart).Abs(), mouseBtn.ShiftPressed);
					else HandleLeftClick(GetGlobalMousePosition(), mouseBtn.ShiftPressed);
					_isDragging = false;
					QueueRedraw();
				}
			}
			else if (mouseBtn.ButtonIndex == MouseButton.Right && mouseBtn.Pressed)
			{
				HandleRightClick(GetGlobalMousePosition());
			}
		}
		else if (@event is InputEventMouseMotion && _mouseDown)
		{
			_dragCurrent = GetGlobalMousePosition();
			if (_dragStart.DistanceTo(_dragCurrent) > DragThreshold) _isDragging = true;
			QueueRedraw();
		}
		else if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			HandleKey(key);
		}
	}

	private void HandleKey(InputEventKey key)
	{
		int squad = key.Keycode switch
		{
			Key.Key1 => 1,
			Key.Key2 => 2,
			Key.Key3 => 3,
			Key.Key4 => 4,
			_ => 0
		};

		if (squad > 0)
		{
			if (key.CtrlPressed) SquadManager.Instance?.AssignSquad(squad, _selectedSoldiers);
			else SelectSquad(squad);
			GetViewport().SetInputAsHandled();
			return;
		}

		switch (key.Keycode)
		{
			case Key.H:
				OrderReturnToBase();
				GetViewport().SetInputAsHandled();
				break;
			case Key.X:
				OrderStop();
				GetViewport().SetInputAsHandled();
				break;
			case Key.A when key.CtrlPressed:
				SelectAll();
				GetViewport().SetInputAsHandled();
				break;
		}
	}

	private void HandleLeftClick(Vector2 mouseWorldPos, bool additive)
	{
		Soldier clicked = FindSoldierAt(mouseWorldPos);
		if (!additive) DeselectAll();

		if (clicked != null)
		{
			if (additive && clicked.IsSelected) RemoveFromSelection(clicked);
			else AddToSelection(clicked);
		}
		EmitSignal(SignalName.SelectionChanged);
	}

	private void BoxSelect(Rect2 rect, bool additive)
	{
		if (!additive) DeselectAll();
		foreach (Soldier soldier in GetAllSoldiers())
		{
			if (rect.HasPoint(soldier.GlobalPosition)) AddToSelection(soldier);
		}
		EmitSignal(SignalName.SelectionChanged);
	}

	private void HandleRightClick(Vector2 mouseWorldPos)
	{
		if (_selectedSoldiers.Count == 0) return;

		Structure clickedStructure = FindStructureAtPosition(mouseWorldPos);
		if (clickedStructure != null)
		{
			OrderOnStructure(clickedStructure);
			return;
		}

		List<Vector2> slots = FormationSlots(mouseWorldPos, _selectedSoldiers.Count);
		for (int i = 0; i < _selectedSoldiers.Count; i++)
			_selectedSoldiers[i].MoveTo(slots[i]);
		Effects.Blast(mouseWorldPos, 14f, new Color(0.6f, 0.9f, 1f));
	}

	private void OrderOnStructure(Structure structure)
	{
		int freeSlots = structure.CanGarrison ? structure.Stats.GarrisonSlots - structure.GarrisonCount : 0;
		foreach (Soldier soldier in _selectedSoldiers)
		{
			if (structure.NeedsWork)
			{
				soldier.AssignToBuild(structure);
			}
			else if (freeSlots > 0 && soldier.Garrison != structure)
			{
				soldier.GarrisonIn(structure);
				freeSlots--;
			}
			else if (soldier.Garrison != structure)
			{
				soldier.MoveTo(structure.GlobalPosition + new Vector2(0, structure.Radius + 30f));
			}
		}
		Effects.Blast(structure.GlobalPosition, structure.Radius + 6f, new Color(1f, 0.8f, 0.2f));
	}

	public void AssignSelectedToBuild(Structure structure)
	{
		foreach (Soldier soldier in _selectedSoldiers)
			soldier.AssignToBuild(structure);
	}

	public void OrderReturnToBase()
	{
		var bases = GetTree().GetNodesInGroup("base");
		if (bases.Count == 0 || _selectedSoldiers.Count == 0) return;
		var baseNode = (Base)bases[0];
		List<Vector2> slots = FormationSlots(baseNode.GlobalPosition + new Vector2(0, 150), _selectedSoldiers.Count);
		for (int i = 0; i < _selectedSoldiers.Count; i++)
			_selectedSoldiers[i].ReturnToBase(slots[i]);
		GameManager.Instance.Radio($"{_selectedSoldiers.Count} unidades vuelven a la base para curarse.", RadioKind.Info);
	}

	public void OrderStop()
	{
		foreach (Soldier soldier in _selectedSoldiers)
			soldier.Stop();
	}

	public void SelectSquad(int squadId)
	{
		if (SquadManager.Instance == null) return;
		var members = SquadManager.Instance.GetMembers(squadId);
		if (members.Count == 0) return;
		DeselectAll();
		foreach (Soldier soldier in members) AddToSelection(soldier);
		EmitSignal(SignalName.SelectionChanged);
	}

	public void SelectAll()
	{
		DeselectAll();
		foreach (Soldier soldier in GetAllSoldiers()) AddToSelection(soldier);
		EmitSignal(SignalName.SelectionChanged);
	}

	public void ClearSelection()
	{
		DeselectAll();
		EmitSignal(SignalName.SelectionChanged);
	}

	private static List<Vector2> FormationSlots(Vector2 center, int count)
	{
		var slots = new List<Vector2>();
		int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
		int rows = Mathf.CeilToInt((float)count / columns);
		const float spacing = 40f;
		for (int i = 0; i < count; i++)
		{
			int col = i % columns;
			int row = i / columns;
			slots.Add(center + new Vector2((col - (columns - 1) / 2f) * spacing, (row - (rows - 1) / 2f) * spacing));
		}
		return slots;
	}

	private IEnumerable<Soldier> GetAllSoldiers()
	{
		return GetTree().GetNodesInGroup("soldiers").OfType<Soldier>().Where(s => IsInstanceValid(s) && !s.IsQueuedForDeletion());
	}

	private Soldier FindSoldierAt(Vector2 worldPos)
	{
		Soldier best = null;
		float bestDistance = ClickRadius;
		foreach (Soldier soldier in GetAllSoldiers())
		{
			float distance = soldier.GlobalPosition.DistanceTo(worldPos);
			if (distance < bestDistance)
			{
				bestDistance = distance;
				best = soldier;
			}
		}
		return best;
	}

	private Structure FindStructureAtPosition(Vector2 worldPos)
	{
		foreach (Node node in GetTree().GetNodesInGroup("structures"))
		{
			if (node is Structure structure && !structure.IsPreview && structure.ContainsPoint(worldPos))
				return structure;
		}
		return null;
	}

	private void AddToSelection(Soldier soldier)
	{
		if (_selectedSoldiers.Contains(soldier)) return;
		soldier.Select();
		_selectedSoldiers.Add(soldier);
	}

	private void RemoveFromSelection(Soldier soldier)
	{
		soldier.Deselect();
		_selectedSoldiers.Remove(soldier);
	}

	private void DeselectAll()
	{
		foreach (Soldier s in _selectedSoldiers)
		{
			if (IsInstanceValid(s)) s.Deselect();
		}
		_selectedSoldiers.Clear();
	}

	public override void _Draw()
	{
		if (!_isDragging) return;
		var rect = new Rect2(ToLocal(_dragStart), _dragCurrent - _dragStart).Abs();
		DrawRect(rect, new Color(0.6f, 0.95f, 0.4f, 0.12f));
		DrawRect(rect, new Color(0.6f, 0.95f, 0.4f, 0.9f), false, 1.5f);
	}
}
