using UnityEngine;

public class PitchRollGuide : MonoBehaviour
{
	private void LateUpdate()
	{
		base.transform.eulerAngles = new Vector3(0f, base.transform.parent.eulerAngles.y, 0f);
	}
}
