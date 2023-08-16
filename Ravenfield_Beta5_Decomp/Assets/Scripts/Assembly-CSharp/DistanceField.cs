using Pathfinding;
using UnityEngine;

public class DistanceField : MonoBehaviour
{
	private const float CELL_SIZE = 5f;

	public static DistanceField instance;

	[SerializeField]
	private int width;

	[SerializeField]
	private int height;

	[SerializeField]
	private int depth;

	[SerializeField]
	[HideInInspector]
	public byte[] field;

	private Bounds bounds;

	private void Awake()
	{
		instance = this;
		FindBounds();
	}

	private void FindBounds()
	{
		AstarPath component = GetComponent<AstarPath>();
		RecastGraph recastGraph = (RecastGraph)component.graphs[0];
		if (recastGraph.CountNodes() == 0)
		{
			component.astarData.LoadFromCache();
		}
		bounds = recastGraph.forcedBounds;
	}

	public void Generate()
	{
		FindBounds();
		AstarPath component = GetComponent<AstarPath>();
		RecastGraph recastGraph = (RecastGraph)component.graphs[0];
		Vector3 vector = bounds.size / 5f;
		width = Mathf.CeilToInt(vector.x);
		height = Mathf.CeilToInt(vector.y);
		depth = Mathf.CeilToInt(vector.z);
		Debug.Log("Generating distance field of size " + width + ", " + height + ", " + depth + " (" + (float)(width * height * depth) * 8E-06f + " MBs)");
		field = new byte[width * height * depth];
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < depth; j++)
			{
				Ray ray = new Ray(CoordinateToPosition(i, height, j) + Vector3.up, Vector3.down);
				RaycastHit hitInfo;
				if (Physics.Raycast(ray, out hitInfo, bounds.size.y + 10f, recastGraph.mask))
				{
					int x;
					int y;
					int z;
					PositionToCoordinate(hitInfo.point, out x, out y, out z);
					Debug.DrawRay(CoordinateToPosition(x, y, z), Vector3.up * 5f, Color.red, 10f);
					for (int k = 0; k < height; k++)
					{
						field[CoordinateToIndex(i, k, j)] = (byte)((k > y) ? ((uint)(k - y)) : 0u);
					}
				}
			}
		}
	}

	public static Vector3 CoordinateToPosition(int x, int y, int z)
	{
		return instance.bounds.min + new Vector3((float)x + 0.5f, (float)y + 0.5f, (float)z + 0.5f) * 5f;
	}

	public static int CoordinateToIndex(int x, int y, int z)
	{
		return y + x * instance.height + z * (instance.width * instance.height);
	}

	public static void PositionToCoordinate(Vector3 position, out int x, out int y, out int z)
	{
		Vector3 vector = (position - instance.bounds.min) / 5f;
		x = Mathf.Clamp(Mathf.RoundToInt(vector.x), 0, instance.width - 1);
		y = Mathf.Clamp(Mathf.RoundToInt(vector.y), 0, instance.height - 1);
		z = Mathf.Clamp(Mathf.RoundToInt(vector.z), 0, instance.depth - 1);
	}

	public static int PositionToIndex(Vector3 position)
	{
		int x;
		int y;
		int z;
		PositionToCoordinate(position, out x, out y, out z);
		return CoordinateToIndex(x, y, z);
	}

	public static float DistanceAtPosition(Vector3 position)
	{
		return (float)(int)instance.field[PositionToIndex(position)] * 5f + Mathf.Sqrt(instance.bounds.SqrDistance(position));
	}
}
