using UnityEngine;

public class RollGuide : MonoBehaviour
{
	private void LateUpdate()
	{
		base.transform.eulerAngles = new Vector3(base.transform.parent.eulerAngles.x, base.transform.parent.eulerAngles.y, 0f);
	}
}
