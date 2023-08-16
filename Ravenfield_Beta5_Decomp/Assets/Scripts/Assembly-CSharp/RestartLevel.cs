using UnityEngine;

public class RestartLevel : MonoBehaviour
{
	private void Start()
	{
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.I))
		{
			Application.LoadLevel(Application.loadedLevel);
		}
	}
}
