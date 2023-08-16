using UnityEngine;

public class WaterLevel : MonoBehaviour
{
	public static float height;

	public static bool InWater(Vector3 position)
	{
		return position.y <= height;
	}

	public static float Depth(Vector3 position)
	{
		return height - position.y;
	}

	private void Awake()
	{
		height = base.transform.position.y;
	}
}
