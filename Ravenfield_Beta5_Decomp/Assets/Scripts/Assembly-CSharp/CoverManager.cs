using UnityEngine;

public class CoverManager : MonoBehaviour
{
	private const float MAX_COVER_DISTANCE = 50f;

	public static CoverManager instance;

	private CoverPoint[] coverPoints;

	public void Awake()
	{
		instance = this;
	}

	public void StartGame()
	{
		coverPoints = Object.FindObjectsOfType<CoverPoint>();
	}

	public CoverPoint ClosestVacant(Vector3 point)
	{
		CoverPoint result = null;
		float num = 50f;
		CoverPoint[] array = coverPoints;
		foreach (CoverPoint coverPoint in array)
		{
			if (!coverPoint.taken)
			{
				float num2 = Vector3.Distance(coverPoint.transform.position, point);
				if (num2 < num)
				{
					num = num2;
					result = coverPoint;
				}
			}
		}
		return result;
	}

	public CoverPoint ClosestVacantCoveringDirection(Vector3 point, Vector3 direction)
	{
		CoverPoint result = null;
		float num = 50f;
		Vector3 normalized = new Vector3(direction.x, 0f, direction.z).normalized;
		CoverPoint[] array = coverPoints;
		foreach (CoverPoint coverPoint in array)
		{
			if (!coverPoint.taken && coverPoint.CoversDirection(normalized))
			{
				float num2 = Vector3.Distance(coverPoint.transform.position, point);
				if (num2 < num)
				{
					num = num2;
					result = coverPoint;
				}
			}
		}
		return result;
	}
}
