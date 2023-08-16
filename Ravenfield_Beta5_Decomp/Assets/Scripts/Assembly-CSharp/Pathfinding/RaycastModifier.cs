using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Pathfinding
{
	[Serializable]
	[RequireComponent(typeof(Seeker))]
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_raycast_modifier.php")]
	[AddComponentMenu("Pathfinding/Modifiers/Raycast Simplifier")]
	public class RaycastModifier : MonoModifier
	{
		[HideInInspector]
		public bool useRaycasting = true;

		[HideInInspector]
		public LayerMask mask = -1;

		[HideInInspector]
		public bool thickRaycast;

		[HideInInspector]
		public float thickRaycastRadius;

		[HideInInspector]
		public Vector3 raycastOffset = Vector3.zero;

		[HideInInspector]
		public bool subdivideEveryIter;

		public int iterations = 2;

		[HideInInspector]
		public bool useGraphRaycasting;

		private static List<Vector3> nodes;

		public override int Order
		{
			get
			{
				return 40;
			}
		}

		public override void Apply(Path p)
		{
			if (iterations <= 0)
			{
				return;
			}
			if (nodes == null)
			{
				nodes = new List<Vector3>(p.vectorPath.Count);
			}
			else
			{
				nodes.Clear();
			}
			nodes.AddRange(p.vectorPath);
			for (int i = 0; i < iterations; i++)
			{
				if (subdivideEveryIter && i != 0)
				{
					if (nodes.Capacity < nodes.Count * 3)
					{
						nodes.Capacity = nodes.Count * 3;
					}
					int count = nodes.Count;
					for (int j = 0; j < count - 1; j++)
					{
						nodes.Add(Vector3.zero);
						nodes.Add(Vector3.zero);
					}
					for (int num = count - 1; num > 0; num--)
					{
						Vector3 a = nodes[num];
						Vector3 b = nodes[num + 1];
						nodes[num * 3] = nodes[num];
						if (num != count - 1)
						{
							nodes[num * 3 + 1] = Vector3.Lerp(a, b, 0.33f);
							nodes[num * 3 + 2] = Vector3.Lerp(a, b, 0.66f);
						}
					}
				}
				int num2 = 0;
				while (num2 < nodes.Count - 2)
				{
					Vector3 v = nodes[num2];
					Vector3 v2 = nodes[num2 + 2];
					Stopwatch stopwatch = Stopwatch.StartNew();
					if (ValidateLine(null, null, v, v2))
					{
						nodes.RemoveAt(num2 + 1);
					}
					else
					{
						num2++;
					}
					stopwatch.Stop();
				}
			}
			p.vectorPath.Clear();
			p.vectorPath.AddRange(nodes);
		}

		public bool ValidateLine(GraphNode n1, GraphNode n2, Vector3 v1, Vector3 v2)
		{
			if (useRaycasting)
			{
				RaycastHit hitInfo2;
				if (thickRaycast && thickRaycastRadius > 0f)
				{
					RaycastHit hitInfo;
					if (Physics.SphereCast(v1 + raycastOffset, thickRaycastRadius, v2 - v1, out hitInfo, (v2 - v1).magnitude, mask))
					{
						return false;
					}
				}
				else if (Physics.Linecast(v1 + raycastOffset, v2 + raycastOffset, out hitInfo2, mask))
				{
					return false;
				}
			}
			if (useGraphRaycasting && n1 == null)
			{
				n1 = AstarPath.active.GetNearest(v1).node;
				n2 = AstarPath.active.GetNearest(v2).node;
			}
			if (useGraphRaycasting && n1 != null && n2 != null)
			{
				NavGraph graph = AstarData.GetGraph(n1);
				NavGraph graph2 = AstarData.GetGraph(n2);
				if (graph != graph2)
				{
					return false;
				}
				if (graph != null)
				{
					IRaycastableGraph raycastableGraph = graph as IRaycastableGraph;
					if (raycastableGraph != null && raycastableGraph.Linecast(v1, v2, n1))
					{
						return false;
					}
				}
			}
			return true;
		}
	}
}
