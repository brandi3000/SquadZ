using Godot;
using System.Collections.Generic;

public static class Combat
{
	public const uint LayerWorld = 1;
	public const uint LayerSoldiers = 2;
	public const uint LayerEnemies = 4;

	public static Health GetHealth(Node2D node)
	{
		if (node == null || !GodotObject.IsInstanceValid(node) || !node.IsInsideTree()) return null;
		return node.GetNodeOrNull<Health>("Health");
	}

	public static bool IsAlive(Node2D node)
	{
		var health = GetHealth(node);
		return health != null && !health.IsDead;
	}

	public static bool Damage(Node2D node, float amount)
	{
		var health = GetHealth(node);
		if (health == null || health.IsDead) return false;
		return health.TakeDamage(amount);
	}

	public static float EdgeDistance(Node2D from, Node2D to)
	{
		return from.GlobalPosition.DistanceTo(to.GlobalPosition) - Radius(from) - Radius(to);
	}

	public static float Radius(Node2D node)
	{
		if (node is Structure structure) return structure.Radius;
		if (node is CollisionObject2D collisionObject)
		{
			foreach (uint ownerId in collisionObject.GetShapeOwners())
			{
				int shapeCount = collisionObject.ShapeOwnerGetShapeCount(ownerId);
				for (int i = 0; i < shapeCount; i++)
				{
					var shape = collisionObject.ShapeOwnerGetShape(ownerId, i);
					var scale = collisionObject.ShapeOwnerGetTransform(ownerId).Scale;
					if (shape is CircleShape2D circle) return circle.Radius * Mathf.Max(scale.X, scale.Y);
					if (shape is RectangleShape2D rect) return Mathf.Min(rect.Size.X, rect.Size.Y) / 2f;
				}
			}
		}
		return 0f;
	}

	public static List<Node2D> InRadius(SceneTree tree, string group, Vector2 center, float radius)
	{
		var result = new List<Node2D>();
		foreach (Node node in tree.GetNodesInGroup(group))
		{
			if (node is Node2D node2D && IsAlive(node2D) && node2D.GlobalPosition.DistanceTo(center) <= radius + Radius(node2D))
				result.Add(node2D);
		}
		return result;
	}

	public static void AreaDamage(SceneTree tree, Vector2 center, float radius, float amount, bool hitsPlayerSide)
	{
		string[] groups = hitsPlayerSide ? new[] { "soldiers", "structures", "base" } : new[] { "enemies" };
		foreach (string group in groups)
		{
			foreach (Node2D target in InRadius(tree, group, center, radius))
			{
				if (target is Structure structure && !structure.IsCompleted) continue;
				Damage(target, amount);
			}
		}
	}
}
