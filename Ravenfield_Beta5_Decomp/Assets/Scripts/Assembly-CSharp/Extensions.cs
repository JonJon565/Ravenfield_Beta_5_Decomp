using UnityEngine;

public static class Extensions
{
	public static float ProjectionScalar(this Vector3 vector, Vector3 onto)
	{
		return Vector3.Dot(vector, onto) / Vector3.Dot(vector, onto);
	}

	public static Vector3 ToGround(this Vector3 v)
	{
		v.y = 0f;
		return v;
	}

	public static Vector3 ToLocalZGround(this Vector3 v)
	{
		v.z = 0f;
		return v;
	}

	public static Vector2 ToVector2XZ(this Vector3 v)
	{
		return new Vector2(v.x, v.z);
	}

	public static Vector3 ToVector3XZ(this Vector2 v)
	{
		return new Vector3(v.x, 0f, v.y);
	}

	public static float Cross(this Vector2 v, Vector2 v2)
	{
		return v.x * v2.y - v.y * v2.x;
	}

	public static float AtanAngle(this Vector2 v)
	{
		return Mathf.Atan2(v.y, v.x);
	}
}
