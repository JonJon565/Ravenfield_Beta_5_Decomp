using System;
using System.Collections.Generic;
using Pathfinding.Serialization;
using Pathfinding.Serialization.JsonFx;
using Pathfinding.Util;
using UnityEngine;

namespace Pathfinding
{
	[JsonOptIn]
	public class GridGraph : NavGraph, IUpdatableGraph, IRaycastableGraph
	{
		public class TextureData
		{
			public enum ChannelUse
			{
				None = 0,
				Penalty = 1,
				Position = 2,
				WalkablePenalty = 3
			}

			public bool enabled;

			public Texture2D source;

			public float[] factors = new float[3];

			public ChannelUse[] channels = new ChannelUse[3];

			private Color32[] data;

			public void Initialize()
			{
				if (!enabled || !(source != null))
				{
					return;
				}
				for (int i = 0; i < channels.Length; i++)
				{
					if (channels[i] != 0)
					{
						try
						{
							data = source.GetPixels32();
							break;
						}
						catch (UnityException ex)
						{
							Debug.LogWarning(ex.ToString());
							data = null;
							break;
						}
					}
				}
			}

			public void Apply(GridNode node, int x, int z)
			{
				if (enabled && data != null && x < source.width && z < source.height)
				{
					Color32 color = data[z * source.width + x];
					if (channels[0] != 0)
					{
						ApplyChannel(node, x, z, color.r, channels[0], factors[0]);
					}
					if (channels[1] != 0)
					{
						ApplyChannel(node, x, z, color.g, channels[1], factors[1]);
					}
					if (channels[2] != 0)
					{
						ApplyChannel(node, x, z, color.b, channels[2], factors[2]);
					}
				}
			}

			private void ApplyChannel(GridNode node, int x, int z, int value, ChannelUse channelUse, float factor)
			{
				switch (channelUse)
				{
				case ChannelUse.Penalty:
					node.Penalty += (uint)Mathf.RoundToInt((float)value * factor);
					break;
				case ChannelUse.Position:
					node.position = GridNode.GetGridGraph(node.GraphIndex).GraphPointToWorld(x, z, value);
					break;
				case ChannelUse.WalkablePenalty:
					if (value == 0)
					{
						node.Walkable = false;
					}
					else
					{
						node.Penalty += (uint)Mathf.RoundToInt((float)(value - 1) * factor);
					}
					break;
				}
			}
		}

		public const int getNearestForceOverlap = 2;

		public int width;

		public int depth;

		[JsonMember]
		public float aspectRatio = 1f;

		[JsonMember]
		public float isometricAngle;

		[JsonMember]
		public bool uniformEdgeCosts;

		[JsonMember]
		public Vector3 rotation;

		public Bounds bounds;

		[JsonMember]
		public Vector3 center;

		[JsonMember]
		public Vector2 unclampedSize;

		[JsonMember]
		public float nodeSize = 1f;

		[JsonMember]
		public GraphCollision collision;

		[JsonMember]
		public float maxClimb = 0.4f;

		[JsonMember]
		public int maxClimbAxis = 1;

		[JsonMember]
		public float maxSlope = 90f;

		[JsonMember]
		public int erodeIterations;

		[JsonMember]
		public bool erosionUseTags;

		[JsonMember]
		public int erosionFirstTag = 1;

		[JsonMember]
		public bool autoLinkGrids;

		[JsonMember]
		public float autoLinkDistLimit = 10f;

		[JsonMember]
		public NumNeighbours neighbours = NumNeighbours.Eight;

		[JsonMember]
		public bool cutCorners = true;

		[JsonMember]
		public float penaltyPositionOffset;

		[JsonMember]
		public bool penaltyPosition;

		[JsonMember]
		public float penaltyPositionFactor = 1f;

		[JsonMember]
		public bool penaltyAngle;

		[JsonMember]
		public float penaltyAngleFactor = 100f;

		[JsonMember]
		public float penaltyAnglePower = 1f;

		[JsonMember]
		public bool useJumpPointSearch;

		[JsonMember]
		public TextureData textureData = new TextureData();

		[NonSerialized]
		public readonly int[] neighbourOffsets = new int[8];

		[NonSerialized]
		public readonly uint[] neighbourCosts = new uint[8];

		[NonSerialized]
		public readonly int[] neighbourXOffsets = new int[8];

		[NonSerialized]
		public readonly int[] neighbourZOffsets = new int[8];

		internal static readonly int[] hexagonNeighbourIndices = new int[6] { 0, 1, 2, 3, 5, 7 };

		public GridNode[] nodes;

		public virtual bool uniformWidthDepthGrid
		{
			get
			{
				return true;
			}
		}

		public bool useRaycastNormal
		{
			get
			{
				return Math.Abs(90f - maxSlope) > float.Epsilon;
			}
		}

		public Vector2 size { get; protected set; }

		public Matrix4x4 boundsMatrix { get; protected set; }

		public int Width
		{
			get
			{
				return width;
			}
			set
			{
				width = value;
			}
		}

		public int Depth
		{
			get
			{
				return depth;
			}
			set
			{
				depth = value;
			}
		}

		public GridGraph()
		{
			unclampedSize = new Vector2(10f, 10f);
			nodeSize = 1f;
			collision = new GraphCollision();
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			RemoveGridGraphFromStatic();
		}

		private void RemoveGridGraphFromStatic()
		{
			GridNode.SetGridGraph(AstarPath.active.astarData.GetGraphIndex(this), null);
		}

		public override int CountNodes()
		{
			return nodes.Length;
		}

		public override void GetNodes(GraphNodeDelegateCancelable del)
		{
			if (nodes != null)
			{
				for (int i = 0; i < nodes.Length && del(nodes[i]); i++)
				{
				}
			}
		}

		public void RelocateNodes(Vector3 center, Quaternion rotation, float nodeSize, float aspectRatio = 1f, float isometricAngle = 0f)
		{
			Matrix4x4 oldMatrix = matrix;
			this.center = center;
			this.rotation = rotation.eulerAngles;
			this.nodeSize = nodeSize;
			this.aspectRatio = aspectRatio;
			this.isometricAngle = isometricAngle;
			UpdateSizeFromWidthDepth();
			RelocateNodes(oldMatrix, matrix);
		}

		public Int3 GraphPointToWorld(int x, int z, float height)
		{
			return (Int3)matrix.MultiplyPoint3x4(new Vector3((float)x + 0.5f, height, (float)z + 0.5f));
		}

		public uint GetConnectionCost(int dir)
		{
			return neighbourCosts[dir];
		}

		public GridNode GetNodeConnection(GridNode node, int dir)
		{
			if (!node.GetConnectionInternal(dir))
			{
				return null;
			}
			if (!node.EdgeNode)
			{
				return nodes[node.NodeInGridIndex + neighbourOffsets[dir]];
			}
			int nodeInGridIndex = node.NodeInGridIndex;
			int num = nodeInGridIndex / Width;
			int x = nodeInGridIndex - num * Width;
			return GetNodeConnection(nodeInGridIndex, x, num, dir);
		}

		public bool HasNodeConnection(GridNode node, int dir)
		{
			if (!node.GetConnectionInternal(dir))
			{
				return false;
			}
			if (!node.EdgeNode)
			{
				return true;
			}
			int nodeInGridIndex = node.NodeInGridIndex;
			int num = nodeInGridIndex / Width;
			int x = nodeInGridIndex - num * Width;
			return HasNodeConnection(nodeInGridIndex, x, num, dir);
		}

		public void SetNodeConnection(GridNode node, int dir, bool value)
		{
			int nodeInGridIndex = node.NodeInGridIndex;
			int num = nodeInGridIndex / Width;
			int x = nodeInGridIndex - num * Width;
			SetNodeConnection(nodeInGridIndex, x, num, dir, value);
		}

		private GridNode GetNodeConnection(int index, int x, int z, int dir)
		{
			if (!nodes[index].GetConnectionInternal(dir))
			{
				return null;
			}
			int num = x + neighbourXOffsets[dir];
			if (num < 0 || num >= Width)
			{
				return null;
			}
			int num2 = z + neighbourZOffsets[dir];
			if (num2 < 0 || num2 >= Depth)
			{
				return null;
			}
			int num3 = index + neighbourOffsets[dir];
			return nodes[num3];
		}

		public void SetNodeConnection(int index, int x, int z, int dir, bool value)
		{
			nodes[index].SetConnectionInternal(dir, value);
		}

		public bool HasNodeConnection(int index, int x, int z, int dir)
		{
			if (!nodes[index].GetConnectionInternal(dir))
			{
				return false;
			}
			int num = x + neighbourXOffsets[dir];
			if (num < 0 || num >= Width)
			{
				return false;
			}
			int num2 = z + neighbourZOffsets[dir];
			if (num2 < 0 || num2 >= Depth)
			{
				return false;
			}
			return true;
		}

		public void UpdateSizeFromWidthDepth()
		{
			unclampedSize = new Vector2(width, depth) * nodeSize;
			GenerateMatrix();
		}

		public void GenerateMatrix()
		{
			Vector2 vector = unclampedSize;
			vector.x *= Mathf.Sign(vector.x);
			vector.y *= Mathf.Sign(vector.y);
			nodeSize = Mathf.Clamp(nodeSize, vector.x / 1024f, float.PositiveInfinity);
			nodeSize = Mathf.Clamp(nodeSize, vector.y / 1024f, float.PositiveInfinity);
			vector.x = ((!(vector.x < nodeSize)) ? vector.x : nodeSize);
			vector.y = ((!(vector.y < nodeSize)) ? vector.y : nodeSize);
			size = vector;
			Matrix4x4 matrix4x = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 45f, 0f), Vector3.one);
			matrix4x = Matrix4x4.Scale(new Vector3(Mathf.Cos((float)Math.PI / 180f * isometricAngle), 1f, 1f)) * matrix4x;
			matrix4x = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, -45f, 0f), Vector3.one) * matrix4x;
			boundsMatrix = Matrix4x4.TRS(center, Quaternion.Euler(rotation), new Vector3(aspectRatio, 1f, 1f)) * matrix4x;
			width = Mathf.FloorToInt(size.x / nodeSize);
			depth = Mathf.FloorToInt(size.y / nodeSize);
			if (Mathf.Approximately(size.x / nodeSize, Mathf.CeilToInt(size.x / nodeSize)))
			{
				width = Mathf.CeilToInt(size.x / nodeSize);
			}
			if (Mathf.Approximately(size.y / nodeSize, Mathf.CeilToInt(size.y / nodeSize)))
			{
				depth = Mathf.CeilToInt(size.y / nodeSize);
			}
			Matrix4x4 matrix4x2 = Matrix4x4.TRS(boundsMatrix.MultiplyPoint3x4(-new Vector3(size.x, 0f, size.y) * 0.5f), Quaternion.Euler(rotation), new Vector3(nodeSize * aspectRatio, 1f, nodeSize)) * matrix4x;
			SetMatrix(matrix4x2);
		}

		public override NNInfo GetNearest(Vector3 position, NNConstraint constraint, GraphNode hint)
		{
			if (nodes == null || depth * width != nodes.Length)
			{
				return default(NNInfo);
			}
			position = inverseMatrix.MultiplyPoint3x4(position);
			float num = position.x - 0.5f;
			float num2 = position.z - 0.5f;
			int num3 = Mathf.Clamp(Mathf.RoundToInt(num), 0, width - 1);
			int num4 = Mathf.Clamp(Mathf.RoundToInt(num2), 0, depth - 1);
			NNInfo result = new NNInfo(nodes[num4 * width + num3]);
			float y = inverseMatrix.MultiplyPoint3x4((Vector3)nodes[num4 * width + num3].position).y;
			result.clampedPosition = matrix.MultiplyPoint3x4(new Vector3(Mathf.Clamp(num, (float)num3 - 0.5f, (float)num3 + 0.5f) + 0.5f, y, Mathf.Clamp(num2, (float)num4 - 0.5f, (float)num4 + 0.5f) + 0.5f));
			return result;
		}

		public override NNInfo GetNearestForce(Vector3 position, NNConstraint constraint)
		{
			if (nodes == null || depth * width != nodes.Length)
			{
				return default(NNInfo);
			}
			Vector3 vector = position;
			position = inverseMatrix.MultiplyPoint3x4(position);
			float num = position.x - 0.5f;
			float num2 = position.z - 0.5f;
			int num3 = Mathf.Clamp(Mathf.RoundToInt(num), 0, width - 1);
			int num4 = Mathf.Clamp(Mathf.RoundToInt(num2), 0, depth - 1);
			GridNode gridNode = nodes[num3 + num4 * width];
			GridNode gridNode2 = null;
			float num5 = float.PositiveInfinity;
			int num6 = 2;
			Vector3 clampedPosition = Vector3.zero;
			NNInfo result = new NNInfo(null);
			if (constraint.Suitable(gridNode))
			{
				gridNode2 = gridNode;
				num5 = ((Vector3)gridNode2.position - vector).sqrMagnitude;
				float y = inverseMatrix.MultiplyPoint3x4((Vector3)gridNode.position).y;
				clampedPosition = matrix.MultiplyPoint3x4(new Vector3(Mathf.Clamp(num, (float)num3 - 0.5f, (float)num3 + 0.5f) + 0.5f, y, Mathf.Clamp(num2, (float)num4 - 0.5f, (float)num4 + 0.5f) + 0.5f));
			}
			if (gridNode2 != null)
			{
				result.node = gridNode2;
				result.clampedPosition = clampedPosition;
				if (num6 == 0)
				{
					return result;
				}
				num6--;
			}
			float num7 = ((!constraint.constrainDistance) ? float.PositiveInfinity : AstarPath.active.maxNearestNodeDistance);
			float num8 = num7 * num7;
			int num9 = 1;
			while (true)
			{
				if (nodeSize * (float)num9 > num7)
				{
					result.node = gridNode2;
					result.clampedPosition = clampedPosition;
					return result;
				}
				bool flag = false;
				int num10 = num4 + num9;
				int num11 = num10 * width;
				int i;
				for (i = num3 - num9; i <= num3 + num9; i++)
				{
					if (i < 0 || num10 < 0 || i >= width || num10 >= depth)
					{
						continue;
					}
					flag = true;
					if (constraint.Suitable(nodes[i + num11]))
					{
						float sqrMagnitude = ((Vector3)nodes[i + num11].position - vector).sqrMagnitude;
						if (sqrMagnitude < num5 && sqrMagnitude < num8)
						{
							num5 = sqrMagnitude;
							gridNode2 = nodes[i + num11];
							clampedPosition = matrix.MultiplyPoint3x4(new Vector3(Mathf.Clamp(num, (float)i - 0.5f, (float)i + 0.5f) + 0.5f, inverseMatrix.MultiplyPoint3x4((Vector3)gridNode2.position).y, Mathf.Clamp(num2, (float)num10 - 0.5f, (float)num10 + 0.5f) + 0.5f));
						}
					}
				}
				num10 = num4 - num9;
				num11 = num10 * width;
				for (i = num3 - num9; i <= num3 + num9; i++)
				{
					if (i < 0 || num10 < 0 || i >= width || num10 >= depth)
					{
						continue;
					}
					flag = true;
					if (constraint.Suitable(nodes[i + num11]))
					{
						float sqrMagnitude2 = ((Vector3)nodes[i + num11].position - vector).sqrMagnitude;
						if (sqrMagnitude2 < num5 && sqrMagnitude2 < num8)
						{
							num5 = sqrMagnitude2;
							gridNode2 = nodes[i + num11];
							clampedPosition = matrix.MultiplyPoint3x4(new Vector3(Mathf.Clamp(num, (float)i - 0.5f, (float)i + 0.5f) + 0.5f, inverseMatrix.MultiplyPoint3x4((Vector3)gridNode2.position).y, Mathf.Clamp(num2, (float)num10 - 0.5f, (float)num10 + 0.5f) + 0.5f));
						}
					}
				}
				i = num3 - num9;
				for (num10 = num4 - num9 + 1; num10 <= num4 + num9 - 1; num10++)
				{
					if (i < 0 || num10 < 0 || i >= width || num10 >= depth)
					{
						continue;
					}
					flag = true;
					if (constraint.Suitable(nodes[i + num10 * width]))
					{
						float sqrMagnitude3 = ((Vector3)nodes[i + num10 * width].position - vector).sqrMagnitude;
						if (sqrMagnitude3 < num5 && sqrMagnitude3 < num8)
						{
							num5 = sqrMagnitude3;
							gridNode2 = nodes[i + num10 * width];
							clampedPosition = matrix.MultiplyPoint3x4(new Vector3(Mathf.Clamp(num, (float)i - 0.5f, (float)i + 0.5f) + 0.5f, inverseMatrix.MultiplyPoint3x4((Vector3)gridNode2.position).y, Mathf.Clamp(num2, (float)num10 - 0.5f, (float)num10 + 0.5f) + 0.5f));
						}
					}
				}
				i = num3 + num9;
				for (num10 = num4 - num9 + 1; num10 <= num4 + num9 - 1; num10++)
				{
					if (i < 0 || num10 < 0 || i >= width || num10 >= depth)
					{
						continue;
					}
					flag = true;
					if (constraint.Suitable(nodes[i + num10 * width]))
					{
						float sqrMagnitude4 = ((Vector3)nodes[i + num10 * width].position - vector).sqrMagnitude;
						if (sqrMagnitude4 < num5 && sqrMagnitude4 < num8)
						{
							num5 = sqrMagnitude4;
							gridNode2 = nodes[i + num10 * width];
							clampedPosition = matrix.MultiplyPoint3x4(new Vector3(Mathf.Clamp(num, (float)i - 0.5f, (float)i + 0.5f) + 0.5f, inverseMatrix.MultiplyPoint3x4((Vector3)gridNode2.position).y, Mathf.Clamp(num2, (float)num10 - 0.5f, (float)num10 + 0.5f) + 0.5f));
						}
					}
				}
				if (gridNode2 != null)
				{
					if (num6 == 0)
					{
						result.node = gridNode2;
						result.clampedPosition = clampedPosition;
						return result;
					}
					num6--;
				}
				if (!flag)
				{
					break;
				}
				num9++;
			}
			result.node = gridNode2;
			result.clampedPosition = clampedPosition;
			return result;
		}

		public virtual void SetUpOffsetsAndCosts()
		{
			neighbourOffsets[0] = -width;
			neighbourOffsets[1] = 1;
			neighbourOffsets[2] = width;
			neighbourOffsets[3] = -1;
			neighbourOffsets[4] = -width + 1;
			neighbourOffsets[5] = width + 1;
			neighbourOffsets[6] = width - 1;
			neighbourOffsets[7] = -width - 1;
			uint num = (uint)Mathf.RoundToInt(nodeSize * 1000f);
			uint num2 = ((!uniformEdgeCosts) ? ((uint)Mathf.RoundToInt(nodeSize * Mathf.Sqrt(2f) * 1000f)) : num);
			neighbourCosts[0] = num;
			neighbourCosts[1] = num;
			neighbourCosts[2] = num;
			neighbourCosts[3] = num;
			neighbourCosts[4] = num2;
			neighbourCosts[5] = num2;
			neighbourCosts[6] = num2;
			neighbourCosts[7] = num2;
			neighbourXOffsets[0] = 0;
			neighbourXOffsets[1] = 1;
			neighbourXOffsets[2] = 0;
			neighbourXOffsets[3] = -1;
			neighbourXOffsets[4] = 1;
			neighbourXOffsets[5] = 1;
			neighbourXOffsets[6] = -1;
			neighbourXOffsets[7] = -1;
			neighbourZOffsets[0] = -1;
			neighbourZOffsets[1] = 0;
			neighbourZOffsets[2] = 1;
			neighbourZOffsets[3] = 0;
			neighbourZOffsets[4] = -1;
			neighbourZOffsets[5] = 1;
			neighbourZOffsets[6] = 1;
			neighbourZOffsets[7] = -1;
		}

		public override void ScanInternal(OnScanStatus statusCallback)
		{
			AstarPath.OnPostScan = (OnScanDelegate)Delegate.Combine(AstarPath.OnPostScan, new OnScanDelegate(OnPostScan));
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
			if (useJumpPointSearch)
			{
				Debug.LogError("Trying to use Jump Point Search, but support for it is not enabled. Please enable it in the inspector (Grid Graph settings).");
			}
			SetUpOffsetsAndCosts();
			int num = AstarPath.active.astarData.GetGraphIndex(this);
			GridNode.SetGridGraph(num, this);
			nodes = new GridNode[width * depth];
			for (int i = 0; i < nodes.Length; i++)
			{
				nodes[i] = new GridNode(active);
				nodes[i].GraphIndex = (uint)num;
			}
			if (collision == null)
			{
				collision = new GraphCollision();
			}
			collision.Initialize(matrix, nodeSize);
			textureData.Initialize();
			for (int j = 0; j < depth; j++)
			{
				for (int k = 0; k < width; k++)
				{
					GridNode gridNode = nodes[j * width + k];
					gridNode.NodeInGridIndex = j * width + k;
					UpdateNodePositionCollision(gridNode, k, j);
					textureData.Apply(gridNode, k, j);
				}
			}
			for (int l = 0; l < depth; l++)
			{
				for (int m = 0; m < width; m++)
				{
					GridNode node = nodes[l * width + m];
					CalculateConnections(m, l, node);
				}
			}
			ErodeWalkableArea();
		}

		public virtual void UpdateNodePositionCollision(GridNode node, int x, int z, bool resetPenalty = true)
		{
			node.position = GraphPointToWorld(x, z, 0f);
			RaycastHit hit;
			bool walkable;
			Vector3 vector = collision.CheckHeight((Vector3)node.position, out hit, out walkable);
			node.position = (Int3)vector;
			if (resetPenalty)
			{
				node.Penalty = initialPenalty;
				if (penaltyPosition)
				{
					node.Penalty += (uint)Mathf.RoundToInt(((float)node.position.y - penaltyPositionOffset) * penaltyPositionFactor);
				}
			}
			if (walkable && useRaycastNormal && collision.heightCheck && hit.normal != Vector3.zero)
			{
				float num = Vector3.Dot(hit.normal.normalized, collision.up);
				if (penaltyAngle && resetPenalty)
				{
					node.Penalty += (uint)Mathf.RoundToInt((1f - Mathf.Pow(num, penaltyAnglePower)) * penaltyAngleFactor);
				}
				float num2 = Mathf.Cos(maxSlope * ((float)Math.PI / 180f));
				if (num < num2)
				{
					walkable = false;
				}
			}
			node.Walkable = walkable && collision.Check((Vector3)node.position);
			node.WalkableErosion = node.Walkable;
		}

		public virtual void ErodeWalkableArea()
		{
			ErodeWalkableArea(0, 0, Width, Depth);
		}

		private bool ErosionAnyFalseConnections(GridNode node)
		{
			if (neighbours == NumNeighbours.Six)
			{
				for (int i = 0; i < 6; i++)
				{
					if (!HasNodeConnection(node, hexagonNeighbourIndices[i]))
					{
						return true;
					}
				}
			}
			else
			{
				for (int j = 0; j < 4; j++)
				{
					if (!HasNodeConnection(node, j))
					{
						return true;
					}
				}
			}
			return false;
		}

		public virtual void ErodeWalkableArea(int xmin, int zmin, int xmax, int zmax)
		{
			xmin = Mathf.Clamp(xmin, 0, Width);
			xmax = Mathf.Clamp(xmax, 0, Width);
			zmin = Mathf.Clamp(zmin, 0, Depth);
			zmax = Mathf.Clamp(zmax, 0, Depth);
			if (!erosionUseTags)
			{
				for (int i = 0; i < erodeIterations; i++)
				{
					for (int j = zmin; j < zmax; j++)
					{
						for (int k = xmin; k < xmax; k++)
						{
							GridNode gridNode = nodes[j * Width + k];
							if (gridNode.Walkable && ErosionAnyFalseConnections(gridNode))
							{
								gridNode.Walkable = false;
							}
						}
					}
					for (int l = zmin; l < zmax; l++)
					{
						for (int m = xmin; m < xmax; m++)
						{
							GridNode node = nodes[l * Width + m];
							CalculateConnections(m, l, node);
						}
					}
				}
				return;
			}
			if (erodeIterations + erosionFirstTag > 31)
			{
				Debug.LogError("Too few tags available for " + erodeIterations + " erode iterations and starting with tag " + erosionFirstTag + " (erodeIterations+erosionFirstTag > 31)");
				return;
			}
			if (erosionFirstTag <= 0)
			{
				Debug.LogError("First erosion tag must be greater or equal to 1");
				return;
			}
			for (int n = 0; n < erodeIterations; n++)
			{
				for (int num = zmin; num < zmax; num++)
				{
					for (int num2 = xmin; num2 < xmax; num2++)
					{
						GridNode gridNode2 = nodes[num * width + num2];
						if (gridNode2.Walkable && gridNode2.Tag >= erosionFirstTag && gridNode2.Tag < erosionFirstTag + n)
						{
							if (neighbours == NumNeighbours.Six)
							{
								for (int num3 = 0; num3 < 6; num3++)
								{
									GridNode nodeConnection = GetNodeConnection(gridNode2, hexagonNeighbourIndices[num3]);
									if (nodeConnection != null)
									{
										uint tag = nodeConnection.Tag;
										if (tag > erosionFirstTag + n || tag < erosionFirstTag)
										{
											nodeConnection.Tag = (uint)(erosionFirstTag + n);
										}
									}
								}
								continue;
							}
							for (int num4 = 0; num4 < 4; num4++)
							{
								GridNode nodeConnection2 = GetNodeConnection(gridNode2, num4);
								if (nodeConnection2 != null)
								{
									uint tag2 = nodeConnection2.Tag;
									if (tag2 > erosionFirstTag + n || tag2 < erosionFirstTag)
									{
										nodeConnection2.Tag = (uint)(erosionFirstTag + n);
									}
								}
							}
						}
						else if (gridNode2.Walkable && n == 0 && ErosionAnyFalseConnections(gridNode2))
						{
							gridNode2.Tag = (uint)(erosionFirstTag + n);
						}
					}
				}
			}
		}

		public virtual bool IsValidConnection(GridNode n1, GridNode n2)
		{
			if (!n1.Walkable || !n2.Walkable)
			{
				return false;
			}
			return maxClimb <= 0f || (float)Math.Abs(n1.position[maxClimbAxis] - n2.position[maxClimbAxis]) <= maxClimb * 1000f;
		}

		public static void CalculateConnections(GridNode node)
		{
			GridGraph gridGraph = AstarData.GetGraph(node) as GridGraph;
			if (gridGraph != null)
			{
				int nodeInGridIndex = node.NodeInGridIndex;
				int x = nodeInGridIndex % gridGraph.width;
				int z = nodeInGridIndex / gridGraph.width;
				gridGraph.CalculateConnections(x, z, node);
			}
		}

		[Obsolete("CalculateConnections no longer takes a node array, it just uses the one on the graph")]
		public virtual void CalculateConnections(GridNode[] nodes, int x, int z, GridNode node)
		{
			CalculateConnections(x, z, node);
		}

		public virtual void CalculateConnections(int x, int z, GridNode node)
		{
			if (!node.Walkable)
			{
				node.ResetConnectionsInternal();
				return;
			}
			int nodeInGridIndex = node.NodeInGridIndex;
			if (neighbours == NumNeighbours.Four || neighbours == NumNeighbours.Eight)
			{
				int num = 0;
				for (int i = 0; i < 4; i++)
				{
					int num2 = x + neighbourXOffsets[i];
					int num3 = z + neighbourZOffsets[i];
					if ((num2 >= 0 && num3 >= 0) & (num2 < width) & (num3 < depth))
					{
						GridNode n = nodes[nodeInGridIndex + neighbourOffsets[i]];
						if (IsValidConnection(node, n))
						{
							num |= 1 << (i & 0x1F);
						}
					}
				}
				int num4 = 0;
				if (neighbours == NumNeighbours.Eight)
				{
					if (cutCorners)
					{
						for (int j = 0; j < 4; j++)
						{
							if ((((num >> j) | (num >> j + 1) | (num >> j + 1 - 4)) & 1) == 0)
							{
								continue;
							}
							int num5 = j + 4;
							int num6 = x + neighbourXOffsets[num5];
							int num7 = z + neighbourZOffsets[num5];
							if ((num6 >= 0 && num7 >= 0) & (num6 < width) & (num7 < depth))
							{
								GridNode n2 = nodes[nodeInGridIndex + neighbourOffsets[num5]];
								if (IsValidConnection(node, n2))
								{
									num4 |= 1 << (num5 & 0x1F);
								}
							}
						}
					}
					else
					{
						for (int k = 0; k < 4; k++)
						{
							if (((uint)(num >> k) & (true ? 1u : 0u)) != 0 && ((uint)((num >> k + 1) | (num >> k + 1 - 4)) & (true ? 1u : 0u)) != 0)
							{
								GridNode n3 = nodes[nodeInGridIndex + neighbourOffsets[k + 4]];
								if (IsValidConnection(node, n3))
								{
									num4 |= 1 << ((k + 4) & 0x1F);
								}
							}
						}
					}
				}
				node.SetAllConnectionInternal(num | num4);
				return;
			}
			node.ResetConnectionsInternal();
			for (int l = 0; l < hexagonNeighbourIndices.Length; l++)
			{
				int num8 = hexagonNeighbourIndices[l];
				int num9 = x + neighbourXOffsets[num8];
				int num10 = z + neighbourZOffsets[num8];
				if ((num9 >= 0 && num10 >= 0) & (num9 < width) & (num10 < depth))
				{
					GridNode n4 = nodes[nodeInGridIndex + neighbourOffsets[num8]];
					node.SetConnectionInternal(num8, IsValidConnection(node, n4));
				}
			}
		}

		public void OnPostScan(AstarPath script)
		{
			AstarPath.OnPostScan = (OnScanDelegate)Delegate.Remove(AstarPath.OnPostScan, new OnScanDelegate(OnPostScan));
			if (!autoLinkGrids || autoLinkDistLimit <= 0f)
			{
				return;
			}
			throw new NotSupportedException();
		}

		public override void OnDrawGizmos(bool drawNodes)
		{
			Gizmos.matrix = boundsMatrix;
			Gizmos.color = Color.white;
			Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 0f, size.y));
			Gizmos.matrix = Matrix4x4.identity;
			if (!drawNodes || nodes == null || nodes.Length != width * depth)
			{
				return;
			}
			PathHandler debugPathData = AstarPath.active.debugPathData;
			bool flag = AstarPath.active.showSearchTree && debugPathData != null;
			for (int i = 0; i < depth; i++)
			{
				for (int j = 0; j < width; j++)
				{
					GridNode gridNode = nodes[i * width + j];
					if (!gridNode.Walkable)
					{
						continue;
					}
					Gizmos.color = NodeColor(gridNode, debugPathData);
					Vector3 from = (Vector3)gridNode.position;
					if (flag)
					{
						if (NavGraph.InSearchTree(gridNode, AstarPath.active.debugPath))
						{
							PathNode pathNode = debugPathData.GetPathNode(gridNode);
							if (pathNode != null && pathNode.parent != null)
							{
								Gizmos.DrawLine(from, (Vector3)pathNode.parent.node.position);
							}
						}
						continue;
					}
					for (int k = 0; k < 8; k++)
					{
						if (gridNode.GetConnectionInternal(k))
						{
							GridNode gridNode2 = nodes[gridNode.NodeInGridIndex + neighbourOffsets[k]];
							Gizmos.DrawLine(from, (Vector3)gridNode2.position);
						}
					}
					if (gridNode.connections != null)
					{
						for (int l = 0; l < gridNode.connections.Length; l++)
						{
							GraphNode graphNode = gridNode.connections[l];
							Gizmos.DrawLine(from, (Vector3)graphNode.position);
						}
					}
				}
			}
		}

		protected static void GetBoundsMinMax(Bounds b, Matrix4x4 matrix, out Vector3 min, out Vector3 max)
		{
			Vector3[] array = new Vector3[8]
			{
				matrix.MultiplyPoint3x4(b.center + new Vector3(b.extents.x, b.extents.y, b.extents.z)),
				matrix.MultiplyPoint3x4(b.center + new Vector3(b.extents.x, b.extents.y, 0f - b.extents.z)),
				matrix.MultiplyPoint3x4(b.center + new Vector3(b.extents.x, 0f - b.extents.y, b.extents.z)),
				matrix.MultiplyPoint3x4(b.center + new Vector3(b.extents.x, 0f - b.extents.y, 0f - b.extents.z)),
				matrix.MultiplyPoint3x4(b.center + new Vector3(0f - b.extents.x, b.extents.y, b.extents.z)),
				matrix.MultiplyPoint3x4(b.center + new Vector3(0f - b.extents.x, b.extents.y, 0f - b.extents.z)),
				matrix.MultiplyPoint3x4(b.center + new Vector3(0f - b.extents.x, 0f - b.extents.y, b.extents.z)),
				matrix.MultiplyPoint3x4(b.center + new Vector3(0f - b.extents.x, 0f - b.extents.y, 0f - b.extents.z))
			};
			min = array[0];
			max = array[0];
			for (int i = 1; i < 8; i++)
			{
				min = Vector3.Min(min, array[i]);
				max = Vector3.Max(max, array[i]);
			}
		}

		public List<GraphNode> GetNodesInArea(Bounds b)
		{
			return GetNodesInArea(b, null);
		}

		public List<GraphNode> GetNodesInArea(GraphUpdateShape shape)
		{
			return GetNodesInArea(shape.GetBounds(), shape);
		}

		private List<GraphNode> GetNodesInArea(Bounds b, GraphUpdateShape shape)
		{
			if (nodes == null || width * depth != nodes.Length)
			{
				return null;
			}
			List<GraphNode> list = ListPool<GraphNode>.Claim();
			Vector3 min;
			Vector3 max;
			GetBoundsMinMax(b, inverseMatrix, out min, out max);
			int xmin = Mathf.RoundToInt(min.x - 0.5f);
			int xmax = Mathf.RoundToInt(max.x - 0.5f);
			int ymin = Mathf.RoundToInt(min.z - 0.5f);
			int ymax = Mathf.RoundToInt(max.z - 0.5f);
			IntRect a = new IntRect(xmin, ymin, xmax, ymax);
			IntRect b2 = new IntRect(0, 0, width - 1, depth - 1);
			IntRect intRect = IntRect.Intersection(a, b2);
			for (int i = intRect.xmin; i <= intRect.xmax; i++)
			{
				for (int j = intRect.ymin; j <= intRect.ymax; j++)
				{
					int num = j * width + i;
					GraphNode graphNode = nodes[num];
					if (b.Contains((Vector3)graphNode.position) && (shape == null || shape.Contains((Vector3)graphNode.position)))
					{
						list.Add(graphNode);
					}
				}
			}
			return list;
		}

		public GraphUpdateThreading CanUpdateAsync(GraphUpdateObject o)
		{
			return GraphUpdateThreading.UnityThread;
		}

		public void UpdateAreaInit(GraphUpdateObject o)
		{
		}

		public void UpdateArea(GraphUpdateObject o)
		{
			if (nodes == null || nodes.Length != width * depth)
			{
				Debug.LogWarning("The Grid Graph is not scanned, cannot update area ");
				return;
			}
			Bounds b = o.bounds;
			Vector3 min;
			Vector3 max;
			GetBoundsMinMax(b, inverseMatrix, out min, out max);
			int xmin = Mathf.RoundToInt(min.x - 0.5f);
			int xmax = Mathf.RoundToInt(max.x - 0.5f);
			int ymin = Mathf.RoundToInt(min.z - 0.5f);
			int ymax = Mathf.RoundToInt(max.z - 0.5f);
			IntRect intRect = new IntRect(xmin, ymin, xmax, ymax);
			IntRect intRect2 = intRect;
			IntRect b2 = new IntRect(0, 0, width - 1, depth - 1);
			IntRect intRect3 = intRect;
			int num = (o.updateErosion ? erodeIterations : 0);
			bool flag = o.updatePhysics || o.modifyWalkability;
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
			for (int i = intRect4.xmin; i <= intRect4.xmax; i++)
			{
				for (int j = intRect4.ymin; j <= intRect4.ymax; j++)
				{
					o.WillUpdateNode(nodes[j * width + i]);
				}
			}
			if (o.updatePhysics && !o.modifyWalkability)
			{
				collision.Initialize(matrix, nodeSize);
				intRect4 = IntRect.Intersection(intRect3, b2);
				for (int k = intRect4.xmin; k <= intRect4.xmax; k++)
				{
					for (int l = intRect4.ymin; l <= intRect4.ymax; l++)
					{
						int num2 = l * width + k;
						GridNode node = nodes[num2];
						UpdateNodePositionCollision(node, k, l, o.resetPenaltyOnPhysics);
					}
				}
			}
			intRect4 = IntRect.Intersection(intRect, b2);
			for (int m = intRect4.xmin; m <= intRect4.xmax; m++)
			{
				for (int n = intRect4.ymin; n <= intRect4.ymax; n++)
				{
					int num3 = n * width + m;
					GridNode gridNode = nodes[num3];
					if (flag)
					{
						gridNode.Walkable = gridNode.WalkableErosion;
						if (o.bounds.Contains((Vector3)gridNode.position))
						{
							o.Apply(gridNode);
						}
						gridNode.WalkableErosion = gridNode.Walkable;
					}
					else if (o.bounds.Contains((Vector3)gridNode.position))
					{
						o.Apply(gridNode);
					}
				}
			}
			if (flag && num == 0)
			{
				intRect4 = IntRect.Intersection(intRect2, b2);
				for (int num4 = intRect4.xmin; num4 <= intRect4.xmax; num4++)
				{
					for (int num5 = intRect4.ymin; num5 <= intRect4.ymax; num5++)
					{
						int num6 = num5 * width + num4;
						GridNode node2 = nodes[num6];
						CalculateConnections(num4, num5, node2);
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
				for (int num7 = a2.xmin; num7 <= a2.xmax; num7++)
				{
					for (int num8 = a2.ymin; num8 <= a2.ymax; num8++)
					{
						int num9 = num8 * width + num7;
						GridNode gridNode2 = nodes[num9];
						bool walkable = gridNode2.Walkable;
						gridNode2.Walkable = gridNode2.WalkableErosion;
						if (!a.Contains(num7, num8))
						{
							gridNode2.TmpWalkable = walkable;
						}
					}
				}
				for (int num10 = a2.xmin; num10 <= a2.xmax; num10++)
				{
					for (int num11 = a2.ymin; num11 <= a2.ymax; num11++)
					{
						int num12 = num11 * width + num10;
						GridNode node3 = nodes[num12];
						CalculateConnections(num10, num11, node3);
					}
				}
				ErodeWalkableArea(a2.xmin, a2.ymin, a2.xmax + 1, a2.ymax + 1);
				for (int num13 = a2.xmin; num13 <= a2.xmax; num13++)
				{
					for (int num14 = a2.ymin; num14 <= a2.ymax; num14++)
					{
						if (!a.Contains(num13, num14))
						{
							int num15 = num14 * width + num13;
							GridNode gridNode3 = nodes[num15];
							gridNode3.Walkable = gridNode3.TmpWalkable;
						}
					}
				}
				for (int num16 = a2.xmin; num16 <= a2.xmax; num16++)
				{
					for (int num17 = a2.ymin; num17 <= a2.ymax; num17++)
					{
						int num18 = num17 * width + num16;
						GridNode node4 = nodes[num18];
						CalculateConnections(num16, num17, node4);
					}
				}
			}
		}

		public bool Linecast(Vector3 _a, Vector3 _b)
		{
			GraphHitInfo hit;
			return Linecast(_a, _b, null, out hit);
		}

		public bool Linecast(Vector3 _a, Vector3 _b, GraphNode hint)
		{
			GraphHitInfo hit;
			return Linecast(_a, _b, hint, out hit);
		}

		public bool Linecast(Vector3 _a, Vector3 _b, GraphNode hint, out GraphHitInfo hit)
		{
			return Linecast(_a, _b, hint, out hit, null);
		}

		protected static float CrossMagnitude(Vector2 a, Vector2 b)
		{
			return a.x * b.y - b.x * a.y;
		}

		protected virtual GridNodeBase GetNeighbourAlongDirection(GridNodeBase node, int direction)
		{
			GridNode gridNode = node as GridNode;
			if (gridNode.GetConnectionInternal(direction))
			{
				return nodes[gridNode.NodeInGridIndex + neighbourOffsets[direction]];
			}
			return null;
		}

		protected bool ClipLineSegmentToBounds(Vector3 a, Vector3 b, out Vector3 outA, out Vector3 outB)
		{
			if (a.x < 0f || a.z < 0f || a.x > (float)width || a.z > (float)depth || b.x < 0f || b.z < 0f || b.x > (float)width || b.z > (float)depth)
			{
				Vector3 vector = new Vector3(0f, 0f, 0f);
				Vector3 vector2 = new Vector3(0f, 0f, depth);
				Vector3 vector3 = new Vector3(width, 0f, depth);
				Vector3 vector4 = new Vector3(width, 0f, 0f);
				int num = 0;
				bool intersects;
				Vector3 vector5 = VectorMath.SegmentIntersectionPointXZ(a, b, vector, vector2, out intersects);
				if (intersects)
				{
					num++;
					if (!VectorMath.RightOrColinearXZ(vector, vector2, a))
					{
						a = vector5;
					}
					else
					{
						b = vector5;
					}
				}
				vector5 = VectorMath.SegmentIntersectionPointXZ(a, b, vector2, vector3, out intersects);
				if (intersects)
				{
					num++;
					if (!VectorMath.RightOrColinearXZ(vector2, vector3, a))
					{
						a = vector5;
					}
					else
					{
						b = vector5;
					}
				}
				vector5 = VectorMath.SegmentIntersectionPointXZ(a, b, vector3, vector4, out intersects);
				if (intersects)
				{
					num++;
					if (!VectorMath.RightOrColinearXZ(vector3, vector4, a))
					{
						a = vector5;
					}
					else
					{
						b = vector5;
					}
				}
				vector5 = VectorMath.SegmentIntersectionPointXZ(a, b, vector4, vector, out intersects);
				if (intersects)
				{
					num++;
					if (!VectorMath.RightOrColinearXZ(vector4, vector, a))
					{
						a = vector5;
					}
					else
					{
						b = vector5;
					}
				}
				if (num == 0)
				{
					outA = Vector3.zero;
					outB = Vector3.zero;
					return false;
				}
			}
			outA = a;
			outB = b;
			return true;
		}

		public bool Linecast(Vector3 _a, Vector3 _b, GraphNode hint, out GraphHitInfo hit, List<GraphNode> trace)
		{
			hit = default(GraphHitInfo);
			hit.origin = _a;
			Vector3 outA = inverseMatrix.MultiplyPoint3x4(_a);
			Vector3 outB = inverseMatrix.MultiplyPoint3x4(_b);
			if (!ClipLineSegmentToBounds(outA, outB, out outA, out outB))
			{
				return false;
			}
			GridNodeBase gridNodeBase = GetNearest(matrix.MultiplyPoint3x4(outA), NNConstraint.None).node as GridNodeBase;
			GridNodeBase gridNodeBase2 = GetNearest(matrix.MultiplyPoint3x4(outB), NNConstraint.None).node as GridNodeBase;
			if (!gridNodeBase.Walkable)
			{
				hit.node = gridNodeBase;
				hit.point = matrix.MultiplyPoint3x4(outA);
				hit.tangentOrigin = hit.point;
				return true;
			}
			Vector2 vector = new Vector2(outA.x, outA.z);
			Vector2 vector2 = new Vector2(outB.x, outB.z);
			vector -= Vector2.one * 0.5f;
			vector2 -= Vector2.one * 0.5f;
			if (gridNodeBase == null || gridNodeBase2 == null)
			{
				hit.node = null;
				hit.point = _a;
				return true;
			}
			Vector2 a = vector2 - vector;
			Int2 @int = new Int2((int)Mathf.Sign(a.x), (int)Mathf.Sign(a.y));
			float num = CrossMagnitude(a, new Vector2(@int.x, @int.y)) * 0.5f;
			int num2;
			int num3;
			if (a.y >= 0f)
			{
				if (a.x >= 0f)
				{
					num2 = 1;
					num3 = 2;
				}
				else
				{
					num2 = 2;
					num3 = 3;
				}
			}
			else if (a.x < 0f)
			{
				num2 = 3;
				num3 = 0;
			}
			else
			{
				num2 = 0;
				num3 = 1;
			}
			GridNodeBase gridNodeBase3 = gridNodeBase;
			while (gridNodeBase3.NodeInGridIndex != gridNodeBase2.NodeInGridIndex)
			{
				if (trace != null)
				{
					trace.Add(gridNodeBase3);
				}
				Vector2 vector3 = new Vector2(gridNodeBase3.NodeInGridIndex % width, gridNodeBase3.NodeInGridIndex / width);
				float num4 = CrossMagnitude(a, vector3 - vector);
				float num5 = num4 + num;
				int num6 = ((!(num5 < 0f)) ? num2 : num3);
				GridNodeBase neighbourAlongDirection = GetNeighbourAlongDirection(gridNodeBase3, num6);
				if (neighbourAlongDirection != null)
				{
					gridNodeBase3 = neighbourAlongDirection;
					continue;
				}
				Vector2 vector4 = vector3 + new Vector2(neighbourXOffsets[num6], neighbourZOffsets[num6]) * 0.5f;
				Vector2 vector5 = ((neighbourXOffsets[num6] != 0) ? new Vector2(0f, 1f) : new Vector2(1f, 0f));
				Vector2 vector6 = VectorMath.LineIntersectionPoint(vector4, vector4 + vector5, vector, vector2);
				Vector3 vector7 = inverseMatrix.MultiplyPoint3x4((Vector3)gridNodeBase3.position);
				Vector3 v = new Vector3(vector6.x + 0.5f, vector7.y, vector6.y + 0.5f);
				Vector3 v2 = new Vector3(vector4.x + 0.5f, vector7.y, vector4.y + 0.5f);
				hit.point = matrix.MultiplyPoint3x4(v);
				hit.tangentOrigin = matrix.MultiplyPoint3x4(v2);
				hit.tangent = matrix.MultiplyVector(new Vector3(vector5.x, 0f, vector5.y));
				hit.node = gridNodeBase3;
				return true;
			}
			if (trace != null)
			{
				trace.Add(gridNodeBase3);
			}
			if (gridNodeBase3 == gridNodeBase2)
			{
				return false;
			}
			hit.point = (Vector3)gridNodeBase3.position;
			hit.tangentOrigin = hit.point;
			return true;
		}

		public bool SnappedLinecast(Vector3 a, Vector3 b, GraphNode hint, out GraphHitInfo hit)
		{
			return Linecast((Vector3)GetNearest(a, NNConstraint.None).node.position, (Vector3)GetNearest(b, NNConstraint.None).node.position, hint, out hit);
		}

		public bool CheckConnection(GridNode node, int dir)
		{
			if (neighbours == NumNeighbours.Eight || neighbours == NumNeighbours.Six || dir < 4)
			{
				return HasNodeConnection(node, dir);
			}
			int num = (dir - 4 - 1) & 3;
			int num2 = (dir - 4 + 1) & 3;
			if (!HasNodeConnection(node, num) || !HasNodeConnection(node, num2))
			{
				return false;
			}
			GridNode gridNode = nodes[node.NodeInGridIndex + neighbourOffsets[num]];
			GridNode gridNode2 = nodes[node.NodeInGridIndex + neighbourOffsets[num2]];
			if (!gridNode.Walkable || !gridNode2.Walkable)
			{
				return false;
			}
			if (!HasNodeConnection(gridNode2, num) || !HasNodeConnection(gridNode, num2))
			{
				return false;
			}
			return true;
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
			nodes = new GridNode[num];
			for (int i = 0; i < nodes.Length; i++)
			{
				nodes[i] = new GridNode(active);
				nodes[i].DeserializeNode(ctx);
			}
		}

		public override void PostDeserialization()
		{
			GenerateMatrix();
			SetUpOffsetsAndCosts();
			if (nodes == null || nodes.Length == 0)
			{
				return;
			}
			if (width * depth != nodes.Length)
			{
				Debug.LogError("Node data did not match with bounds data. Probably a change to the bounds/width/depth data was made after scanning the graph just prior to saving it. Nodes will be discarded");
				nodes = new GridNode[0];
				return;
			}
			GridNode.SetGridGraph(AstarPath.active.astarData.GetGraphIndex(this), this);
			for (int i = 0; i < depth; i++)
			{
				for (int j = 0; j < width; j++)
				{
					GridNode gridNode = nodes[i * width + j];
					if (gridNode == null)
					{
						Debug.LogError("Deserialization Error : Couldn't cast the node to the appropriate type - GridGenerator");
						return;
					}
					gridNode.NodeInGridIndex = i * width + j;
				}
			}
		}
	}
}
