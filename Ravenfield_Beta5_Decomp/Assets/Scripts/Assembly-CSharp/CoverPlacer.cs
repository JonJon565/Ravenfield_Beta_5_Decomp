using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class CoverPlacer : MonoBehaviour
{
	private const float UNDERWATER_PENALTY = 100000f;

	private const float COVER_POINT_SAMPLE_SPACING = 0.05f;

	private const float COVER_POINT_LOW_HEIGHT = 0.7f;

	private const float COVER_POINT_HIGH_HEIGHT = 1.5f;

	private const float LEAN_DISTANCE = 0.3f;

	private const float COVER_POINT_MIN_SPACING = 1f;

	private const int FOUND_POINT_SKIP_SAMPLES = 20;

	private const float SPHERECAST_RADIUS = 0.1f;

	private const float SPHERECAST_PLAYER_RADIUS = 0.5f;

	private const float MIN_CAR_NORMAL_Y = 0.88f;

	private AstarPath pathfinding;

	private RecastGraph graph;

	private List<CoverPoint> newCoverPoints;

	private int nFlat;

	private int nNotFlat;

	private int nUnderWater;

	public float vehicleInclinationPenalty = 1000f;

	public bool drawWidgets;

	private float waterHeight;

	public void Generate()
	{
		WaterLevel waterLevel = Object.FindObjectOfType<WaterLevel>();
		if (waterLevel != null)
		{
			waterHeight = waterLevel.transform.position.y;
		}
		Debug.Log(waterHeight);
		nFlat = 0;
		nNotFlat = 0;
		newCoverPoints = new List<CoverPoint>();
		CoverPoint[] componentsInChildren = GetComponentsInChildren<CoverPoint>();
		foreach (CoverPoint coverPoint in componentsInChildren)
		{
			Object.DestroyImmediate(coverPoint.gameObject);
		}
		pathfinding = GetComponent<AstarPath>();
		if (pathfinding.graphs[0].CountNodes() == 0)
		{
			pathfinding.astarData.LoadFromCache();
		}
		graph = (RecastGraph)pathfinding.graphs[0];
		graph.GetNodes(HandleInfantryNode);
		PruneCloseCoverPoints();
		Debug.Log("--- Infantry Graph ---");
		Debug.Log("Flat: " + nFlat);
		Debug.Log("Not Flat: " + nNotFlat);
		Debug.Log("Underwater: " + nUnderWater);
		RecastGraph recastGraph = (RecastGraph)pathfinding.graphs[1];
		RecastGraph recastGraph2 = (RecastGraph)pathfinding.graphs[2];
		nFlat = 0;
		nNotFlat = 0;
		nUnderWater = 0;
		recastGraph2.GetNodes(HandleNodePenalty);
		Debug.Log("--- Car Graph ---");
		Debug.Log("Flat: " + nFlat);
		Debug.Log("Not Flat: " + nNotFlat);
		Debug.Log("Underwater: " + nUnderWater);
	}

	public void ScanAndGenerate()
	{
		pathfinding = GetComponent<AstarPath>();
		Debug.Log("Scanning pathfinding.");
		pathfinding.Scan();
		Debug.Log("Scan complete, generating cover & penalties.");
		Generate();
		Debug.Log("Generate complete.");
	}

	private bool HandleInfantryNode(GraphNode gNode)
	{
		HandleNodeCover(gNode);
		HandleNodePenalty(gNode);
		return true;
	}

	private bool HandleNodeCover(GraphNode gNode)
	{
		MeshNode meshNode = (MeshNode)gNode;
		int vertexCount = meshNode.GetVertexCount();
		Vector3 normalized = Vector3.Cross((Vector3)(meshNode.GetVertex(1) - meshNode.GetVertex(0)), (Vector3)(meshNode.GetVertex(2) - meshNode.GetVertex(0))).normalized;
		for (int i = 0; i < vertexCount; i++)
		{
			FindCoverPoints((Vector3)meshNode.GetVertex(i), (Vector3)meshNode.GetVertex((i + 1) % vertexCount), (Vector3)meshNode.position);
		}
		return true;
	}

	private bool HandleNodePenalty(GraphNode gNode)
	{
		MeshNode meshNode = (MeshNode)gNode;
		int vertexCount = meshNode.GetVertexCount();
		Vector3 normalized = Vector3.Cross((Vector3)(meshNode.GetVertex(1) - meshNode.GetVertex(0)), (Vector3)(meshNode.GetVertex(2) - meshNode.GetVertex(0))).normalized;
		bool flag = false;
		for (int i = 0; i < vertexCount; i++)
		{
			if (((Vector3)meshNode.GetVertex(i)).y < waterHeight)
			{
				flag = true;
			}
		}
		float num = Mathf.Abs(normalized.y);
		bool flag2 = num > 0.88f;
		meshNode.Tag = (flag2 ? 1u : 0u);
		meshNode.Penalty = (uint)Mathf.RoundToInt((1f - num) * vehicleInclinationPenalty + ((!flag) ? 0f : 100000f));
		if (flag2)
		{
			nFlat++;
		}
		else
		{
			nNotFlat++;
		}
		if (flag)
		{
			nUnderWater++;
		}
		return true;
	}

	private void FindCoverPoints(Vector3 v1, Vector3 v2, Vector3 center)
	{
		float num = Vector3.Distance(v1, v2);
		int num2 = Mathf.CeilToInt(num / 0.05f);
		Vector3 vector = (v2 - v1) / num2;
		Vector3 vector2 = Vector3.Cross(vector, Vector3.up);
		float maxDistance = graph.characterRadius * 2f;
		if (Vector3.Dot(vector2, v1 - center) < -1f)
		{
			vector2 = -vector2;
		}
		vector2 = vector2.normalized;
		for (int i = 0; i < num2; i++)
		{
			Vector3 vector3 = v1 + vector * i;
			RaycastHit hitInfo;
			if (Physics.Raycast(new Ray(vector3 + Vector3.up * 2f, Vector3.down), out hitInfo, 10f, graph.mask))
			{
				vector3 = hitInfo.point;
				CoverPoint.Type type;
				if (SuitableCoverPoint(vector3, vector2, maxDistance, out type))
				{
					GenerateCoverPoint(vector3, vector2, type);
					i += 20;
				}
			}
		}
	}

	private bool SuitableCoverPoint(Vector3 point, Vector3 outwards, float maxDistance, out CoverPoint.Type type)
	{
		type = CoverPoint.Type.Crouch;
		if (!Physics.Raycast(new Ray(point + Vector3.up * 0.7f, outwards), maxDistance, graph.mask))
		{
			return false;
		}
		Vector3 vector = Vector3.Cross(Vector3.up, outwards);
		if (!Physics.SphereCast(new Ray(point + Vector3.up * 1.5f, outwards), 0.1f, 3f * maxDistance, graph.mask))
		{
			return true;
		}
		if (!Physics.SphereCast(new Ray(point + Vector3.up * 1.5f + vector * 0.3f, outwards), 0.1f, 3f * maxDistance, graph.mask))
		{
			type = CoverPoint.Type.LeanRight;
			return true;
		}
		if (!Physics.SphereCast(new Ray(point + Vector3.up * 1.5f - vector * 0.3f, outwards), 0.1f, 3f * maxDistance, graph.mask))
		{
			type = CoverPoint.Type.LeanLeft;
			return true;
		}
		return false;
	}

	private void GenerateCoverPoint(Vector3 point, Vector3 direction, CoverPoint.Type type)
	{
		Vector3 position = point;
		RaycastHit hitInfo;
		if (Physics.SphereCast(new Ray(point + Vector3.up * 0.5f * 1f, direction), 0.5f, out hitInfo, (int)graph.mask))
		{
			position = hitInfo.point - direction * 0.5f;
		}
		position.y = point.y;
		GameObject gameObject = new GameObject("Cover Point");
		gameObject.transform.position = position;
		gameObject.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
		CoverPoint coverPoint = gameObject.AddComponent<CoverPoint>();
		coverPoint.type = type;
		newCoverPoints.Add(coverPoint);
		gameObject.transform.parent = base.transform;
	}

	private void PruneCloseCoverPoints()
	{
		CoverPoint[] array = newCoverPoints.ToArray();
		foreach (CoverPoint coverPoint in array)
		{
			CoverPoint[] array2 = newCoverPoints.ToArray();
			foreach (CoverPoint coverPoint2 in array2)
			{
				if (coverPoint != coverPoint2 && coverPoint.CoversDirection(coverPoint2.transform.forward) && Vector3.Distance(coverPoint.transform.position, coverPoint2.transform.position) < 0.8f)
				{
					newCoverPoints.Remove(coverPoint);
					Object.DestroyImmediate(coverPoint.gameObject);
					break;
				}
			}
		}
	}
}
