using Godot;

public partial class Main : Node2D
{
    private Health _baseHealth;
    private ProgressBar _baseHealthBar;

    public override void _Ready()
    {
        _baseHealth = GetNode<Health>("Base/Health");
        _baseHealthBar = GetNode<ProgressBar>("UI/BaseHealthBar");

        // Colores bien visibles, igual que hicimos con las unidades
        var bgStyle = new StyleBoxFlat();
        bgStyle.BgColor = new Color(0.2f, 0.2f, 0.2f);

        var fillStyle = new StyleBoxFlat();
        fillStyle.BgColor = new Color(0.1f, 0.6f, 0.9f); // celeste, para diferenciarla de la vida roja de las unidades

        _baseHealthBar.AddThemeStyleboxOverride("background", bgStyle);
        _baseHealthBar.AddThemeStyleboxOverride("fill", fillStyle);

        _baseHealthBar.MaxValue = _baseHealth.MaxHealth;
        _baseHealthBar.Value = _baseHealth.MaxHealth;

        _baseHealth.HealthChanged += OnBaseHealthChanged;
        _baseHealth.Died += OnBaseDied;
    }

    private void OnBaseHealthChanged(float current, float max)
    {
        _baseHealthBar.Value = current;
    }

    private void OnBaseDied()
    {
        GD.Print("¡La base fue destruida! Game over.");
        // Acá después va la lógica real de fin de partida
    }
}