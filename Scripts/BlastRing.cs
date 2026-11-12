using Godot;

public partial class BlastRing : Node2D
{
	public float Radius = 60f;
	public Color RingColor = new Color(1f, 0.5f, 0.1f);

	private float _t;

	public override void _Process(double delta)
	{
		_t += (float)delta / 0.35f;
		if (_t >= 1f)
		{
			QueueFree();
			return;
		}
		QueueRedraw();
	}

	public override void _Draw()
	{
		var fill = RingColor;
		fill.A = 0.35f * (1f - _t);
		DrawCircle(Vector2.Zero, Radius * (0.4f + 0.6f * _t), fill);
		var edge = RingColor;
		edge.A = 1f - _t;
		DrawArc(Vector2.Zero, Radius * (0.4f + 0.6f * _t), 0, Mathf.Tau, 40, edge, 3f);
	}
}
