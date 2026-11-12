using Godot;
using System.Collections.Generic;
using System.Text.Json;

public class UnitSave
{
	public string Type { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
	public float Health { get; set; }
	public int Squad { get; set; }
	public bool Leader { get; set; }
}

public class StructureSave
{
	public string Type { get; set; }
	public float X { get; set; }
	public float Y { get; set; }
	public float Progress { get; set; }
	public float Health { get; set; }
}

public class SaveData
{
	public int Version { get; set; } = 1;
	public string SavedAt { get; set; }
	public int Day { get; set; }
	public int Scrap { get; set; }
	public float BaseHealth { get; set; }
	public Dictionary<string, int> Perks { get; set; } = new();
	public GameStats Stats { get; set; }
	public List<UnitSave> Units { get; set; } = new();
	public List<StructureSave> Structures { get; set; } = new();
}

public class RecordsData
{
	public int BestDay { get; set; }
	public int MostKills { get; set; }
	public int GamesPlayed { get; set; }
	public int TotalKills { get; set; }
}

public static class SaveManager
{
	public const string SavePath = "user://squadz_partida.json";
	public const string RecordsPath = "user://squadz_records.json";

	private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

	public static bool HasSave() => FileAccess.FileExists(SavePath);

	public static bool Save(Main main)
	{
		var gm = GameManager.Instance;
		var data = new SaveData
		{
			SavedAt = Time.GetDatetimeStringFromSystem(false, true),
			Day = gm.CurrentDay,
			Scrap = gm.CurrentScrap,
			BaseHealth = main.BaseHealth.CurrentHealth,
			Stats = gm.Stats
		};

		foreach (var pair in gm.Perks)
			data.Perks[pair.Key.ToString()] = pair.Value;

		foreach (Soldier soldier in main.GetSoldiers())
		{
			data.Units.Add(new UnitSave
			{
				Type = soldier.Type.ToString(),
				X = soldier.GlobalPosition.X,
				Y = soldier.GlobalPosition.Y,
				Health = soldier.Health.CurrentHealth,
				Squad = soldier.SquadId,
				Leader = soldier.IsLeader
			});
		}

		foreach (Structure structure in main.GetStructures())
		{
			if (structure.IsPreview) continue;
			data.Structures.Add(new StructureSave
			{
				Type = structure.Type.ToString(),
				X = structure.GlobalPosition.X,
				Y = structure.GlobalPosition.Y,
				Progress = structure.Buildable.CurrentProgress,
				Health = structure.Health.CurrentHealth
			});
		}

		return WriteJson(SavePath, JsonSerializer.Serialize(data, Options));
	}

	public static SaveData Load()
	{
		string json = ReadJson(SavePath);
		if (string.IsNullOrEmpty(json)) return null;
		try
		{
			return JsonSerializer.Deserialize<SaveData>(json);
		}
		catch (JsonException e)
		{
			GD.PrintErr($"Partida guardada dañada: {e.Message}");
			return null;
		}
	}

	public static void DeleteSave()
	{
		if (HasSave())
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
	}

	public static RecordsData LoadRecords()
	{
		string json = ReadJson(RecordsPath);
		if (string.IsNullOrEmpty(json)) return new RecordsData();
		try
		{
			return JsonSerializer.Deserialize<RecordsData>(json) ?? new RecordsData();
		}
		catch (JsonException)
		{
			return new RecordsData();
		}
	}

	public static RecordsData UpdateRecords(GameStats stats)
	{
		var records = LoadRecords();
		records.GamesPlayed++;
		records.TotalKills += stats.TotalKills;
		records.BestDay = Mathf.Max(records.BestDay, stats.DaysSurvived);
		records.MostKills = Mathf.Max(records.MostKills, stats.TotalKills);
		WriteJson(RecordsPath, JsonSerializer.Serialize(records, Options));
		return records;
	}

	private static bool WriteJson(string path, string json)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		if (file == null)
		{
			GD.PrintErr($"No se pudo escribir {path}: {FileAccess.GetOpenError()}");
			return false;
		}
		file.StoreString(json);
		return true;
	}

	private static string ReadJson(string path)
	{
		if (!FileAccess.FileExists(path)) return null;
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		return file?.GetAsText();
	}
}
