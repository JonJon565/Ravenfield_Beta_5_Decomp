using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorSpawner : MonoBehaviour
{
	public GameObject prefab;

	public float time = 5f;

	public bool digIn;

	private int superSpawn;

	private void Start()
	{
		StartCoroutine(Spawn());
	}

	private IEnumerator Spawn()
	{
		yield return new WaitForSeconds(1f);
		while (true)
		{
			int j = 3;
			if (superSpawn == 0)
			{
				superSpawn = 5;
				j = 5;
			}
			else
			{
				superSpawn--;
			}
			List<AiActorController> newControllers = new List<AiActorController>();
			for (int i = 0; i < j; i++)
			{
				AiActorController ai = ((GameObject)Object.Instantiate(prefab, base.transform.position + Vector3.Scale(Random.insideUnitSphere, new Vector3(2f, 0f, 2f)), base.transform.rotation)).GetComponent<AiActorController>();
				newControllers.Add(ai);
			}
			Squad newSquad = new Squad(newControllers, 0f);
			yield return new WaitForSeconds(2f);
			if (digIn)
			{
				newSquad.DigInTowards(base.transform.forward);
			}
			yield return new WaitForSeconds(time);
		}
	}
}
