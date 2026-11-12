using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class SquadManager : Node
{
	public const int MaxSquads = 4;

	public static SquadManager Instance { get; private set; }

	private readonly Dictionary<int, List<Soldier>> _squads = new();

	public override void _Ready()
	{
		Instance = this;
	}

	public override void _ExitTree()
	{
		if (Instance == this)
			Instance = null;
	}

	public void AssignSquad(int squadId, IEnumerable<Soldier> soldiers)
	{
		var newMembers = soldiers.Where(IsInstanceValid).ToList();
		if (newMembers.Count == 0) return;

		foreach (Soldier old in GetMembers(squadId))
			old.SetSquad(0, false);

		foreach (Soldier soldier in newMembers)
			RemoveFromCurrentSquad(soldier);

		_squads[squadId] = newMembers;
		for (int i = 0; i < newMembers.Count; i++)
			newMembers[i].SetSquad(squadId, i == 0);

		GameManager.Instance.Radio($"Escuadra {squadId} formada: {newMembers.Count} unidades. Líder: {newMembers[0].Stats.Name}.", RadioKind.Info);
	}

	public void RestoreMember(Soldier soldier, int squadId, bool isLeader)
	{
		if (squadId <= 0) return;
		if (!_squads.TryGetValue(squadId, out var members))
		{
			members = new List<Soldier>();
			_squads[squadId] = members;
		}
		if (isLeader) members.Insert(0, soldier);
		else members.Add(soldier);
		soldier.SetSquad(squadId, isLeader);
	}

	public List<Soldier> GetMembers(int squadId)
	{
		if (!_squads.TryGetValue(squadId, out var members)) return new List<Soldier>();
		members.RemoveAll(s => !IsInstanceValid(s));
		return new List<Soldier>(members);
	}

	public Soldier GetLeader(int squadId)
	{
		foreach (Soldier soldier in GetMembers(squadId))
		{
			if (soldier.IsLeader) return soldier;
		}
		return null;
	}

	public void OnSoldierDied(Soldier soldier)
	{
		int squadId = soldier.SquadId;
		if (squadId == 0 || !_squads.TryGetValue(squadId, out var members)) return;

		members.Remove(soldier);
		members.RemoveAll(s => !IsInstanceValid(s) || s.IsQueuedForDeletion());
		if (!soldier.IsLeader) return;

		if (members.Count == 0)
		{
			GameManager.Instance.Radio($"La Escuadra {squadId} fue aniquilada.", RadioKind.Danger);
			_squads.Remove(squadId);
			return;
		}

		Soldier newLeader = members.OrderByDescending(s => s.Health.CurrentHealth).First();
		members.Remove(newLeader);
		members.Insert(0, newLeader);
		foreach (Soldier member in members)
		{
			member.SetSquad(squadId, member == newLeader);
			member.SetDemoralized(true);
		}

		GameManager.Instance.Radio($"¡Cayó el líder de la Escuadra {squadId}! {newLeader.Stats.Name} asume el mando. Vuelvan a la base para recuperar la moral.", RadioKind.Danger);
	}

	private void RemoveFromCurrentSquad(Soldier soldier)
	{
		if (soldier.SquadId == 0) return;
		if (_squads.TryGetValue(soldier.SquadId, out var members))
		{
			bool wasLeader = soldier.IsLeader;
			members.Remove(soldier);
			if (wasLeader && members.Count > 0)
				members[0].SetSquad(soldier.SquadId, true);
		}
		soldier.SetSquad(0, false);
	}
}
