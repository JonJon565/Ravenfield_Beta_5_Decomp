using Pathfinding;
using UnityEngine;

[HelpURL("http://arongranberg.com/astar/docs/class_bezier_mover.php")]
public class BezierMover : MonoBehaviour
{
	public Transform[] points;

	public float tangentLengths = 5f;

	public float speed = 1f;

	private float time;

	private void Update()
	{
		Move(true);
	}

	private Vector3 Plot(float t)
	{
		int num = points.Length;
		int num2 = Mathf.FloorToInt(t);
		Vector3 normalized = ((points[(num2 + 1) % num].position - points[num2 % num].position).normalized - (points[(num2 - 1 + num) % num].position - points[num2 % num].position).normalized).normalized;
		Vector3 normalized2 = ((points[(num2 + 2) % num].position - points[(num2 + 1) % num].position).normalized - (points[(num2 + num) % num].position - points[(num2 + 1) % num].position).normalized).normalized;
		Debug.DrawLine(points[num2 % num].position, points[num2 % num].position + normalized * tangentLengths, Color.red);
		Debug.DrawLine(points[(num2 + 1) % num].position - normalized2 * tangentLengths, points[(num2 + 1) % num].position, Color.green);
		return AstarSplines.CubicBezier(points[num2 % num].position, points[num2 % num].position + normalized * tangentLengths, points[(num2 + 1) % num].position - normalized2 * tangentLengths, points[(num2 + 1) % num].position, t - (float)num2);
	}

	private void Move(bool progress)
	{
		float num = time;
		float num2 = time + 1f;
		while (num2 - num > 0.0001f)
		{
			float num3 = (num + num2) / 2f;
			Vector3 vector = Plot(num3);
			if ((vector - base.transform.position).sqrMagnitude > speed * Time.deltaTime * (speed * Time.deltaTime))
			{
				num2 = num3;
			}
			else
			{
				num = num3;
			}
		}
		time = (num + num2) / 2f;
		Vector3 vector2 = Plot(time);
		Vector3 vector3 = Plot(time + 0.001f);
		base.transform.position = vector2;
		base.transform.rotation = Quaternion.LookRotation(vector3 - vector2);
	}

	public void OnDrawGizmos()
	{
		if (points.Length < 3)
		{
			return;
		}
		for (int i = 0; i < points.Length; i++)
		{
			if (points[i] == null)
			{
				return;
			}
		}
		for (int j = 0; j < points.Length; j++)
		{
			int num = points.Length;
			Vector3 normalized = ((points[(j + 1) % num].position - points[j].position).normalized - (points[(j - 1 + num) % num].position - points[j].position).normalized).normalized;
			Vector3 normalized2 = ((points[(j + 2) % num].position - points[(j + 1) % num].position).normalized - (points[(j + num) % num].position - points[(j + 1) % num].position).normalized).normalized;
			Vector3 from = points[j].position;
			for (int k = 1; k <= 100; k++)
			{
				Vector3 vector = AstarSplines.CubicBezier(points[j].position, points[j].position + normalized * tangentLengths, points[(j + 1) % num].position - normalized2 * tangentLengths, points[(j + 1) % num].position, (float)k / 100f);
				Gizmos.DrawLine(from, vector);
				from = vector;
			}
		}
	}
}
