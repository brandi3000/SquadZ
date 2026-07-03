using Godot;

public partial class Structure : StaticBody2D
{
    private Buildable _buildable;
    private CollisionShape2D _collisionShape;
    private ColorRect _colorRect; // 👈 antes era Sprite2D

    private bool _isPreview = true;

    public override void _Ready()
    {
		AddToGroup("structures");

        _buildable = GetNode<Buildable>("Buildable");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        _colorRect = GetNode<ColorRect>("ColorRect"); // 👈 ajustá el nombre exacto del nodo en tu árbol

        _collisionShape.Disabled = true;
        UpdatePreviewVisual();

        _buildable.ProgressChanged += OnProgressChanged;
        _buildable.ConstructionCompleted += OnConstructionCompleted;

        _buildable.SetProcess(false);
    }

    public void SetPreviewMode(bool isPreview)
    {
        _isPreview = isPreview;
        UpdatePreviewVisual();

        if (!isPreview)
        {
            _buildable.SetProcess(true);
        }
    }

    private void UpdatePreviewVisual()
    {
        if (_isPreview)
        {
            _colorRect.Modulate = new Color(0.5f, 1f, 0.5f, 0.5f); // verdoso, "modo fantasma"
        }
        else
        {
            _colorRect.Modulate = new Color(1, 1, 1, 0.35f); // blueprint confirmado, semi-transparente
        }
    }

    private void OnProgressChanged(float current, float max)
    {
        float t = current / max;
        _colorRect.Modulate = new Color(1, 1, 1, 0.35f + 0.65f * t);
    }

    private void OnConstructionCompleted()
    {
        _collisionShape.Disabled = false;
        _colorRect.Modulate = Colors.White;
        GD.Print("¡Estructura completada!");
    }

    public Buildable Buildable => _buildable;
}