using Godot;

public partial class Move : Node
{
	[Export] public float Speed = 100f;

	private CharacterBody2D _body;

	public override void _Ready()
	{
		_body = GetParent<CharacterBody2D>();
	}

	public Vector2 GetDirectionTo(Vector2 targetPosition)
	{
		return (targetPosition - _body.GlobalPosition).Normalized();
	}

	public void MoveTowards(Vector2 targetPosition)
	{
		Vector2 toTarget = targetPosition - _body.GlobalPosition;
		float step = Speed * (float)_body.GetPhysicsProcessDeltaTime();
		_body.Velocity = toTarget.Length() <= step ? toTarget / (float)_body.GetPhysicsProcessDeltaTime() : toTarget.Normalized() * Speed;
		_body.MoveAndSlide();
	}

	public void Stop()
	{
		_body.Velocity = Vector2.Zero;
		_body.MoveAndSlide();
	}
}
