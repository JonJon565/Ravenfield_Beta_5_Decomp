using UnityEngine;

public class SplashRotor : MonoBehaviour
{
	private const float ROTOR_SPEED = 1000f;

	private void LateUpdate()
	{
		base.transform.localEulerAngles = new Vector3(0f, 0f, 1000f * Time.time);
	}
}
