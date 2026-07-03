using Godot;

public partial class Main : Node2D
{
    [Export] private PackedScene _soldierScene;
    [Export] public int SoldierCost = 20;

    private Node2D _unitsContainer;
    private Node2D _base;
    private Health _baseHealth;
    private Node2D _spawnPoint;

    public override void _Ready()
    {
        _unitsContainer = GetNode<Node2D>("Units");
        _base = GetNode<Node2D>("Base");
        _baseHealth = GetNode<Health>("Base/Health");
        _spawnPoint = GetNode<Node2D>("Base/SoldierSpawnPoint"); // 👈 nuevo

        _baseHealth.Died += OnBaseDied;
    }

    public void TrySpawnSoldier()
    {
        if (!GameManager.Instance.SpendScrap(SoldierCost))
        {
            GD.Print("No hay suficiente chatarra para spawnear un soldado");
            return;
        }

        Soldier soldier = _soldierScene.Instantiate<Soldier>();
        _unitsContainer.AddChild(soldier);

        // Pequeño offset para que no se apilen exactamente en el mismo pixel
        // si spawneas varios seguidos, pero mucho más chico que antes
        Vector2 spawnOffset = new Vector2(
            (float)GD.RandRange(-10, 10),
            (float)GD.RandRange(-10, 10)
        );
        soldier.GlobalPosition = _spawnPoint.GlobalPosition + spawnOffset; // 👈 antes usaba _base.GlobalPosition
    }

    private void OnBaseDied()
    {
        GD.Print("¡La base fue destruida! Game over.");
        // Acá después va la lógica real de fin de partida
    }
}