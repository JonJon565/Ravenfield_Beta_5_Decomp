using UnityEngine;

public class TestSMath : MonoBehaviour
{
	public Transform end;

	public Transform probe;

	private void Update()
	{
		Debug.DrawLine(base.transform.position, end.position, Color.white);
		Debug.DrawLine(probe.position, SMath.LineSegmentVsPointClosest(base.transform.position, end.position, probe.position), Color.red);
	}
}
