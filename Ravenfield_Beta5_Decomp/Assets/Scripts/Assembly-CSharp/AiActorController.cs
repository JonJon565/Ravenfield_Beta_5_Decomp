using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class AiActorController : ActorController
{
	public struct AiParameters
	{
		public float LEAD_SWAY_MAGNITUDE;

		public float LEAD_NOISE_MAGNITUDE;

		public float SWAY_MAGNITUDE;

		public float ACQUIRE_TARGET_OFFSET_PER_METER;

		public float ACQUIRE_TARGET_DEPTH_EXTRA_OFFSET_PER_METER;

		public float ACQUIRE_TARGET_DURATION_BASE;

		public float ACQUIRE_TARGET_DURATION_PER_METER;

		public float AIM_BASE_SWAY;

		public float AIM_MAX_SWAY;

		public float VISIBILITY_MULTIPLIER;

		public float AI_FIRE_RECTANGLE_BOUND;

		public float TAKING_FIRE_REACTION_TIME;
	}

	private const float AI_TICK_PERIOD = 0.2f;

	private const float AI_KEEP_TARGET_TIME = 0.5f;

	private const float AI_ORDER_PERIOD = 0.5f;

	private const float AI_VEHICLE_PERIOD = 0.5f;

	private const float MAX_VEHICLE_DISTANCE = 150f;

	private const float SPRINT_DURATION_MIN = 3f;

	private const float SPRINT_DURATION_MAX = 6f;

	private const float SPRINT_COOLDOWN_MIN = 5f;

	private const float SPRINT_COOLDOWN_MAX = 11f;

	private const float NORMAL_WALK_SPEED = 3.2f;

	private const float SPRINT_SPEED = 5.5f;

	private const float HAS_TARGET_WALK_SPEED = 2f;

	private const float VEHICLE_STUCK_DISTANCE = 0.4f;

	private const float VEHICLE_STUCK_TIME = 1.5f;

	private const float VEHICLE_STUCK_RECOVER_TIME = 1f;

	private const float MAX_RECENT_ANTI_STUCK_EVENTS = 2f;

	private const float ANTI_STUCK_EVENT_LIFETIME = 30f;

	private const int CAR_UNEVEN_SURFACE_PENALTY = 100000;

	private const float CAR_TARGET_MAX_SPEED = 15f;

	private const float CAR_REVERSE_SPEED = 7f;

	private const int GRAPH_MASK_ON_FOOT = 1;

	private const int GRAPH_MASK_BOAT = 2;

	private const int GRAPH_MASK_CAR = 4;

	private const float HELICOPTER_TARGET_FLIGHT_HEIGHT_MIN = 30f;

	private const float HELICOPTER_TARGET_FLIGHT_HEIGHT_MAX = 60f;

	private const float HELICOPTER_HEIGHT_EXTRAPOLATION_TIME = 3f;

	private const float HELICOPTER_MAX_PITCH = 25f;

	private const float HELICOPTER_MAX_ROLL = 25f;

	private const float HELICOPTER_ATTACK_RANGE = 200f;

	private const float TANK_PROJECTED_DRIVING_MIN_DISTANCE = 3f;

	private const float TANK_PROJECTED_DRIVING_SPEED_GAIN = 1f;

	private const float CAR_PROJECTED_DRIVING_MIN_DISTANCE = 4f;

	private const float CAR_PROJECTED_DRIVING_SPEED_GAIN = 0.5f;

	private const float CAR_PREDICTION_PROJECTED_DRIVING_MIN_DISTANCE = 4f;

	private const float CAR_PREDICTION_PROJECTED_DRIVING_SPEED_GAIN = 3f;

	private const float CAR_TURN_MULTIPLIER = 5f;

	private const float TRANSPORT_EXIT_AND_WALK_MAX_DISTANCE = 40f;

	private const float AI_WEAPON_FAST_TICK_PERIOD = 0.05f;

	private const float AI_WEAPON_SLOW_TICK_PERIOD = 0.5f;

	private const float AI_WEAPON_SLOW_TICK_DISTANCE = 40f;

	public const float TAKING_FIRE_MAX_DISTANCE = 5f;

	private const float FOOT_BLOCK_SPHERECAST_RADIUS = 0.5f;

	private const float FOOT_CHECK_BLOCKER_AHEAD_RANGE = 2f;

	private const int FOOT_BLOCK_MASK = 4096;

	private const float VEHICLE_BLOCK_AHEAD_TIME = 1f;

	private const float VEHICLE_BLOCK_AVOID_MULTIPLIER = 0.3f;

	private const int VEHICLE_BLOCK_MASK = 256;

	private const float CAR_TURNING_SPEED_MULTIPLIER = 0.5f;

	private const float CAR_DRIVING_FORWARD_TURN_MULTIPLIER = 0.5f;

	private const float AI_MIN_SCAN_TIME = 0.8f;

	private const float AI_MAX_SCAN_TIME = 3f;

	private const float LOOK_FORWARD_CHANCE = 0.8f;

	private const float AI_FACE_HIGHLIGHTED_DISTANCE = 30f;

	private const float AI_FACE_HIGHLIGHTED_CHANCE = 0.2f;

	private const float AI_CHASE_EXTRAPOLATION_TIME = 2f;

	private const float AI_INVESTIGATE_MIN_TIME = 3f;

	private const float AI_UPDATE_CLOSE_ACTORS_TIME = 1f;

	private const float CLOSE_ACTORS_RANGE = 10f;

	private const float LOCAL_AVOIDANCE_MIN_DISTANCE = 1.5f;

	private const float LOCAL_AVOIDANCE_SPEED = 2f;

	private const int FRIENDLY_LAYER_MASK = 5376;

	private const int GROUND_LAYER_MASK = 1;

	private const float FATIGUE_GAIN = 0.04f;

	private const float FATIGUE_DRAIN = 0.4f;

	private const float AIM_SLERP_SPEED = 6f;

	private const float AIM_CONSTANT_SPEED = 5f;

	private const float MIN_GOTO_DELTA = 2f;

	private const int CAN_SEE_RAYCAST_SAMPLES = 3;

	private const float FOV_MIN_DOT = 0.1f;

	private const float WAYPOINT_COMPLETE_DISTANCE = 0.2f;

	private const float WAYPOINT_COMPLETE_DISTANCE_LQ = 1f;

	private const float WAYPOINT_COMPLETE_DISTANCE_VEHICLE = 2.5f;

	private const float WAYPOINT_COMPLETE_DISTANCE_VEHICLE_AQUATIC = 4f;

	private const float LEAN_SPEED = 2f;

	private const float EYE_HEIGHT = 0.2f;

	private const float MAX_ENTER_SEAT_DISTANCE = 4f;

	private const float SELECT_NON_FRONTLINE_SPAWN_CHANCE = 0.3f;

	private const float PLAYER_APPROACHING_DOT = 0.7f;

	private const float PLAYER_APPROACHING_LOOK_DOT = 0.9f;

	private const float PLAYER_APPROACHING_MAX_RANGE = 30f;

	private const float GET_IN_PLAYER_VEHICLE_RANGE = 8f;

	private static string[] primaryWeaponNames = new string[6] { "RK-44", "RK-44", "76 EAGLE", "SL-DEFENDER", "SIGNAL DMR", "RECON LRR" };

	private static string[] secondaryWeaponNames = new string[1] { "S-IND7" };

	private static string[] gearNames = new string[10] { "BEU AW1", "BEU AW1", "FRAG", "FRAG", "FRAG", "SPEARHEAD", "AMMO BAG", "AMMO BAG", "MEDIPACK", "MEDIPACK" };

	private static AiParameters PARAMETERS_EASY;

	private static AiParameters PARAMETERS_NORMAL;

	public Transform eyeTransform;

	public Transform weaponParent;

	private Quaternion facingDirection = Quaternion.identity;

	private Quaternion targetFacingDirection = Quaternion.identity;

	[NonSerialized]
	public Actor target;

	private bool hasPath;

	private bool calculatingPath;

	private Seeker seeker;

	private Path path;

	private int waypoint;

	private Vector3 lastSeenTargetPosition = Vector3.zero;

	private Vector3 lastSeenTargetVelocity = Vector3.zero;

	private bool skipNextScan;

	private RadiusModifier radiusModifier;

	private AlternativePath alternatePathModifier;

	private bool fire;

	private float randomTimeOffset;

	private float fatigue;

	private Vector3 acquireTargetOffset;

	private Action acquireTargetAction = new Action(1f);

	private List<Actor> closeActors;

	private CoverPoint cover;

	private bool inCover;

	private Action stayInCoverAction = new Action(3f);

	private float lean;

	private Action takingFireAction = new Action(3f);

	[NonSerialized]
	public Vector3 takingFireDirection;

	private Vehicle targetVehicle;

	private bool forceAntiStuckReverse;

	private bool waitForPlayer;

	private int recentAntiStuckEvents;

	private bool canTurnCarTowardsWaypoint = true;

	private List<Vehicle> avoidedVehicles = new List<Vehicle>();

	private bool aquatic;

	private bool flying;

	private bool hasFlightTarget;

	private float helicopterTargetFlightHeight;

	private Vector3 flightTargetPosition;

	private Action helicopterAttackAction = new Action(4f);

	private Action helicopterAttackCooldownAction = new Action(8f);

	private Action helicopterTakeoffAction = new Action(2f);

	private Action helicopterNewOrderAction = new Action(10f);

	private Action sprintAction = new Action(1f);

	private Action sprintCooldownAction = new Action(4f);

	private Action ragdollAutokillAction = new Action(60f);

	private Action moveTimeoutAction = new Action(3f);

	private float smoothNoisePhase;

	[NonSerialized]
	public Squad squad;

	[NonSerialized]
	public bool squadLeader;

	private Vector3 lastWaypoint;

	private Vector3 lastGotoPoint;

	private bool blockerAhead;

	private Vector3 blockerPosition;

	public static AiParameters PARAMETERS
	{
		get
		{
			if (OptionsUi.GetOptions().difficulty == 0)
			{
				return PARAMETERS_EASY;
			}
			return PARAMETERS_NORMAL;
		}
	}

	public static void SetupParameters()
	{
		PARAMETERS_EASY = default(AiParameters);
		PARAMETERS_EASY.LEAD_SWAY_MAGNITUDE = 0.3f;
		PARAMETERS_EASY.LEAD_NOISE_MAGNITUDE = 0.1f;
		PARAMETERS_EASY.SWAY_MAGNITUDE = 1.5f;
		PARAMETERS_EASY.ACQUIRE_TARGET_OFFSET_PER_METER = 0.2f;
		PARAMETERS_EASY.ACQUIRE_TARGET_DEPTH_EXTRA_OFFSET_PER_METER = 1f;
		PARAMETERS_EASY.ACQUIRE_TARGET_DURATION_BASE = 2f;
		PARAMETERS_EASY.ACQUIRE_TARGET_DURATION_PER_METER = 0.02f;
		PARAMETERS_EASY.AIM_BASE_SWAY = 0.01f;
		PARAMETERS_EASY.AIM_MAX_SWAY = 0.1f;
		PARAMETERS_EASY.AI_FIRE_RECTANGLE_BOUND = 2.5f;
		PARAMETERS_EASY.VISIBILITY_MULTIPLIER = 1f;
		PARAMETERS_EASY.TAKING_FIRE_REACTION_TIME = 0.5f;
		PARAMETERS_NORMAL = default(AiParameters);
		PARAMETERS_NORMAL.LEAD_SWAY_MAGNITUDE = 0.1f;
		PARAMETERS_NORMAL.LEAD_NOISE_MAGNITUDE = 0.05f;
		PARAMETERS_NORMAL.SWAY_MAGNITUDE = 0.5f;
		PARAMETERS_NORMAL.ACQUIRE_TARGET_OFFSET_PER_METER = 0.1f;
		PARAMETERS_NORMAL.ACQUIRE_TARGET_DEPTH_EXTRA_OFFSET_PER_METER = 0.5f;
		PARAMETERS_EASY.ACQUIRE_TARGET_DURATION_BASE = 1f;
		PARAMETERS_NORMAL.ACQUIRE_TARGET_DURATION_PER_METER = 0.01f;
		PARAMETERS_NORMAL.AIM_BASE_SWAY = 0.002f;
		PARAMETERS_NORMAL.AIM_MAX_SWAY = 0.05f;
		PARAMETERS_NORMAL.AI_FIRE_RECTANGLE_BOUND = 1f;
		PARAMETERS_NORMAL.VISIBILITY_MULTIPLIER = 2f;
		PARAMETERS_NORMAL.TAKING_FIRE_REACTION_TIME = 0.15f;
	}

	private void Awake()
	{
		seeker = GetComponent<Seeker>();
		Seeker obj = seeker;
		obj.pathCallback = (OnPathDelegate)Delegate.Combine(obj.pathCallback, new OnPathDelegate(OnPathComplete));
		randomTimeOffset = UnityEngine.Random.Range(0f, 10f);
		radiusModifier = GetComponent<RadiusModifier>();
		alternatePathModifier = GetComponent<AlternativePath>();
		smoothNoisePhase = UnityEngine.Random.Range(0f, (float)Math.PI * 2f);
		helicopterTargetFlightHeight = UnityEngine.Random.Range(30f, 60f);
	}

	private List<Actor> FindPotentialTargets()
	{
		int team = ((actor.team == 0) ? 1 : 0);
		List<Actor> list = new List<Actor>(ActorManager.AliveActorsOnTeam(team));
		list.RemoveAll((Actor target) => !HasEffectiveWeaponAgainst(target) || (target.IsSeated() && target.seat.vehicle.burning));
		Dictionary<Actor, float> distanceTo = new Dictionary<Actor, float>(list.Count);
		foreach (Actor item in list)
		{
			float num = ((!item.fallenOver) ? 0f : 30f);
			distanceTo.Add(item, Vector3.Distance(item.Position(), actor.Position()) + num);
		}
		list.Sort((Actor x, Actor y) => distanceTo[x].CompareTo(distanceTo[y]));
		return list;
	}


	private void StartAiCoroutines()
	{
		StartCoroutine(AiBlocked());
		StartCoroutine(AiVehicle());
		StartCoroutine(AiOrders());
		StartCoroutine(AiTarget());
		StartCoroutine(AiWeapon());
		StartCoroutine(AiTrack());
		StartCoroutine(AiScan());
		StartCoroutine(AiTrackClosestActors());
	}

	private IEnumerator AiBlocked()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 0.4f));
		Collider[] colliders = new Collider[128];
		while (true)
		{
			if (hasPath)
			{
				Ray ray = new Ray(base.actor.CenterPosition(), GetWaypointDelta());
				blockerAhead = false;
				if (base.actor.IsSeated())
				{
					Vehicle vehicle = base.actor.seat.vehicle;
					if (vehicle.HasBlockSensor())
					{
						int nHits = vehicle.BlockTest(colliders, 1f, 256);
						for (int i = 0; i < nHits; i++)
						{
							Collider collider = colliders[i];
							Hurtable hurtable = collider.GetComponent<Hitbox>().parent;
							blockerAhead = hurtable.team == base.actor.team;
							if (blockerAhead)
							{
								Actor actor = hurtable as Actor;
								if (actor != null)
								{
									blockerPosition = actor.Position();
								}
								break;
							}
						}
					}
				}
			}
			yield return new WaitForSeconds(0.2f);
		}
	}

	private bool PlayerIsApproaching()
	{
		Actor actor = FpsActorController.instance.actor;
		if (!actor.fallenOver && !actor.IsSeated())
		{
			Vector3 vector = actor.Position();
			Vector3 normalized = (base.actor.Position() - vector).normalized;
			return Vector3.Dot(normalized, actor.controller.FacingDirection()) > 0.9f && Vector3.Dot(normalized, actor.Velocity().normalized) > 0.7f && Vector3.Distance(vector, base.actor.Position()) < 30f;
		}
		return false;
	}

	private IEnumerator AiVehicle()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1f));
		Vector3 lastSampledVehiclePosition = Vector3.zero;
		float lastSampleTime = 0f;
		while (true)
		{
			if (actor.IsSeated() && actor.seat.vehicle != null && actor.IsDriver())
			{
				Type vehicleType = actor.seat.vehicle.GetType();
				if (IsSquadLeader() && vehicleType != typeof(Boat) && WaterLevel.InWater(actor.seat.vehicle.transform.position))
				{
					actor.seat.vehicle.stuck = true;
					squad.ExitVehicle();
					if (hasPath)
					{
						squad.MoveTo(lastGotoPoint);
					}
				}
				else if (hasPath)
				{
					forceAntiStuckReverse = false;
					bool inNonAirVehicle = vehicleType == typeof(Car) || vehicleType == typeof(Boat) || vehicleType == typeof(Tank);
					waitForPlayer = inNonAirVehicle && !actor.seat.vehicle.IsFull() && actor.team == FpsActorController.playerTeam && PlayerIsApproaching();
					if (vehicleType == typeof(Car))
					{
						Vector3 waypointDelta = GetWaypointDelta();
						Car car = (Car)actor.seat.vehicle;
						canTurnCarTowardsWaypoint = car.CanTurnTowards(waypointDelta);
					}
					if (!LastWaypoint() && inNonAirVehicle)
					{
						Vector3 upcomingDeltaWaypointFlat = GetUpcomingBetweenWaypointsDelta().ToGround();
						Vector3 deltaWaypointFlat = GetWaypointDelta().ToGround();
						if (upcomingDeltaWaypointFlat == Vector3.zero)
						{
							waypoint++;
						}
					}
					Vector3 newPosition = actor.seat.vehicle.transform.position;
					if (Vector3.Distance(newPosition, lastSampledVehiclePosition) > 0.4f)
					{
						lastSampledVehiclePosition = newPosition;
						lastSampleTime = Time.time;
					}
					else if (Time.time > lastSampleTime + 1.5f)
					{
						if (vehicleType == typeof(Boat))
						{
							actor.seat.vehicle.stuck = true;
							squad.ExitVehicle();
							squad.MoveTo(lastGotoPoint);
						}
						else if (vehicleType == typeof(Car) || vehicleType == typeof(Tank))
						{
							PushAntiStuckEvent();
							forceAntiStuckReverse = true;
							yield return new WaitForSeconds(1f);
							forceAntiStuckReverse = false;
							yield return new WaitForSeconds(1f);
							if (!actor.IsSeated())
							{
								continue;
							}
							RecalculatePath();
							lastSampleTime = Time.time;
						}
					}
				}
				if (vehicleType == typeof(Helicopter))
				{
					if (!squad.AllSeated())
					{
						helicopterTakeoffAction.Start();
					}
					if (HasTarget() && Vector3.Dot(actor.seat.vehicle.transform.forward, target.CenterPosition() - actor.seat.vehicle.transform.position) > 0f && helicopterAttackAction.TrueDone() && helicopterAttackCooldownAction.TrueDone() && Vector3.Distance(base.transform.position, target.transform.position) < 200f)
					{
						helicopterAttackAction.Start();
						helicopterAttackCooldownAction.Start();
					}
				}
			}
			if (actor.CanEnterSeat() && (!actor.fallenOver || actor.inWater) && HasTargetVehicle() && Vector3.Distance(actor.CenterPosition(), targetVehicle.transform.position) < 4f)
			{
				CancelPath();
				if (!targetVehicle.IsFull())
				{
					actor.EnterSeat(targetVehicle.GetEmptySeat());
				}
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	private void PushAntiStuckEvent()
	{
		if ((float)recentAntiStuckEvents > 2f)
		{
			squad.squadVehicle.stuck = true;
			squad.ExitVehicle();
			squad.MoveTo(lastGotoPoint);
			recentAntiStuckEvents = 0;
			CancelInvoke("PopAntiStuckEvent");
		}
		recentAntiStuckEvents++;
		Invoke("PopAntiStuckEvent", 30f);
	}

	private void PopAntiStuckEvent()
	{
		recentAntiStuckEvents--;
	}

	private bool ShouldHavePath()
	{
		return (!actor.IsSeated() || actor.IsDriver()) && !inCover && squad.hasAssignedOrder;
	}

	private void CreateRougeSquad()
	{
		List<AiActorController> list = new List<AiActorController>(1);
		list.Add(this);
		squad.SplitSquad(list);
		moveTimeoutAction.Start();
	}

	private IEnumerator AiOrders()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1f));
		while (true)
		{
			if (!hasPath && ShouldHavePath() && moveTimeoutAction.TrueDone())
			{
				CreateRougeSquad();
			}
			if (IsSquadLeader())
			{
				squad.Update();
				if (actor.IsSeated() && flying && helicopterNewOrderAction.Done())
				{
					squad.NewAttackOrder();
					helicopterNewOrderAction.Start();
				}
				if (actor.IsSeated() && actor.seat.vehicle.exitWhenTakingFire && squad.IsTakingFire() && Vector3.Distance(actor.Position(), lastGotoPoint) < 40f)
				{
					if (squad.squadVehicle != null)
					{
						squad.squadVehicle.MarkTakingFire();
					}
					squad.ExitVehicle();
				}
			}
			if (IsSquadLeader() && squad.Ready())
			{
				if (squad.state == Squad.State.EnterVehicle && squad.squadVehicle.dead)
				{
					squad.NewAttackOrder();
				}
				if (!squad.HasVehicle() && squad.state == Squad.State.Moving)
				{
					Actor playerActor = FpsActorController.instance.actor;
					if (playerActor.IsSeated() && !playerActor.seat.vehicle.IsFull() && playerActor.seat.vehicle.HasUnclaimedSeats() && Vector3.Distance(playerActor.Position(), base.transform.position) < 8f)
					{
						squad.EnterVehicle(playerActor.seat.vehicle);
					}
				}
				if (squad.HasVehicle() && squad.squadVehicle.burning)
				{
					squad.ExitVehicle();
				}
				if (!actor.fallenOver && squad.HasVehicle() && squad.AllSeated() && !squad.squadVehicle.HasDriver())
				{
					actor.SwitchSeat(0);
				}
				if (!squad.HasVehicle() && squad.state != Squad.State.DigIn && squad.IsTakingFire())
				{
					squad.DigInTowards(takingFireDirection);
				}
				else if (!squad.IsTakingFire())
				{
					SpawnPoint closestSpawnPoint = squad.ClosestSpawnPoint();
					if ((!squad.HasTargetSpawnPoint() || squad.targetSpawnPoint != closestSpawnPoint) && squad.ShouldGotoSpawnPoint(closestSpawnPoint))
					{
						squad.AttackSpawnPoint(closestSpawnPoint);
					}
					else if (squad.HasTargetSpawnPoint() && squad.targetSpawnPoint == closestSpawnPoint && !squad.ShouldGotoSpawnPoint(closestSpawnPoint))
					{
						squad.NewAttackOrder();
					}
					else if (!hasPath && !hasFlightTarget && squad.state != Squad.State.EnterVehicle)
					{
						bool enteringVehicle = false;
						if (!squad.HasVehicle())
						{
							List<Vehicle> nearbyVehicles = NearbyNonFullVehicles();
							foreach (Vehicle vehicle in nearbyVehicles)
							{
								int emptySeats = vehicle.EmptySeats();
								if (vehicle.claimedByPlayer)
								{
									if (actor.team != FpsActorController.playerTeam)
									{
										break;
									}
									emptySeats++;
								}
								if (emptySeats >= squad.members.Count)
								{
									squad.EnterVehicle(vehicle);
									enteringVehicle = true;
									break;
								}
							}
						}
						if (!enteringVehicle && !actor.IsPassenger())
						{
							if (!squad.HasTargetSpawnPoint() || !squad.ShouldGotoSpawnPoint(squad.targetSpawnPoint))
							{
								squad.NewAttackOrder();
							}
							else if (squad.HasTargetSpawnPoint() && squad.ShouldGotoSpawnPoint(squad.targetSpawnPoint))
							{
								squad.ReissueAttackOrder();
							}
						}
					}
				}
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	private IEnumerator AiTarget()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.2f));
		Action investigateAction = new Action(3f);
		while (true)
		{
			List<Actor> potentialTargets = FindPotentialTargets();
			Actor closestHighlighted = null;
			foreach (Actor a in potentialTargets)
			{
				if (!a.dead && HasEffectiveWeaponAgainst(a) && CanSeeActor(a, true))
				{
					SetTarget(a);
					break;
				}
				if (!a.dead && a.IsHighlighted() && Vector3.Distance(a.Position(), actor.Position()) < 30f && UnityEngine.Random.Range(0f, 1f) < 0.2f)
				{
					LookAt(a.Position());
					skipNextScan = true;
					if (closestHighlighted == null)
					{
						closestHighlighted = a;
					}
				}
				yield return new WaitForSeconds(0.2f);
			}
			if (!HasTarget() && !actor.fallenOver)
			{
				Actor squadTarget = squad.GetTarget();
				if (squadTarget != null && HasEffectiveWeaponAgainst(squadTarget))
				{
					SetTarget(squadTarget);
				}
				else if (IsSquadLeader() && closestHighlighted != null && !closestHighlighted.IsSeated() && !HasTargetVehicle() && !actor.IsSeated() && investigateAction.TrueDone() && !HasCover() && stayInCoverAction.TrueDone())
				{
					squad.MoveTo(closestHighlighted.Position());
					investigateAction.Start();
				}
				if (!HasTarget() && hasPath && sprintCooldownAction.TrueDone())
				{
					StartSprint();
				}
			}
			else if (inCover)
			{
				stayInCoverAction.Start();
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	private void StartSprint()
	{
		sprintAction.StartLifetime(UnityEngine.Random.Range(3f, 6f));
		sprintCooldownAction.StartLifetime(UnityEngine.Random.Range(5f, 11f));
	}

	private void StopSprint()
	{
		sprintAction.Stop();
	}

	private void SetTarget(Actor target)
	{
		if (target != this.target)
		{
			this.target = target;
			SwitchToEffectiveWeapon(target);
			Vector3 vector = target.Position() - actor.Position();
			float magnitude = vector.magnitude;
			acquireTargetOffset = UnityEngine.Random.insideUnitSphere.normalized * PARAMETERS.ACQUIRE_TARGET_OFFSET_PER_METER * magnitude + UnityEngine.Random.Range(-1f, 1f) * vector * PARAMETERS.ACQUIRE_TARGET_DEPTH_EXTRA_OFFSET_PER_METER;
			acquireTargetAction.StartLifetime(PARAMETERS.ACQUIRE_TARGET_DURATION_BASE + PARAMETERS.ACQUIRE_TARGET_DURATION_PER_METER * magnitude);
			StopSprint();
		}
	}

	private void DropTarget()
	{
		target = null;
		if (!actor.fallenOver)
		{
			SwitchToPrimaryWeapon();
		}
	}

	private IEnumerator AiWeapon()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.5f));
		while (true)
		{
			fire = false;
			if (actor.HasUnholsteredWeapon() && !actor.activeWeapon.HasAnyAmmo())
			{
				if (target != null)
				{
					if (HasEffectiveWeaponAgainst(target))
					{
						SwitchToEffectiveWeapon(target);
					}
					else
					{
						DropTarget();
						SwitchToPrimaryWeapon();
					}
				}
				else
				{
					SwitchToPrimaryWeapon();
				}
			}
			if (actor.IsSeated() && actor.seat.vehicle.GetType() == typeof(Car) && actor.IsDriver())
			{
				fire = blockerAhead;
				yield return new WaitForSeconds(0.2f);
				continue;
			}
			if (!HasTarget() && actor.hasAmmoBox && actor.weapons[actor.ammoBoxSlot].AmmoFull() && (squad.MemberNeedsResupply() || (!ActorManager.instance.player.dead && actor.team == ActorManager.instance.player.team && ActorManager.instance.player.needsResupply && Vector3.Distance(ActorManager.instance.player.transform.position, base.transform.position) < 10f)))
			{
				if (actor.activeWeapon == actor.weapons[actor.ammoBoxSlot])
				{
					if (!ActorManager.instance.player.dead && actor.team == ActorManager.instance.player.team && ActorManager.instance.player.needsResupply && Vector3.Distance(ActorManager.instance.player.transform.position, base.transform.position) < 10f)
					{
						LookAt(ActorManager.instance.player.transform.position);
					}
					fire = !IsMovingToCover() && UnityEngine.Random.Range(0, 2) == 0;
				}
				else
				{
					actor.SwitchWeapon(actor.ammoBoxSlot);
				}
				yield return new WaitForSeconds(0.2f);
				continue;
			}
			if (!HasTarget() && actor.hasMedipack && actor.weapons[actor.medipackSlot].AmmoFull() && (squad.MemberNeedsHealth() || (!ActorManager.instance.player.dead && actor.team == ActorManager.instance.player.team && ActorManager.instance.player.health < 80f && Vector3.Distance(ActorManager.instance.player.transform.position, base.transform.position) < 10f)))
			{
				if (actor.activeWeapon == actor.weapons[actor.medipackSlot])
				{
					if (!ActorManager.instance.player.dead && actor.team == ActorManager.instance.player.team && ActorManager.instance.player.health < 80f && Vector3.Distance(ActorManager.instance.player.transform.position, base.transform.position) < 10f)
					{
						LookAt(ActorManager.instance.player.transform.position);
					}
					fire = !IsMovingToCover() && UnityEngine.Random.Range(0, 2) == 0;
				}
				else
				{
					actor.SwitchWeapon(actor.medipackSlot);
				}
				yield return new WaitForSeconds(0.2f);
				continue;
			}
			if (!HasTarget() || !actor.HasUnholsteredWeapon())
			{
				fire = false;
				yield return new WaitForSeconds(0.2f);
				continue;
			}
			if (HasTarget() && actor.activeWeapon.IsEmpty())
			{
				SwitchToEffectiveWeapon(target);
				yield return new WaitForSeconds(0.2f);
				continue;
			}
			if (!actor.activeWeapon.configuration.auto && fire)
			{
				fire = false;
				yield return new WaitForSeconds(0.05f);
				continue;
			}
			Vector3 muzzlePosition = actor.WeaponMuzzlePosition();
			Vector3 deltaTarget = GetTargetAcquiredPosition() - muzzlePosition + WeaponLead();
			float distance = deltaTarget.magnitude;
			Vector3 forward = ((!actor.IsSeated() || !actor.seat.HasMountedWeapon()) ? FacingDirection() : actor.activeWeapon.configuration.muzzle.forward);
			Vector3 orth1 = Vector3.Cross(forward, Vector3.up).normalized;
			Vector3 orth2 = Vector3.Cross(forward, orth1);
			float a = Vector3.Dot(deltaTarget, orth1);
			float b = Vector3.Dot(deltaTarget, orth2);
			float allowedAimSpread = actor.activeWeapon.configuration.aiAllowedAimSpread;
			bool insideAimCube = Vector3.Dot(deltaTarget, forward) > 0f && Mathf.Abs(a) < PARAMETERS.AI_FIRE_RECTANGLE_BOUND * allowedAimSpread && Mathf.Abs(b) < PARAMETERS.AI_FIRE_RECTANGLE_BOUND * allowedAimSpread;
			if (actor.activeWeapon.CanFire() && insideAimCube && CanSeeActor(target))
			{
				Ray friendlyRay = new Ray(muzzlePosition + 0.3f * forward, forward);
				RaycastHit hitInfo;
				if (distance > 5f && Physics.Raycast(friendlyRay, out hitInfo, distance - 5f, 5376))
				{
					if (hitInfo.collider.gameObject.layer == 8)
					{
						fire = hitInfo.collider.GetComponent<Hitbox>().parent.team != actor.team;
					}
					else
					{
						fire = true;
					}
				}
				else
				{
					fire = true;
				}
			}
			yield return new WaitForSeconds(Mathf.Lerp(0.05f, 0.5f, distance / 40f));
		}
	}

	private IEnumerator AiTrack()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.2f));
		while (true)
		{
			if (HasTarget())
			{
				if (target.dead)
				{
					DropTarget();
				}
				else if (CanSeeActor(target))
				{
					lastSeenTargetPosition = target.Position();
					lastSeenTargetVelocity = target.Velocity();
				}
				else
				{
					DropTarget();
				}
			}
			yield return new WaitForSeconds(0.2f);
		}
	}

	private IEnumerator AiScan()
	{
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.2f));
		while (true)
		{
			yield return new WaitForSeconds(UnityEngine.Random.Range(0.8f, 3f));
			if (!skipNextScan && !HasTarget())
			{
				Vector3 facingDirection;
				if (IsTakingFire())
				{
					facingDirection = takingFireDirection;
				}
				if (InCover())
				{
					facingDirection = cover.transform.forward + UnityEngine.Random.insideUnitSphere * 0.1f;
				}
				else
				{
					bool lookForward = IsSprinting() || UnityEngine.Random.Range(0f, 1f) < 0.8f;
					facingDirection = FacingDirection() * 0.5f + UnityEngine.Random.insideUnitSphere;
					if (IsSprinting())
					{
						facingDirection = Vector3.zero;
					}
					if (hasPath && lookForward)
					{
						facingDirection += Velocity().normalized * 1.5f;
					}
					if (!lookForward)
					{
						facingDirection += 0.4f * SquadFacingBias();
					}
					else
					{
						facingDirection += 0.1f * SquadFacingBias();
					}
					facingDirection.Normalize();
					if (IsSprinting())
					{
						facingDirection.y = 0f;
					}
					else
					{
						facingDirection.y *= UnityEngine.Random.Range(0.1f, 1f);
						if (facingDirection.y < 0f)
						{
							facingDirection.y *= 0.2f;
						}
					}
				}
				if (facingDirection != Vector3.zero)
				{
					targetFacingDirection = Quaternion.LookRotation(facingDirection, Vector3.up);
				}
			}
			skipNextScan = false;
		}
	}

	private IEnumerator AiTrackClosestActors()
	{
		closeActors = new List<Actor>();
		yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 1f));
		while (true)
		{
			closeActors = ActorManager.AliveActorsInRange(base.transform.position, 10f);
			yield return new WaitForSeconds(1f);
		}
	}

	private Vector3 SquadFacingBias()
	{
		Vector3 zero = Vector3.zero;
		int num = 0;
		foreach (AiActorController member in squad.members)
		{
			if (member != this)
			{
				zero -= member.transform.position - base.transform.position;
				num++;
			}
		}
		if (num == 0)
		{
			return Vector3.zero;
		}
		return zero / num;
	}

	private bool HasEffectiveWeaponAgainst(Actor targetActor)
	{
		float range = Vector3.Distance(targetActor.Position(), actor.Position());
		Actor.TargetType targetType = targetActor.GetTargetType();
		Weapon[] weapons = actor.weapons;
		foreach (Weapon weapon in weapons)
		{
			if (weapon != null && weapon.HasAnyAmmo() && weapon.EffectivenessAgainst(targetType) != 0 && weapon.EffectiveAtRange(range))
			{
				return true;
			}
		}
		return false;
	}

	private void LookAt(Vector3 position)
	{
		LookDirection(position - actor.Position());
	}

	private void LookDirection(Vector3 direction)
	{
		targetFacingDirection = Quaternion.LookRotation(direction);
	}

	private void OnPathComplete(Path p)
	{
		if (!p.error)
		{
			calculatingPath = false;
			hasPath = true;
			path = p;
			waypoint = 0;
			avoidedVehicles.Clear();
			if (!inCover && HasCover())
			{
				path.vectorPath.Add(cover.transform.position);
			}
		}
		else
		{
			Debug.LogError(p.errorLog);
		}
	}

	private void RecalculatePath()
	{
		if (hasPath)
		{
			Vector3 targetPoint = lastGotoPoint;
			CancelPath();
			Goto(targetPoint);
		}
	}

	public void Goto(Vector3 targetPoint)
	{
		if (flying && actor.IsDriver())
		{
			flightTargetPosition = targetPoint;
			hasFlightTarget = true;
		}
		else if (!calculatingPath && (!actor.IsSeated() || actor.IsDriver()) && (!hasPath || Vector3.Distance(path.vectorPath[path.vectorPath.Count - 1], targetPoint) > 2f))
		{
			calculatingPath = true;
			int graphMask = 1;
			if (actor.IsDriver())
			{
				graphMask = ((!aquatic) ? 4 : 2);
			}
			lastGotoPoint = targetPoint;
			seeker.StartPath(actor.Position(), targetPoint, null, graphMask);
			lastWaypoint = base.transform.position;
		}
	}

	public void CancelPath()
	{
		calculatingPath = false;
		path = null;
		hasPath = false;
		moveTimeoutAction.Start();
	}

	private Vector3 GetTargetAcquireOffset()
	{
		float f = acquireTargetAction.Ratio();
		return acquireTargetOffset * (1f - Mathf.Pow(f, 2f));
	}

	private void Update()
	{
		if (actor.dead)
		{
			return;
		}
		if (!actor.fallenOver)
		{
			ragdollAutokillAction.Start();
		}
		else if (ragdollAutokillAction.TrueDone())
		{
			actor.Damage(100f, 0f, true, actor.Position(), Vector3.zero, Vector3.zero);
		}
		if (!InCover() || IsReloading() || CoolingDown())
		{
			lean = Mathf.MoveTowards(lean, 0f, 2f * Time.deltaTime);
		}
		else if (InCover() && cover.type == CoverPoint.Type.LeanLeft)
		{
			lean = Mathf.MoveTowards(lean, -1f, 2f * Time.deltaTime);
		}
		else if (InCover() && cover.type == CoverPoint.Type.LeanRight)
		{
			lean = Mathf.MoveTowards(lean, 1f, 2f * Time.deltaTime);
		}
		else
		{
			lean = Mathf.MoveTowards(lean, 0f, 2f * Time.deltaTime);
		}
		if (HasTarget())
		{
			if (actor.HasUnholsteredWeapon())
			{
				targetFacingDirection = Quaternion.LookRotation(GetTargetAcquiredPosition() - actor.WeaponMuzzlePosition() + WeaponLead(), Vector3.up);
			}
			else
			{
				targetFacingDirection = Quaternion.LookRotation(target.CenterPosition() - actor.CenterPosition(), Vector3.up);
			}
		}
		facingDirection = Quaternion.Slerp(facingDirection, targetFacingDirection, 6f * Time.deltaTime);
		facingDirection = Quaternion.RotateTowards(facingDirection, targetFacingDirection, 5f * Time.deltaTime);
		fatigue = Mathf.Clamp01(fatigue - 0.4f * Time.deltaTime);
	}

	private Vector3 GetTargetAcquiredPosition()
	{
		return target.CenterPosition() + GetTargetAcquireOffset();
	}

	private bool IsReloading()
	{
		return actor.HasUnholsteredWeapon() && actor.activeWeapon.reloading;
	}

	private bool CoolingDown()
	{
		return actor.HasUnholsteredWeapon() && actor.activeWeapon.configuration.cooldown > 0.3f && actor.activeWeapon.CoolingDown();
	}

	private Vector3 WeaponLead()
	{
		Vector3 vector = target.Position() - actor.Position();
		float num = vector.magnitude / actor.activeWeapon.projectileSpeed;
		Vector3 normalized = vector.normalized;
		Vector3 vector2 = vector;
		float num2 = num;
		for (int i = 0; i < 1; i++)
		{
			Vector3 vector3 = Physics.gravity * Mathf.Pow(num2, 2f) / 2f;
			num2 = num / Vector3.Dot((vector - vector3).normalized, normalized);
		}
		Vector3 vector4 = SmoothNoise(0.2f);
		Vector3 vector5 = SmoothNoise(0.2333f);
		Vector3 vector6 = target.Velocity();
		Vector3 vector7 = FacingDirection();
		Vector3 vector8 = vector6 - Vector3.Dot(vector6, vector7) * vector7;
		Vector3 vector9 = target.Velocity() + vector4 * vector8.magnitude * 0.3f;
		float num3 = num2 * (1f + PARAMETERS.LEAD_SWAY_MAGNITUDE * (vector4.x + vector4.z) + UnityEngine.Random.Range(0f - PARAMETERS.LEAD_NOISE_MAGNITUDE, PARAMETERS.LEAD_NOISE_MAGNITUDE));
		Vector3 vector10 = vector9 * num3 - Physics.gravity * Mathf.Pow(num3, 2f) / 2f;
		return vector10 + vector5 * PARAMETERS.SWAY_MAGNITUDE;
	}

	private Vector3 SmoothNoise(float frequency)
	{
		float num = frequency * Time.time;
		return new Vector3(Mathf.Sin(num * 7.9f + smoothNoisePhase), Mathf.Sin(num * 8.3f + smoothNoisePhase), Mathf.Sin(num * 8.9f + smoothNoisePhase));
	}

	public override float Lean()
	{
		return lean;
	}

	public override bool Fire()
	{
		return fire;
	}

	public override bool Aiming()
	{
		return false;
	}

	public override bool Reload()
	{
		return actor.HasUnholsteredWeapon() && actor.activeWeapon.IsEmpty();
	}

	public override bool OnGround()
	{
		return true;
	}

	public override bool ProjectToGround()
	{
		return true;
	}

	private Vector3 GetWaypointDeltaBlockable()
	{
		if (blockerAhead)
		{
			return Vector3.zero;
		}
		return GetWaypointDelta();
	}

	private Vector3 GetWaypointDelta()
	{
		Vector3 vector = ((!actor.IsDriver()) ? actor.Position() : actor.seat.vehicle.transform.position);
		Vector3 vector2 = path.vectorPath[waypoint] - vector;
		Vector3 vector3 = vector2;
		vector3.y = 0f;
		float num = (actor.IsSeated() ? ((!aquatic) ? 2.5f : 4f) : ((!actor.IsLowQuality()) ? 0.2f : 1f));
		if (vector3.magnitude < num)
		{
			NextWaypoint();
		}
		return vector2;
	}

	private void NextWaypoint()
	{
		lastWaypoint = path.vectorPath[waypoint];
		if (!LastWaypoint())
		{
			waypoint++;
			NewWaypoint(lastWaypoint, path.vectorPath[waypoint]);
		}
		else
		{
			PathDone();
			hasPath = false;
		}
	}

	private void NewWaypoint(Vector3 origin, Vector3 target)
	{
		if (!HasTarget() && UnityEngine.Random.Range(0f, 1f) < 0.5f)
		{
			LookAt(target);
		}
		float num = ((!(targetVehicle != null)) ? 0f : targetVehicle.pathingRadius);
		Vehicle vehicle = null;
		float num2 = 9999999f;
		foreach (Vehicle vehicle2 in ActorManager.instance.vehicles)
		{
			if (!(vehicle2 == targetVehicle) && !avoidedVehicles.Contains(vehicle2) && vehicle2.ShouldBeAvoided() && vehicle2.CoarseLineOverlap(origin, target, num))
			{
				float num3 = Vector3.Distance(vehicle2.transform.position, actor.Position());
				if (num3 < num2)
				{
					num2 = num3;
					vehicle = vehicle2;
				}
			}
		}
		if (vehicle != null)
		{
			AvoidVehicle(vehicle, origin, target, num);
			avoidedVehicles.Add(vehicle);
		}
	}

	private void AvoidVehicle(Vehicle vehicle, Vector3 origin, Vector3 target, float pathingRadius)
	{
		int num = waypoint;
		bool flag = false;
		for (int i = waypoint; i < path.vectorPath.Count; i++)
		{
			if (!vehicle.IsCoarseOverlapping(path.vectorPath[i], pathingRadius))
			{
				num = i;
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		Vector3 vector = vehicle.transform.forward * (vehicle.avoidanceSize.y + pathingRadius);
		Vector3 vector2 = vehicle.transform.right * (vehicle.avoidanceSize.x + pathingRadius);
		Debug.DrawRay(path.vectorPath[num], Vector3.up, Color.green, 5f);
		float origin2 = (origin - vehicle.transform.position).ToVector2XZ().AtanAngle();
		float num2 = (path.vectorPath[num] - vehicle.transform.position).ToVector2XZ().AtanAngle();
		float num3 = SMath.V2D.RadiansFromTo(origin2, num2);
		bool shortLtNext = num3 < (float)Math.PI;
		Vector3[] array = new Vector3[4];
		float[] cornerFromOriginAngles = new float[4];
		array[0] = vector + vector2;
		array[1] = vector - vector2;
		array[2] = -vector - vector2;
		array[3] = -vector + vector2;
		List<int> list = new List<int>(4);
		List<int> list2 = new List<int>(4);
		for (int j = 0; j < 4; j++)
		{
			Vector2 v = array[j].ToVector2XZ();
			float num4 = SMath.V2D.RadiansFromTo(origin2, v.AtanAngle());
			cornerFromOriginAngles[j] = num4;
			if (shortLtNext ^ (num4 < num3))
			{
				list2.Add(j);
				Debug.DrawRay(vehicle.transform.position, array[j], Color.red, 5f);
			}
			else
			{
				list.Add(j);
				Debug.DrawRay(vehicle.transform.position, array[j], Color.blue, 5f);
			}
		}
		list.Sort(delegate (int x, int y)
		{
			int num6 = cornerFromOriginAngles[x].CompareTo(cornerFromOriginAngles[y]);
			return shortLtNext ? num6 : (-num6);
		});
		List<Vector3> list3 = new List<Vector3>(5);
		foreach (int item2 in list)
		{
			list3.Add(vehicle.transform.position + array[item2]);
		}
		List<Vector3> list4 = new List<Vector3>(list3);
		list4.Insert(0, origin);
		Vector3 vector3 = path.vectorPath[num];
		Vector3 vector4 = vector3 - list4[list4.Count - 1];
		list4.Add(list4[list4.Count - 1] + Vector3.ClampMagnitude(vector4, 5f));
		if (!RayPathClear(list4, 1))
		{
			list2.Sort(delegate (int x, int y)
			{
				int num5 = cornerFromOriginAngles[x].CompareTo(cornerFromOriginAngles[y]);
				return shortLtNext ? (-num5) : num5;
			});
			Vector3 item = vehicle.transform.position + array[list2[list2.Count - 1]];
			list3.Add(item);
			list4.Insert(list4.Count - 1, item);
			if (!RayPathClear(list4, 1))
			{
				list3 = new List<Vector3>(5);
				foreach (int item3 in list2)
				{
					list3.Add(vehicle.transform.position + array[item3]);
				}
				list4 = new List<Vector3>(list3);
				list4.Insert(0, origin);
				vector3 = path.vectorPath[num];
				vector4 = vector3 - list4[list4.Count - 1];
				list4.Add(list4[list4.Count - 1] + Vector3.ClampMagnitude(vector4, 5f));
				if (!RayPathClear(list4, 1))
				{
					return;
				}
			}
		}
		path.vectorPath.RemoveRange(waypoint, num - waypoint);
		path.vectorPath.InsertRange(waypoint, list3);
	}

	private bool RayPathClear(List<Vector3> points, int mask)
	{
		for (int i = 0; i < points.Count - 1; i++)
		{
			if (Physics.Linecast(points[i], points[i + 1], mask) || !Physics.Raycast(points[i], Vector3.down, 3f, mask))
			{
				return false;
			}
		}
		return true;
	}

	private Vector3 GetUpcomingBetweenWaypointsDelta()
	{
		return path.vectorPath[waypoint + 1] - path.vectorPath[waypoint];
	}

	private Vector3 GetNextWaypointDelta()
	{
		Vector3 vector = ((!actor.IsDriver()) ? actor.Position() : actor.seat.vehicle.transform.position);
		return path.vectorPath[waypoint + 1] - vector;
	}

	private bool LastWaypoint()
	{
		return path.vectorPath.Count <= waypoint + 1;
	}

	private void PathDone()
	{
		if (HasCover())
		{
			LookDirection(cover.transform.forward);
			inCover = true;
			stayInCoverAction.Start();
			StopSprint();
		}
		moveTimeoutAction.Start();
	}

	public override Vector3 Velocity()
	{
		if (hasPath)
		{
			float num = 3.2f;
			if (IsSprinting())
			{
				num = 5.5f;
			}
			else if (HasTarget())
			{
				num = 2f;
			}
			fatigue = Mathf.Clamp01(fatigue + num * 0.04f * Time.deltaTime);
			return (GetWaypointDeltaBlockable().ToGround().normalized + LocalAvoidanceVelocity() * 0.4f).normalized * num;
		}
		return Vector3.zero;
	}

	public override Vector3 SwimInput()
	{
		if (hasPath)
		{
			return GetWaypointDeltaBlockable().ToGround().normalized;
		}
		return Vector3.zero;
	}

	private Vector3 LocalAvoidanceVelocity()
	{
		Vector3 zero = Vector3.zero;
		int num = 0;
		foreach (AiActorController member in squad.members)
		{
			if (member == this)
			{
				continue;
			}
			Actor actor = member.actor;
			if (!actor.fallenOver && !actor.IsSeated())
			{
				Vector3 vector = (base.actor.Position() - actor.Position()).ToGround();
				float magnitude = vector.magnitude;
				if (magnitude < 1.5f)
				{
					float num2 = Mathf.Lerp(2f, 0f, magnitude / 1.5f);
					zero += vector.normalized * num2;
					num++;
				}
			}
		}
		if (num > 0)
		{
			return zero / num;
		}
		return Vector3.zero;
	}

	public override Vector2 BoatInput()
	{
		if (!hasPath)
		{
			return Vector2.zero;
		}
		Vehicle vehicle = actor.seat.vehicle;
		float z = vehicle.LocalVelocity().z;
		Vector3 waypointDeltaBlockable = GetWaypointDeltaBlockable();
		waypointDeltaBlockable.y = 0f;
		float magnitude = waypointDeltaBlockable.magnitude;
		Vector3 normalized = waypointDeltaBlockable.normalized;
		Debug.DrawRay(vehicle.transform.position, waypointDeltaBlockable, Color.red);
		Vector2 vector = new Vector2(Vector3.Dot(normalized, actor.transform.right), Vector3.Dot(normalized, actor.transform.forward));
		return Vector2.ClampMagnitude(vector, 1f);
	}

	public override Vector2 CarInput()
	{
		if (!hasPath)
		{
			return Vector2.zero;
		}
		Vehicle vehicle = actor.seat.vehicle;
		if (waitForPlayer)
		{
			return new Vector2(0f, 0f - vehicle.LocalVelocity().z);
		}
		if (vehicle.GetType() == typeof(Tank))
		{
			return GetTankInput();
		}
		return GetCarInput();
	}

	private Vector2 GetTankInput()
	{
		Vehicle vehicle = actor.seat.vehicle;
		float z = vehicle.LocalVelocity().z;
		if (blockerAhead)
		{
			float num = Mathf.Sign(vehicle.transform.worldToLocalMatrix.MultiplyPoint(blockerPosition).x) * 0.3f;
			if (z > 0.1f)
			{
				return new Vector2(0f - num, -1f);
			}
			return new Vector2(num, 1f);
		}
		Vector3 waypointDeltaBlockable = GetWaypointDeltaBlockable();
		if (waypointDeltaBlockable == Vector3.zero)
		{
			return Vector2.zero;
		}
		float magnitude = waypointDeltaBlockable.magnitude;
		Vector3 projectedDrivingTarget = GetProjectedDrivingTarget(3f, 1f, vehicle);
		waypointDeltaBlockable = (projectedDrivingTarget - vehicle.transform.position).ToGround();
		Vector3 vector = base.transform.worldToLocalMatrix.MultiplyVector(waypointDeltaBlockable);
		vector.y = 0f;
		bool flag = Mathf.Abs(vector.z) > Mathf.Abs(vector.x);
		if (forceAntiStuckReverse && flag && magnitude > 2.5f)
		{
			return new Vector2(0f, Mathf.Sign(0f - vector.z) * 0.5f);
		}
		return new Vector2(Mathf.Clamp(vector.x, -1f, 1f), (!flag) ? 0f : Mathf.Sign(vector.z));
	}

	private Vector2 GetCarInput()
	{
		Vehicle vehicle = actor.seat.vehicle;
		float z = vehicle.LocalVelocity().z;
		if (blockerAhead)
		{
			float num = Mathf.Sign(vehicle.transform.worldToLocalMatrix.MultiplyPoint(blockerPosition).x) * 0.3f;
			if (z > 0.1f)
			{
				return new Vector2(0f - num, -1f);
			}
			return new Vector2(num, 1f);
		}
		Vector3 waypointDeltaBlockable = GetWaypointDeltaBlockable();
		waypointDeltaBlockable.y = 0f;
		float magnitude = waypointDeltaBlockable.magnitude;
		Vector3 projectedDrivingTarget = GetProjectedDrivingTarget(4f, 0.5f, vehicle);
		waypointDeltaBlockable = (projectedDrivingTarget - vehicle.transform.position).ToGround();
		Vector3 futureProjectedDrivingTarget = GetFutureProjectedDrivingTarget(4f, 3f, vehicle);
		float value = Vector3.Dot((futureProjectedDrivingTarget - vehicle.transform.position).ToGround().normalized, waypointDeltaBlockable.normalized);
		float magnitude2 = (vehicle.Velocity() * 1f).magnitude;
		float magnitude3 = vehicle.rigidbody.velocity.magnitude;
		float num2 = ((!forceAntiStuckReverse) ? (15f * (0.15f + 0.85f * Mathf.Pow(Mathf.Clamp01(value), 3f))) : 7f);
		float num3 = Mathf.Clamp01(num2 - magnitude3);
		if (magnitude3 > 1.1f * num2)
		{
			num3 = -1f;
		}
		Debug.DrawRay(vehicle.transform.position + Vector3.up, Vector3.up, Color.black);
		Debug.DrawRay(vehicle.transform.position + Vector3.up, Vector3.up * num3, Color.red);
		Vector2 result = new Vector2(Vector3.Dot(waypointDeltaBlockable * 5f, actor.transform.right), Vector3.Dot(waypointDeltaBlockable, actor.transform.forward));
		bool flag = !canTurnCarTowardsWaypoint ^ forceAntiStuckReverse;
		Color color = Color.blue;
		result.y = Mathf.Clamp(Mathf.Sign(result.y) * num3, -1f, 0.8f);
		result.x = Mathf.Clamp(result.x / (1f + Mathf.Abs(z)), -1f, 1f);
		if (flag)
		{
			result.y = Mathf.Abs(result.y);
			color = Color.red;
		}
		if (forceAntiStuckReverse)
		{
			result.y = -0.7f;
		}
		if (z < 0f)
		{
			result.x *= -1f;
		}
		Vector3 rhs = vehicle.transform.forward.ToGround();
		rhs.Normalize();
		float t = Mathf.Abs(Vector3.Dot(waypointDeltaBlockable.normalized, rhs));
		float num4 = Mathf.Lerp(1f, 0.5f, t);
		float num5 = Mathf.Lerp(0.5f, 1f, t);
		result.x *= num4;
		result.y *= num5;
		Debug.DrawRay(actor.seat.vehicle.transform.position, GetWaypointDelta(), color);
		return result;
	}

	private Vector3 GetProjectedDrivingTarget(float minDistance, float speedGain, Vehicle vehicle)
	{
		if (path.vectorPath[waypoint] == lastWaypoint)
		{
			NextWaypoint();
			return lastWaypoint;
		}
		Vector3 vector = path.vectorPath[waypoint] - lastWaypoint;
		float num = Mathf.Max(minDistance, speedGain * vehicle.rigidbody.velocity.magnitude);
		Vector3 vector2 = SMath.LineSegmentVsPointClosest(lastWaypoint, path.vectorPath[waypoint], vehicle.transform.position + vector.normalized * num);
		Debug.DrawLine(vehicle.transform.position, vector2, Color.red);
		Debug.DrawRay(vector2, Vector3.up * 5f, new Color(255f, 0f, 255f));
		if (Vector3.Distance(vector2, path.vectorPath[waypoint]) < 0.2f)
		{
			NextWaypoint();
		}
		return vector2;
	}

	private Vector3 GetFutureProjectedDrivingTarget(float minDistance, float speedGain, Vehicle vehicle)
	{
		Vector3 vector = path.vectorPath[waypoint];
		if (waypoint + 1 < path.vectorPath.Count)
		{
			float magnitude = (vector - vehicle.transform.position).magnitude;
			float num = Mathf.Max(minDistance, speedGain * vehicle.rigidbody.velocity.magnitude);
			Vector3 vector2 = path.vectorPath[waypoint + 1] - path.vectorPath[waypoint];
			Vector3 vector3 = vector + Mathf.Max(0f, num - magnitude) * vector2.normalized;
			Debug.DrawLine(vehicle.transform.position, vector3, Color.red);
			return vector3;
		}
		return vector;
	}

	public override Vector4 HelicopterInput()
	{
		if (!squad.AllSeated() || !helicopterTakeoffAction.TrueDone())
		{
			return new Vector4(0f, -1f + helicopterTakeoffAction.Ratio() * 1.5f, 0f, 0f);
		}
		Rigidbody rigidbody = actor.seat.vehicle.rigidbody;
		Transform transform = actor.seat.vehicle.transform;
		Vector3 position = transform.position;
		Vector3 localEulerAngles = transform.localEulerAngles;
		float y = transform.eulerAngles.y;
		float num = position.y;
		Vector3 vector = position + rigidbody.velocity * 3f;
		RaycastHit hitInfo;
		if (Physics.SphereCast(new Ray(vector + Vector3.up * 10f, Vector3.down), 1f, out hitInfo, 999f, 1))
		{
			num = hitInfo.distance;
		}
		float num2 = 0f;
		Vector3 zero = Vector3.zero;
		Vector3 forward = transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Vector3 rhs = new Vector3(forward.z, 0f, 0f - forward.x);
		bool flag = HasTarget() && !helicopterAttackAction.TrueDone();
		Vector3 vector2 = position + forward;
		if (flag)
		{
			vector2 = target.CenterPosition() + WeaponLead();
			Debug.DrawLine(base.transform.position, vector2, Color.red);
		}
		else if (hasFlightTarget)
		{
			vector2 = flightTargetPosition;
		}
		num2 = Heading(position, vector2);
		zero = vector2 - position;
		float y2 = zero.y;
		zero.y = 0f;
		float magnitude = zero.magnitude;
		float num3 = Mathf.DeltaAngle(y, num2);
		float num4 = helicopterTargetFlightHeight - num;
		float num5 = 25f * Mathf.Clamp(Vector3.Dot(zero * 0.02f, forward), -1f, 1f);
		float current = -25f * Mathf.Clamp(Vector3.Dot(zero * 0.02f, rhs), -1f, 1f);
		if (num4 > 5f)
		{
			num5 = 0f;
			current = 0f;
		}
		float num6 = 1f;
		if (flag)
		{
			num5 = (0f - Mathf.Atan2(y2, magnitude)) * 57.29578f;
			num6 = 2.5f;
		}
		Vector3 vector3 = transform.InverseTransformDirection(rigidbody.angularVelocity);
		float x = 0.01f * num6 * num3 - vector3.y;
		float w = 0.1f * num6 * Mathf.DeltaAngle(localEulerAngles.x, num5) - 2f * vector3.x;
		float z = 0.1f * num6 * Mathf.DeltaAngle(current, localEulerAngles.z) + 2f * vector3.z;
		float y3 = ((!(num4 > 0f)) ? (0.01f * num4) : (1f * num4));
		return new Vector4(x, y3, z, w);
	}

	private float Heading(Vector3 root, Vector3 target)
	{
		Vector3 vector = target - root;
		return (0f - Mathf.Atan2(vector.z, vector.x)) * 57.29578f + 90f;
	}

	public override Vector3 FacingDirection()
	{
		float num = Time.time + randomTimeOffset;
		Vector3 vector = new Vector3(Mathf.Sin(num * 3.1f), Mathf.Cos(num * 5.3f), Mathf.Cos(num * 3.7f));
		return facingDirection * Vector3.forward + vector * (PARAMETERS.AIM_BASE_SWAY + PARAMETERS.AIM_MAX_SWAY * fatigue);
	}

	public override bool UseMuzzleDirection()
	{
		return false;
	}

	public override void ReceivedDamage(float damage, float balanceDamage, Vector3 point, Vector3 direction, Vector3 force)
	{
		if (!HasTarget())
		{
			LookAt(point - direction * 10f);
		}
	}

	public override void DisableInput()
	{
	}

	public override void EnableInput()
	{
	}

	public override void StartSeated(Seat seat)
	{
		if (seat.type == Seat.Type.Driver || seat.type == Seat.Type.Pilot)
		{
			squad.MakeLeader(this);
		}
		flying = seat.vehicle.GetType() == typeof(Helicopter);
		if (seat.vehicle.GetType() == typeof(Tank))
		{
			seeker.tagPenalties[0] = 100000;
			Tank tank = (Tank)seat.vehicle;
			radiusModifier.radius = tank.pathingRadius;
			radiusModifier.enabled = true;
			alternatePathModifier.enabled = true;
		}
		else if (seat.vehicle.GetType() == typeof(Car))
		{
			Car car = (Car)seat.vehicle;
			radiusModifier.enabled = car.pathingRadius > 0f;
			radiusModifier.radius = car.pathingRadius;
			alternatePathModifier.enabled = true;
			seeker.tagPenalties[0] = 100000;
		}
		else if (seat.vehicle.GetType() == typeof(Boat))
		{
			aquatic = true;
			seeker.startEndModifier.exactEndPoint = StartEndModifier.Exactness.Original;
		}
	}

	public override void EndSeated(Vector3 exitPosition, Quaternion flatFacing)
	{
		flying = false;
		aquatic = false;
		radiusModifier.enabled = false;
		alternatePathModifier.enabled = false;
		seeker.tagPenalties[0] = 0;
		seeker.startEndModifier.exactEndPoint = StartEndModifier.Exactness.ClosestOnNode;
	}

	public override void StartRagdoll()
	{
	}

	public override void GettingUp()
	{
	}

	public override void EndRagdoll()
	{
		if (inCover)
		{
			Goto(cover.transform.position);
		}
		else if (hasPath)
		{
			RecalculatePath();
		}
	}

	public override void Die()
	{
		LeaveCover();
		CancelPath();
		squad.DropMember(this);
		squad = null;
		StopAllCoroutines();
		CancelInvoke();
	}

	public bool HasTarget()
	{
		return target != null;
	}

	private bool CanSeeActor(Actor target, bool considerFov = false)
	{
		Vector3 vector = target.Position() - actor.Position();
		float magnitude = vector.magnitude;
		Vector3 normalized = vector.normalized;
		float num = 1f;
		float num2 = Vector3.Dot(normalized, FacingDirection());
		if (HasTarget() && target == this.target)
		{
			num = 1f;
		}
		else if (RenderSettings.fog)
		{
			float f = Mathf.Exp(0f - Mathf.Pow(magnitude * RenderSettings.fogDensity, 2f));
			num = num2 * Mathf.Pow(f, 2f);
			if (target.IsHighlighted())
			{
				num *= 2f;
			}
			else if (target.StandingStill())
			{
				num = Mathf.Pow(num, 2f);
			}
			num *= PARAMETERS.VISIBILITY_MULTIPLIER;
		}
		if (target.IsSeated())
		{
			num *= target.seat.vehicle.spotChanceMultiplier;
		}
		if (UnityEngine.Random.Range(0f, 1f) < num && (!considerFov || target.IsHighlighted() || num2 > 0.1f))
		{
			for (int i = 0; i < 3; i++)
			{
				Vector3 vector2 = vector + Vector3.down * 0.5f + Vector3.Scale(UnityEngine.Random.insideUnitSphere, new Vector3(0.7f, 0.8f, 0.7f));
				Ray ray = new Ray(eyeTransform.position - eyeTransform.right * 0.2f, vector2.normalized);
				if (!Physics.Raycast(ray, vector.magnitude, 1))
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void SpawnAt(Vector3 position)
	{
		target = null;
		targetVehicle = null;
		hasFlightTarget = false;
		takingFireAction.Stop();
		radiusModifier.enabled = false;
		recentAntiStuckEvents = 0;
		ragdollAutokillAction.Start();
		moveTimeoutAction.Start();
		StartAiCoroutines();
	}

	public override void ApplyRecoil(Vector3 impulse)
	{
		facingDirection = Quaternion.LookRotation(FacingDirection() * 20f + impulse.z * Vector3.down + Vector3.right * impulse.x, Vector3.up);
	}

	public bool FindCover()
	{
		return FindCoverAtPoint(actor.Position());
	}

	public bool FindCoverAtPoint(Vector3 point)
	{
		if (HasCover())
		{
			LeaveCover();
		}
		inCover = false;
		cover = CoverManager.instance.ClosestVacant(point);
		if (HasCover())
		{
			CancelPath();
			cover.taken = true;
			Goto(cover.transform.position);
			StartSprint();
			return true;
		}
		CancelPath();
		Goto(point);
		return false;
	}

	public bool FindCoverTowards(Vector3 direction)
	{
		if (HasCover())
		{
			LeaveCover();
		}
		inCover = false;
		cover = CoverManager.instance.ClosestVacantCoveringDirection(base.transform.position, direction);
		if (HasCover())
		{
			cover.taken = true;
			Goto(cover.transform.position);
			StartSprint();
			return true;
		}
		return false;
	}

	public void LeaveCover()
	{
		inCover = false;
		if (HasCover())
		{
			cover.taken = false;
			cover = null;
		}
	}

	public bool HasCover()
	{
		return cover != null;
	}

	public bool IsMovingToCover()
	{
		return HasCover() && !InCover();
	}

	public bool InSquad()
	{
		return squad != null;
	}

	public void AssignedToSquad(Squad squad)
	{
		this.squad = squad;
		if (IsSquadLeader())
		{
			EmoteRegroup();
		}
		else
		{
			EmoteHailLeaderSlow();
		}
	}

	public bool IsSquadLeader()
	{
		return squad.Leader() == this;
	}

	public bool InCover()
	{
		return inCover;
	}

	public void EmoteRegroup()
	{
		actor.EmoteRegroup();
	}

	public void EmoteMoveOrder(Vector3 target)
	{
		LookAt(target);
		actor.EmoteMove();
	}

	public void EmoteHailLeaderSlow()
	{
		Invoke("EmoteHailLeader", UnityEngine.Random.Range(0.6f, 1.5f));
	}

	public void EmoteHailPlayer()
	{
		if (!HasTarget())
		{
			LookAt(FpsActorController.instance.actor.CenterPosition());
			actor.EmoteHail();
		}
	}

	public void EmoteHailLeader()
	{
		if (!HasTarget())
		{
			LookAt(squad.Leader().transform.position);
			actor.EmoteHail();
		}
	}

	public void EmoteHalt()
	{
		actor.EmoteHalt();
	}

	public void MarkTakingFireFrom(Vector3 direction)
	{
		takingFireDirection = direction;
		takingFireAction.Start();
	}

	public bool IsTakingFire()
	{
		return !takingFireAction.TrueDone();
	}

	public override SpawnPoint SelectedSpawnPoint()
	{
		if (UnityEngine.Random.Range(0f, 1f) < 0.3f)
		{
			return ActorManager.RandomSpawnPointForTeam(actor.team);
		}
		return ActorManager.RandomFrontlineSpawnPointForTeam(actor.team);
	}

	public override Transform WeaponParent()
	{
		return weaponParent;
	}

	public bool HasTargetVehicle()
	{
		return targetVehicle != null;
	}

	public void GotoAndEnterVehicle(Vehicle vehicle)
	{
		targetVehicle = vehicle;
		Goto(vehicle.transform.position);
	}

	public void LeaveVehicle()
	{
		targetVehicle = null;
		if (actor.IsSeated())
		{
			actor.LeaveSeat();
		}
	}

	private List<Vehicle> NearbyNonFullVehicles()
	{
		List<Vehicle> list = new List<Vehicle>(ActorManager.instance.vehicles);
		Vector3 squadPosition = actor.CenterPosition();
		list.RemoveAll((Vehicle vehicle) => !vehicle.AiShouldEnter() || (vehicle.ownerTeam >= 0 && vehicle.ownerTeam != actor.team) || Vector3.Distance(vehicle.transform.position, squadPosition) > 150f);
		list.Sort((Vehicle x, Vehicle y) => Vector3.Distance(x.transform.position, squadPosition).CompareTo(Vector3.Distance(y.transform.position, squadPosition)));
		return list;
	}

	public override void SwitchedToWeapon(Weapon weapon)
	{
	}

	public override bool Crouch()
	{
		return InCover() && cover.type == CoverPoint.Type.Crouch && (IsReloading() || CoolingDown());
	}

	public override void StartCrouch()
	{
	}

	public override bool EndCrouch()
	{
		return true;
	}

	public override WeaponManager.LoadoutSet GetLoadout()
	{
		WeaponManager.LoadoutSet loadoutSet = new WeaponManager.LoadoutSet();
		loadoutSet.primary = WeaponManager.EntryNamed(primaryWeaponNames[UnityEngine.Random.Range(0, primaryWeaponNames.Length)]);
		loadoutSet.secondary = WeaponManager.EntryNamed(secondaryWeaponNames[UnityEngine.Random.Range(0, secondaryWeaponNames.Length)]);
		loadoutSet.gear1 = WeaponManager.EntryNamed(gearNames[UnityEngine.Random.Range(0, gearNames.Length)]);
		return loadoutSet;
	}

	private void SwitchToPrimaryWeapon()
	{
		for (int i = 0; i < actor.weapons.Length; i++)
		{
			if (actor.weapons[i] != null && actor.weapons[i].HasAnyAmmo())
			{
				actor.SwitchWeapon(i);
				break;
			}
		}
	}

	private void SwitchToEffectiveWeapon(Actor target)
	{
		Actor.TargetType targetType = target.GetTargetType();
		float range = Vector3.Distance(base.transform.position, target.transform.position);
		int num = -1;
		for (int i = 0; i < actor.weapons.Length; i++)
		{
			Weapon weapon = actor.weapons[i];
			if (!(weapon != null) || !weapon.HasAnyAmmo() || !weapon.EffectiveAtRange(range))
			{
				continue;
			}
			switch (weapon.EffectivenessAgainst(targetType))
			{
				case Weapon.Effectiveness.Preferred:
					if (!weapon.IsEmpty())
					{
						actor.SwitchWeapon(i);
						return;
					}
					num = i;
					break;
				case Weapon.Effectiveness.Yes:
					if (weapon.HasAnyAmmo() && weapon.EffectiveAtRange(range))
					{
						num = i;
					}
					break;
			}
		}
		if (num != -1)
		{
			actor.SwitchWeapon(num);
		}
	}

	private void OnGUI()
	{
		if (!ActorManager.instance.debug || actor.dead || !(Camera.main != null))
		{
			return;
		}
		float num = Vector3.Dot(actor.CenterPosition() - Camera.main.transform.position, Camera.main.transform.forward);
		if (num > 1f && num < 100f)
		{
			Vector3 vector = Camera.main.WorldToScreenPoint(actor.CenterPosition() + Vector3.up);
			GUI.skin.label.alignment = TextAnchor.UpperCenter;
			GUI.Label(new Rect(vector.x - 100f, (float)Screen.height - vector.y, 200f, 50f), string.Concat("Squad #", squad.number, ": ", squad.state, (!squad.IsGroupedUp()) ? string.Empty : " grouped"));
			if (!stayInCoverAction.TrueDone())
			{
				GUI.Label(new Rect(vector.x - 100f, (float)Screen.height - vector.y + 20f, 200f, 50f), "Staying in cover");
			}
			if (blockerAhead)
			{
				GUI.Label(new Rect(vector.x - 100f, (float)Screen.height - vector.y + 40f, 200f, 50f), "Blocker ahead!");
			}
		}
	}

	public override bool IsGroupedUp()
	{
		return squad != null && squad.IsGroupedUp();
	}

	public override bool IsSprinting()
	{
		return !sprintAction.TrueDone();
	}
}
