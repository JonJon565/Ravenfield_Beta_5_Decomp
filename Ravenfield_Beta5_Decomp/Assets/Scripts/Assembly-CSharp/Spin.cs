using UnityEngine;

public class Spin : MonoBehaviour
{
	public float speed = 180f;

	private void Update()
	{
		base.transform.Rotate(Vector3.up * speed * Time.deltaTime, Space.World);
	}
}
