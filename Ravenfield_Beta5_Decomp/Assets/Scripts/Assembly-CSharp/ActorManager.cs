using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ActorManager : MonoBehaviour
{
	private const int MIN_SQUAD_SIZE = 2;

	private const float MIN_DEAD_TIME = 6f;

	private const float AI_MAX_FIRST_SPAWN_TIME = 10f;

	public static ActorManager instance;

	public float spawnTime = 10f;

	public int team0Bots = 16;

	public int team1Bots = 16;

	public GameObject actorPrefab;

	[NonSerialized]
	public SpawnPoint[] spawnPoints;

	[NonSerialized]
	public List<Actor> actors;

	[NonSerialized]
	public Actor player;

	[NonSerialized]
	public List<Vehicle> vehicles;

	[NonSerialized]
	public bool debug;

	private Dictionary<int, List<Actor>> aliveActors;

	public static void Register(Actor actor)
	{
		instance.actors.Add(actor);
		MinimapUi.AddActorBlip(actor);
		if (!actor.aiControlled)
		{
			instance.player = actor;
		}
	}

	public static void Drop(Actor actor)
	{
		instance.actors.Remove(actor);
	}

	private void Awake()
	{
		instance = this;
		AiActorController.SetupParameters();
		SceneManager.sceneLoaded += OnLevelLoaded;
		spawnTime = Mathf.Max(0.1f, spawnTime);
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnLevelLoaded;
	}

	public void StartGame()
	{
		actors = new List<Actor>();
		spawnPoints = UnityEngine.Object.FindObjectsOfType<SpawnPoint>();
		vehicles = new List<Vehicle>();
		aliveActors = new Dictionary<int, List<Actor>>();
		aliveActors.Add(0, new List<Actor>());
		aliveActors.Add(1, new List<Actor>());
		FillEmptySlotsWithAI();
		InvokeRepeating("SpawnWave", 1f, spawnTime);
	}

	private void FillEmptySlotsWithAI()
	{
		for (int i = 0; i < team0Bots; i++)
		{
			CreateAIActor(0, (float)i / (float)team0Bots);
		}
		for (int j = 0; j < team1Bots; j++)
		{
			CreateAIActor(1, (float)j / (float)team1Bots);
		}
	}

	private void CreateAIActor(int team, float fillRatio)
	{
		Actor component = UnityEngine.Object.Instantiate(actorPrefab).GetComponent<Actor>();
		component.SetTeam(team);
		component.deathTimestamp = Time.time + Mathf.Max(spawnTime, 10f);
		component.lqUpdatePhase = fillRatio * 0.2f;
	}

	private void SpawnWave()
	{
		List<Actor> list = new List<Actor>();
		foreach (Actor actor in actors)
		{
			if (actor.dead && actor.deathTimestamp + 6f < Time.time)
			{
				list.Add(actor);
			}
		}
		StartCoroutine(SpawnActorList(list));
	}

	private IEnumerator SpawnActorList(List<Actor> actorsToSpawn)
	{
		Dictionary<SpawnPoint, List<Actor>> spawnedActors = new Dictionary<SpawnPoint, List<Actor>>();
		SpawnPoint[] array = spawnPoints;
		foreach (SpawnPoint spawnPoint in array)
		{
			spawnedActors.Add(spawnPoint, new List<Actor>());
		}
		foreach (Actor actor in actorsToSpawn)
		{
			SpawnPoint spawnPoint3 = actor.controller.SelectedSpawnPoint();
			if (spawnPoint3 != null)
			{
				actor.SpawnAt(spawnPoint3.GetSpawnPosition());
				spawnedActors[spawnPoint3].Add(actor);
			}
		}
		SpawnPoint[] array2 = spawnPoints;
		foreach (SpawnPoint spawnPoint2 in array2)
		{
			List<AiActorController> aiSquad = new List<AiActorController>();
			int members = 0;
			int squadSize = UnityEngine.Random.Range(2, UnityEngine.Random.Range(2, spawnPoint2.maxSquadSize + 2));
			float squadReadyTime = 0f;
			foreach (Actor spawnedActor in spawnedActors[spawnPoint2])
			{
				if (spawnedActor.aiControlled)
				{
					aiSquad.Add((AiActorController)spawnedActor.controller);
					members++;
					if (members >= squadSize)
					{
						new Squad(aiSquad, squadReadyTime);
						squadSize = UnityEngine.Random.Range(2, UnityEngine.Random.Range(2, spawnPoint2.maxSquadSize + 2));
						aiSquad = new List<AiActorController>();
						members = 0;
						squadReadyTime += 0.3f;
					}
				}
			}
			if (aiSquad.Count > 0)
			{
				new Squad(aiSquad, squadReadyTime);
			}
		}
		yield break;
	}

	public static void SetAlive(Actor actor)
	{
		instance.aliveActors[actor.team].Add(actor);
	}

	public static void SetDead(Actor actor)
	{
		instance.aliveActors[actor.team].Remove(actor);
	}

	public static List<Actor> AliveActorsOnTeam(int team)
	{
		return instance.aliveActors[team];
	}

	public static SpawnPoint RandomSpawnPointForTeam(int team)
	{
		int num = UnityEngine.Random.Range(0, instance.spawnPoints.Length);
		for (int i = 0; i < instance.spawnPoints.Length; i++)
		{
			int num2 = (num + i) % instance.spawnPoints.Length;
			if (instance.spawnPoints[num2].owner == team)
			{
				return instance.spawnPoints[num2];
			}
		}
		return null;
	}

	public static SpawnPoint RandomFrontlineSpawnPointForTeam(int team)
	{
		int num = UnityEngine.Random.Range(0, instance.spawnPoints.Length);
		for (int i = 0; i < instance.spawnPoints.Length; i++)
		{
			int num2 = (num + i) % instance.spawnPoints.Length;
			SpawnPoint spawnPoint = instance.spawnPoints[num2];
			if (spawnPoint.owner == team && (!spawnPoint.IsSafe() || spawnPoint.IsFrontLine()))
			{
				return spawnPoint;
			}
		}
		return RandomSpawnPointForTeam(team);
	}

	public static bool HasSpawnPoint(int team)
	{
		SpawnPoint[] array = instance.spawnPoints;
		foreach (SpawnPoint spawnPoint in array)
		{
			if (spawnPoint.owner == team)
			{
				return true;
			}
		}
		return false;
	}

	public static SpawnPoint ClosestSpawnPoint(Vector3 position)
	{
		SpawnPoint result = null;
		float num = 9999999f;
		SpawnPoint[] array = instance.spawnPoints;
		foreach (SpawnPoint spawnPoint in array)
		{
			float num2 = Vector3.Distance(position, spawnPoint.transform.position);
			if (num2 < num)
			{
				num = num2;
				result = spawnPoint;
			}
		}
		return result;
	}

	public static SpawnPoint RandomEnemySpawnPoint(int team)
	{
		int num = UnityEngine.Random.Range(0, instance.spawnPoints.Length);
		for (int i = 0; i < instance.spawnPoints.Length; i++)
		{
			int num2 = (num + i) % instance.spawnPoints.Length;
			if (instance.spawnPoints[num2].owner != team)
			{
				return instance.spawnPoints[num2];
			}
		}
		return null;
	}

	public static List<Actor> AliveActorsInRange(Vector3 point, float range)
	{
		List<Actor> list = new List<Actor>();
		foreach (Actor actor in instance.actors)
		{
			if (!actor.dead && Vector3.Distance(point, actor.Position()) < range)
			{
				list.Add(actor);
			}
		}
		return list;
	}

	public static List<Actor> ActorsInRange(Vector3 point, float range)
	{
		List<Actor> list = new List<Actor>();
		foreach (Actor actor in instance.actors)
		{
			if (Vector3.Distance(point, actor.Position()) < range)
			{
				list.Add(actor);
			}
		}
		return list;
	}

	public static void RegisterProjectile(Projectile p)
	{
		Ray ray = new Ray(p.transform.position, p.transform.forward);
		float num = 9999f;
		RaycastHit hitInfo;
		if (Physics.Raycast(ray, out hitInfo, 9999f, 1))
		{
			num = hitInfo.distance;
		}
		int team = 1 - p.source.team;
		foreach (Actor item in AliveActorsOnTeam(team))
		{
			if (!item.aiControlled)
			{
				continue;
			}
			Vector3 lhs = p.transform.position - item.Position();
			float magnitude = lhs.magnitude;
			float num2 = magnitude / p.configuration.speed;
			lhs -= item.Velocity() * num2;
			float num3 = Vector3.Dot(lhs, p.transform.forward);
			if (!(Mathf.Abs(num3) > num + 5f))
			{
				float num4 = (0f - num3) / p.configuration.speed;
				Vector3 vector = Physics.gravity * Mathf.Pow(num4, 2f) / 2f;
				Vector3 b = p.transform.position + p.transform.forward * p.configuration.speed * num4 + vector;
				Vector3 a = item.Position() + item.Velocity() * num4;
				float num5 = Vector3.Distance(a, b);
				if (num5 < 5f)
				{
					instance.StartCoroutine(instance.MarkTakingFire((AiActorController)item.controller, -p.transform.forward, num4));
				}
			}
		}
	}

	private IEnumerator MarkTakingFire(AiActorController ai, Vector3 direction, float duration)
	{
		yield return new WaitForSeconds(duration + AiActorController.PARAMETERS.TAKING_FIRE_REACTION_TIME);
		ai.MarkTakingFireFrom(direction);
	}

	public static void RegisterVehicle(Vehicle vehicle)
	{
		instance.vehicles.Add(vehicle);
	}

	public static void DropVehicle(Vehicle vehicle)
	{
		instance.vehicles.Remove(vehicle);
	}

	public static bool Explode(Vector3 point, ExplodingProjectile.ExplosionConfiguration configuration)
	{
		List<Actor> list = ActorsInRange(point, configuration.balanceRange);
		bool result = false;
		foreach (Actor item in list)
		{
			Vector3 vector = item.CenterPosition() - point;
			float magnitude = vector.magnitude;
			float num = configuration.damageFalloff.Evaluate(Mathf.Clamp01(magnitude / configuration.damageRange));
			float num2 = configuration.balanceFalloff.Evaluate(Mathf.Clamp01(magnitude / configuration.balanceRange));
			if (!item.dead)
			{
				item.Damage(configuration.damage * num, configuration.balanceDamage * num2, false, item.CenterPosition(), vector.normalized, vector.normalized * configuration.force * num2);
				result = true;
			}
			else
			{
				item.ApplyRigidbodyForce(vector.normalized * configuration.force * num2);
			}
		}
		Vehicle[] array = instance.vehicles.ToArray();
		foreach (Vehicle vehicle in array)
		{
			float num3 = Vector3.Distance(vehicle.transform.position, point);
			if (num3 < configuration.damageRange)
			{
				float num4 = configuration.damageFalloff.Evaluate(Mathf.Clamp01(num3 / configuration.damageRange));
				vehicle.Damage(configuration.damage * num4);
				result = true;
			}
		}
		return result;
	}

	private void OnLevelLoaded(Scene arg0, LoadSceneMode arg1)
	{
		actors = null;
		CancelInvoke();
	}
}
