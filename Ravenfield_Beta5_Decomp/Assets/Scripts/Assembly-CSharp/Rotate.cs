using UnityEngine;

public class Rotate : MonoBehaviour
{
	private Rigidbody rigidbody;

	private void Start()
	{
		rigidbody = GetComponent<Rigidbody>();
	}

	private void FixedUpdate()
	{
		rigidbody.MoveRotation(rigidbody.rotation * Quaternion.AngleAxis(20f * Time.deltaTime, Vector3.up));
	}
}
