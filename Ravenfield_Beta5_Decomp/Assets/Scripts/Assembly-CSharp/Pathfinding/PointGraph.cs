using System;
using System.Collections.Generic;
using Pathfinding.Serialization;
using Pathfinding.Serialization.JsonFx;
using Pathfinding.Util;
using UnityEngine;

namespace Pathfinding
{
	[JsonOptIn]
	public class PointGraph : NavGraph, IUpdatableGraph
	{
		[JsonMember]
		public Transform root;

		[JsonMember]
		public string searchTag;

		[JsonMember]
		public float maxDistance;

		[JsonMember]
		public Vector3 limits;

		[JsonMember]
		public bool raycast = true;

		[JsonMember]
		public bool use2DPhysics;

		[JsonMember]
		public bool thickRaycast;

		[JsonMember]
		public float thickRaycastRadius = 1f;

		[JsonMember]
		public bool recursive = true;

		[JsonMember]
		public bool autoLinkNodes = true;

		[JsonMember]
		public LayerMask mask;

		[JsonMember]
		public bool optimizeForSparseGraph;

		[JsonMember]
		public bool optimizeFor2D;

		private static readonly Int3[] ThreeDNeighbours = new Int3[27]
		{
			new Int3(-1, 0, -1),
			new Int3(0, 0, -1),
			new Int3(1, 0, -1),
			new Int3(-1, 0, 0),
			new Int3(0, 0, 0),
			new Int3(1, 0, 0),
			new Int3(-1, 0, 1),
			new Int3(0, 0, 1),
			new Int3(1, 0, 1),
			new Int3(-1, -1, -1),
			new Int3(0, -1, -1),
			new Int3(1, -1, -1),
			new Int3(-1, -1, 0),
			new Int3(0, -1, 0),
			new Int3(1, -1, 0),
			new Int3(-1, -1, 1),
			new Int3(0, -1, 1),
			new Int3(1, -1, 1),
			new Int3(-1, 1, -1),
			new Int3(0, 1, -1),
			new Int3(1, 1, -1),
			new Int3(-1, 1, 0),
			new Int3(0, 1, 0),
			new Int3(1, 1, 0),
			new Int3(-1, 1, 1),
			new Int3(0, 1, 1),
			new Int3(1, 1, 1)
		};

		private Dictionary<Int3, PointNode> nodeLookup;

		private Int3 minLookup;

		private Int3 maxLookup;

		private Int3 lookupCellSize;

		public PointNode[] nodes;

		public int nodeCount;

		private Int3 WorldToLookupSpace(Int3 p)
		{
			Int3 zero = Int3.zero;
			zero.x = ((lookupCellSize.x != 0) ? (p.x / lookupCellSize.x) : 0);
			zero.y = ((lookupCellSize.y != 0) ? (p.y / lookupCellSize.y) : 0);
			zero.z = ((lookupCellSize.z != 0) ? (p.z / lookupCellSize.z) : 0);
			return zero;
		}

		public override int CountNodes()
		{
			return nodeCount;
		}

		public override void GetNodes(GraphNodeDelegateCancelable del)
		{
			if (nodes != null)
			{
				for (int i = 0; i < nodeCount && del(nodes[i]); i++)
				{
				}
			}
		}

		public override NNInfo GetNearest(Vector3 position, NNConstraint constraint, GraphNode hint)
		{
			return GetNearestForce(position, constraint);
		}

		public override NNInfo GetNearestForce(Vector3 position, NNConstraint constraint)
		{
			if (nodes == null)
			{
				return default(NNInfo);
			}
			float num = ((!constraint.constrainDistance) ? float.PositiveInfinity : AstarPath.active.maxNearestNodeDistanceSqr);
			float num2 = float.PositiveInfinity;
			GraphNode graphNode = null;
			float num3 = float.PositiveInfinity;
			GraphNode graphNode2 = null;
			if (optimizeForSparseGraph)
			{
				Int3 @int = WorldToLookupSpace((Int3)position);
				Int3 int2 = @int - minLookup;
				int val = 0;
				val = Math.Max(val, Math.Abs(int2.x));
				val = Math.Max(val, Math.Abs(int2.y));
				val = Math.Max(val, Math.Abs(int2.z));
				int2 = @int - maxLookup;
				val = Math.Max(val, Math.Abs(int2.x));
				val = Math.Max(val, Math.Abs(int2.y));
				val = Math.Max(val, Math.Abs(int2.z));
				PointNode value;
				if (nodeLookup.TryGetValue(@int, out value))
				{
					while (value != null)
					{
						float sqrMagnitude = (position - (Vector3)value.position).sqrMagnitude;
						if (sqrMagnitude < num2)
						{
							num2 = sqrMagnitude;
							graphNode = value;
						}
						if (constraint == null || (sqrMagnitude < num3 && sqrMagnitude < num && constraint.Suitable(value)))
						{
							num3 = sqrMagnitude;
							graphNode2 = value;
						}
						value = value.next;
					}
				}
				for (int i = 1; i <= val; i++)
				{
					if (i >= 20)
					{
						Debug.LogWarning("Aborting GetNearest call at maximum distance because it has iterated too many times.\nIf you get this regularly, check your settings for PointGraph -> <b>Optimize For Sparse Graph</b> and PointGraph -> <b>Optimize For 2D</b>.\nThis happens when the closest node was very far away (20*link distance between nodes). When optimizing for sparse graphs, getting the nearest node from far away positions is <b>very slow</b>.\n");
						break;
					}
					if (lookupCellSize.y == 0)
					{
						Int3 int3 = @int + new Int3(-i, 0, -i);
						for (int j = 0; j <= 2 * i; j++)
						{
							if (nodeLookup.TryGetValue(int3 + new Int3(j, 0, 0), out value))
							{
								while (value != null)
								{
									float sqrMagnitude2 = (position - (Vector3)value.position).sqrMagnitude;
									if (sqrMagnitude2 < num2)
									{
										num2 = sqrMagnitude2;
										graphNode = value;
									}
									if (constraint == null || (sqrMagnitude2 < num3 && sqrMagnitude2 < num && constraint.Suitable(value)))
									{
										num3 = sqrMagnitude2;
										graphNode2 = value;
									}
									value = value.next;
								}
							}
							if (!nodeLookup.TryGetValue(int3 + new Int3(j, 0, 2 * i), out value))
							{
								continue;
							}
							while (value != null)
							{
								float sqrMagnitude3 = (position - (Vector3)value.position).sqrMagnitude;
								if (sqrMagnitude3 < num2)
								{
									num2 = sqrMagnitude3;
									graphNode = value;
								}
								if (constraint == null || (sqrMagnitude3 < num3 && sqrMagnitude3 < num && constraint.Suitable(value)))
								{
									num3 = sqrMagnitude3;
									graphNode2 = value;
								}
								value = value.next;
							}
						}
						for (int k = 1; k < 2 * i; k++)
						{
							if (nodeLookup.TryGetValue(int3 + new Int3(0, 0, k), out value))
							{
								while (value != null)
								{
									float sqrMagnitude4 = (position - (Vector3)value.position).sqrMagnitude;
									if (sqrMagnitude4 < num2)
									{
										num2 = sqrMagnitude4;
										graphNode = value;
									}
									if (constraint == null || (sqrMagnitude4 < num3 && sqrMagnitude4 < num && constraint.Suitable(value)))
									{
										num3 = sqrMagnitude4;
										graphNode2 = value;
									}
									value = value.next;
								}
							}
							if (!nodeLookup.TryGetValue(int3 + new Int3(2 * i, 0, k), out value))
							{
								continue;
							}
							while (value != null)
							{
								float sqrMagnitude5 = (position - (Vector3)value.position).sqrMagnitude;
								if (sqrMagnitude5 < num2)
								{
									num2 = sqrMagnitude5;
									graphNode = value;
								}
								if (constraint == null || (sqrMagnitude5 < num3 && sqrMagnitude5 < num && constraint.Suitable(value)))
								{
									num3 = sqrMagnitude5;
									graphNode2 = value;
								}
								value = value.next;
							}
						}
					}
					else
					{
						Int3 int4 = @int + new Int3(-i, -i, -i);
						for (int l = 0; l <= 2 * i; l++)
						{
							for (int m = 0; m <= 2 * i; m++)
							{
								if (nodeLookup.TryGetValue(int4 + new Int3(l, m, 0), out value))
								{
									while (value != null)
									{
										float sqrMagnitude6 = (position - (Vector3)value.position).sqrMagnitude;
										if (sqrMagnitude6 < num2)
										{
											num2 = sqrMagnitude6;
											graphNode = value;
										}
										if (constraint == null || (sqrMagnitude6 < num3 && sqrMagnitude6 < num && constraint.Suitable(value)))
										{
											num3 = sqrMagnitude6;
											graphNode2 = value;
										}
										value = value.next;
									}
								}
								if (!nodeLookup.TryGetValue(int4 + new Int3(l, m, 2 * i), out value))
								{
									continue;
								}
								while (value != null)
								{
									float sqrMagnitude7 = (position - (Vector3)value.position).sqrMagnitude;
									if (sqrMagnitude7 < num2)
									{
										num2 = sqrMagnitude7;
										graphNode = value;
									}
									if (constraint == null || (sqrMagnitude7 < num3 && sqrMagnitude7 < num && constraint.Suitable(value)))
									{
										num3 = sqrMagnitude7;
										graphNode2 = value;
									}
									value = value.next;
								}
							}
						}
						for (int n = 1; n < 2 * i; n++)
						{
							for (int num4 = 0; num4 <= 2 * i; num4++)
							{
								if (nodeLookup.TryGetValue(int4 + new Int3(0, num4, n), out value))
								{
									while (value != null)
									{
										float sqrMagnitude8 = (position - (Vector3)value.position).sqrMagnitude;
										if (sqrMagnitude8 < num2)
										{
											num2 = sqrMagnitude8;
											graphNode = value;
										}
										if (constraint == null || (sqrMagnitude8 < num3 && sqrMagnitude8 < num && constraint.Suitable(value)))
										{
											num3 = sqrMagnitude8;
											graphNode2 = value;
										}
										value = value.next;
									}
								}
								if (!nodeLookup.TryGetValue(int4 + new Int3(2 * i, num4, n), out value))
								{
									continue;
								}
								while (value != null)
								{
									float sqrMagnitude9 = (position - (Vector3)value.position).sqrMagnitude;
									if (sqrMagnitude9 < num2)
									{
										num2 = sqrMagnitude9;
										graphNode = value;
									}
									if (constraint == null || (sqrMagnitude9 < num3 && sqrMagnitude9 < num && constraint.Suitable(value)))
									{
										num3 = sqrMagnitude9;
										graphNode2 = value;
									}
									value = value.next;
								}
							}
						}
						for (int num5 = 1; num5 < 2 * i; num5++)
						{
							for (int num6 = 1; num6 < 2 * i; num6++)
							{
								if (nodeLookup.TryGetValue(int4 + new Int3(num5, 0, num6), out value))
								{
									while (value != null)
									{
										float sqrMagnitude10 = (position - (Vector3)value.position).sqrMagnitude;
										if (sqrMagnitude10 < num2)
										{
											num2 = sqrMagnitude10;
											graphNode = value;
										}
										if (constraint == null || (sqrMagnitude10 < num3 && sqrMagnitude10 < num && constraint.Suitable(value)))
										{
											num3 = sqrMagnitude10;
											graphNode2 = value;
										}
										value = value.next;
									}
								}
								if (!nodeLookup.TryGetValue(int4 + new Int3(num5, 2 * i, num6), out value))
								{
									continue;
								}
								while (value != null)
								{
									float sqrMagnitude11 = (position - (Vector3)value.position).sqrMagnitude;
									if (sqrMagnitude11 < num2)
									{
										num2 = sqrMagnitude11;
										graphNode = value;
									}
									if (constraint == null || (sqrMagnitude11 < num3 && sqrMagnitude11 < num && constraint.Suitable(value)))
									{
										num3 = sqrMagnitude11;
										graphNode2 = value;
									}
									value = value.next;
								}
							}
						}
					}
					if (graphNode2 != null)
					{
						val = Math.Min(val, i + 1);
					}
				}
			}
			else
			{
				for (int num7 = 0; num7 < nodeCount; num7++)
				{
					PointNode pointNode = nodes[num7];
					float sqrMagnitude12 = (position - (Vector3)pointNode.position).sqrMagnitude;
					if (sqrMagnitude12 < num2)
					{
						num2 = sqrMagnitude12;
						graphNode = pointNode;
					}
					if (constraint == null || (sqrMagnitude12 < num3 && sqrMagnitude12 < num && constraint.Suitable(pointNode)))
					{
						num3 = sqrMagnitude12;
						graphNode2 = pointNode;
					}
				}
			}
			NNInfo result = new NNInfo(graphNode);
			result.constrainedNode = graphNode2;
			if (graphNode2 != null)
			{
				result.constClampedPosition = (Vector3)graphNode2.position;
			}
			else if (graphNode != null)
			{
				result.constrainedNode = graphNode;
				result.constClampedPosition = (Vector3)graphNode.position;
			}
			return result;
		}

		public PointNode AddNode(Int3 position)
		{
			return AddNode(new PointNode(active), position);
		}

		public T AddNode<T>(T node, Int3 position) where T : PointNode
		{
			if (nodes == null || nodeCount == nodes.Length)
			{
				PointNode[] array = new PointNode[(nodes == null) ? 4 : Math.Max(nodes.Length + 4, nodes.Length * 2)];
				for (int i = 0; i < nodeCount; i++)
				{
					array[i] = nodes[i];
				}
				nodes = array;
			}
			node.SetPosition(position);
			node.GraphIndex = graphIndex;
			node.Walkable = true;
			nodes[nodeCount] = node;
			nodeCount++;
			AddToLookup(node);
			return node;
		}

		public static int CountChildren(Transform tr)
		{
			int num = 0;
			foreach (Transform item in tr)
			{
				num++;
				num += CountChildren(item);
			}
			return num;
		}

		public void AddChildren(ref int c, Transform tr)
		{
			foreach (Transform item in tr)
			{
				nodes[c].SetPosition((Int3)item.position);
				nodes[c].Walkable = true;
				nodes[c].gameObject = item.gameObject;
				c++;
				AddChildren(ref c, item);
			}
		}

		public void RebuildNodeLookup()
		{
			if (optimizeForSparseGraph)
			{
				if (maxDistance == 0f)
				{
					lookupCellSize = (Int3)limits;
				}
				else
				{
					lookupCellSize.x = Mathf.CeilToInt(1000f * ((limits.x == 0f) ? maxDistance : Mathf.Min(maxDistance, limits.x)));
					lookupCellSize.y = Mathf.CeilToInt(1000f * ((limits.y == 0f) ? maxDistance : Mathf.Min(maxDistance, limits.y)));
					lookupCellSize.z = Mathf.CeilToInt(1000f * ((limits.z == 0f) ? maxDistance : Mathf.Min(maxDistance, limits.z)));
				}
				if (optimizeFor2D)
				{
					lookupCellSize.y = 0;
				}
				if (nodeLookup == null)
				{
					nodeLookup = new Dictionary<Int3, PointNode>();
				}
				nodeLookup.Clear();
				for (int i = 0; i < nodeCount; i++)
				{
					PointNode node = nodes[i];
					AddToLookup(node);
				}
			}
		}

		public void AddToLookup(PointNode node)
		{
			if (nodeLookup != null)
			{
				Int3 key = WorldToLookupSpace(node.position);
				if (nodeLookup.Count == 0)
				{
					minLookup = key;
					maxLookup = key;
				}
				else
				{
					minLookup = new Int3(Math.Min(minLookup.x, key.x), Math.Min(minLookup.y, key.y), Math.Min(minLookup.z, key.z));
					maxLookup = new Int3(Math.Max(minLookup.x, key.x), Math.Max(minLookup.y, key.y), Math.Max(minLookup.z, key.z));
				}
				if (node.next != null)
				{
					throw new Exception("This node has already been added to the lookup structure");
				}
				PointNode value;
				if (nodeLookup.TryGetValue(key, out value))
				{
					node.next = value.next;
					value.next = node;
				}
				else
				{
					nodeLookup[key] = node;
				}
			}
		}

		public override void ScanInternal(OnScanStatus statusCallback)
		{
			if (root == null)
			{
				GameObject[] array = ((searchTag == null) ? null : GameObject.FindGameObjectsWithTag(searchTag));
				if (array == null)
				{
					nodes = new PointNode[0];
					nodeCount = 0;
					return;
				}
				nodes = new PointNode[array.Length];
				nodeCount = nodes.Length;
				for (int i = 0; i < nodes.Length; i++)
				{
					nodes[i] = new PointNode(active);
				}
				for (int j = 0; j < array.Length; j++)
				{
					nodes[j].SetPosition((Int3)array[j].transform.position);
					nodes[j].Walkable = true;
					nodes[j].gameObject = array[j].gameObject;
				}
			}
			else if (!recursive)
			{
				nodes = new PointNode[root.childCount];
				nodeCount = nodes.Length;
				for (int k = 0; k < nodes.Length; k++)
				{
					nodes[k] = new PointNode(active);
				}
				int num = 0;
				foreach (Transform item in root)
				{
					nodes[num].SetPosition((Int3)item.position);
					nodes[num].Walkable = true;
					nodes[num].gameObject = item.gameObject;
					num++;
				}
			}
			else
			{
				nodes = new PointNode[CountChildren(root)];
				nodeCount = nodes.Length;
				for (int l = 0; l < nodes.Length; l++)
				{
					nodes[l] = new PointNode(active);
				}
				int c = 0;
				AddChildren(ref c, root);
			}
			if (optimizeForSparseGraph)
			{
				RebuildNodeLookup();
			}
			if (!(maxDistance >= 0f))
			{
				return;
			}
			List<PointNode> list = new List<PointNode>(3);
			List<uint> list2 = new List<uint>(3);
			for (int m = 0; m < nodes.Length; m++)
			{
				list.Clear();
				list2.Clear();
				PointNode pointNode = nodes[m];
				if (optimizeForSparseGraph)
				{
					Int3 @int = WorldToLookupSpace(pointNode.position);
					int num2 = ((lookupCellSize.y != 0) ? ThreeDNeighbours.Length : 9);
					for (int n = 0; n < num2; n++)
					{
						Int3 key = @int + ThreeDNeighbours[n];
						PointNode value;
						if (!nodeLookup.TryGetValue(key, out value))
						{
							continue;
						}
						while (value != null)
						{
							float dist;
							if (IsValidConnection(pointNode, value, out dist))
							{
								list.Add(value);
								list2.Add((uint)Mathf.RoundToInt(dist * 1000f));
							}
							value = value.next;
						}
					}
				}
				else
				{
					for (int num3 = 0; num3 < nodes.Length; num3++)
					{
						if (m != num3)
						{
							PointNode pointNode2 = nodes[num3];
							float dist2;
							if (IsValidConnection(pointNode, pointNode2, out dist2))
							{
								list.Add(pointNode2);
								list2.Add((uint)Mathf.RoundToInt(dist2 * 1000f));
							}
						}
					}
				}
				pointNode.connections = list.ToArray();
				pointNode.connectionCosts = list2.ToArray();
			}
		}

		public virtual bool IsValidConnection(GraphNode a, GraphNode b, out float dist)
		{
			dist = 0f;
			if (!a.Walkable || !b.Walkable)
			{
				return false;
			}
			Vector3 vector = (Vector3)(a.position - b.position);
			if ((!Mathf.Approximately(limits.x, 0f) && Mathf.Abs(vector.x) > limits.x) || (!Mathf.Approximately(limits.y, 0f) && Mathf.Abs(vector.y) > limits.y) || (!Mathf.Approximately(limits.z, 0f) && Mathf.Abs(vector.z) > limits.z))
			{
				return false;
			}
			dist = vector.magnitude;
			if (maxDistance == 0f || dist < maxDistance)
			{
				if (!raycast)
				{
					return true;
				}
				Ray ray = new Ray((Vector3)a.position, (Vector3)(b.position - a.position));
				Ray ray2 = new Ray((Vector3)b.position, (Vector3)(a.position - b.position));
				if (use2DPhysics)
				{
					if (thickRaycast)
					{
						if (!Physics2D.CircleCast(ray.origin, thickRaycastRadius, ray.direction, dist, mask) && !Physics2D.CircleCast(ray2.origin, thickRaycastRadius, ray2.direction, dist, mask))
						{
							return true;
						}
					}
					else if (!Physics2D.Linecast((Vector3)a.position, (Vector3)b.position, mask) && !Physics2D.Linecast((Vector3)b.position, (Vector3)a.position, mask))
					{
						return true;
					}
				}
				else if (thickRaycast)
				{
					if (!Physics.SphereCast(ray, thickRaycastRadius, dist, mask) && !Physics.SphereCast(ray2, thickRaycastRadius, dist, mask))
					{
						return true;
					}
				}
				else if (!Physics.Linecast((Vector3)a.position, (Vector3)b.position, mask) && !Physics.Linecast((Vector3)b.position, (Vector3)a.position, mask))
				{
					return true;
				}
			}
			return false;
		}

		public GraphUpdateThreading CanUpdateAsync(GraphUpdateObject o)
		{
			return GraphUpdateThreading.UnityThread;
		}

		public void UpdateAreaInit(GraphUpdateObject o)
		{
		}

		public void UpdateArea(GraphUpdateObject guo)
		{
			if (nodes == null)
			{
				return;
			}
			for (int i = 0; i < nodeCount; i++)
			{
				if (guo.bounds.Contains((Vector3)nodes[i].position))
				{
					guo.WillUpdateNode(nodes[i]);
					guo.Apply(nodes[i]);
				}
			}
			if (!guo.updatePhysics)
			{
				return;
			}
			Bounds bounds = guo.bounds;
			if (thickRaycast)
			{
				bounds.Expand(thickRaycastRadius * 2f);
			}
			List<GraphNode> list = ListPool<GraphNode>.Claim();
			List<uint> list2 = ListPool<uint>.Claim();
			for (int j = 0; j < nodeCount; j++)
			{
				PointNode pointNode = nodes[j];
				Vector3 a = (Vector3)pointNode.position;
				List<GraphNode> list3 = null;
				List<uint> list4 = null;
				for (int k = 0; k < nodeCount; k++)
				{
					if (k == j)
					{
						continue;
					}
					Vector3 b = (Vector3)nodes[k].position;
					if (!VectorMath.SegmentIntersectsBounds(bounds, a, b))
					{
						continue;
					}
					PointNode pointNode2 = nodes[k];
					bool flag = pointNode.ContainsConnection(pointNode2);
					float dist;
					bool flag2 = IsValidConnection(pointNode, pointNode2, out dist);
					if (!flag && flag2)
					{
						if (list3 == null)
						{
							list.Clear();
							list2.Clear();
							list3 = list;
							list4 = list2;
							list3.AddRange(pointNode.connections);
							list4.AddRange(pointNode.connectionCosts);
						}
						uint item = (uint)Mathf.RoundToInt(dist * 1000f);
						list3.Add(pointNode2);
						list4.Add(item);
					}
					else if (flag && !flag2)
					{
						if (list3 == null)
						{
							list.Clear();
							list2.Clear();
							list3 = list;
							list4 = list2;
							list3.AddRange(pointNode.connections);
							list4.AddRange(pointNode.connectionCosts);
						}
						int num = list3.IndexOf(pointNode2);
						if (num != -1)
						{
							list3.RemoveAt(num);
							list4.RemoveAt(num);
						}
					}
				}
				if (list3 != null)
				{
					pointNode.connections = list3.ToArray();
					pointNode.connectionCosts = list4.ToArray();
				}
			}
			ListPool<GraphNode>.Release(list);
			ListPool<uint>.Release(list2);
		}

		public override void PostDeserialization()
		{
			RebuildNodeLookup();
		}

		public override void RelocateNodes(Matrix4x4 oldMatrix, Matrix4x4 newMatrix)
		{
			base.RelocateNodes(oldMatrix, newMatrix);
			RebuildNodeLookup();
		}

		public override void SerializeExtraInfo(GraphSerializationContext ctx)
		{
			if (nodes == null)
			{
				ctx.writer.Write(-1);
			}
			ctx.writer.Write(nodeCount);
			for (int i = 0; i < nodeCount; i++)
			{
				if (nodes[i] == null)
				{
					ctx.writer.Write(-1);
					continue;
				}
				ctx.writer.Write(0);
				nodes[i].SerializeNode(ctx);
			}
		}

		public override void DeserializeExtraInfo(GraphSerializationContext ctx)
		{
			int num = ctx.reader.ReadInt32();
			if (num == -1)
			{
				nodes = null;
				return;
			}
			nodes = new PointNode[num];
			nodeCount = num;
			for (int i = 0; i < nodes.Length; i++)
			{
				if (ctx.reader.ReadInt32() != -1)
				{
					nodes[i] = new PointNode(active);
					nodes[i].DeserializeNode(ctx);
				}
			}
		}
	}
}
