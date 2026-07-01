using Godot;
using System.Collections.Generic;

public partial class SelectionManager : Node
{
	// Referencia al contenedor de soldados en Main
	[Export] private NodePath UnitsContainerPath;
	private Node2D _unitsContainer;

	// Lista de soldados actualmente seleccionados
	private List<Soldier> _selectedSoldiers = new List<Soldier>();

	public override void _Ready()
	{
		_unitsContainer = GetNode<Node2D>(UnitsContainerPath);
	}

	public override void _Input(InputEvent @event)
	{
		// _UnhandledInput se llama cuando ningún elemento de UI consumió el input.
		// Esto evita conflictos con clicks en botones de la UI.

		// CLICK IZQUIERDO: seleccionar soldado
		if (@event is InputEventMouseButton mouseBtn
			&& mouseBtn.ButtonIndex == MouseButton.Left
			&& mouseBtn.Pressed)
		{
			HandleLeftClick(mouseBtn.GlobalPosition);
		}

		// CLICK DERECHO: mover soldados seleccionados
		if (@event is InputEventMouseButton rightBtn
			&& rightBtn.ButtonIndex == MouseButton.Right
			&& rightBtn.Pressed)
		{
			HandleRightClick(rightBtn.GlobalPosition);
		}
	}

	private void HandleLeftClick(Vector2 mouseWorldPos)
	{
		// Deseleccionamos todo primero
		DeselectAll();

		// Buscamos si hay un soldado bajo el cursor
		foreach (Node child in _unitsContainer.GetChildren())
		{
			if (child is Soldier soldier)
			{
				// Chequeo simple de distancia al click
				if (soldier.GlobalPosition.DistanceTo(mouseWorldPos) < 64f)
				{
					soldier.Select();
					_selectedSoldiers.Add(soldier);
					break; // Solo seleccionamos uno por ahora
				}
			}
		}
	}

	private void HandleRightClick(Vector2 mouseWorldPos)
	{
		// Le decimos a cada soldado seleccionado que se mueva
		// Si hay varios, los repartimos en formación simple
		int count = _selectedSoldiers.Count;
		for (int i = 0; i < count; i++)
		{
			// Offset para que no se apilen en el mismo punto
			Vector2 offset = new Vector2((i % 3 - 1) * 30f, (i / 3) * 30f);
			_selectedSoldiers[i].MoveTo(mouseWorldPos + offset);
		}
	}

	private void DeselectAll()
	{
		foreach (Soldier s in _selectedSoldiers)
			s.Deselect();
		_selectedSoldiers.Clear();
	}
}
