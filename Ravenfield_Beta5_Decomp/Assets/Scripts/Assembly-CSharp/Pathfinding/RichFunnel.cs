using System;
using System.Collections.Generic;
using Pathfinding.Util;
using UnityEngine;

namespace Pathfinding
{
	public class RichFunnel : RichPathPart
	{
		public enum FunnelSimplification
		{
			None = 0,
			Iterative = 1,
			RecursiveBinary = 2,
			RecursiveTrinary = 3
		}

		private readonly List<Vector3> left;

		private readonly List<Vector3> right;

		private List<TriangleMeshNode> nodes;

		public Vector3 exactStart;

		public Vector3 exactEnd;

		private NavGraph graph;

		private int currentNode;

		private Vector3 currentPosition;

		private int checkForDestroyedNodesCounter;

		private RichPath path;

		private int[] triBuffer = new int[3];

		public FunnelSimplification funnelSimplificationMode = FunnelSimplification.Iterative;

		public RichFunnel()
		{
			left = ListPool<Vector3>.Claim();
			right = ListPool<Vector3>.Claim();
			nodes = new List<TriangleMeshNode>();
			graph = null;
		}

		public RichFunnel Initialize(RichPath path, NavGraph graph)
		{
			if (graph == null)
			{
				throw new ArgumentNullException("graph");
			}
			if (this.graph != null)
			{
				throw new InvalidOperationException("Trying to initialize an already initialized object. " + graph);
			}
			this.graph = graph;
			this.path = path;
			return this;
		}

		public override void OnEnterPool()
		{
			left.Clear();
			right.Clear();
			nodes.Clear();
			graph = null;
			currentNode = 0;
			checkForDestroyedNodesCounter = 0;
		}

		public void BuildFunnelCorridor(List<GraphNode> nodes, int start, int end)
		{
			exactStart = (nodes[start] as MeshNode).ClosestPointOnNode(exactStart);
			exactEnd = (nodes[end] as MeshNode).ClosestPointOnNode(exactEnd);
			left.Clear();
			right.Clear();
			left.Add(exactStart);
			right.Add(exactStart);
			this.nodes.Clear();
			IRaycastableGraph raycastableGraph = graph as IRaycastableGraph;
			if (raycastableGraph != null && funnelSimplificationMode != 0)
			{
				List<GraphNode> list = ListPool<GraphNode>.Claim(end - start);
				switch (funnelSimplificationMode)
				{
				case FunnelSimplification.Iterative:
					SimplifyPath(raycastableGraph, nodes, start, end, list, exactStart, exactEnd);
					break;
				case FunnelSimplification.RecursiveBinary:
					SimplifyPath2(raycastableGraph, nodes, start, end, list, exactStart, exactEnd);
					break;
				case FunnelSimplification.RecursiveTrinary:
					SimplifyPath3(raycastableGraph, nodes, start, end, list, exactStart, exactEnd);
					break;
				}
				if (this.nodes.Capacity < list.Count)
				{
					this.nodes.Capacity = list.Count;
				}
				for (int i = 0; i < list.Count; i++)
				{
					TriangleMeshNode triangleMeshNode = list[i] as TriangleMeshNode;
					if (triangleMeshNode != null)
					{
						this.nodes.Add(triangleMeshNode);
					}
				}
				ListPool<GraphNode>.Release(list);
			}
			else
			{
				if (this.nodes.Capacity < end - start)
				{
					this.nodes.Capacity = end - start;
				}
				for (int j = start; j <= end; j++)
				{
					TriangleMeshNode triangleMeshNode2 = nodes[j] as TriangleMeshNode;
					if (triangleMeshNode2 != null)
					{
						this.nodes.Add(triangleMeshNode2);
					}
				}
			}
			for (int k = 0; k < this.nodes.Count - 1; k++)
			{
				this.nodes[k].GetPortal(this.nodes[k + 1], left, right, false);
			}
			left.Add(exactEnd);
			right.Add(exactEnd);
		}

		public static void SimplifyPath3(IRaycastableGraph rcg, List<GraphNode> nodes, int start, int end, List<GraphNode> result, Vector3 startPoint, Vector3 endPoint, int depth = 0)
		{
			if (start == end)
			{
				result.Add(nodes[start]);
				return;
			}
			if (start + 1 == end)
			{
				result.Add(nodes[start]);
				result.Add(nodes[end]);
				return;
			}
			int count = result.Count;
			GraphHitInfo hit;
			if (!rcg.Linecast(startPoint, endPoint, nodes[start], out hit, result) && result[result.Count - 1] == nodes[end])
			{
				return;
			}
			result.RemoveRange(count, result.Count - count);
			int num = 0;
			float num2 = 0f;
			for (int i = start + 1; i < end - 1; i++)
			{
				float num3 = VectorMath.SqrDistancePointSegment(startPoint, endPoint, (Vector3)nodes[i].position);
				if (num3 > num2)
				{
					num = i;
					num2 = num3;
				}
			}
			int num4 = (num + start) / 2;
			int num5 = (num + end) / 2;
			if (num4 == num5)
			{
				SimplifyPath3(rcg, nodes, start, num4, result, startPoint, (Vector3)nodes[num4].position);
				result.RemoveAt(result.Count - 1);
				SimplifyPath3(rcg, nodes, num4, end, result, (Vector3)nodes[num4].position, endPoint, depth + 1);
			}
			else
			{
				SimplifyPath3(rcg, nodes, start, num4, result, startPoint, (Vector3)nodes[num4].position, depth + 1);
				result.RemoveAt(result.Count - 1);
				SimplifyPath3(rcg, nodes, num4, num5, result, (Vector3)nodes[num4].position, (Vector3)nodes[num5].position, depth + 1);
				result.RemoveAt(result.Count - 1);
				SimplifyPath3(rcg, nodes, num5, end, result, (Vector3)nodes[num5].position, endPoint, depth + 1);
			}
		}

		public static void SimplifyPath2(IRaycastableGraph rcg, List<GraphNode> nodes, int start, int end, List<GraphNode> result, Vector3 startPoint, Vector3 endPoint)
		{
			int count = result.Count;
			if (end <= start + 1)
			{
				result.Add(nodes[start]);
				result.Add(nodes[end]);
			}
			else
			{
				GraphHitInfo hit;
				if (!rcg.Linecast(startPoint, endPoint, nodes[start], out hit, result) && result[result.Count - 1] == nodes[end])
				{
					return;
				}
				result.RemoveRange(count, result.Count - count);
				int num = -1;
				float num2 = float.PositiveInfinity;
				for (int i = start + 1; i < end; i++)
				{
					float num3 = VectorMath.SqrDistancePointSegment(startPoint, endPoint, (Vector3)nodes[i].position);
					if (num == -1 || num3 < num2)
					{
						num = i;
						num2 = num3;
					}
				}
				SimplifyPath2(rcg, nodes, start, num, result, startPoint, (Vector3)nodes[num].position);
				result.RemoveAt(result.Count - 1);
				SimplifyPath2(rcg, nodes, num, end, result, (Vector3)nodes[num].position, endPoint);
			}
		}

		public void SimplifyPath(IRaycastableGraph graph, List<GraphNode> nodes, int start, int end, List<GraphNode> result, Vector3 startPoint, Vector3 endPoint)
		{
			if (graph == null)
			{
				throw new ArgumentNullException("graph");
			}
			if (start > end)
			{
				throw new ArgumentException("start >= end");
			}
			int num = start;
			int num2 = 0;
			while (true)
			{
				if (num2++ > 1000)
				{
					Debug.LogError("!!!");
					return;
				}
				if (start == end)
				{
					break;
				}
				int count = result.Count;
				int num3 = end + 1;
				int num4 = start + 1;
				bool flag = false;
				while (num3 > num4 + 1)
				{
					int num5 = (num3 + num4) / 2;
					Vector3 start2 = ((start != num) ? ((Vector3)nodes[start].position) : startPoint);
					Vector3 end2 = ((num5 != end) ? ((Vector3)nodes[num5].position) : endPoint);
					GraphHitInfo hit;
					if (graph.Linecast(start2, end2, nodes[start], out hit))
					{
						num3 = num5;
						continue;
					}
					flag = true;
					num4 = num5;
				}
				if (!flag)
				{
					result.Add(nodes[start]);
					start = num4;
					continue;
				}
				Vector3 start3 = ((start != num) ? ((Vector3)nodes[start].position) : startPoint);
				Vector3 end3 = ((num4 != end) ? ((Vector3)nodes[num4].position) : endPoint);
				GraphHitInfo hit2;
				graph.Linecast(start3, end3, nodes[start], out hit2, result);
				long num6 = 0L;
				long num7 = 0L;
				for (int i = start; i <= num4; i++)
				{
					num6 += nodes[i].Penalty + ((path.seeker != null) ? path.seeker.tagPenalties[nodes[i].Tag] : 0);
				}
				for (int j = count; j < result.Count; j++)
				{
					num7 += result[j].Penalty + ((path.seeker != null) ? path.seeker.tagPenalties[result[j].Tag] : 0);
				}
				if ((double)num6 * 1.4 * (double)(num4 - start + 1) < (double)(num7 * (result.Count - count)) || result[result.Count - 1] != nodes[num4])
				{
					result.RemoveRange(count, result.Count - count);
					result.Add(nodes[start]);
					start++;
				}
				else
				{
					result.RemoveAt(result.Count - 1);
					start = num4;
				}
			}
			result.Add(nodes[end]);
		}

		public void UpdateFunnelCorridor(int splitIndex, TriangleMeshNode prefix)
		{
			if (splitIndex > 0)
			{
				nodes.RemoveRange(0, splitIndex - 1);
				nodes[0] = prefix;
			}
			else
			{
				nodes.Insert(0, prefix);
			}
			left.Clear();
			right.Clear();
			left.Add(exactStart);
			right.Add(exactStart);
			for (int i = 0; i < nodes.Count - 1; i++)
			{
				nodes[i].GetPortal(nodes[i + 1], left, right, false);
			}
			left.Add(exactEnd);
			right.Add(exactEnd);
		}

		public Vector3 Update(Vector3 position, List<Vector3> buffer, int numCorners, out bool lastCorner, out bool requiresRepath)
		{
			lastCorner = false;
			requiresRepath = false;
			Int3 p = (Int3)position;
			if (nodes[currentNode].Destroyed)
			{
				requiresRepath = true;
				lastCorner = false;
				buffer.Add(position);
				return position;
			}
			if (nodes[currentNode].ContainsPoint(p))
			{
				if (checkForDestroyedNodesCounter >= 10)
				{
					checkForDestroyedNodesCounter = 0;
					int i = 0;
					for (int count = nodes.Count; i < count; i++)
					{
						if (nodes[i].Destroyed)
						{
							requiresRepath = true;
							break;
						}
					}
				}
				else
				{
					checkForDestroyedNodesCounter++;
				}
			}
			else
			{
				bool flag = false;
				int j = currentNode + 1;
				for (int num = Math.Min(currentNode + 3, nodes.Count); j < num; j++)
				{
					if (flag)
					{
						break;
					}
					if (nodes[j].Destroyed)
					{
						requiresRepath = true;
						lastCorner = false;
						buffer.Add(position);
						return position;
					}
					if (nodes[j].ContainsPoint(p))
					{
						currentNode = j;
						flag = true;
					}
				}
				int num2 = currentNode - 1;
				int num3 = Math.Max(currentNode - 3, 0);
				while (num2 > num3 && !flag)
				{
					if (nodes[num2].Destroyed)
					{
						requiresRepath = true;
						lastCorner = false;
						buffer.Add(position);
						return position;
					}
					if (nodes[num2].ContainsPoint(p))
					{
						currentNode = num2;
						flag = true;
					}
					num2--;
				}
				if (!flag)
				{
					int index = 0;
					int closestIsNeighbourOf = 0;
					float closestDist = float.PositiveInfinity;
					bool closestIsInPath = false;
					TriangleMeshNode closestNode = null;
					int containingIndex = nodes.Count - 1;
					checkForDestroyedNodesCounter = 0;
					int k = 0;
					for (int count2 = nodes.Count; k < count2; k++)
					{
						if (nodes[k].Destroyed)
						{
							requiresRepath = true;
							lastCorner = false;
							buffer.Add(position);
							return position;
						}
						Vector3 vector = nodes[k].ClosestPointOnNode(position);
						float sqrMagnitude = (vector - position).sqrMagnitude;
						if (sqrMagnitude < closestDist)
						{
							closestDist = sqrMagnitude;
							index = k;
							closestNode = nodes[k];
							closestIsInPath = true;
						}
					}
					Vector3 posCopy = position;
					GraphNodeDelegate del = delegate(GraphNode node)
					{
						if ((containingIndex <= 0 || node != nodes[containingIndex - 1]) && (containingIndex >= nodes.Count - 1 || node != nodes[containingIndex + 1]))
						{
							TriangleMeshNode triangleMeshNode = node as TriangleMeshNode;
							if (triangleMeshNode != null)
							{
								Vector3 vector2 = triangleMeshNode.ClosestPointOnNode(posCopy);
								float sqrMagnitude2 = (vector2 - posCopy).sqrMagnitude;
								if (sqrMagnitude2 < closestDist)
								{
									closestDist = sqrMagnitude2;
									closestIsNeighbourOf = containingIndex;
									closestNode = triangleMeshNode;
									closestIsInPath = false;
								}
							}
						}
					};
					for (; containingIndex >= 0; containingIndex--)
					{
						nodes[containingIndex].GetConnections(del);
					}
					if (closestIsInPath)
					{
						currentNode = index;
						position = nodes[index].ClosestPointOnNodeXZ(position);
					}
					else
					{
						position = closestNode.ClosestPointOnNodeXZ(position);
						exactStart = position;
						UpdateFunnelCorridor(closestIsNeighbourOf, closestNode);
						currentNode = 0;
					}
				}
			}
			currentPosition = position;
			if (!FindNextCorners(position, currentNode, buffer, numCorners, out lastCorner))
			{
				Debug.LogError("Oh oh");
				buffer.Add(position);
				return position;
			}
			return position;
		}

		public void FindWalls(List<Vector3> wallBuffer, float range)
		{
			FindWalls(currentNode, wallBuffer, currentPosition, range);
		}

		private void FindWalls(int nodeIndex, List<Vector3> wallBuffer, Vector3 position, float range)
		{
			if (range <= 0f)
			{
				return;
			}
			bool flag = false;
			bool flag2 = false;
			range *= range;
			position.y = 0f;
			int num = 0;
			while (!flag || !flag2)
			{
				if ((num >= 0 || !flag) && (num <= 0 || !flag2))
				{
					if (num < 0 && nodeIndex + num < 0)
					{
						flag = true;
					}
					else if (num > 0 && nodeIndex + num >= nodes.Count)
					{
						flag2 = true;
					}
					else
					{
						TriangleMeshNode triangleMeshNode = ((nodeIndex + num - 1 >= 0) ? nodes[nodeIndex + num - 1] : null);
						TriangleMeshNode triangleMeshNode2 = nodes[nodeIndex + num];
						TriangleMeshNode triangleMeshNode3 = ((nodeIndex + num + 1 < nodes.Count) ? nodes[nodeIndex + num + 1] : null);
						if (triangleMeshNode2.Destroyed)
						{
							break;
						}
						if ((triangleMeshNode2.ClosestPointOnNodeXZ(position) - position).sqrMagnitude > range)
						{
							if (num < 0)
							{
								flag = true;
							}
							else
							{
								flag2 = true;
							}
						}
						else
						{
							for (int i = 0; i < 3; i++)
							{
								triBuffer[i] = 0;
							}
							for (int j = 0; j < triangleMeshNode2.connections.Length; j++)
							{
								TriangleMeshNode triangleMeshNode4 = triangleMeshNode2.connections[j] as TriangleMeshNode;
								if (triangleMeshNode4 == null)
								{
									continue;
								}
								int num2 = -1;
								for (int k = 0; k < 3; k++)
								{
									for (int l = 0; l < 3; l++)
									{
										if (triangleMeshNode2.GetVertex(k) == triangleMeshNode4.GetVertex((l + 1) % 3) && triangleMeshNode2.GetVertex((k + 1) % 3) == triangleMeshNode4.GetVertex(l))
										{
											num2 = k;
											k = 3;
											break;
										}
									}
								}
								if (num2 != -1)
								{
									triBuffer[num2] = ((triangleMeshNode4 != triangleMeshNode && triangleMeshNode4 != triangleMeshNode3) ? 1 : 2);
								}
							}
							for (int m = 0; m < 3; m++)
							{
								if (triBuffer[m] == 0)
								{
									wallBuffer.Add((Vector3)triangleMeshNode2.GetVertex(m));
									wallBuffer.Add((Vector3)triangleMeshNode2.GetVertex((m + 1) % 3));
								}
							}
						}
					}
				}
				num = ((num >= 0) ? (-num - 1) : (-num));
			}
		}

		public bool FindNextCorners(Vector3 origin, int startIndex, List<Vector3> funnelPath, int numCorners, out bool lastCorner)
		{
			lastCorner = false;
			if (left == null)
			{
				throw new Exception("left list is null");
			}
			if (right == null)
			{
				throw new Exception("right list is null");
			}
			if (funnelPath == null)
			{
				throw new ArgumentNullException("funnelPath");
			}
			if (left.Count != right.Count)
			{
				throw new ArgumentException("left and right lists must have equal length");
			}
			int count = left.Count;
			if (count == 0)
			{
				throw new ArgumentException("no diagonals");
			}
			if (count - startIndex < 3)
			{
				funnelPath.Add(left[count - 1]);
				lastCorner = true;
				return true;
			}
			while (left[startIndex + 1] == left[startIndex + 2] && right[startIndex + 1] == right[startIndex + 2])
			{
				startIndex++;
				if (count - startIndex <= 3)
				{
					return false;
				}
			}
			Vector3 vector = left[startIndex + 2];
			if (vector == left[startIndex + 1])
			{
				vector = right[startIndex + 2];
			}
			while (VectorMath.IsColinearXZ(origin, left[startIndex + 1], right[startIndex + 1]) || VectorMath.RightOrColinearXZ(left[startIndex + 1], right[startIndex + 1], vector) == VectorMath.RightOrColinearXZ(left[startIndex + 1], right[startIndex + 1], origin))
			{
				startIndex++;
				if (count - startIndex < 3)
				{
					funnelPath.Add(left[count - 1]);
					lastCorner = true;
					return true;
				}
				vector = left[startIndex + 2];
				if (vector == left[startIndex + 1])
				{
					vector = right[startIndex + 2];
				}
			}
			Vector3 vector2 = origin;
			Vector3 vector3 = left[startIndex + 1];
			Vector3 vector4 = right[startIndex + 1];
			int num = startIndex;
			int num2 = startIndex + 1;
			int num3 = startIndex + 1;
			for (int i = startIndex + 2; i < count; i++)
			{
				if (funnelPath.Count >= numCorners)
				{
					return true;
				}
				if (funnelPath.Count > 2000)
				{
					Debug.LogWarning("Avoiding infinite loop. Remove this check if you have this long paths.");
					break;
				}
				Vector3 vector5 = left[i];
				Vector3 vector6 = right[i];
				if (VectorMath.SignedTriangleAreaTimes2XZ(vector2, vector4, vector6) >= 0f)
				{
					if (!(vector2 == vector4) && !(VectorMath.SignedTriangleAreaTimes2XZ(vector2, vector3, vector6) <= 0f))
					{
						funnelPath.Add(vector3);
						vector2 = vector3;
						num = num3;
						vector3 = vector2;
						vector4 = vector2;
						num3 = num;
						num2 = num;
						i = num;
						continue;
					}
					vector4 = vector6;
					num2 = i;
				}
				if (VectorMath.SignedTriangleAreaTimes2XZ(vector2, vector3, vector5) <= 0f)
				{
					if (vector2 == vector3 || VectorMath.SignedTriangleAreaTimes2XZ(vector2, vector4, vector5) >= 0f)
					{
						vector3 = vector5;
						num3 = i;
						continue;
					}
					funnelPath.Add(vector4);
					vector2 = vector4;
					num = num2;
					vector3 = vector2;
					vector4 = vector2;
					num3 = num;
					num2 = num;
					i = num;
				}
			}
			lastCorner = true;
			funnelPath.Add(left[count - 1]);
			return true;
		}
	}
}
