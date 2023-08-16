using System.Collections;
using UnityEngine;

public class VehicleSpawner : MonoBehaviour
{
	public enum RespawnType
	{
		AfterDestroyed = 0,
		AfterMoved = 1,
		Never = 2
	}

	private const int SPAWN_BLOCK_MASK = 5376;

	private static Collider[] spawnCollisions = new Collider[1];

	public float spawnTime = 16f;

	public RespawnType respawnType;

	public GameObject prefab;

	private Vehicle lastSpawnedVehicle;

	private bool lastSpawnedVehicleHasBeenUsed;

	private float collisionCheckRadius;

	private bool spawningQueued;

	private void Awake()
	{
		GetComponent<Renderer>().enabled = false;
		collisionCheckRadius = prefab.GetComponent<Vehicle>().avoidanceSize.magnitude;
	}

	private void Start()
	{
		SpawnVehicle();
	}

	private void StartSpawnCountdown()
	{
		Invoke("SpawnVehicle", spawnTime);
	}

	private void SpawnVehicle()
	{
		if (!GameManager.instance.noVehicles)
		{
			StartCoroutine(SpawnCoroutine());
		}
	}

	private IEnumerator SpawnCoroutine()
	{
		while (SpawnIsBlocked())
		{
			yield return new WaitForSeconds(1f);
		}
		lastSpawnedVehicle = ((GameObject)Object.Instantiate(prefab, base.transform.position, base.transform.rotation)).GetComponent<Vehicle>();
		lastSpawnedVehicle.SetSpawner(this);
		lastSpawnedVehicleHasBeenUsed = false;
	}

	private bool SpawnIsBlocked()
	{
		return Physics.OverlapSphereNonAlloc(base.transform.position, collisionCheckRadius, spawnCollisions, 5376) > 0;
	}

	public void VehicleDied(Vehicle vehicle)
	{
		if (respawnType == RespawnType.AfterDestroyed)
		{
			StartSpawnCountdown();
		}
		else if (respawnType == RespawnType.AfterMoved && vehicle == lastSpawnedVehicle && !lastSpawnedVehicleHasBeenUsed)
		{
			StartSpawnCountdown();
		}
	}

	public void FirstDriverEntered(Vehicle vehicle)
	{
		if (vehicle == lastSpawnedVehicle)
		{
			lastSpawnedVehicleHasBeenUsed = true;
			if (respawnType == RespawnType.AfterMoved)
			{
				StartSpawnCountdown();
			}
		}
	}
}
