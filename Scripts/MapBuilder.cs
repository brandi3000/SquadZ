using Godot;
using System.Collections.Generic;

public partial class MapBuilder : Node2D
{
	[Export] public int Seed = 1990;
	[Export] public int CoverProps = 34;
	[Export] public int DecorProps = 70;
	[Export] public float RoadWidth = 140f;

	private RandomNumberGenerator _rng;
	private readonly List<Vector2> _occupied = new();
	private Vector2 _center;

	public override void _Ready()
	{
		ZIndex = -10;
		_rng = new RandomNumberGenerator { Seed = (ulong)Seed };
		_center = new Vector2(GameData.MapWidth / 2f, GameData.MapHeight / 2f);

		AddTiled("res://Assets/Sprites/texSuelo.png", new Rect2(0, 0, GameData.MapWidth, GameData.MapHeight));
		AddTiled("res://Assets/Sprites/texAsfalto.png", new Rect2(0, _center.Y - RoadWidth / 2f, GameData.MapWidth, RoadWidth));
		AddTiled("res://Assets/Sprites/texAsfalto.png", new Rect2(_center.X - RoadWidth / 2f, 0, RoadWidth, GameData.MapHeight));
		AddRoadMarks();
		AddBorder();

		_occupied.Add(_center);
		PlaceProps(CoverProps, new[] { "spriteAuto", "spriteAuto", "spriteBarricada", "spriteBarril" }, true);
		PlaceProps(DecorProps, new[] { "spriteArbolSeco", "spriteEscombros", "spriteEscombros", "spriteSangre" }, false);
	}

	private void AddTiled(string texturePath, Rect2 rect)
	{
		var tiled = new TextureRect
		{
			Texture = GD.Load<Texture2D>(texturePath),
			StretchMode = TextureRect.StretchModeEnum.Tile,
			Position = rect.Position,
			Size = rect.Size,
			TextureFilter = TextureFilterEnum.Nearest,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		AddChild(tiled);
	}

	private void AddRoadMarks()
	{
		var color = new Color(0.85f, 0.75f, 0.35f, 0.55f);
		for (float x = 20; x < GameData.MapWidth; x += 90)
		{
			if (Mathf.Abs(x - _center.X) < RoadWidth) continue;
			AddChild(new ColorRect { Position = new Vector2(x, _center.Y - 3), Size = new Vector2(46, 6), Color = color, MouseFilter = Control.MouseFilterEnum.Ignore });
		}
		for (float y = 20; y < GameData.MapHeight; y += 90)
		{
			if (Mathf.Abs(y - _center.Y) < RoadWidth) continue;
			AddChild(new ColorRect { Position = new Vector2(_center.X - 3, y), Size = new Vector2(6, 46), Color = color, MouseFilter = Control.MouseFilterEnum.Ignore });
		}
	}

	private void AddBorder()
	{
		var shade = new Color(0.06f, 0.07f, 0.06f, 0.55f);
		float t = 24f;
		AddChild(new ColorRect { Position = Vector2.Zero, Size = new Vector2(GameData.MapWidth, t), Color = shade, MouseFilter = Control.MouseFilterEnum.Ignore });
		AddChild(new ColorRect { Position = new Vector2(0, GameData.MapHeight - t), Size = new Vector2(GameData.MapWidth, t), Color = shade, MouseFilter = Control.MouseFilterEnum.Ignore });
		AddChild(new ColorRect { Position = Vector2.Zero, Size = new Vector2(t, GameData.MapHeight), Color = shade, MouseFilter = Control.MouseFilterEnum.Ignore });
		AddChild(new ColorRect { Position = new Vector2(GameData.MapWidth - t, 0), Size = new Vector2(t, GameData.MapHeight), Color = shade, MouseFilter = Control.MouseFilterEnum.Ignore });
	}

	private void PlaceProps(int count, string[] sprites, bool isCover)
	{
		int placed = 0;
		int attempts = 0;
		while (placed < count && attempts < count * 30)
		{
			attempts++;
			string name = sprites[_rng.RandiRange(0, sprites.Length - 1)];
			bool nearRoad = name == "spriteAuto" && _rng.Randf() < 0.6f;
			Vector2 position = nearRoad ? RoadsidePosition() : new Vector2(_rng.RandfRange(80, GameData.MapWidth - 80), _rng.RandfRange(80, GameData.MapHeight - 80));

			if (position.DistanceTo(_center) < 360f) continue;
			if (!IsFree(position, isCover ? 90f : 50f)) continue;

			var sprite = new Sprite2D
			{
				Texture = GD.Load<Texture2D>($"res://Assets/Sprites/{name}.png"),
				Position = position,
				TextureFilter = TextureFilterEnum.Nearest,
				Rotation = name == "spriteAuto" && nearRoad ? (_rng.Randf() < 0.5f ? 0f : Mathf.Pi / 2f) + _rng.RandfRange(-0.3f, 0.3f) : _rng.RandfRange(0, Mathf.Tau),
				Scale = Vector2.One * (name == "spriteSangre" ? _rng.RandfRange(0.8f, 1.6f) : 1.25f)
			};
			if (name == "spriteSangre") sprite.Modulate = new Color(1, 1, 1, 0.7f);
			AddChild(sprite);
			if (isCover) sprite.AddToGroup("cover");

			_occupied.Add(position);
			placed++;
		}
	}

	private Vector2 RoadsidePosition()
	{
		if (_rng.Randf() < 0.5f)
			return new Vector2(_rng.RandfRange(80, GameData.MapWidth - 80), _center.Y + _rng.RandfRange(-RoadWidth / 2f + 20, RoadWidth / 2f - 20));
		return new Vector2(_center.X + _rng.RandfRange(-RoadWidth / 2f + 20, RoadWidth / 2f - 20), _rng.RandfRange(80, GameData.MapHeight - 80));
	}

	private bool IsFree(Vector2 position, float spacing)
	{
		foreach (Vector2 other in _occupied)
		{
			if (other.DistanceTo(position) < spacing) return false;
		}
		return true;
	}
}
