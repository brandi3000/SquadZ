using Godot;

public partial class Minimap : Control
{
	private float _redrawTimer;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(208, 130);
		MouseFilter = MouseFilterEnum.Stop;
		TooltipText = "Minimapa: click para mover la cámara";
	}

	public override void _Process(double delta)
	{
		_redrawTimer -= (float)delta;
		if (_redrawTimer <= 0f)
		{
			_redrawTimer = 0.2f;
			QueueRedraw();
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		bool click = @event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true };
		bool drag = @event is InputEventMouseMotion motion && (motion.ButtonMask & MouseButtonMask.Left) != 0;
		if (!click && !drag) return;

		Vector2 local = GetLocalMousePosition();
		Vector2 world = new Vector2(local.X / Size.X * GameData.MapWidth, local.Y / Size.Y * GameData.MapHeight);
		var camera = GetViewport().GetCamera2D() as CameraController;
		camera?.CenterOn(world);
		AcceptEvent();
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(Vector2.Zero, Size), UiTheme.LcdBack);
		Vector2 scale = new Vector2(Size.X / GameData.MapWidth, Size.Y / GameData.MapHeight);
		var tree = GetTree();

		DrawRect(new Rect2(new Vector2(0, (GameData.MapHeight / 2f - 70) * scale.Y), new Vector2(Size.X, 140 * scale.Y)), new Color(UiTheme.LcdDim, 0.35f));
		DrawRect(new Rect2(new Vector2((GameData.MapWidth / 2f - 70) * scale.X, 0), new Vector2(140 * scale.X, Size.Y)), new Color(UiTheme.LcdDim, 0.35f));

		foreach (Node node in tree.GetNodesInGroup("base"))
		{
			if (node is Node2D baseNode)
				DrawRect(new Rect2(baseNode.GlobalPosition * scale - new Vector2(6, 5), new Vector2(12, 10)), UiTheme.Sand);
		}
		foreach (Node node in tree.GetNodesInGroup("structures"))
		{
			if (node is Structure s && !s.IsPreview)
				DrawRect(new Rect2(s.GlobalPosition * scale - Vector2.One * 1.5f, Vector2.One * 3f), UiTheme.Khaki);
		}
		foreach (Node node in tree.GetNodesInGroup("soldiers"))
		{
			if (node is Node2D soldier)
				DrawCircle(soldier.GlobalPosition * scale, 2f, UiTheme.Good);
		}
		foreach (Node node in tree.GetNodesInGroup("enemies"))
		{
			if (node is not Node2D enemy) continue;
			bool boss = enemy.IsInGroup("boss");
			bool raider = enemy.IsInGroup("raiders");
			DrawCircle(enemy.GlobalPosition * scale, boss ? 4.5f : 1.8f, raider ? UiTheme.Warning : UiTheme.Danger);
		}

		var camera = GetViewport().GetCamera2D();
		if (camera != null)
		{
			Vector2 viewSize = GetViewport().GetVisibleRect().Size / camera.Zoom;
			Vector2 topLeft = camera.GetScreenCenterPosition() - viewSize / 2f;
			DrawRect(new Rect2(topLeft * scale, viewSize * scale), UiTheme.LcdText, false, 1f);
		}
		DrawRect(new Rect2(Vector2.Zero, Size), UiTheme.Black, false, 2f);
	}
}
