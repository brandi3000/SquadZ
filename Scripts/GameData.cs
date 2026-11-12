using Godot;
using System.Collections.Generic;

public enum UnitType { Fusilero, Escopetero, Subfusilero, Francotirador, Ingeniero }

public enum EnemyType { Normal, Corredor, Tanque, Boomer, Jefe, Saqueador }

public enum StructureType { Muro, Torre }

public enum PerkType
{
	Blindaje,
	MunicionPerforante,
	GatilloFacil,
	Chatarrero,
	BotasDeCombate,
	OjoDeHalcon,
	MurosReforzados,
	IngenieriaRapida,
	Medico,
	Fortaleza,
	Reclutamiento,
	Adrenalina
}

public class UnitStats
{
	public string Name;
	public string Role;
	public int Cost;
	public float MaxHealth;
	public float Damage;
	public float Range;
	public float Cooldown;
	public float Speed;
	public float SplashRadius;
	public float BuildPower;
	public string Texture;
	public Color ShotColor;
	public float ShotWidth;
	public Key Hotkey;
}

public class EnemyStats
{
	public string Name;
	public float MaxHealth;
	public float Damage;
	public float Range;
	public float Cooldown;
	public float Speed;
	public float AggroRadius;
	public float SplashRadius;
	public int Scrap;
	public float Scale;
	public float CollisionRadius;
	public string Texture;
}

public class StructureStats
{
	public string Name;
	public int Cost;
	public float MaxHealth;
	public float BuildWork;
	public Vector2 Size;
	public int GarrisonSlots;
	public string Texture;
	public Key Hotkey;
}

public class PerkInfo
{
	public string Name;
	public string Description;
}

public static class GameData
{
	public const float MapWidth = 3200f;
	public const float MapHeight = 2000f;

	public static readonly Dictionary<UnitType, UnitStats> Units = new()
	{
		[UnitType.Fusilero] = new UnitStats
		{
			Name = "Fusilero", Role = "Balanceado a media distancia", Cost = 20, MaxHealth = 100, Damage = 34, Range = 230, Cooldown = 0.8f, Speed = 115,
			Texture = "res://Assets/spriteSoldado.png", ShotColor = new Color(1f, 0.9f, 0.2f), ShotWidth = 2f, Hotkey = Key.F1
		},
		[UnitType.Escopetero] = new UnitStats
		{
			Name = "Escopetero", Role = "Daño en área a corta distancia", Cost = 30, MaxHealth = 130, Damage = 45, Range = 140, Cooldown = 1.2f, Speed = 105, SplashRadius = 60,
			Texture = "res://Assets/Sprites/spriteEscopetero.png", ShotColor = new Color(1f, 0.6f, 0.2f), ShotWidth = 5f, Hotkey = Key.F2
		},
		[UnitType.Subfusilero] = new UnitStats
		{
			Name = "Subfusilero", Role = "Alta cadencia contra enemigos rápidos", Cost = 30, MaxHealth = 90, Damage = 11, Range = 180, Cooldown = 0.18f, Speed = 125,
			Texture = "res://Assets/Sprites/spriteSubfusilero.png", ShotColor = new Color(1f, 1f, 0.6f), ShotWidth = 1.5f, Hotkey = Key.F3
		},
		[UnitType.Francotirador] = new UnitStats
		{
			Name = "Francotirador", Role = "Gran alcance y daño único", Cost = 45, MaxHealth = 70, Damage = 130, Range = 430, Cooldown = 2.2f, Speed = 95,
			Texture = "res://Assets/Sprites/spriteFrancotirador.png", ShotColor = new Color(0.7f, 0.95f, 1f), ShotWidth = 3f, Hotkey = Key.F4
		},
		[UnitType.Ingeniero] = new UnitStats
		{
			Name = "Ingeniero", Role = "Construye y repara 3 veces más rápido", Cost = 25, MaxHealth = 90, Damage = 10, Range = 150, Cooldown = 0.9f, Speed = 115, BuildPower = 3f,
			Texture = "res://Assets/Sprites/spriteIngeniero.png", ShotColor = new Color(1f, 0.85f, 0.4f), ShotWidth = 1.5f, Hotkey = Key.F5
		}
	};

	public static readonly Dictionary<EnemyType, EnemyStats> Enemies = new()
	{
		[EnemyType.Normal] = new EnemyStats
		{
			Name = "Zombie", MaxHealth = 150, Damage = 10, Range = 8, Cooldown = 1.5f, Speed = 55, AggroRadius = 260, Scrap = 10, Scale = 1f, CollisionRadius = 20,
			Texture = "res://Assets/spriteZombie.png"
		},
		[EnemyType.Corredor] = new EnemyStats
		{
			Name = "Corredor", MaxHealth = 80, Damage = 7, Range = 8, Cooldown = 0.8f, Speed = 120, AggroRadius = 320, Scrap = 8, Scale = 0.9f, CollisionRadius = 16,
			Texture = "res://Assets/Sprites/spriteCorredor.png"
		},
		[EnemyType.Tanque] = new EnemyStats
		{
			Name = "Tanque", MaxHealth = 650, Damage = 32, Range = 10, Cooldown = 2f, Speed = 32, AggroRadius = 220, Scrap = 35, Scale = 1.5f, CollisionRadius = 30,
			Texture = "res://Assets/Sprites/spriteTanque.png"
		},
		[EnemyType.Boomer] = new EnemyStats
		{
			Name = "Boomer", MaxHealth = 120, Damage = 60, Range = 12, Cooldown = 1f, Speed = 50, AggroRadius = 260, SplashRadius = 95, Scrap = 15, Scale = 1.1f, CollisionRadius = 22,
			Texture = "res://Assets/Sprites/spriteBoomer.png"
		},
		[EnemyType.Jefe] = new EnemyStats
		{
			Name = "Abominación", MaxHealth = 3200, Damage = 45, Range = 20, Cooldown = 2.4f, Speed = 38, AggroRadius = 380, SplashRadius = 120, Scrap = 200, Scale = 1.6f, CollisionRadius = 46,
			Texture = "res://Assets/Sprites/spriteJefe.png"
		},
		[EnemyType.Saqueador] = new EnemyStats
		{
			Name = "Saqueador", MaxHealth = 170, Damage = 12, Range = 210, Cooldown = 1.1f, Speed = 85, AggroRadius = 260, Scrap = 25, Scale = 1f, CollisionRadius = 14,
			Texture = "res://Assets/Sprites/spriteSaqueador.png"
		}
	};

	public static readonly Dictionary<StructureType, StructureStats> Structures = new()
	{
		[StructureType.Muro] = new StructureStats
		{
			Name = "Muro", Cost = 10, MaxHealth = 250, BuildWork = 100, Size = new Vector2(32, 32), Texture = "res://Assets/Sprites/spriteMuro.png", Hotkey = Key.B
		},
		[StructureType.Torre] = new StructureStats
		{
			Name = "Torre", Cost = 40, MaxHealth = 500, BuildWork = 220, Size = new Vector2(64, 64), GarrisonSlots = 2, Texture = "res://Assets/Sprites/spriteTorre.png", Hotkey = Key.T
		}
	};

	public static readonly Dictionary<PerkType, PerkInfo> Perks = new()
	{
		[PerkType.Blindaje] = new PerkInfo { Name = "Blindaje", Description = "+20% de vida máxima para todas las unidades." },
		[PerkType.MunicionPerforante] = new PerkInfo { Name = "Munición perforante", Description = "+20% de daño de todas las armas." },
		[PerkType.GatilloFacil] = new PerkInfo { Name = "Gatillo fácil", Description = "Las armas disparan un 12% más rápido." },
		[PerkType.Chatarrero] = new PerkInfo { Name = "Chatarrero", Description = "+25% de chatarra por cada enemigo abatido." },
		[PerkType.BotasDeCombate] = new PerkInfo { Name = "Botas de combate", Description = "+15% de velocidad de movimiento de las unidades." },
		[PerkType.OjoDeHalcon] = new PerkInfo { Name = "Ojo de halcón", Description = "+15% de alcance de todas las armas." },
		[PerkType.MurosReforzados] = new PerkInfo { Name = "Muros reforzados", Description = "+50% de vida para muros y torres." },
		[PerkType.IngenieriaRapida] = new PerkInfo { Name = "Ingeniería rápida", Description = "Construcción y reparación un 35% más rápidas." },
		[PerkType.Medico] = new PerkInfo { Name = "Médico de campaña", Description = "La base cura el doble de rápido." },
		[PerkType.Fortaleza] = new PerkInfo { Name = "Fortaleza", Description = "+30% de vida máxima de la base y la repara por completo." },
		[PerkType.Reclutamiento] = new PerkInfo { Name = "Reclutamiento", Description = "Las unidades cuestan un 15% menos." },
		[PerkType.Adrenalina] = new PerkInfo { Name = "Adrenalina", Description = "Cada baja cura 8 de vida a la unidad que la consiguió." }
	};

	public static float DayHealthMultiplier(int day) => 1f + 0.12f * Mathf.Max(0, day - 1);

	public static float DayDamageMultiplier(int day) => 1f + 0.06f * Mathf.Max(0, day - 1);
}
