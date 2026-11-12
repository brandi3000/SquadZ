using System.Collections.Generic;

public class GameStats
{
	public int DaysSurvived { get; set; }
	public Dictionary<string, int> Kills { get; set; } = new();
	public int BossesKilled { get; set; }
	public int ScrapEarned { get; set; }
	public int ScrapSpent { get; set; }
	public int ScrapStolen { get; set; }
	public int UnitsRecruited { get; set; }
	public int UnitsLost { get; set; }
	public int StructuresBuilt { get; set; }
	public int StructuresLost { get; set; }
	public float DamageDealt { get; set; }
	public float TimePlayed { get; set; }

	public int TotalKills
	{
		get
		{
			int total = 0;
			foreach (int value in Kills.Values)
				total += value;
			return total;
		}
	}

	public void AddKill(EnemyType type)
	{
		string key = type.ToString();
		Kills[key] = Kills.TryGetValue(key, out int current) ? current + 1 : 1;
		if (type == EnemyType.Jefe)
			BossesKilled++;
	}

	public int GetKills(EnemyType type) => Kills.TryGetValue(type.ToString(), out int value) ? value : 0;
}
