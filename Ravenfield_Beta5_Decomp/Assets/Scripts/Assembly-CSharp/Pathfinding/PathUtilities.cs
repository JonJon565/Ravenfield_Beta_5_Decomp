using System;
using System.Collections.Generic;
using Pathfinding.Util;
using UnityEngine;

namespace Pathfinding
{
	public static class PathUtilities
	{
		private static Queue<GraphNode> BFSQueue;

		private static Dictionary<GraphNode, int> BFSMap;

		public static bool IsPathPossible(GraphNode n1, GraphNode n2)
		{
			return n1.Walkable && n2.Walkable && n1.Area == n2.Area;
		}

		public static bool IsPathPossible(List<GraphNode> nodes)
		{
			if (nodes.Count == 0)
			{
				return true;
			}
			uint area = nodes[0].Area;
			for (int i = 0; i < nodes.Count; i++)
			{
				if (!nodes[i].Walkable || nodes[i].Area != area)
				{
					return false;
				}
			}
			return true;
		}

		public static bool IsPathPossible(List<GraphNode> nodes, int tagMask)
		{
			if (nodes.Count == 0)
			{
				return true;
			}
			if (((tagMask >> (int)nodes[0].Tag) & 1) == 0)
			{
				return false;
			}
			if (!IsPathPossible(nodes))
			{
				return false;
			}
			List<GraphNode> reachableNodes = GetReachableNodes(nodes[0], tagMask);
			bool result = true;
			for (int i = 1; i < nodes.Count; i++)
			{
				if (!reachableNodes.Contains(nodes[i]))
				{
					result = false;
					break;
				}
			}
			ListPool<GraphNode>.Release(reachableNodes);
			return result;
		}

		public static List<GraphNode> GetReachableNodes(GraphNode seed, int tagMask = -1)
		{
			Stack<GraphNode> stack = StackPool<GraphNode>.Claim();
			List<GraphNode> list = ListPool<GraphNode>.Claim();
			HashSet<GraphNode> map = new HashSet<GraphNode>();
			GraphNodeDelegate graphNodeDelegate = ((tagMask != -1) ? ((GraphNodeDelegate)delegate(GraphNode node)
			{
				if (node.Walkable && ((uint)(tagMask >> (int)node.Tag) & (true ? 1u : 0u)) != 0 && map.Add(node))
				{
					list.Add(node);
					stack.Push(node);
				}
			}) : ((GraphNodeDelegate)delegate(GraphNode node)
			{
				if (node.Walkable && map.Add(node))
				{
					list.Add(node);
					stack.Push(node);
				}
			}));
			graphNodeDelegate(seed);
			while (stack.Count > 0)
			{
				stack.Pop().GetConnections(graphNodeDelegate);
			}
			StackPool<GraphNode>.Release(stack);
			return list;
		}

		public static List<GraphNode> BFS(GraphNode seed, int depth, int tagMask = -1)
		{
			BFSQueue = BFSQueue ?? new Queue<GraphNode>();
			Queue<GraphNode> que = BFSQueue;
			BFSMap = BFSMap ?? new Dictionary<GraphNode, int>();
			Dictionary<GraphNode, int> map = BFSMap;
			que.Clear();
			map.Clear();
			List<GraphNode> result = ListPool<GraphNode>.Claim();
			int currentDist = -1;
			GraphNodeDelegate graphNodeDelegate = ((tagMask != -1) ? ((GraphNodeDelegate)delegate(GraphNode node)
			{
				if (node.Walkable && ((uint)(tagMask >> (int)node.Tag) & (true ? 1u : 0u)) != 0 && !map.ContainsKey(node))
				{
					map.Add(node, currentDist + 1);
					result.Add(node);
					que.Enqueue(node);
				}
			}) : ((GraphNodeDelegate)delegate(GraphNode node)
			{
				if (node.Walkable && !map.ContainsKey(node))
				{
					map.Add(node, currentDist + 1);
					result.Add(node);
					que.Enqueue(node);
				}
			}));
			graphNodeDelegate(seed);
			while (que.Count > 0)
			{
				GraphNode graphNode = que.Dequeue();
				currentDist = map[graphNode];
				if (currentDist >= depth)
				{
					break;
				}
				graphNode.GetConnections(graphNodeDelegate);
			}
			que.Clear();
			map.Clear();
			return result;
		}

		public static List<Vector3> GetSpiralPoints(int count, float clearance)
		{
			List<Vector3> list = ListPool<Vector3>.Claim(count);
			float num = clearance / ((float)Math.PI * 2f);
			float num2 = 0f;
			list.Add(InvoluteOfCircle(num, num2));
			for (int i = 0; i < count; i++)
			{
				Vector3 vector = list[list.Count - 1];
				float num3 = (0f - num2) / 2f + Mathf.Sqrt(num2 * num2 / 4f + 2f * clearance / num);
				float num4 = num2 + num3;
				float num5 = num2 + 2f * num3;
				while (num5 - num4 > 0.01f)
				{
					float num6 = (num4 + num5) / 2f;
					Vector3 vector2 = InvoluteOfCircle(num, num6);
					if ((vector2 - vector).sqrMagnitude < clearance * clearance)
					{
						num4 = num6;
					}
					else
					{
						num5 = num6;
					}
				}
				list.Add(InvoluteOfCircle(num, num5));
				num2 = num5;
			}
			return list;
		}

		private static Vector3 InvoluteOfCircle(float a, float t)
		{
			return new Vector3(a * (Mathf.Cos(t) + t * Mathf.Sin(t)), 0f, a * (Mathf.Sin(t) - t * Mathf.Cos(t)));
		}

		public static void GetPointsAroundPointWorld(Vector3 p, IRaycastableGraph g, List<Vector3> previousPoints, float radius, float clearanceRadius)
		{
			if (previousPoints.Count != 0)
			{
				Vector3 zero = Vector3.zero;
				for (int i = 0; i < previousPoints.Count; i++)
				{
					zero += previousPoints[i];
				}
				zero /= (float)previousPoints.Count;
				for (int j = 0; j < previousPoints.Count; j++)
				{
					List<Vector3> list;
					List<Vector3> list2 = (list = previousPoints);
					int index;
					int index2 = (index = j);
					Vector3 vector = list[index];
					list2[index2] = vector - zero;
				}
				GetPointsAroundPoint(p, g, previousPoints, radius, clearanceRadius);
			}
		}

		public static void GetPointsAroundPoint(Vector3 p, IRaycastableGraph g, List<Vector3> previousPoints, float radius, float clearanceRadius)
		{
			if (g == null)
			{
				throw new ArgumentNullException("g");
			}
			NavGraph navGraph = g as NavGraph;
			if (navGraph == null)
			{
				throw new ArgumentException("g is not a NavGraph");
			}
			NNInfo nearestForce = navGraph.GetNearestForce(p, NNConstraint.Default);
			p = nearestForce.clampedPosition;
			if (nearestForce.node == null)
			{
				return;
			}
			radius = Mathf.Max(radius, 1.4142f * clearanceRadius * Mathf.Sqrt(previousPoints.Count));
			clearanceRadius *= clearanceRadius;
			for (int i = 0; i < previousPoints.Count; i++)
			{
				Vector3 vector = previousPoints[i];
				float magnitude = vector.magnitude;
				if (magnitude > 0f)
				{
					vector /= magnitude;
				}
				float num = radius;
				vector *= num;
				bool flag = false;
				int num2 = 0;
				do
				{
					Vector3 vector2 = p + vector;
					GraphHitInfo hit;
					if (g.Linecast(p, vector2, nearestForce.node, out hit))
					{
						vector2 = hit.point;
					}
					for (float num3 = 0.1f; num3 <= 1f; num3 += 0.05f)
					{
						Vector3 vector3 = (vector2 - p) * num3 + p;
						flag = true;
						for (int j = 0; j < i; j++)
						{
							if ((previousPoints[j] - vector3).sqrMagnitude < clearanceRadius)
							{
								flag = false;
								break;
							}
						}
						if (flag)
						{
							previousPoints[i] = vector3;
							break;
						}
					}
					if (!flag)
					{
						if (num2 > 8)
						{
							flag = true;
							continue;
						}
						clearanceRadius *= 0.9f;
						vector = UnityEngine.Random.onUnitSphere * Mathf.Lerp(num, radius, num2 / 5);
						vector.y = 0f;
						num2++;
					}
				}
				while (!flag);
			}
		}

		public static List<Vector3> GetPointsOnNodes(List<GraphNode> nodes, int count, float clearanceRadius = 0f)
		{
			if (nodes == null)
			{
				throw new ArgumentNullException("nodes");
			}
			if (nodes.Count == 0)
			{
				throw new ArgumentException("no nodes passed");
			}
			System.Random random = new System.Random();
			List<Vector3> list = ListPool<Vector3>.Claim(count);
			clearanceRadius *= clearanceRadius;
			if (nodes[0] is TriangleMeshNode || nodes[0] is GridNode)
			{
				List<float> list2 = ListPool<float>.Claim(nodes.Count);
				float num = 0f;
				for (int i = 0; i < nodes.Count; i++)
				{
					TriangleMeshNode triangleMeshNode = nodes[i] as TriangleMeshNode;
					if (triangleMeshNode != null)
					{
						float num2 = Math.Abs(VectorMath.SignedTriangleAreaTimes2XZ(triangleMeshNode.GetVertex(0), triangleMeshNode.GetVertex(1), triangleMeshNode.GetVertex(2)));
						num += num2;
						list2.Add(num);
						continue;
					}
					GridNode gridNode = nodes[i] as GridNode;
					if (gridNode != null)
					{
						GridGraph gridGraph = GridNode.GetGridGraph(gridNode.GraphIndex);
						float num3 = gridGraph.nodeSize * gridGraph.nodeSize;
						num += num3;
						list2.Add(num);
					}
					else
					{
						list2.Add(num);
					}
				}
				for (int j = 0; j < count; j++)
				{
					int num4 = 0;
					int num5 = 10;
					bool flag = false;
					while (!flag)
					{
						flag = true;
						if (num4 >= num5)
						{
							clearanceRadius *= 0.8f;
							num5 += 10;
							if (num5 > 100)
							{
								clearanceRadius = 0f;
							}
						}
						float item = (float)random.NextDouble() * num;
						int num6 = list2.BinarySearch(item);
						if (num6 < 0)
						{
							num6 = ~num6;
						}
						if (num6 >= nodes.Count)
						{
							flag = false;
							continue;
						}
						TriangleMeshNode triangleMeshNode2 = nodes[num6] as TriangleMeshNode;
						Vector3 vector;
						if (triangleMeshNode2 != null)
						{
							float num7;
							float num8;
							do
							{
								num7 = (float)random.NextDouble();
								num8 = (float)random.NextDouble();
							}
							while (num7 + num8 > 1f);
							vector = (Vector3)(triangleMeshNode2.GetVertex(1) - triangleMeshNode2.GetVertex(0)) * num7 + (Vector3)(triangleMeshNode2.GetVertex(2) - triangleMeshNode2.GetVertex(0)) * num8 + (Vector3)triangleMeshNode2.GetVertex(0);
						}
						else
						{
							GridNode gridNode2 = nodes[num6] as GridNode;
							if (gridNode2 == null)
							{
								list.Add((Vector3)nodes[num6].position);
								break;
							}
							GridGraph gridGraph2 = GridNode.GetGridGraph(gridNode2.GraphIndex);
							float num9 = (float)random.NextDouble();
							float num10 = (float)random.NextDouble();
							vector = (Vector3)gridNode2.position + new Vector3(num9 - 0.5f, 0f, num10 - 0.5f) * gridGraph2.nodeSize;
						}
						if (clearanceRadius > 0f)
						{
							for (int k = 0; k < list.Count; k++)
							{
								if ((list[k] - vector).sqrMagnitude < clearanceRadius)
								{
									flag = false;
									break;
								}
							}
						}
						if (flag)
						{
							list.Add(vector);
							break;
						}
						num4++;
					}
				}
				ListPool<float>.Release(list2);
			}
			else
			{
				for (int l = 0; l < count; l++)
				{
					list.Add((Vector3)nodes[random.Next(nodes.Count)].position);
				}
			}
			return list;
		}
	}
}
