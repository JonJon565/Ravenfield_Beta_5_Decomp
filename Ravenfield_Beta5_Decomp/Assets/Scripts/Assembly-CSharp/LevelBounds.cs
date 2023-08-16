using UnityEngine;

public class LevelBounds : MonoBehaviour
{
	public static LevelBounds instance;

	private Bounds bounds;

	public static bool IsInside(Vector3 point)
	{
		if (instance == null)
		{
			return true;
		}
		return instance.bounds.Contains(point);
	}

	private void SetupBounds()
	{
		instance = this;
		bounds = new Bounds(base.transform.position, base.transform.localScale);
	}

	private void Awake()
	{
		SetupBounds();
		GetComponent<Renderer>().enabled = false;
	}
}
