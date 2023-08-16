using UnityEngine;

public class Spawner : MonoBehaviour
{
	public KeyCode key;

	public GameObject prefab;

	private void Update()
	{
		if (Input.GetKeyDown(key))
		{
			Object.Instantiate(prefab, base.transform.position, base.transform.rotation);
		}
	}
}
