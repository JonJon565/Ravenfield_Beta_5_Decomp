using System.Collections.Generic;
using UnityEngine;

public class CommandMap : MonoBehaviour
{
	private CommandMapData data;

	private void Awake()
	{
		data = Object.FindObjectOfType<CommandMapData>();
	}

	private void Start()
	{
		CreateConnectionLines();
	}

	private void CreateConnectionLines()
	{
		Mesh mesh = new Mesh();
		List<Vector3> list = new List<Vector3>();
		List<int> list2 = new List<int>();
		CommandMapData.LevelConnection[] levelConnections = data.levelConnections;
		foreach (CommandMapData.LevelConnection levelConnection in levelConnections)
		{
			Vector3 position = levelConnection.l1.transform.position;
			Vector3 position2 = levelConnection.l2.transform.position;
			Vector3 normalized = (position2 - position).normalized;
			Vector3 vector = Vector3.Cross(normalized, Vector3.up).normalized * data.connectionWidth;
			position += normalized * data.nodeRadius;
			position2 -= normalized * data.nodeRadius;
			list.Add(position - vector);
			list.Add(position + vector);
			list.Add(position2 + vector);
			list.Add(position2 - vector);
			list2.Add(list.Count - 4);
			list2.Add(list.Count - 3);
			list2.Add(list.Count - 2);
			list2.Add(list.Count - 2);
			list2.Add(list.Count - 1);
			list2.Add(list.Count - 4);
		}
		mesh.vertices = list.ToArray();
		mesh.triangles = list2.ToArray();
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		GetComponent<MeshFilter>().mesh = mesh;
	}

	private void Update()
	{
	}
}
