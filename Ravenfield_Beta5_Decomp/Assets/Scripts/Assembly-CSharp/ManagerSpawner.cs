using UnityEngine;

public class ManagerSpawner : MonoBehaviour
{
	public GameObject prefab;

	private void Awake()
	{
		if (ActorManager.instance != null)
		{
			Object.Destroy(ActorManager.instance.gameObject);
		}
		Object.Instantiate(prefab);
	}
}
