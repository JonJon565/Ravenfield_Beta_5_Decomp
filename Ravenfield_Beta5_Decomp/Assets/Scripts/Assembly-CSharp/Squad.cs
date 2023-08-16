using System.Collections.Generic;
using UnityEngine;

public class Squad
{
	public enum State
	{
		Stationary = 0,
		Moving = 1,
		DigIn = 2,
		MovingThenDigIn = 3,
		EnterVehicle = 4
	}

	private const float GROUPED_UP_DISTANCE = 7f;

	private static int nextNumber = 1;

	private AiActorController leader;

	public List<AiActorController> members;

	public bool hasAssignedOrder;

	public State state;

	public Vehicle squadVehicle;

	public int number;

	public SpawnPoint targetSpawnPoint;

	private float readyTime;

	private bool groupedUp;

	private bool hasSquadVehicle;

	private int recentTakingFireEvents;

	public Squad(List<AiActorController> members, float timeUntilReady)
	{
		number = nextNumber++;
		state = State.Stationary;
		this.members = members;
		leader = this.members[0];
		foreach (AiActorController member in this.members)
		{
			member.AssignedToSquad(this);
		}
		readyTime = Time.time + timeUntilReady;
	}

	public bool Ready()
	{
		return Time.time > readyTime;
	}

	public void DropMember(AiActorController a)
	{
		members.Remove(a);
		if (squadVehicle != null)
		{
			squadVehicle.DropSeatClaim();
		}
		if (members.Count == 0)
		{
			Disband();
		}
		else if (leader == a)
		{
			leader = members[0];
		}
	}

	public AiActorController Leader()
	{
		return leader;
	}

	public Actor GetTarget()
	{
		foreach (AiActorController member in members)
		{
			if (member.HasTarget())
			{
				return member.target;
			}
		}
		return null;
	}

	public bool HasTargetSpawnPoint()
	{
		return targetSpawnPoint != null;
	}

	public bool ShouldGotoSpawnPoint(SpawnPoint spawnPoint)
	{
		return spawnPoint.owner != Leader().actor.team || !spawnPoint.IsSafe();
	}

	public SpawnPoint ClosestSpawnPoint()
	{
		return ActorManager.ClosestSpawnPoint(Leader().actor.Position());
	}

	public void NewAttackOrder()
	{
		Actor actor = Leader().actor;
		SpawnPoint spawnPoint = ClosestSpawnPoint();
		if (spawnPoint.owner != actor.team)
		{
			AttackSpawnPoint(spawnPoint);
			return;
		}
		List<SpawnPoint> list = new List<SpawnPoint>();
		foreach (SpawnPoint adjacentSpawnPoint in spawnPoint.adjacentSpawnPoints)
		{
			if (adjacentSpawnPoint.owner != actor.team)
			{
				list.Add(adjacentSpawnPoint);
				if (adjacentSpawnPoint.owner >= 0)
				{
					list.Add(adjacentSpawnPoint);
				}
			}
		}
		if (list.Count > 0)
		{
			AttackSpawnPoint(list[Random.Range(0, list.Count)]);
		}
		else
		{
			AttackSpawnPoint(ActorManager.RandomEnemySpawnPoint(actor.team));
		}
	}

	public void ReissueAttackOrder()
	{
		AttackSpawnPoint(targetSpawnPoint);
	}

	public void AttackSpawnPoint(SpawnPoint spawnPoint)
	{
		targetSpawnPoint = spawnPoint;
		if (spawnPoint != null)
		{
			Vector3 vector = Random.insideUnitSphere.ToGround() * spawnPoint.GotoRadius();
			MoveTo(spawnPoint.transform.position + vector);
		}
	}

	public void MoveTo(Vector3 point)
	{
		hasAssignedOrder = true;
		LeaveAnyCover();
		state = State.Moving;
		foreach (AiActorController member in members)
		{
			member.Goto(point + Vector3.Scale(Random.insideUnitSphere, new Vector3(3f, 0f, 3f)));
			if (member.squadLeader)
			{
				member.EmoteMoveOrder(point);
			}
		}
	}

	public void MoveToAndDigIn(Vector3 point)
	{
		hasAssignedOrder = true;
		if (HasVehicle())
		{
			Debug.LogWarning("Squad dig in while in vehicle, ignore.");
			return;
		}
		state = State.DigIn;
		foreach (AiActorController member in members)
		{
			member.FindCoverAtPoint(point);
			member.EmoteHailPlayer();
		}
	}

	public void DigIn()
	{
		hasAssignedOrder = true;
		if (HasVehicle())
		{
			Debug.LogWarning("Squad dig in while in vehicle, ignore.");
		}
		else
		{
			if (state == State.DigIn)
			{
				return;
			}
			state = State.DigIn;
			foreach (AiActorController member in members)
			{
				member.FindCover();
				if (member.squadLeader)
				{
					member.EmoteHalt();
				}
			}
		}
	}

	public void DigInTowards(Vector3 direction)
	{
		hasAssignedOrder = true;
		if (state == State.DigIn)
		{
			return;
		}
		state = State.DigIn;
		foreach (AiActorController member in members)
		{
			member.FindCoverTowards(direction);
			if (member.squadLeader)
			{
				member.EmoteHalt();
			}
		}
	}

	public void SetAlreadyInVehicle(Vehicle vehicle)
	{
		squadVehicle = vehicle;
		squadVehicle.ClaimSeat();
	}

	public void EnterVehicle(Vehicle vehicle)
	{
		hasAssignedOrder = true;
		if (state == State.EnterVehicle)
		{
			return;
		}
		state = State.EnterVehicle;
		hasSquadVehicle = true;
		squadVehicle = vehicle;
		vehicle.ownerTeam = Leader().actor.team;
		for (int i = 0; i < members.Count; i++)
		{
			members[i].GotoAndEnterVehicle(vehicle);
			vehicle.ClaimSeat();
			if (members[i].squadLeader)
			{
				members[i].EmoteMoveOrder(vehicle.transform.position);
			}
		}
	}

	public void ExitVehicle()
	{
		foreach (AiActorController member in members)
		{
			member.LeaveVehicle();
		}
		state = State.Stationary;
		hasSquadVehicle = false;
	}

	public bool IsTakingFire()
	{
		foreach (AiActorController member in members)
		{
			if (member.IsTakingFire())
			{
				return true;
			}
		}
		return false;
	}

	public bool AllSeated()
	{
		foreach (AiActorController member in members)
		{
			if (!member.actor.IsSeated())
			{
				return false;
			}
		}
		return true;
	}

	public bool HasVehicle()
	{
		return squadVehicle != null;
	}

	private void LeaveAnyCover()
	{
		if (state != State.DigIn)
		{
			return;
		}
		foreach (AiActorController member in members)
		{
			member.LeaveCover();
		}
	}

	private void Disband()
	{
	}

	public bool IsGroupedUp()
	{
		return groupedUp;
	}

	public void Update()
	{
		UpdateGroupedUpFlag();
		UpdateVehicleStatus();
	}

	private void UpdateGroupedUpFlag()
	{
		if (members.Count < 2)
		{
			groupedUp = false;
			return;
		}
		Vector3 zero = Vector3.zero;
		foreach (AiActorController member in members)
		{
			zero += member.transform.position;
		}
		zero /= (float)members.Count;
		int num = 0;
		foreach (AiActorController member2 in members)
		{
			if (Vector3.Distance(member2.transform.position, zero) < 7f)
			{
				num++;
			}
		}
		groupedUp = num >= 2;
	}

	private void UpdateVehicleStatus()
	{
		if (state != State.EnterVehicle)
		{
			return;
		}
		if (AllSeated())
		{
			state = State.Stationary;
		}
		else
		{
			if (!squadVehicle.IsFull())
			{
				return;
			}
			List<AiActorController> list = new List<AiActorController>();
			foreach (AiActorController member in members)
			{
				if (!member.actor.IsSeated())
				{
					list.Add(member);
				}
			}
			SplitSquad(list);
			state = State.Stationary;
		}
	}

	public Squad SplitSquad(List<AiActorController> leavingMembers)
	{
		foreach (AiActorController leavingMember in leavingMembers)
		{
			DropMember(leavingMember);
		}
		Squad squad = new Squad(leavingMembers, 0.5f);
		foreach (AiActorController leavingMember2 in leavingMembers)
		{
			leavingMember2.squad = squad;
			if (leavingMember2.actor.IsSeated())
			{
				leavingMember2.squad.SetAlreadyInVehicle(leavingMember2.actor.seat.vehicle);
			}
		}
		return squad;
	}

	public bool MemberNeedsResupply()
	{
		foreach (AiActorController member in members)
		{
			if (member.actor.needsResupply)
			{
				return true;
			}
		}
		return false;
	}

	public bool MemberNeedsHealth()
	{
		foreach (AiActorController member in members)
		{
			if (member.actor.health < 80f)
			{
				return true;
			}
		}
		return false;
	}

	public void MakeLeader(AiActorController member)
	{
		leader = member;
	}
}
