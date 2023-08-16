using UnityEngine;

public class HeadingGuide : MonoBehaviour
{
	private const float ROTATION_SPEED = 200f;

	private const float VELOCITY_THRESHOLD = 0.2f;

	private Rigidbody rigidbody;

	private void Awake()
	{
		rigidbody = GetComponentInParent<Rigidbody>();
	}

	private void LateUpdate()
	{
		Quaternion to = base.transform.parent.rotation;
		if (rigidbody.velocity.magnitude > 0.2f)
		{
			to = Quaternion.LookRotation(rigidbody.velocity, rigidbody.transform.up);
		}
		base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, 200f * Time.deltaTime);
	}
}
