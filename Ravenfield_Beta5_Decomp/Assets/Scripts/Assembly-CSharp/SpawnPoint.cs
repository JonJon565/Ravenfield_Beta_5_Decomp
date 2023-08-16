using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
	public int owner = -1;

	public int maxSquadSize = 5;

	public List<SpawnPoint> adjacentSpawnPoints;

	public Transform spawnpointContainer;

	protected virtual void Awake()
	{
		if (spawnpointContainer != null)
		{
			Renderer[] componentsInChildren = spawnpointContainer.GetComponentsInChildren<Renderer>();
			Renderer[] array = componentsInChildren;
			foreach (Renderer renderer in array)
			{
				renderer.enabled = false;
			}
		}
	}

	public virtual Vector3 GetSpawnPosition()
	{
		if (spawnpointContainer == null)
		{
			return RandomPosition();
		}
		return RandomSpawnPointPosition();
	}

	public Vector3 RandomPosition()
	{
		Vector3 vector = base.transform.position + Vector3.Scale(Random.insideUnitSphere, new Vector3(3f, 0f, 3f));
		Ray ray = new Ray(vector + Vector3.up * 3f, Vector3.down);
		RaycastHit hitInfo;
		if (Physics.Raycast(ray, out hitInfo))
		{
			return hitInfo.point;
		}
		return vector;
	}

	public Vector3 RandomSpawnPointPosition()
	{
		int childCount = spawnpointContainer.childCount;
		if (childCount == 0)
		{
			return RandomPosition();
		}
		return spawnpointContainer.GetChild(Random.Range(0, childCount)).position;
	}

	public virtual bool IsSafe()
	{
		return false;
	}

	public virtual float GotoRadius()
	{
		return 5f;
	}

	public virtual bool IsFrontLine()
	{
		foreach (SpawnPoint adjacentSpawnPoint in adjacentSpawnPoints)
		{
			if (adjacentSpawnPoint.owner != owner)
			{
				return true;
			}
		}
		return false;
	}
}
