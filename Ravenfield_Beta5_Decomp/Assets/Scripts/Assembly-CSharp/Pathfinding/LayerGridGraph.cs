using System;
using Pathfinding.Serialization;
using Pathfinding.Serialization.JsonFx;
using UnityEngine;

namespace Pathfinding
{
	public class LayerGridGraph : GridGraph, IUpdatableGraph
	{
		public int[] nodeCellIndices;

		[JsonMember]
		public int layerCount;

		[JsonMember]
		public float mergeSpanRange = 0.5f;

		[JsonMember]
		public float characterHeight = 0.4f;

		internal int lastScannedWidth;

		internal int lastScannedDepth;

		public new LevelGridNode[] nodes;

		public override bool uniformWidthDepthGrid
		{
			get
			{
				return false;
			}
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			RemoveGridGraphFromStatic();
		}

		private void RemoveGridGraphFromStatic()
		{
			LevelGridNode.SetGridGraph(active.astarData.GetGraphIndex(this), null);
		}

		public override int CountNodes()
		{
			if (nodes == null)
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < nodes.Length; i++)
			{
				if (nodes[i] != null)
				{
					num++;
				}
			}
			return num;
		}

		public override void GetNodes(GraphNodeDelegateCancelable del)
		{
			if (nodes != null)
			{
				for (int i = 0; i < nodes.Length && (nodes[i] == null || del(nodes[i])); i++)
				{
				}
			}
		}

		public new void UpdateArea(GraphUpdateObject o)
		{
			if (nodes == null || nodes.Length != width * depth * layerCount)
			{
				Debug.LogWarning("The Grid Graph is not scanned, cannot update area ");
				return;
			}
			Bounds b = o.bounds;
			Vector3 min;
			Vector3 max;
			GridGraph.GetBoundsMinMax(b, inverseMatrix, out min, out max);
			int xmin = Mathf.RoundToInt(min.x - 0.5f);
			int xmax = Mathf.RoundToInt(max.x - 0.5f);
			int ymin = Mathf.RoundToInt(min.z - 0.5f);
			int ymax = Mathf.RoundToInt(max.z - 0.5f);
			IntRect intRect = new IntRect(xmin, ymin, xmax, ymax);
			IntRect intRect2 = intRect;
			IntRect b2 = new IntRect(0, 0, width - 1, depth - 1);
			IntRect intRect3 = intRect;
			bool flag = o.updatePhysics || o.modifyWalkability;
			bool flag2 = o is LayerGridGraphUpdate && ((LayerGridGraphUpdate)o).recalculateNodes;
			bool preserveExistingNodes = !(o is LayerGridGraphUpdate) || ((LayerGridGraphUpdate)o).preserveExistingNodes;
			int num = (o.updateErosion ? erodeIterations : 0);
			if (o.trackChangedNodes && flag2)
			{
				Debug.LogError("Cannot track changed nodes when creating or deleting nodes.\nWill not update LayerGridGraph");
				return;
			}
			if (o.updatePhysics && !o.modifyWalkability && collision.collisionCheck)
			{
				Vector3 vector = new Vector3(collision.diameter, 0f, collision.diameter) * 0.5f;
				min -= vector * 1.02f;
				max += vector * 1.02f;
				intRect3 = new IntRect(Mathf.RoundToInt(min.x - 0.5f), Mathf.RoundToInt(min.z - 0.5f), Mathf.RoundToInt(max.x - 0.5f), Mathf.RoundToInt(max.z - 0.5f));
				intRect2 = IntRect.Union(intRect3, intRect2);
			}
			if (flag || num > 0)
			{
				intRect2 = intRect2.Expand(num + 1);
			}
			IntRect intRect4 = IntRect.Intersection(intRect2, b2);
			if (!flag2)
			{
				for (int i = intRect4.xmin; i <= intRect4.xmax; i++)
				{
					for (int j = intRect4.ymin; j <= intRect4.ymax; j++)
					{
						for (int k = 0; k < layerCount; k++)
						{
							o.WillUpdateNode(nodes[k * width * depth + j * width + i]);
						}
					}
				}
			}
			if (o.updatePhysics && !o.modifyWalkability)
			{
				collision.Initialize(matrix, nodeSize);
				intRect4 = IntRect.Intersection(intRect3, b2);
				bool flag3 = false;
				for (int l = intRect4.xmin; l <= intRect4.xmax; l++)
				{
					for (int m = intRect4.ymin; m <= intRect4.ymax; m++)
					{
						flag3 |= RecalculateCell(l, m, preserveExistingNodes);
					}
				}
				for (int n = intRect4.xmin; n <= intRect4.xmax; n++)
				{
					for (int num2 = intRect4.ymin; num2 <= intRect4.ymax; num2++)
					{
						for (int num3 = 0; num3 < layerCount; num3++)
						{
							int num4 = num3 * width * depth + num2 * width + n;
							LevelGridNode levelGridNode = nodes[num4];
							if (levelGridNode != null)
							{
								CalculateConnections(nodes, levelGridNode, n, num2, num3);
							}
						}
					}
				}
			}
			intRect4 = IntRect.Intersection(intRect, b2);
			for (int num5 = intRect4.xmin; num5 <= intRect4.xmax; num5++)
			{
				for (int num6 = intRect4.ymin; num6 <= intRect4.ymax; num6++)
				{
					for (int num7 = 0; num7 < layerCount; num7++)
					{
						int num8 = num7 * width * depth + num6 * width + num5;
						LevelGridNode levelGridNode2 = nodes[num8];
						if (levelGridNode2 == null)
						{
							continue;
						}
						if (flag)
						{
							levelGridNode2.Walkable = levelGridNode2.WalkableErosion;
							if (o.bounds.Contains((Vector3)levelGridNode2.position))
							{
								o.Apply(levelGridNode2);
							}
							levelGridNode2.WalkableErosion = levelGridNode2.Walkable;
						}
						else if (o.bounds.Contains((Vector3)levelGridNode2.position))
						{
							o.Apply(levelGridNode2);
						}
					}
				}
			}
			if (flag && num == 0)
			{
				intRect4 = IntRect.Intersection(intRect2, b2);
				for (int num9 = intRect4.xmin; num9 <= intRect4.xmax; num9++)
				{
					for (int num10 = intRect4.ymin; num10 <= intRect4.ymax; num10++)
					{
						for (int num11 = 0; num11 < layerCount; num11++)
						{
							int num12 = num11 * width * depth + num10 * width + num9;
							LevelGridNode levelGridNode3 = nodes[num12];
							if (levelGridNode3 != null)
							{
								CalculateConnections(nodes, levelGridNode3, num9, num10, num11);
							}
						}
					}
				}
			}
			else
			{
				if (!flag || num <= 0)
				{
					return;
				}
				IntRect a = IntRect.Union(intRect, intRect3).Expand(num);
				IntRect a2 = a.Expand(num);
				a = IntRect.Intersection(a, b2);
				a2 = IntRect.Intersection(a2, b2);
				for (int num13 = a2.xmin; num13 <= a2.xmax; num13++)
				{
					for (int num14 = a2.ymin; num14 <= a2.ymax; num14++)
					{
						for (int num15 = 0; num15 < layerCount; num15++)
						{
							int num16 = num15 * width * depth + num14 * width + num13;
							LevelGridNode levelGridNode4 = nodes[num16];
							if (levelGridNode4 != null)
							{
								bool walkable = levelGridNode4.Walkable;
								levelGridNode4.Walkable = levelGridNode4.WalkableErosion;
								if (!a.Contains(num13, num14))
								{
									levelGridNode4.TmpWalkable = walkable;
								}
							}
						}
					}
				}
				for (int num17 = a2.xmin; num17 <= a2.xmax; num17++)
				{
					for (int num18 = a2.ymin; num18 <= a2.ymax; num18++)
					{
						for (int num19 = 0; num19 < layerCount; num19++)
						{
							int num20 = num19 * width * depth + num18 * width + num17;
							LevelGridNode levelGridNode5 = nodes[num20];
							if (levelGridNode5 != null)
							{
								CalculateConnections(nodes, levelGridNode5, num17, num18, num19);
							}
						}
					}
				}
				ErodeWalkableArea(a2.xmin, a2.ymin, a2.xmax + 1, a2.ymax + 1);
				for (int num21 = a2.xmin; num21 <= a2.xmax; num21++)
				{
					for (int num22 = a2.ymin; num22 <= a2.ymax; num22++)
					{
						if (a.Contains(num21, num22))
						{
							continue;
						}
						for (int num23 = 0; num23 < layerCount; num23++)
						{
							int num24 = num23 * width * depth + num22 * width + num21;
							LevelGridNode levelGridNode6 = nodes[num24];
							if (levelGridNode6 != null)
							{
								levelGridNode6.Walkable = levelGridNode6.TmpWalkable;
							}
						}
					}
				}
				for (int num25 = a2.xmin; num25 <= a2.xmax; num25++)
				{
					for (int num26 = a2.ymin; num26 <= a2.ymax; num26++)
					{
						for (int num27 = 0; num27 < layerCount; num27++)
						{
							int num28 = num27 * width * depth + num26 * width + num25;
							LevelGridNode levelGridNode7 = nodes[num28];
							if (levelGridNode7 != null)
							{
								CalculateConnections(nodes, levelGridNode7, num25, num26, num27);
							}
						}
					}
				}
			}
		}

		public override void ScanInternal(OnScanStatus statusCallback)
		{
			if (nodeSize <= 0f)
			{
				return;
			}
			GenerateMatrix();
			if (width > 1024 || depth > 1024)
			{
				Debug.LogError("One of the grid's sides is longer than 1024 nodes");
				return;
			}
			lastScannedWidth = width;
			lastScannedDepth = depth;
			SetUpOffsetsAndCosts();
			LevelGridNode.SetGridGraph(active.astarData.GetGraphIndex(this), this);
			maxClimb = Mathf.Clamp(maxClimb, 0f, characterHeight);
			LinkedLevelCell[] array = new LinkedLevelCell[width * depth];
			collision = collision ?? new GraphCollision();
			collision.Initialize(matrix, nodeSize);
			for (int i = 0; i < depth; i++)
			{
				for (int j = 0; j < width; j++)
				{
					array[i * width + j] = new LinkedLevelCell();
					LinkedLevelCell linkedLevelCell = array[i * width + j];
					Vector3 position = matrix.MultiplyPoint3x4(new Vector3((float)j + 0.5f, 0f, (float)i + 0.5f));
					RaycastHit[] array2 = collision.CheckHeightAll(position);
					for (int k = 0; k < array2.Length / 2; k++)
					{
						RaycastHit raycastHit = array2[k];
						array2[k] = array2[array2.Length - 1 - k];
						array2[array2.Length - 1 - k] = raycastHit;
					}
					if (array2.Length > 0)
					{
						LinkedLevelNode linkedLevelNode = null;
						for (int l = 0; l < array2.Length; l++)
						{
							LinkedLevelNode linkedLevelNode2 = new LinkedLevelNode();
							linkedLevelNode2.position = array2[l].point;
							if (linkedLevelNode != null && linkedLevelNode2.position.y - linkedLevelNode.position.y <= mergeSpanRange)
							{
								linkedLevelNode.position = linkedLevelNode2.position;
								linkedLevelNode.hit = array2[l];
								linkedLevelNode.walkable = collision.Check(linkedLevelNode2.position);
								continue;
							}
							linkedLevelNode2.walkable = collision.Check(linkedLevelNode2.position);
							linkedLevelNode2.hit = array2[l];
							linkedLevelNode2.height = float.PositiveInfinity;
							if (linkedLevelCell.first == null)
							{
								linkedLevelCell.first = linkedLevelNode2;
								linkedLevelNode = linkedLevelNode2;
							}
							else
							{
								linkedLevelNode.next = linkedLevelNode2;
								linkedLevelNode.height = linkedLevelNode2.position.y - linkedLevelNode.position.y;
								linkedLevelNode = linkedLevelNode.next;
							}
						}
					}
					else
					{
						LinkedLevelNode linkedLevelNode3 = new LinkedLevelNode();
						linkedLevelNode3.position = position;
						linkedLevelNode3.height = float.PositiveInfinity;
						linkedLevelNode3.walkable = !collision.unwalkableWhenNoGround;
						linkedLevelCell.first = linkedLevelNode3;
					}
				}
			}
			int num = 0;
			layerCount = 0;
			for (int m = 0; m < depth; m++)
			{
				for (int n = 0; n < width; n++)
				{
					LinkedLevelCell linkedLevelCell2 = array[m * width + n];
					LinkedLevelNode linkedLevelNode4 = linkedLevelCell2.first;
					int num2 = 0;
					do
					{
						num2++;
						num++;
						linkedLevelNode4 = linkedLevelNode4.next;
					}
					while (linkedLevelNode4 != null);
					layerCount = ((num2 <= layerCount) ? layerCount : num2);
				}
			}
			if (layerCount > 255)
			{
				Debug.LogError("Too many layers, a maximum of LevelGridNode.MaxLayerCount are allowed (found " + layerCount + ")");
				return;
			}
			nodes = new LevelGridNode[width * depth * layerCount];
			for (int num3 = 0; num3 < nodes.Length; num3++)
			{
				nodes[num3] = new LevelGridNode(active);
				nodes[num3].Penalty = initialPenalty;
			}
			int num4 = 0;
			float num5 = Mathf.Cos(maxSlope * ((float)Math.PI / 180f));
			for (int num6 = 0; num6 < depth; num6++)
			{
				for (int num7 = 0; num7 < width; num7++)
				{
					LinkedLevelCell linkedLevelCell3 = array[num6 * width + num7];
					LinkedLevelNode linkedLevelNode5 = linkedLevelCell3.first;
					linkedLevelCell3.index = num4;
					int num8 = 0;
					int num9 = 0;
					do
					{
						LevelGridNode levelGridNode = nodes[num6 * width + num7 + width * depth * num9];
						levelGridNode.SetPosition((Int3)linkedLevelNode5.position);
						levelGridNode.Walkable = linkedLevelNode5.walkable;
						if (linkedLevelNode5.hit.normal != Vector3.zero && (penaltyAngle || num5 < 1f))
						{
							float num10 = Vector3.Dot(linkedLevelNode5.hit.normal.normalized, collision.up);
							if (penaltyAngle)
							{
								levelGridNode.Penalty += (uint)Mathf.RoundToInt((1f - num10) * penaltyAngleFactor);
							}
							if (num10 < num5)
							{
								levelGridNode.Walkable = false;
							}
						}
						levelGridNode.NodeInGridIndex = num6 * width + num7;
						if (linkedLevelNode5.height < characterHeight)
						{
							levelGridNode.Walkable = false;
						}
						levelGridNode.WalkableErosion = levelGridNode.Walkable;
						num4++;
						num8++;
						linkedLevelNode5 = linkedLevelNode5.next;
						num9++;
					}
					while (linkedLevelNode5 != null);
					for (; num9 < layerCount; num9++)
					{
						nodes[num6 * width + num7 + width * depth * num9] = null;
					}
					linkedLevelCell3.count = num8;
				}
			}
			num4 = 0;
			nodeCellIndices = new int[array.Length];
			for (int num11 = 0; num11 < depth; num11++)
			{
				for (int num12 = 0; num12 < width; num12++)
				{
					for (int num13 = 0; num13 < layerCount; num13++)
					{
						GraphNode node = nodes[num11 * width + num12 + width * depth * num13];
						CalculateConnections(nodes, node, num12, num11, num13);
					}
				}
			}
			uint num14 = (uint)active.astarData.GetGraphIndex(this);
			for (int num15 = 0; num15 < nodes.Length; num15++)
			{
				LevelGridNode levelGridNode2 = nodes[num15];
				if (levelGridNode2 != null)
				{
					UpdatePenalty(levelGridNode2);
					levelGridNode2.GraphIndex = num14;
					if (!levelGridNode2.HasAnyGridConnections())
					{
						levelGridNode2.Walkable = false;
						levelGridNode2.WalkableErosion = levelGridNode2.Walkable;
					}
				}
			}
			ErodeWalkableArea();
		}

		public bool RecalculateCell(int x, int z, bool preserveExistingNodes)
		{
			LinkedLevelCell linkedLevelCell = new LinkedLevelCell();
			Vector3 position = matrix.MultiplyPoint3x4(new Vector3((float)x + 0.5f, 0f, (float)z + 0.5f));
			RaycastHit[] array = collision.CheckHeightAll(position);
			for (int i = 0; i < array.Length / 2; i++)
			{
				RaycastHit raycastHit = array[i];
				array[i] = array[array.Length - 1 - i];
				array[array.Length - 1 - i] = raycastHit;
			}
			bool result = false;
			if (array.Length > 0)
			{
				LinkedLevelNode linkedLevelNode = null;
				for (int j = 0; j < array.Length; j++)
				{
					LinkedLevelNode linkedLevelNode2 = new LinkedLevelNode();
					linkedLevelNode2.position = array[j].point;
					if (linkedLevelNode != null && linkedLevelNode2.position.y - linkedLevelNode.position.y <= mergeSpanRange)
					{
						linkedLevelNode.position = linkedLevelNode2.position;
						linkedLevelNode.hit = array[j];
						linkedLevelNode.walkable = collision.Check(linkedLevelNode2.position);
						continue;
					}
					linkedLevelNode2.walkable = collision.Check(linkedLevelNode2.position);
					linkedLevelNode2.hit = array[j];
					linkedLevelNode2.height = float.PositiveInfinity;
					if (linkedLevelCell.first == null)
					{
						linkedLevelCell.first = linkedLevelNode2;
						linkedLevelNode = linkedLevelNode2;
					}
					else
					{
						linkedLevelNode.next = linkedLevelNode2;
						linkedLevelNode.height = linkedLevelNode2.position.y - linkedLevelNode.position.y;
						linkedLevelNode = linkedLevelNode.next;
					}
				}
			}
			else
			{
				LinkedLevelNode linkedLevelNode3 = new LinkedLevelNode();
				linkedLevelNode3.position = position;
				linkedLevelNode3.height = float.PositiveInfinity;
				linkedLevelNode3.walkable = !collision.unwalkableWhenNoGround;
				linkedLevelCell.first = linkedLevelNode3;
			}
			uint num = (uint)active.astarData.GetGraphIndex(this);
			LinkedLevelNode linkedLevelNode4 = linkedLevelCell.first;
			int num2 = 0;
			int k = 0;
			do
			{
				if (k >= layerCount)
				{
					if (k + 1 > 255)
					{
						Debug.LogError("Too many layers, a maximum of LevelGridNode.MaxLayerCount are allowed (required " + (k + 1) + ")");
						return result;
					}
					AddLayers(1);
					result = true;
				}
				LevelGridNode levelGridNode = nodes[z * width + x + width * depth * k];
				if (levelGridNode == null || !preserveExistingNodes)
				{
					nodes[z * width + x + width * depth * k] = new LevelGridNode(active);
					levelGridNode = nodes[z * width + x + width * depth * k];
					levelGridNode.Penalty = initialPenalty;
					levelGridNode.GraphIndex = num;
					result = true;
				}
				levelGridNode.SetPosition((Int3)linkedLevelNode4.position);
				levelGridNode.Walkable = linkedLevelNode4.walkable;
				levelGridNode.WalkableErosion = levelGridNode.Walkable;
				if (linkedLevelNode4.hit.normal != Vector3.zero)
				{
					float num3 = Vector3.Dot(linkedLevelNode4.hit.normal.normalized, collision.up);
					if (penaltyAngle)
					{
						levelGridNode.Penalty += (uint)Mathf.RoundToInt((1f - num3) * penaltyAngleFactor);
					}
					float num4 = Mathf.Cos(maxSlope * ((float)Math.PI / 180f));
					if (num3 < num4)
					{
						levelGridNode.Walkable = false;
					}
				}
				levelGridNode.NodeInGridIndex = z * width + x;
				if (linkedLevelNode4.height < characterHeight)
				{
					levelGridNode.Walkable = false;
				}
				num2++;
				linkedLevelNode4 = linkedLevelNode4.next;
				k++;
			}
			while (linkedLevelNode4 != null);
			for (; k < layerCount; k++)
			{
				nodes[z * width + x + width * depth * k] = null;
			}
			linkedLevelCell.count = num2;
			return result;
		}

		public void AddLayers(int count)
		{
			int num = layerCount + count;
			if (num > 255)
			{
				Debug.LogError("Too many layers, a maximum of LevelGridNode.MaxLayerCount are allowed (required " + num + ")");
				return;
			}
			LevelGridNode[] array = nodes;
			nodes = new LevelGridNode[width * depth * num];
			for (int i = 0; i < array.Length; i++)
			{
				nodes[i] = array[i];
			}
			layerCount = num;
		}

		public virtual void UpdatePenalty(LevelGridNode node)
		{
			node.Penalty = 0u;
			node.Penalty = initialPenalty;
			if (penaltyPosition)
			{
				node.Penalty += (uint)Mathf.RoundToInt(((float)node.position.y - penaltyPositionOffset) * penaltyPositionFactor);
			}
		}

		public override void ErodeWalkableArea(int xmin, int zmin, int xmax, int zmax)
		{
			xmin = Mathf.Clamp(xmin, 0, base.Width);
			xmax = Mathf.Clamp(xmax, 0, base.Width);
			zmin = Mathf.Clamp(zmin, 0, base.Depth);
			zmax = Mathf.Clamp(zmax, 0, base.Depth);
			if (erosionUseTags)
			{
				Debug.LogError("Erosion Uses Tags is not supported for LayerGridGraphs yet");
			}
			for (int i = 0; i < erodeIterations; i++)
			{
				for (int j = 0; j < layerCount; j++)
				{
					for (int k = zmin; k < zmax; k++)
					{
						for (int l = xmin; l < xmax; l++)
						{
							LevelGridNode levelGridNode = nodes[k * width + l + width * depth * j];
							if (levelGridNode == null || !levelGridNode.Walkable)
							{
								continue;
							}
							bool flag = false;
							for (int m = 0; m < 4; m++)
							{
								if (!levelGridNode.GetConnection(m))
								{
									flag = true;
									break;
								}
							}
							if (flag)
							{
								levelGridNode.Walkable = false;
							}
						}
					}
				}
				for (int n = 0; n < layerCount; n++)
				{
					for (int num = zmin; num < zmax; num++)
					{
						for (int num2 = xmin; num2 < xmax; num2++)
						{
							LevelGridNode levelGridNode2 = nodes[num * width + num2 + width * depth * n];
							if (levelGridNode2 != null)
							{
								CalculateConnections(nodes, levelGridNode2, num2, num, n);
							}
						}
					}
				}
			}
		}

		public void CalculateConnections(GraphNode[] nodes, GraphNode node, int x, int z, int layerIndex)
		{
			if (node == null)
			{
				return;
			}
			LevelGridNode levelGridNode = (LevelGridNode)node;
			levelGridNode.ResetAllGridConnections();
			if (!node.Walkable)
			{
				return;
			}
			float num = ((layerIndex != layerCount - 1 && nodes[levelGridNode.NodeInGridIndex + width * depth * (layerIndex + 1)] != null) ? ((float)Math.Abs(levelGridNode.position.y - nodes[levelGridNode.NodeInGridIndex + width * depth * (layerIndex + 1)].position.y) * 0.001f) : float.PositiveInfinity);
			for (int i = 0; i < 4; i++)
			{
				int num2 = x + neighbourXOffsets[i];
				int num3 = z + neighbourZOffsets[i];
				if (num2 < 0 || num3 < 0 || num2 >= width || num3 >= depth)
				{
					continue;
				}
				int num4 = num3 * width + num2;
				int value = 255;
				for (int j = 0; j < layerCount; j++)
				{
					GraphNode graphNode = nodes[num4 + width * depth * j];
					if (graphNode != null && graphNode.Walkable)
					{
						float num5 = ((j != layerCount - 1 && nodes[num4 + width * depth * (j + 1)] != null) ? ((float)Math.Abs(graphNode.position.y - nodes[num4 + width * depth * (j + 1)].position.y) * 0.001f) : float.PositiveInfinity);
						float num6 = Mathf.Max((float)graphNode.position.y * 0.001f, (float)levelGridNode.position.y * 0.001f);
						float num7 = Mathf.Min((float)graphNode.position.y * 0.001f + num5, (float)levelGridNode.position.y * 0.001f + num);
						float num8 = num7 - num6;
						if (num8 >= characterHeight && (float)Mathf.Abs(graphNode.position.y - levelGridNode.position.y) * 0.001f <= maxClimb)
						{
							value = j;
						}
					}
				}
				levelGridNode.SetConnectionValue(i, value);
			}
		}

		public override NNInfo GetNearest(Vector3 position, NNConstraint constraint, GraphNode hint)
		{
			if (nodes == null || depth * width * layerCount != nodes.Length)
			{
				return default(NNInfo);
			}
			Vector3 vector = inverseMatrix.MultiplyPoint3x4(position);
			int x = Mathf.Clamp(Mathf.RoundToInt(vector.x - 0.5f), 0, width - 1);
			int z = Mathf.Clamp(Mathf.RoundToInt(vector.z - 0.5f), 0, depth - 1);
			LevelGridNode nearestNode = GetNearestNode(position, x, z, null);
			return new NNInfo(nearestNode);
		}

		private LevelGridNode GetNearestNode(Vector3 position, int x, int z, NNConstraint constraint)
		{
			int num = width * z + x;
			float num2 = float.PositiveInfinity;
			LevelGridNode result = null;
			for (int i = 0; i < layerCount; i++)
			{
				LevelGridNode levelGridNode = nodes[num + width * depth * i];
				if (levelGridNode != null)
				{
					float sqrMagnitude = ((Vector3)levelGridNode.position - position).sqrMagnitude;
					if (sqrMagnitude < num2 && (constraint == null || constraint.Suitable(levelGridNode)))
					{
						num2 = sqrMagnitude;
						result = levelGridNode;
					}
				}
			}
			return result;
		}

		public override NNInfo GetNearestForce(Vector3 position, NNConstraint constraint)
		{
			if (nodes == null || depth * width * layerCount != nodes.Length || layerCount == 0)
			{
				return default(NNInfo);
			}
			Vector3 vector = position;
			position = inverseMatrix.MultiplyPoint3x4(position);
			int num = Mathf.Clamp(Mathf.RoundToInt(position.x - 0.5f), 0, width - 1);
			int num2 = Mathf.Clamp(Mathf.RoundToInt(position.z - 0.5f), 0, depth - 1);
			float num3 = float.PositiveInfinity;
			int num4 = 2;
			LevelGridNode levelGridNode = GetNearestNode(vector, num, num2, constraint);
			if (levelGridNode != null)
			{
				num3 = ((Vector3)levelGridNode.position - vector).sqrMagnitude;
			}
			if (levelGridNode != null)
			{
				if (num4 == 0)
				{
					return new NNInfo(levelGridNode);
				}
				num4--;
			}
			float num5 = ((!constraint.constrainDistance) ? float.PositiveInfinity : AstarPath.active.maxNearestNodeDistance);
			float num6 = num5 * num5;
			int num7 = 1;
			while (true)
			{
				int num8 = num2 + num7;
				if (nodeSize * (float)num7 > num5)
				{
					return new NNInfo(levelGridNode);
				}
				int i;
				for (i = num - num7; i <= num + num7; i++)
				{
					if (i < 0 || num8 < 0 || i >= width || num8 >= depth)
					{
						continue;
					}
					LevelGridNode nearestNode = GetNearestNode(vector, i, num8, constraint);
					if (nearestNode != null)
					{
						float sqrMagnitude = ((Vector3)nearestNode.position - vector).sqrMagnitude;
						if (sqrMagnitude < num3 && sqrMagnitude < num6)
						{
							num3 = sqrMagnitude;
							levelGridNode = nearestNode;
						}
					}
				}
				num8 = num2 - num7;
				for (i = num - num7; i <= num + num7; i++)
				{
					if (i < 0 || num8 < 0 || i >= width || num8 >= depth)
					{
						continue;
					}
					LevelGridNode nearestNode2 = GetNearestNode(vector, i, num8, constraint);
					if (nearestNode2 != null)
					{
						float sqrMagnitude2 = ((Vector3)nearestNode2.position - vector).sqrMagnitude;
						if (sqrMagnitude2 < num3 && sqrMagnitude2 < num6)
						{
							num3 = sqrMagnitude2;
							levelGridNode = nearestNode2;
						}
					}
				}
				i = num - num7;
				for (num8 = num2 - num7 + 1; num8 <= num2 + num7 - 1; num8++)
				{
					if (i < 0 || num8 < 0 || i >= width || num8 >= depth)
					{
						continue;
					}
					LevelGridNode nearestNode3 = GetNearestNode(vector, i, num8, constraint);
					if (nearestNode3 != null)
					{
						float sqrMagnitude3 = ((Vector3)nearestNode3.position - vector).sqrMagnitude;
						if (sqrMagnitude3 < num3 && sqrMagnitude3 < num6)
						{
							num3 = sqrMagnitude3;
							levelGridNode = nearestNode3;
						}
					}
				}
				i = num + num7;
				for (num8 = num2 - num7 + 1; num8 <= num2 + num7 - 1; num8++)
				{
					if (i < 0 || num8 < 0 || i >= width || num8 >= depth)
					{
						continue;
					}
					LevelGridNode nearestNode4 = GetNearestNode(vector, i, num8, constraint);
					if (nearestNode4 != null)
					{
						float sqrMagnitude4 = ((Vector3)nearestNode4.position - vector).sqrMagnitude;
						if (sqrMagnitude4 < num3 && sqrMagnitude4 < num6)
						{
							num3 = sqrMagnitude4;
							levelGridNode = nearestNode4;
						}
					}
				}
				if (levelGridNode != null)
				{
					if (num4 == 0)
					{
						break;
					}
					num4--;
				}
				num7++;
			}
			return new NNInfo(levelGridNode);
		}

		protected override GridNodeBase GetNeighbourAlongDirection(GridNodeBase node, int direction)
		{
			LevelGridNode levelGridNode = node as LevelGridNode;
			if (levelGridNode.GetConnection(direction))
			{
				return nodes[levelGridNode.NodeInGridIndex + neighbourOffsets[direction] + width * depth * levelGridNode.GetConnectionValue(direction)];
			}
			return null;
		}

		public static bool CheckConnection(LevelGridNode node, int dir)
		{
			return node.GetConnection(dir);
		}

		public override void OnDrawGizmos(bool drawNodes)
		{
			if (!drawNodes)
			{
				return;
			}
			base.OnDrawGizmos(false);
			if (nodes == null)
			{
				return;
			}
			PathHandler debugPathData = AstarPath.active.debugPathData;
			for (int i = 0; i < nodes.Length; i++)
			{
				LevelGridNode levelGridNode = nodes[i];
				if (levelGridNode == null || !levelGridNode.Walkable)
				{
					continue;
				}
				Gizmos.color = NodeColor(levelGridNode, AstarPath.active.debugPathData);
				if (AstarPath.active.showSearchTree && AstarPath.active.debugPathData != null)
				{
					if (NavGraph.InSearchTree(levelGridNode, AstarPath.active.debugPath))
					{
						PathNode pathNode = debugPathData.GetPathNode(levelGridNode);
						if (pathNode != null && pathNode.parent != null)
						{
							Gizmos.DrawLine((Vector3)levelGridNode.position, (Vector3)pathNode.parent.node.position);
						}
					}
					continue;
				}
				for (int j = 0; j < 4; j++)
				{
					int connectionValue = levelGridNode.GetConnectionValue(j);
					if (connectionValue == 255)
					{
						continue;
					}
					int num = levelGridNode.NodeInGridIndex + neighbourOffsets[j] + width * depth * connectionValue;
					if (num >= 0 && num < nodes.Length)
					{
						GraphNode graphNode = nodes[num];
						if (graphNode != null)
						{
							Gizmos.DrawLine((Vector3)levelGridNode.position, (Vector3)graphNode.position);
						}
					}
				}
			}
		}

		public override void SerializeExtraInfo(GraphSerializationContext ctx)
		{
			if (nodes == null)
			{
				ctx.writer.Write(-1);
				return;
			}
			ctx.writer.Write(nodes.Length);
			for (int i = 0; i < nodes.Length; i++)
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
			nodes = new LevelGridNode[num];
			for (int i = 0; i < nodes.Length; i++)
			{
				if (ctx.reader.ReadInt32() != -1)
				{
					nodes[i] = new LevelGridNode(active);
					nodes[i].DeserializeNode(ctx);
				}
				else
				{
					nodes[i] = null;
				}
			}
		}

		public override void PostDeserialization()
		{
			GenerateMatrix();
			lastScannedWidth = width;
			lastScannedDepth = depth;
			SetUpOffsetsAndCosts();
			if (nodes == null || nodes.Length == 0)
			{
				return;
			}
			LevelGridNode.SetGridGraph(AstarPath.active.astarData.GetGraphIndex(this), this);
			for (int i = 0; i < depth; i++)
			{
				for (int j = 0; j < width; j++)
				{
					for (int k = 0; k < layerCount; k++)
					{
						LevelGridNode levelGridNode = nodes[i * width + j + width * depth * k];
						if (levelGridNode != null)
						{
							levelGridNode.NodeInGridIndex = i * width + j;
						}
					}
				}
			}
		}
	}
}
