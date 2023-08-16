using System;
using UnityEngine;

public static class SMath
{
	public static class V2D
	{
		public static bool LineSegementsIntersect(Vector2 p, Vector2 p2, Vector2 q, Vector2 q2, out Vector2 intersection)
		{
			intersection = default(Vector2);
			Vector2 vector = p2 - p;
			Vector2 v = q2 - q;
			float num = vector.Cross(v);
			float a = (q - p).Cross(vector);
			if (Mathf.Approximately(num, 0f) && Mathf.Approximately(a, 0f))
			{
				return false;
			}
			if (Mathf.Approximately(num, 0f) && !Mathf.Approximately(a, 0f))
			{
				return false;
			}
			float num2 = (q - p).Cross(v) / num;
			float num3 = (q - p).Cross(vector) / num;
			if (!Mathf.Approximately(num, 0f) && 0f <= num2 && num2 <= 1f && 0f <= num3 && num3 <= 1f)
			{
				intersection = p + num2 * vector;
				return true;
			}
			return false;
		}

		public static float RadiansFromTo(float origin, float target)
		{
			float num = Mathf.DeltaAngle(origin, target);
			if (num < 0f)
			{
				num += (float)Math.PI * 2f;
			}
			return num;
		}
	}

	public static Vector3 LineVsPointClosest(Vector3 origin, Vector3 direction, Vector3 point)
	{
		Vector3 vector = point - origin;
		return Vector3.Project(vector, direction) + origin;
	}

	public static float LineVsPointClosestT(Vector3 origin, Vector3 direction, Vector3 point)
	{
		return ProjectScalar(point - origin, direction);
	}

	public static Vector3 LineSegmentVsPointClosest(Vector3 a, Vector3 b, Vector3 point)
	{
		Vector3 vector = b - a;
		return a + Mathf.Clamp01(LineVsPointClosestT(a, vector, point)) * vector;
	}

	public static float BearingRadian(Vector3 delta)
	{
		return Mathf.Atan2(delta.z, delta.x);
	}

	public static float Bearing(Vector3 delta)
	{
		return BearingRadian(delta) * 57.29578f;
	}

	public static float LineSegmentVsPointClosestT(Vector3 a, Vector3 b, Vector3 point)
	{
		return Mathf.Clamp01(LineVsPointClosestT(a, b - a, point));
	}

	public static float ProjectScalar(Vector3 a, Vector3 onto)
	{
		return Vector3.Dot(a, onto) / Mathf.Pow(onto.magnitude, 2f);
	}
}
