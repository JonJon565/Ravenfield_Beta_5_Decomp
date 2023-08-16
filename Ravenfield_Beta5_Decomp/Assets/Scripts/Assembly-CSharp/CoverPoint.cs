using System;
using UnityEngine;

public class CoverPoint : MonoBehaviour
{
	public enum Type
	{
		LeanLeft = 0,
		LeanRight = 1,
		Crouch = 2
	}

	private const float COS_MAX_COVER_ANGLE = 0.866f;

	public Type type;

	[NonSerialized]
	public bool taken;

	public bool CoversDirection(Vector3 direction)
	{
		return Vector3.Dot(direction, base.transform.forward) >= 0.866f;
	}
}
