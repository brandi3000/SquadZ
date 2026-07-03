using Godot;

public partial class Move : Node
{
    [Export] public float Speed = 100f;

    private CharacterBody2D _body; // referencia al nodo padre (Soldier o Zombie)

    public override void _Ready()
    {
        _body = GetParent<CharacterBody2D>();
    }

    // Devuelve la dirección hacia la que hay que moverse este frame
    public Vector2 GetDirectionTo(Vector2 targetPosition)
    {
        return (targetPosition - _body.GlobalPosition).Normalized();
    }

    // Aplica el movimiento directamente (opcional, para simplificar aún más el uso)
    public void MoveTowards(Vector2 targetPosition)
    {
        Vector2 direction = GetDirectionTo(targetPosition);
        _body.Velocity = direction * Speed;
        _body.MoveAndSlide();
    }
}