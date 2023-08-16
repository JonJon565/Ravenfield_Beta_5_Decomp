using UnityEngine;

public class DeltaYawGui : MonoBehaviour
{
	public Transform a;

	public Transform b;

	private void Update()
	{
		Quaternion quaternion = a.rotation * Quaternion.Inverse(b.rotation);
		base.transform.localEulerAngles = new Vector3(0f, quaternion.eulerAngles.y, 0f);
	}
}
