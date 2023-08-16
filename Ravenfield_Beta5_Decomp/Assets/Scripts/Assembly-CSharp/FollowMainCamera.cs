using UnityEngine;

public class FollowMainCamera : MonoBehaviour
{
	private void LateUpdate()
	{
		base.transform.position = Camera.main.transform.position;
	}
}
