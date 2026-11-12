using Godot;

public partial class CameraController : Camera2D
{
	public const string ActionUp = "camara_arriba";
	public const string ActionDown = "camara_abajo";
	public const string ActionLeft = "camara_izquierda";
	public const string ActionRight = "camara_derecha";

	[Export] public float PanSpeed = 900f;
	[Export] public float EdgeMargin = 24f;
	[Export] public bool EdgeScrolling = true;
	[Export] public float MinZoom = 0.45f;
	[Export] public float MaxZoom = 1.6f;
	[Export] public float ZoomStep = 0.1f;

	private bool _dragging;
	private bool _mouseInside = true;

	public override void _Ready()
	{
		EnsureInputActions();
		LimitLeft = 0;
		LimitTop = 0;
		LimitRight = (int)GameData.MapWidth;
		LimitBottom = (int)GameData.MapHeight;
		PositionSmoothingEnabled = true;
		PositionSmoothingSpeed = 12f;
		MakeCurrent();
		CenterOnBase();
		ResetSmoothing();
	}

	public static void EnsureInputActions()
	{
		AddKeys(ActionUp, Key.W, Key.Up);
		AddKeys(ActionDown, Key.S, Key.Down);
		AddKeys(ActionLeft, Key.A, Key.Left);
		AddKeys(ActionRight, Key.D, Key.Right);
	}

	private static void AddKeys(string action, Key letter, Key arrow)
	{
		if (InputMap.HasAction(action)) return;
		InputMap.AddAction(action);
		InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = letter });
		InputMap.ActionAddEvent(action, new InputEventKey { Keycode = arrow });
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMMouseExit) _mouseInside = false;
		else if (what == NotificationWMMouseEnter) _mouseInside = true;
		else if (what == NotificationApplicationFocusOut) _mouseInside = false;
	}

	public override void _Process(double delta)
	{
		Vector2 direction = Input.IsKeyPressed(Key.Ctrl) ? Vector2.Zero : Input.GetVector(ActionLeft, ActionRight, ActionUp, ActionDown);

		if (EdgeScrolling && _mouseInside && !_dragging && GetViewport().GuiGetHoveredControl() == null)
		{
			Vector2 mouse = GetViewport().GetMousePosition();
			Vector2 size = GetViewportRect().Size;
			if (mouse.X < EdgeMargin) direction.X -= 1;
			if (mouse.X > size.X - EdgeMargin) direction.X += 1;
			if (mouse.Y < EdgeMargin) direction.Y -= 1;
			if (mouse.Y > size.Y - EdgeMargin) direction.Y += 1;
		}

		if (direction != Vector2.Zero)
		{
			Position += direction.LimitLength(1f) * PanSpeed * (float)delta / Zoom.X;
			ClampPosition();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseBtn)
		{
			if (mouseBtn.ButtonIndex == MouseButton.WheelUp && mouseBtn.Pressed) ZoomAt(Zoom.X + ZoomStep, mouseBtn.Position);
			else if (mouseBtn.ButtonIndex == MouseButton.WheelDown && mouseBtn.Pressed) ZoomAt(Zoom.X - ZoomStep, mouseBtn.Position);
			else if (mouseBtn.ButtonIndex == MouseButton.Middle) _dragging = mouseBtn.Pressed;
		}
		else if (@event is InputEventMouseMotion motion && _dragging)
		{
			Position -= motion.Relative / Zoom.X;
			ClampPosition();
			ResetSmoothing();
		}
		else if (@event is InputEventKey key && key.Pressed && !key.Echo && key.Keycode == Key.Space)
		{
			CenterOnBase();
			GetViewport().SetInputAsHandled();
		}
	}

	public void CenterOn(Vector2 worldPosition)
	{
		Position = worldPosition;
		ClampPosition();
	}

	public void CenterOnBase()
	{
		var bases = GetTree().GetNodesInGroup("base");
		CenterOn(bases.Count > 0 ? ((Node2D)bases[0]).GlobalPosition : new Vector2(GameData.MapWidth / 2f, GameData.MapHeight / 2f));
	}

	private void ZoomAt(float value, Vector2 screenPoint)
	{
		Vector2 size = GetViewportRect().Size;
		float minZoomToFit = Mathf.Max(size.X / GameData.MapWidth, size.Y / GameData.MapHeight);
		float z = Mathf.Clamp(value, Mathf.Max(MinZoom, minZoomToFit), MaxZoom);
		Vector2 offsetFromCenter = screenPoint - size / 2f;
		Vector2 worldUnderMouse = Position + offsetFromCenter / Zoom.X;
		Zoom = new Vector2(z, z);
		Position = worldUnderMouse - offsetFromCenter / z;
		ClampPosition();
		ResetSmoothing();
	}

	private void ClampPosition()
	{
		Vector2 half = GetViewportRect().Size / (2f * Zoom.X);
		Position = new Vector2(
			Mathf.Clamp(Position.X, half.X, Mathf.Max(half.X, GameData.MapWidth - half.X)),
			Mathf.Clamp(Position.Y, half.Y, Mathf.Max(half.Y, GameData.MapHeight - half.Y)));
	}
}
