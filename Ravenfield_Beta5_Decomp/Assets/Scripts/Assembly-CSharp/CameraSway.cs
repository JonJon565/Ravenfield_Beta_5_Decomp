using UnityEngine;

public class CameraSway : MonoBehaviour
{
	private Vector3 startPosition;

	private void Start()
	{
		startPosition = base.transform.position;
	}

	private void Update()
	{
		float num = Time.time * 0.001f;
		base.transform.position = startPosition + new Vector3(Mathf.Sin(num * 31f), Mathf.Sin(num * 83f), Mathf.Sin(num * 23f)) * 0.1f;
	}
}
