using System.Collections;
using Pathfinding;
using UnityEngine;

public class PathfindingManager : MonoBehaviour
{
	private const uint DEATH_PENALTY_AMOUNT = 200000u;

	private const float DEATH_PENALTY_DURATION = 60f;

	public static PathfindingManager instance;

	private RecastGraph infantryGraph;

	public static void RegisterDeath(Vector3 point)
	{
		GraphNode node = instance.infantryGraph.GetNearest(point).node;
		instance.StartCoroutine(instance.PenalizeNode(node));
	}

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		AstarPath component = GetComponent<AstarPath>();
		infantryGraph = (RecastGraph)component.graphs[0];
	}

	private IEnumerator PenalizeNode(GraphNode node)
	{
		node.Penalty += 200000u;
		yield return new WaitForSeconds(60f);
		node.Penalty -= 200000u;
	}
}
