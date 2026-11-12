using Godot;

public partial class Effects : Node2D
{
	public static Effects Instance { get; private set; }

	public override void _Ready()
	{
		Instance = this;
		ZIndex = 20;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;
	}

	public static void Shot(Vector2 from, Vector2 to, Color color, float width)
	{
		if (Instance == null) return;
		var line = new Line2D { Width = width, DefaultColor = color };
		line.AddPoint(from);
		line.AddPoint(to);
		Instance.AddChild(line);
		var tween = line.CreateTween();
		tween.TweenProperty(line, "modulate:a", 0f, 0.25f);
		tween.TweenCallback(Callable.From(line.QueueFree));
	}

	public static void Blast(Vector2 position, float radius, Color color)
	{
		if (Instance == null) return;
		var ring = new BlastRing { Radius = radius, RingColor = color, GlobalPosition = position };
		Instance.AddChild(ring);
	}

	public static void Decal(Vector2 position, float scale)
	{
		if (Instance == null || Instance.GetParent() == null) return;
		var decal = new Sprite2D
		{
			Texture = GD.Load<Texture2D>("res://Assets/Sprites/spriteSangre.png"),
			GlobalPosition = position,
			Rotation = GD.Randf() * Mathf.Tau,
			Scale = Vector2.One * scale,
			ZIndex = -5,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest
		};
		Instance.GetParent().AddChild(decal);
		var tween = decal.CreateTween();
		tween.TweenInterval(25f);
		tween.TweenProperty(decal, "modulate:a", 0f, 5f);
		tween.TweenCallback(Callable.From(decal.QueueFree));
	}

	public static void FloatingText(Vector2 position, string text, Color color)
	{
		if (Instance == null) return;
		var label = new Label { Text = text, Position = position + new Vector2(-30, -40), Size = new Vector2(60, 20), HorizontalAlignment = HorizontalAlignment.Center };
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeColorOverride("font_outline_color", Colors.Black);
		label.AddThemeConstantOverride("outline_size", 4);
		label.AddThemeFontSizeOverride("font_size", 14);
		Instance.AddChild(label);
		var tween = label.CreateTween();
		tween.SetParallel(true);
		tween.TweenProperty(label, "position:y", label.Position.Y - 30, 0.9f);
		tween.TweenProperty(label, "modulate:a", 0f, 0.9f);
		tween.Chain().TweenCallback(Callable.From(label.QueueFree));
	}
}
