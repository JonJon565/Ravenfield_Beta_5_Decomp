using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Pathfinding.Serialization;
using Pathfinding.Serialization.JsonFx;
using Pathfinding.Util;
using Pathfinding.Voxels;
using UnityEngine;

namespace Pathfinding
{
	[Serializable]
	[JsonOptIn]
	public class RecastGraph : NavGraph, IUpdatableGraph, IRaycastableGraph, INavmesh, INavmeshHolder
	{
		public enum RelevantGraphSurfaceMode
		{
			DoNotRequire = 0,
			OnlyForCompletelyInsideTile = 1,
			RequireForAll = 2
		}

		public class NavmeshTile : INavmesh, INavmeshHolder
		{
			public int[] tris;

			public Int3[] verts;

			public int x;

			public int z;

			public int w;

			public int d;

			public TriangleMeshNode[] nodes;

			public BBTree bbTree;

			public bool flag;

			public void GetTileCoordinates(int tileIndex, out int x, out int z)
			{
				x = this.x;
				z = this.z;
			}

			public int GetVertexArrayIndex(int index)
			{
				return index & 0xFFFFF;
			}

			public Int3 GetVertex(int index)
			{
				int num = index & 0xFFFFF;
				return verts[num];
			}

			public void GetNodes(GraphNodeDelegateCancelable del)
			{
				if (nodes != null)
				{
					for (int i = 0; i < nodes.Length && del(nodes[i]); i++)
					{
					}
				}
			}
		}

		public struct SceneMesh
		{
			public Mesh mesh;

			public Matrix4x4 matrix;

			public Bounds bounds;
		}

		private class CapsuleCache
		{
			public int rows;

			public float height;

			public Vector3[] verts;

			public int[] tris;
		}

		public const int VertexIndexMask = 1048575;

		public const int TileIndexMask = 2047;

		public const int TileIndexOffset = 20;

		public const int BorderVertexMask = 1;

		public const int BorderVertexOffset = 31;

		public bool dynamic = true;

		[JsonMember]
		public float characterRadius = 0.5f;

		[JsonMember]
		public float contourMaxError = 2f;

		[JsonMember]
		public float cellSize = 0.5f;

		[JsonMember]
		public float cellHeight = 0.01f;

		[JsonMember]
		public float walkableHeight = 2f;

		[JsonMember]
		public float walkableClimb = 0.5f;

		[JsonMember]
		public float maxSlope = 30f;

		[JsonMember]
		public float maxEdgeLength = 20f;

		[JsonMember]
		public float minRegionSize = 3f;

		[JsonMember]
		public int editorTileSize = 128;

		[JsonMember]
		public int tileSizeX = 128;

		[JsonMember]
		public int tileSizeZ = 128;

		[JsonMember]
		public bool nearestSearchOnlyXZ;

		[JsonMember]
		public bool useTiles;

		public bool scanEmptyGraph;

		[JsonMember]
		public RelevantGraphSurfaceMode relevantGraphSurfaceMode;

		[JsonMember]
		public bool rasterizeColliders;

		[JsonMember]
		public bool rasterizeMeshes = true;

		[JsonMember]
		public bool rasterizeTerrain = true;

		[JsonMember]
		public bool rasterizeTrees = true;

		[JsonMember]
		public float colliderRasterizeDetail = 10f;

		[JsonMember]
		public Vector3 forcedBoundsCenter;

		[JsonMember]
		public Vector3 forcedBoundsSize = new Vector3(100f, 40f, 100f);

		[JsonMember]
		public LayerMask mask = -1;

		[JsonMember]
		public List<string> tagMask = new List<string>();

		[JsonMember]
		public bool showMeshOutline = true;

		[JsonMember]
		public bool showNodeConnections;

		[JsonMember]
		public bool showMeshSurface;

		[JsonMember]
		public int terrainSampleSize = 3;

		private Voxelize globalVox;

		public int tileXCount;

		public int tileZCount;

		private NavmeshTile[] tiles;

		private bool batchTileUpdate;

		private List<int> batchUpdatedTiles = new List<int>();

		private Dictionary<Int2, int> cachedInt2_int_dict = new Dictionary<Int2, int>();

		private Dictionary<Int3, int> cachedInt3_int_dict = new Dictionary<Int3, int>();

		private readonly int[] BoxColliderTris = new int[36]
		{
			0, 1, 2, 0, 2, 3, 6, 5, 4, 7,
			6, 4, 0, 5, 1, 0, 4, 5, 1, 6,
			2, 1, 5, 6, 2, 7, 3, 2, 6, 7,
			3, 4, 0, 3, 7, 4
		};

		private readonly Vector3[] BoxColliderVerts = new Vector3[8]
		{
			new Vector3(-1f, -1f, -1f),
			new Vector3(1f, -1f, -1f),
			new Vector3(1f, -1f, 1f),
			new Vector3(-1f, -1f, 1f),
			new Vector3(-1f, 1f, -1f),
			new Vector3(1f, 1f, -1f),
			new Vector3(1f, 1f, 1f),
			new Vector3(-1f, 1f, 1f)
		};

		private List<CapsuleCache> capsuleCache = new List<CapsuleCache>();

		public Bounds forcedBounds
		{
			get
			{
				return new Bounds(forcedBoundsCenter, forcedBoundsSize);
			}
		}

		public Int3 GetVertex(int index)
		{
			int num = (index >> 20) & 0x7FF;
			return tiles[num].GetVertex(index);
		}

		public int GetTileIndex(int index)
		{
			return (index >> 20) & 0x7FF;
		}

		public int GetVertexArrayIndex(int index)
		{
			return index & 0xFFFFF;
		}

		public void GetTileCoordinates(int tileIndex, out int x, out int z)
		{
			z = tileIndex / tileXCount;
			x = tileIndex - z * tileXCount;
		}

		public NavmeshTile[] GetTiles()
		{
			return tiles;
		}

		public Bounds GetTileBounds(IntRect rect)
		{
			return GetTileBounds(rect.xmin, rect.ymin, rect.Width, rect.Height);
		}

		public Bounds GetTileBounds(int x, int z, int width = 1, int depth = 1)
		{
			Bounds result = default(Bounds);
			result.SetMinMax(new Vector3((float)(x * tileSizeX) * cellSize, 0f, (float)(z * tileSizeZ) * cellSize) + forcedBounds.min, new Vector3((float)((x + width) * tileSizeX) * cellSize, forcedBounds.size.y, (float)((z + depth) * tileSizeZ) * cellSize) + forcedBounds.min);
			return result;
		}

		public Int2 GetTileCoordinates(Vector3 p)
		{
			p -= forcedBounds.min;
			p.x /= cellSize * (float)tileSizeX;
			p.z /= cellSize * (float)tileSizeZ;
			return new Int2((int)p.x, (int)p.z);
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			TriangleMeshNode.SetNavmeshHolder(active.astarData.GetGraphIndex(this), null);
		}

		public override void RelocateNodes(Matrix4x4 oldMatrix, Matrix4x4 newMatrix)
		{
			if (tiles != null)
			{
				Matrix4x4 inverse = oldMatrix.inverse;
				Matrix4x4 matrix4x = newMatrix * inverse;
				if (tiles.Length > 1)
				{
					throw new Exception("RelocateNodes cannot be used on tiled recast graphs");
				}
				for (int i = 0; i < tiles.Length; i++)
				{
					NavmeshTile navmeshTile = tiles[i];
					if (navmeshTile != null)
					{
						Int3[] verts = navmeshTile.verts;
						for (int j = 0; j < verts.Length; j++)
						{
							verts[j] = (Int3)matrix4x.MultiplyPoint((Vector3)verts[j]);
						}
						for (int k = 0; k < navmeshTile.nodes.Length; k++)
						{
							TriangleMeshNode triangleMeshNode = navmeshTile.nodes[k];
							triangleMeshNode.UpdatePositionFromVertices();
						}
						navmeshTile.bbTree.RebuildFrom(navmeshTile.nodes);
					}
				}
			}
			SetMatrix(newMatrix);
		}

		private static NavmeshTile NewEmptyTile(int x, int z)
		{
			NavmeshTile navmeshTile = new NavmeshTile();
			navmeshTile.x = x;
			navmeshTile.z = z;
			navmeshTile.w = 1;
			navmeshTile.d = 1;
			navmeshTile.verts = new Int3[0];
			navmeshTile.tris = new int[0];
			navmeshTile.nodes = new TriangleMeshNode[0];
			navmeshTile.bbTree = new BBTree();
			return navmeshTile;
		}

		public override void GetNodes(GraphNodeDelegateCancelable del)
		{
			if (tiles == null)
			{
				return;
			}
			for (int i = 0; i < tiles.Length; i++)
			{
				if (tiles[i] == null || tiles[i].x + tiles[i].z * tileXCount != i)
				{
					continue;
				}
				TriangleMeshNode[] nodes = tiles[i].nodes;
				if (nodes != null)
				{
					for (int j = 0; j < nodes.Length && del(nodes[j]); j++)
					{
					}
				}
			}
		}

		public Vector3 ClosestPointOnNode(TriangleMeshNode node, Vector3 pos)
		{
			return Polygon.ClosestPointOnTriangle((Vector3)GetVertex(node.v0), (Vector3)GetVertex(node.v1), (Vector3)GetVertex(node.v2), pos);
		}

		public bool ContainsPoint(TriangleMeshNode node, Vector3 pos)
		{
			if (VectorMath.IsClockwiseXZ((Vector3)GetVertex(node.v0), (Vector3)GetVertex(node.v1), pos) && VectorMath.IsClockwiseXZ((Vector3)GetVertex(node.v1), (Vector3)GetVertex(node.v2), pos) && VectorMath.IsClockwiseXZ((Vector3)GetVertex(node.v2), (Vector3)GetVertex(node.v0), pos))
			{
				return true;
			}
			return false;
		}

		public void SnapForceBoundsToScene()
		{
			List<ExtraMesh> extraMeshes;
			CollectMeshes(out extraMeshes, new Bounds(Vector3.zero, new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity)));
			if (extraMeshes.Count != 0)
			{
				Bounds bounds = extraMeshes[0].bounds;
				for (int i = 1; i < extraMeshes.Count; i++)
				{
					bounds.Encapsulate(extraMeshes[i].bounds);
				}
				forcedBoundsCenter = bounds.center;
				forcedBoundsSize = bounds.size;
			}
		}

		public void GetRecastMeshObjs(Bounds bounds, List<ExtraMesh> buffer)
		{
			List<RecastMeshObj> list = ListPool<RecastMeshObj>.Claim();
			RecastMeshObj.GetAllInBounds(list, bounds);
			Dictionary<Mesh, Vector3[]> dictionary = new Dictionary<Mesh, Vector3[]>();
			Dictionary<Mesh, int[]> dictionary2 = new Dictionary<Mesh, int[]>();
			for (int i = 0; i < list.Count; i++)
			{
				MeshFilter meshFilter = list[i].GetMeshFilter();
				Renderer renderer = ((!(meshFilter != null)) ? null : meshFilter.GetComponent<Renderer>());
				if (meshFilter != null && renderer != null)
				{
					Mesh sharedMesh = meshFilter.sharedMesh;
					ExtraMesh item = default(ExtraMesh);
					item.matrix = renderer.localToWorldMatrix;
					item.original = meshFilter;
					item.area = list[i].area;
					if (dictionary.ContainsKey(sharedMesh))
					{
						item.vertices = dictionary[sharedMesh];
						item.triangles = dictionary2[sharedMesh];
					}
					else
					{
						item.vertices = sharedMesh.vertices;
						item.triangles = sharedMesh.triangles;
						dictionary[sharedMesh] = item.vertices;
						dictionary2[sharedMesh] = item.triangles;
					}
					item.bounds = renderer.bounds;
					buffer.Add(item);
					continue;
				}
				Collider collider = list[i].GetCollider();
				if (collider == null)
				{
					UnityEngine.Debug.LogError("RecastMeshObject (" + list[i].gameObject.name + ") didn't have a collider or MeshFilter+Renderer attached");
					continue;
				}
				ExtraMesh item2 = RasterizeCollider(collider);
				item2.area = list[i].area;
				if (item2.vertices != null)
				{
					buffer.Add(item2);
				}
			}
			capsuleCache.Clear();
			ListPool<RecastMeshObj>.Release(list);
		}

		private static void GetSceneMeshes(Bounds bounds, List<string> tagMask, LayerMask layerMask, List<ExtraMesh> meshes)
		{
			if ((tagMask == null || tagMask.Count <= 0) && (int)layerMask == 0)
			{
				return;
			}
			MeshFilter[] array = UnityEngine.Object.FindObjectsOfType(typeof(MeshFilter)) as MeshFilter[];
			List<MeshFilter> list = new List<MeshFilter>(array.Length / 3);
			foreach (MeshFilter meshFilter in array)
			{
				Renderer component = meshFilter.GetComponent<Renderer>();
				if (component != null && meshFilter.sharedMesh != null && component.enabled && (((1 << meshFilter.gameObject.layer) & (int)layerMask) != 0 || tagMask.Contains(meshFilter.tag)) && meshFilter.GetComponent<RecastMeshObj>() == null)
				{
					list.Add(meshFilter);
				}
			}
			Dictionary<Mesh, Vector3[]> dictionary = new Dictionary<Mesh, Vector3[]>();
			Dictionary<Mesh, int[]> dictionary2 = new Dictionary<Mesh, int[]>();
			bool flag = false;
			for (int j = 0; j < list.Count; j++)
			{
				MeshFilter meshFilter2 = list[j];
				Renderer component2 = meshFilter2.GetComponent<Renderer>();
				if (component2.isPartOfStaticBatch)
				{
					flag = true;
				}
				else if (component2.bounds.Intersects(bounds))
				{
					Mesh sharedMesh = meshFilter2.sharedMesh;
					ExtraMesh item = default(ExtraMesh);
					item.matrix = component2.localToWorldMatrix;
					item.original = meshFilter2;
					if (dictionary.ContainsKey(sharedMesh))
					{
						item.vertices = dictionary[sharedMesh];
						item.triangles = dictionary2[sharedMesh];
					}
					else
					{
						item.vertices = sharedMesh.vertices;
						item.triangles = sharedMesh.triangles;
						dictionary[sharedMesh] = item.vertices;
						dictionary2[sharedMesh] = item.triangles;
					}
					item.bounds = component2.bounds;
					meshes.Add(item);
				}
				if (flag)
				{
					UnityEngine.Debug.LogWarning("Some meshes were statically batched. These meshes can not be used for navmesh calculation due to technical constraints.\nDuring runtime scripts cannot access the data of meshes which have been statically batched.\nOne way to solve this problem is to use cached startup (Save & Load tab in the inspector) to only calculate the graph when the game is not playing.");
				}
			}
		}

		public IntRect GetTouchingTiles(Bounds b)
		{
			b.center -= forcedBounds.min;
			IntRect a = new IntRect(Mathf.FloorToInt(b.min.x / ((float)tileSizeX * cellSize)), Mathf.FloorToInt(b.min.z / ((float)tileSizeZ * cellSize)), Mathf.FloorToInt(b.max.x / ((float)tileSizeX * cellSize)), Mathf.FloorToInt(b.max.z / ((float)tileSizeZ * cellSize)));
			return IntRect.Intersection(a, new IntRect(0, 0, tileXCount - 1, tileZCount - 1));
		}

		public IntRect GetTouchingTilesRound(Bounds b)
		{
			b.center -= forcedBounds.min;
			IntRect a = new IntRect(Mathf.RoundToInt(b.min.x / ((float)tileSizeX * cellSize)), Mathf.RoundToInt(b.min.z / ((float)tileSizeZ * cellSize)), Mathf.RoundToInt(b.max.x / ((float)tileSizeX * cellSize)) - 1, Mathf.RoundToInt(b.max.z / ((float)tileSizeZ * cellSize)) - 1);
			return IntRect.Intersection(a, new IntRect(0, 0, tileXCount - 1, tileZCount - 1));
		}

		public GraphUpdateThreading CanUpdateAsync(GraphUpdateObject o)
		{
			return (!o.updatePhysics) ? GraphUpdateThreading.SeparateThread : GraphUpdateThreading.SeparateAndUnityInit;
		}

		public void UpdateAreaInit(GraphUpdateObject o)
		{
			if (!o.updatePhysics)
			{
				return;
			}
			if (!dynamic)
			{
				throw new Exception("Recast graph must be marked as dynamic to enable graph updates");
			}
			RelevantGraphSurface.UpdateAllPositions();
			IntRect touchingTiles = GetTouchingTiles(o.bounds);
			Bounds tileBounds = GetTileBounds(touchingTiles);
			int num = Mathf.CeilToInt(characterRadius / cellSize);
			int num2 = num + 3;
			tileBounds.Expand(new Vector3(num2, 0f, num2) * cellSize * 2f);
			List<ExtraMesh> extraMeshes;
			CollectMeshes(out extraMeshes, tileBounds);
			Voxelize voxelize = globalVox;
			if (voxelize == null)
			{
				voxelize = new Voxelize(cellHeight, cellSize, walkableClimb, walkableHeight, maxSlope);
				voxelize.maxEdgeLength = maxEdgeLength;
				if (dynamic)
				{
					globalVox = voxelize;
				}
			}
			voxelize.inputExtraMeshes = extraMeshes;
		}

		public void UpdateArea(GraphUpdateObject guo)
		{
			IntRect touchingTiles = GetTouchingTiles(guo.bounds);
			if (!guo.updatePhysics)
			{
				for (int i = touchingTiles.ymin; i <= touchingTiles.ymax; i++)
				{
					for (int j = touchingTiles.xmin; j <= touchingTiles.xmax; j++)
					{
						NavmeshTile graph = tiles[i * tileXCount + j];
						NavMeshGraph.UpdateArea(guo, graph);
					}
				}
				return;
			}
			if (!dynamic)
			{
				throw new Exception("Recast graph must be marked as dynamic to enable graph updates with updatePhysics = true");
			}
			Voxelize voxelize = globalVox;
			if (voxelize == null)
			{
				throw new InvalidOperationException("No Voxelizer object. UpdateAreaInit should have been called before this function.");
			}
			for (int k = touchingTiles.xmin; k <= touchingTiles.xmax; k++)
			{
				for (int l = touchingTiles.ymin; l <= touchingTiles.ymax; l++)
				{
					RemoveConnectionsFromTile(tiles[k + l * tileXCount]);
				}
			}
			for (int m = touchingTiles.xmin; m <= touchingTiles.xmax; m++)
			{
				for (int n = touchingTiles.ymin; n <= touchingTiles.ymax; n++)
				{
					BuildTileMesh(voxelize, m, n);
				}
			}
			uint num = (uint)AstarPath.active.astarData.GetGraphIndex(this);
			for (int num2 = touchingTiles.xmin; num2 <= touchingTiles.xmax; num2++)
			{
				for (int num3 = touchingTiles.ymin; num3 <= touchingTiles.ymax; num3++)
				{
					NavmeshTile navmeshTile = tiles[num2 + num3 * tileXCount];
					GraphNode[] nodes = navmeshTile.nodes;
					for (int num4 = 0; num4 < nodes.Length; num4++)
					{
						nodes[num4].GraphIndex = num;
					}
				}
			}
			touchingTiles = touchingTiles.Expand(1);
			touchingTiles = IntRect.Intersection(touchingTiles, new IntRect(0, 0, tileXCount - 1, tileZCount - 1));
			for (int num5 = touchingTiles.xmin; num5 <= touchingTiles.xmax; num5++)
			{
				for (int num6 = touchingTiles.ymin; num6 <= touchingTiles.ymax; num6++)
				{
					if (num5 < tileXCount - 1 && touchingTiles.Contains(num5 + 1, num6))
					{
						ConnectTiles(tiles[num5 + num6 * tileXCount], tiles[num5 + 1 + num6 * tileXCount]);
					}
					if (num6 < tileZCount - 1 && touchingTiles.Contains(num5, num6 + 1))
					{
						ConnectTiles(tiles[num5 + num6 * tileXCount], tiles[num5 + (num6 + 1) * tileXCount]);
					}
				}
			}
		}

		public void ConnectTileWithNeighbours(NavmeshTile tile)
		{
			if (tile.x > 0)
			{
				int num = tile.x - 1;
				for (int i = tile.z; i < tile.z + tile.d; i++)
				{
					ConnectTiles(tiles[num + i * tileXCount], tile);
				}
			}
			if (tile.x + tile.w < tileXCount)
			{
				int num2 = tile.x + tile.w;
				for (int j = tile.z; j < tile.z + tile.d; j++)
				{
					ConnectTiles(tiles[num2 + j * tileXCount], tile);
				}
			}
			if (tile.z > 0)
			{
				int num3 = tile.z - 1;
				for (int k = tile.x; k < tile.x + tile.w; k++)
				{
					ConnectTiles(tiles[k + num3 * tileXCount], tile);
				}
			}
			if (tile.z + tile.d < tileZCount)
			{
				int num4 = tile.z + tile.d;
				for (int l = tile.x; l < tile.x + tile.w; l++)
				{
					ConnectTiles(tiles[l + num4 * tileXCount], tile);
				}
			}
		}

		public void RemoveConnectionsFromTile(NavmeshTile tile)
		{
			if (tile.x > 0)
			{
				int num = tile.x - 1;
				for (int i = tile.z; i < tile.z + tile.d; i++)
				{
					RemoveConnectionsFromTo(tiles[num + i * tileXCount], tile);
				}
			}
			if (tile.x + tile.w < tileXCount)
			{
				int num2 = tile.x + tile.w;
				for (int j = tile.z; j < tile.z + tile.d; j++)
				{
					RemoveConnectionsFromTo(tiles[num2 + j * tileXCount], tile);
				}
			}
			if (tile.z > 0)
			{
				int num3 = tile.z - 1;
				for (int k = tile.x; k < tile.x + tile.w; k++)
				{
					RemoveConnectionsFromTo(tiles[k + num3 * tileXCount], tile);
				}
			}
			if (tile.z + tile.d < tileZCount)
			{
				int num4 = tile.z + tile.d;
				for (int l = tile.x; l < tile.x + tile.w; l++)
				{
					RemoveConnectionsFromTo(tiles[l + num4 * tileXCount], tile);
				}
			}
		}

		public void RemoveConnectionsFromTo(NavmeshTile a, NavmeshTile b)
		{
			if (a == null || b == null || a == b)
			{
				return;
			}
			int num = b.x + b.z * tileXCount;
			for (int i = 0; i < a.nodes.Length; i++)
			{
				TriangleMeshNode triangleMeshNode = a.nodes[i];
				if (triangleMeshNode.connections == null)
				{
					continue;
				}
				for (int j = 0; j < triangleMeshNode.connections.Length; j++)
				{
					TriangleMeshNode triangleMeshNode2 = triangleMeshNode.connections[j] as TriangleMeshNode;
					if (triangleMeshNode2 != null)
					{
						int vertexIndex = triangleMeshNode2.GetVertexIndex(0);
						vertexIndex = (vertexIndex >> 20) & 0x7FF;
						if (vertexIndex == num)
						{
							triangleMeshNode.RemoveConnection(triangleMeshNode.connections[j]);
							j--;
						}
					}
				}
			}
		}

		public override NNInfo GetNearest(Vector3 position, NNConstraint constraint, GraphNode hint)
		{
			return GetNearestForce(position, null);
		}

		public override NNInfo GetNearestForce(Vector3 position, NNConstraint constraint)
		{
			if (tiles == null)
			{
				return default(NNInfo);
			}
			Vector3 vector = position - forcedBounds.min;
			int value = Mathf.FloorToInt(vector.x / (cellSize * (float)tileSizeX));
			int value2 = Mathf.FloorToInt(vector.z / (cellSize * (float)tileSizeZ));
			value = Mathf.Clamp(value, 0, tileXCount - 1);
			value2 = Mathf.Clamp(value2, 0, tileZCount - 1);
			int num = Math.Max(tileXCount, tileZCount);
			NNInfo nNInfo = default(NNInfo);
			float distance = float.PositiveInfinity;
			bool flag = nearestSearchOnlyXZ || (constraint != null && constraint.distanceXZ);
			for (int i = 0; i < num && (flag || !(distance < (float)(i - 1) * cellSize * (float)Math.Max(tileSizeX, tileSizeZ))); i++)
			{
				int num2 = Math.Min(i + value2 + 1, tileZCount);
				for (int j = Math.Max(-i + value2, 0); j < num2; j++)
				{
					int num3 = Math.Abs(i - Math.Abs(j - value2));
					if (-num3 + value >= 0)
					{
						int num4 = -num3 + value;
						NavmeshTile navmeshTile = tiles[num4 + j * tileXCount];
						if (navmeshTile != null)
						{
							if (flag)
							{
								nNInfo = navmeshTile.bbTree.QueryClosestXZ(position, constraint, ref distance, nNInfo);
								if (distance < float.PositiveInfinity)
								{
									break;
								}
							}
							else
							{
								nNInfo = navmeshTile.bbTree.QueryClosest(position, constraint, ref distance, nNInfo);
							}
						}
					}
					if (num3 == 0 || num3 + value >= tileXCount)
					{
						continue;
					}
					int num5 = num3 + value;
					NavmeshTile navmeshTile2 = tiles[num5 + j * tileXCount];
					if (navmeshTile2 == null)
					{
						continue;
					}
					if (flag)
					{
						nNInfo = navmeshTile2.bbTree.QueryClosestXZ(position, constraint, ref distance, nNInfo);
						if (distance < float.PositiveInfinity)
						{
							break;
						}
					}
					else
					{
						nNInfo = navmeshTile2.bbTree.QueryClosest(position, constraint, ref distance, nNInfo);
					}
				}
			}
			nNInfo.node = nNInfo.constrainedNode;
			nNInfo.constrainedNode = null;
			nNInfo.clampedPosition = nNInfo.constClampedPosition;
			return nNInfo;
		}

		public GraphNode PointOnNavmesh(Vector3 position, NNConstraint constraint)
		{
			if (tiles == null)
			{
				return null;
			}
			Vector3 vector = position - forcedBounds.min;
			int num = Mathf.FloorToInt(vector.x / (cellSize * (float)tileSizeX));
			int num2 = Mathf.FloorToInt(vector.z / (cellSize * (float)tileSizeZ));
			if (num < 0 || num2 < 0 || num >= tileXCount || num2 >= tileZCount)
			{
				return null;
			}
			NavmeshTile navmeshTile = tiles[num + num2 * tileXCount];
			if (navmeshTile != null)
			{
				return navmeshTile.bbTree.QueryInside(position, constraint);
			}
			return null;
		}

		public override void ScanInternal(OnScanStatus statusCallback)
		{
			TriangleMeshNode.SetNavmeshHolder(AstarPath.active.astarData.GetGraphIndex(this), this);
			ScanTiledNavmesh(statusCallback);
		}

		protected void ScanTiledNavmesh(OnScanStatus statusCallback)
		{
			ScanAllTiles(statusCallback);
		}

		protected void ScanAllTiles(OnScanStatus statusCallback)
		{
			int num = (int)(forcedBounds.size.x / cellSize + 0.5f);
			int num2 = (int)(forcedBounds.size.z / cellSize + 0.5f);
			if (!useTiles)
			{
				tileSizeX = num;
				tileSizeZ = num2;
			}
			else
			{
				tileSizeX = editorTileSize;
				tileSizeZ = editorTileSize;
			}
			int num3 = (num + tileSizeX - 1) / tileSizeX;
			int num4 = (num2 + tileSizeZ - 1) / tileSizeZ;
			tileXCount = num3;
			tileZCount = num4;
			if (tileXCount * tileZCount > 2048)
			{
				throw new Exception("Too many tiles (" + tileXCount * tileZCount + ") maximum is " + 2048 + "\nTry disabling ASTAR_RECAST_LARGER_TILES under the 'Optimizations' tab in the A* inspector.");
			}
			tiles = new NavmeshTile[tileXCount * tileZCount];
			if (scanEmptyGraph)
			{
				for (int i = 0; i < num4; i++)
				{
					for (int j = 0; j < num3; j++)
					{
						tiles[i * tileXCount + j] = NewEmptyTile(j, i);
					}
				}
				return;
			}
			Console.WriteLine("Collecting Meshes");
			List<ExtraMesh> extraMeshes;
			CollectMeshes(out extraMeshes, forcedBounds);
			walkableClimb = Mathf.Min(walkableClimb, walkableHeight);
			Voxelize voxelize = new Voxelize(cellHeight, cellSize, walkableClimb, walkableHeight, maxSlope);
			voxelize.inputExtraMeshes = extraMeshes;
			voxelize.maxEdgeLength = maxEdgeLength;
			int num5 = -1;
			Stopwatch stopwatch = Stopwatch.StartNew();
			for (int k = 0; k < num4; k++)
			{
				for (int l = 0; l < num3; l++)
				{
					int num6 = k * tileXCount + l;
					Console.WriteLine("Generating Tile #" + num6 + " of " + num4 * num3);
					if (statusCallback != null && (num6 * 10 / tiles.Length > num5 || stopwatch.ElapsedMilliseconds > 2000))
					{
						num5 = num6 * 10 / tiles.Length;
						stopwatch.Reset();
						stopwatch.Start();
						statusCallback(new Progress(Mathf.Lerp(0.1f, 0.9f, (float)num6 / (float)tiles.Length), "Building Tile " + num6 + "/" + tiles.Length));
					}
					BuildTileMesh(voxelize, l, k);
				}
			}
			Console.WriteLine("Assigning Graph Indices");
			if (statusCallback != null)
			{
				statusCallback(new Progress(0.9f, "Connecting tiles"));
			}
			uint graphIndex = (uint)AstarPath.active.astarData.GetGraphIndex(this);
			GraphNodeDelegateCancelable del = delegate(GraphNode n)
			{
				n.GraphIndex = graphIndex;
				return true;
			};
			GetNodes(del);
			for (int m = 0; m < num4; m++)
			{
				for (int num7 = 0; num7 < num3; num7++)
				{
					Console.WriteLine("Connecing Tile #" + (m * tileXCount + num7) + " of " + num4 * num3);
					if (num7 < num3 - 1)
					{
						ConnectTiles(tiles[num7 + m * tileXCount], tiles[num7 + 1 + m * tileXCount]);
					}
					if (m < num4 - 1)
					{
						ConnectTiles(tiles[num7 + m * tileXCount], tiles[num7 + (m + 1) * tileXCount]);
					}
				}
			}
		}

		protected void BuildTileMesh(Voxelize vox, int x, int z)
		{
			float num = (float)tileSizeX * cellSize;
			float num2 = (float)tileSizeZ * cellSize;
			int num3 = Mathf.CeilToInt(characterRadius / cellSize);
			Vector3 min = forcedBounds.min;
			Vector3 max = forcedBounds.max;
			Bounds bounds = default(Bounds);
			bounds.SetMinMax(new Vector3((float)x * num, 0f, (float)z * num2) + min, new Vector3((float)(x + 1) * num + min.x, max.y, (float)(z + 1) * num2 + min.z));
			vox.borderSize = num3 + 3;
			bounds.Expand(new Vector3(vox.borderSize, 0f, vox.borderSize) * cellSize * 2f);
			vox.forcedBounds = bounds;
			vox.width = tileSizeX + vox.borderSize * 2;
			vox.depth = tileSizeZ + vox.borderSize * 2;
			if (!useTiles && relevantGraphSurfaceMode == RelevantGraphSurfaceMode.OnlyForCompletelyInsideTile)
			{
				vox.relevantGraphSurfaceMode = RelevantGraphSurfaceMode.RequireForAll;
			}
			else
			{
				vox.relevantGraphSurfaceMode = relevantGraphSurfaceMode;
			}
			vox.minRegionSize = Mathf.RoundToInt(minRegionSize / (cellSize * cellSize));
			vox.Init();
			vox.CollectMeshes();
			vox.VoxelizeInput();
			vox.FilterLedges(vox.voxelWalkableHeight, vox.voxelWalkableClimb, vox.cellSize, vox.cellHeight, vox.forcedBounds.min);
			vox.FilterLowHeightSpans(vox.voxelWalkableHeight, vox.cellSize, vox.cellHeight, vox.forcedBounds.min);
			vox.BuildCompactField();
			vox.BuildVoxelConnections();
			vox.ErodeWalkableArea(num3);
			vox.BuildDistanceField();
			vox.BuildRegions();
			VoxelContourSet cset = new VoxelContourSet();
			vox.BuildContours(contourMaxError, 1, cset, 1);
			VoxelMesh mesh;
			vox.BuildPolyMesh(cset, 3, out mesh);
			for (int i = 0; i < mesh.verts.Length; i++)
			{
				mesh.verts[i] = mesh.verts[i] * 1000 * vox.cellScale + (Int3)vox.voxelOffset;
			}
			NavmeshTile navmeshTile = CreateTile(vox, mesh, x, z);
			tiles[navmeshTile.x + navmeshTile.z * tileXCount] = navmeshTile;
		}

		private NavmeshTile CreateTile(Voxelize vox, VoxelMesh mesh, int x, int z)
		{
			if (mesh.tris == null)
			{
				throw new ArgumentNullException("mesh.tris");
			}
			if (mesh.verts == null)
			{
				throw new ArgumentNullException("mesh.verts");
			}
			NavmeshTile navmeshTile = new NavmeshTile();
			navmeshTile.x = x;
			navmeshTile.z = z;
			navmeshTile.w = 1;
			navmeshTile.d = 1;
			navmeshTile.tris = mesh.tris;
			navmeshTile.verts = mesh.verts;
			navmeshTile.bbTree = new BBTree();
			if (navmeshTile.tris.Length % 3 != 0)
			{
				throw new ArgumentException("Indices array's length must be a multiple of 3 (mesh.tris)");
			}
			if (navmeshTile.verts.Length >= 1048575)
			{
				if (tileXCount * tileZCount == 1)
				{
					throw new ArgumentException("Too many vertices per tile (more than " + 1048575 + ").\n<b>Try enabling tiling in the recast graph settings.</b>\n");
				}
				throw new ArgumentException("Too many vertices per tile (more than " + 1048575 + ").\n<b>Try reducing tile size or enabling ASTAR_RECAST_LARGER_TILES under the 'Optimizations' tab in the A* Inspector</b>");
			}
			Dictionary<Int3, int> dictionary = cachedInt3_int_dict;
			dictionary.Clear();
			int[] array = new int[navmeshTile.verts.Length];
			int num = 0;
			for (int i = 0; i < navmeshTile.verts.Length; i++)
			{
				if (!dictionary.ContainsKey(navmeshTile.verts[i]))
				{
					dictionary.Add(navmeshTile.verts[i], num);
					array[i] = num;
					navmeshTile.verts[num] = navmeshTile.verts[i];
					num++;
				}
				else
				{
					array[i] = dictionary[navmeshTile.verts[i]];
				}
			}
			for (int j = 0; j < navmeshTile.tris.Length; j++)
			{
				navmeshTile.tris[j] = array[navmeshTile.tris[j]];
			}
			Int3[] array2 = new Int3[num];
			for (int k = 0; k < num; k++)
			{
				array2[k] = navmeshTile.verts[k];
			}
			navmeshTile.verts = array2;
			TriangleMeshNode[] array3 = (navmeshTile.nodes = new TriangleMeshNode[navmeshTile.tris.Length / 3]);
			int num2 = AstarPath.active.astarData.graphs.Length;
			TriangleMeshNode.SetNavmeshHolder(num2, navmeshTile);
			int num3 = x + z * tileXCount;
			num3 <<= 20;
			for (int l = 0; l < array3.Length; l++)
			{
				TriangleMeshNode triangleMeshNode = (array3[l] = new TriangleMeshNode(active));
				triangleMeshNode.GraphIndex = (uint)num2;
				triangleMeshNode.v0 = navmeshTile.tris[l * 3] | num3;
				triangleMeshNode.v1 = navmeshTile.tris[l * 3 + 1] | num3;
				triangleMeshNode.v2 = navmeshTile.tris[l * 3 + 2] | num3;
				if (!VectorMath.IsClockwiseXZ(triangleMeshNode.GetVertex(0), triangleMeshNode.GetVertex(1), triangleMeshNode.GetVertex(2)))
				{
					int v = triangleMeshNode.v0;
					triangleMeshNode.v0 = triangleMeshNode.v2;
					triangleMeshNode.v2 = v;
				}
				triangleMeshNode.Walkable = true;
				triangleMeshNode.Penalty = initialPenalty;
				triangleMeshNode.UpdatePositionFromVertices();
			}
			navmeshTile.bbTree.RebuildFrom(array3);
			CreateNodeConnections(navmeshTile.nodes);
			TriangleMeshNode.SetNavmeshHolder(num2, null);
			return navmeshTile;
		}

		private void CreateNodeConnections(TriangleMeshNode[] nodes)
		{
			List<MeshNode> list = ListPool<MeshNode>.Claim();
			List<uint> list2 = ListPool<uint>.Claim();
			Dictionary<Int2, int> dictionary = cachedInt2_int_dict;
			dictionary.Clear();
			for (int i = 0; i < nodes.Length; i++)
			{
				TriangleMeshNode triangleMeshNode = nodes[i];
				int vertexCount = triangleMeshNode.GetVertexCount();
				for (int j = 0; j < vertexCount; j++)
				{
					Int2 key = new Int2(triangleMeshNode.GetVertexIndex(j), triangleMeshNode.GetVertexIndex((j + 1) % vertexCount));
					if (!dictionary.ContainsKey(key))
					{
						dictionary.Add(key, i);
					}
				}
			}
			foreach (TriangleMeshNode triangleMeshNode2 in nodes)
			{
				list.Clear();
				list2.Clear();
				int vertexCount2 = triangleMeshNode2.GetVertexCount();
				for (int l = 0; l < vertexCount2; l++)
				{
					int vertexIndex = triangleMeshNode2.GetVertexIndex(l);
					int vertexIndex2 = triangleMeshNode2.GetVertexIndex((l + 1) % vertexCount2);
					int value;
					if (!dictionary.TryGetValue(new Int2(vertexIndex2, vertexIndex), out value))
					{
						continue;
					}
					TriangleMeshNode triangleMeshNode3 = nodes[value];
					int vertexCount3 = triangleMeshNode3.GetVertexCount();
					for (int m = 0; m < vertexCount3; m++)
					{
						if (triangleMeshNode3.GetVertexIndex(m) == vertexIndex2 && triangleMeshNode3.GetVertexIndex((m + 1) % vertexCount3) == vertexIndex)
						{
							uint costMagnitude = (uint)(triangleMeshNode2.position - triangleMeshNode3.position).costMagnitude;
							list.Add(triangleMeshNode3);
							list2.Add(costMagnitude);
							break;
						}
					}
				}
				triangleMeshNode2.connections = list.ToArray();
				triangleMeshNode2.connectionCosts = list2.ToArray();
			}
			ListPool<MeshNode>.Release(list);
			ListPool<uint>.Release(list2);
		}

		private void ConnectTiles(NavmeshTile tile1, NavmeshTile tile2)
		{
			if (tile1 == null || tile2 == null)
			{
				return;
			}
			if (tile1.nodes == null)
			{
				throw new ArgumentException("tile1 does not contain any nodes");
			}
			if (tile2.nodes == null)
			{
				throw new ArgumentException("tile2 does not contain any nodes");
			}
			int num = Mathf.Clamp(tile2.x, tile1.x, tile1.x + tile1.w - 1);
			int num2 = Mathf.Clamp(tile1.x, tile2.x, tile2.x + tile2.w - 1);
			int num3 = Mathf.Clamp(tile2.z, tile1.z, tile1.z + tile1.d - 1);
			int num4 = Mathf.Clamp(tile1.z, tile2.z, tile2.z + tile2.d - 1);
			int num5;
			int i;
			int num6;
			int num7;
			float num8;
			if (num == num2)
			{
				num5 = 2;
				i = 0;
				num6 = num3;
				num7 = num4;
				num8 = (float)tileSizeZ * cellSize;
			}
			else
			{
				if (num3 != num4)
				{
					throw new ArgumentException("Tiles are not adjacent (neither x or z coordinates match)");
				}
				num5 = 0;
				i = 2;
				num6 = num;
				num7 = num2;
				num8 = (float)tileSizeX * cellSize;
			}
			if (Math.Abs(num6 - num7) != 1)
			{
				UnityEngine.Debug.Log(tile1.x + " " + tile1.z + " " + tile1.w + " " + tile1.d + "\n" + tile2.x + " " + tile2.z + " " + tile2.w + " " + tile2.d + "\n" + num + " " + num3 + " " + num2 + " " + num4);
				throw new ArgumentException("Tiles are not adjacent (tile coordinates must differ by exactly 1. Got '" + num6 + "' and '" + num7 + "')");
			}
			int num9 = (int)Math.Round(((float)Math.Max(num6, num7) * num8 + forcedBounds.min[num5]) * 1000f);
			TriangleMeshNode[] nodes = tile1.nodes;
			TriangleMeshNode[] nodes2 = tile2.nodes;
			foreach (TriangleMeshNode triangleMeshNode in nodes)
			{
				int vertexCount = triangleMeshNode.GetVertexCount();
				for (int k = 0; k < vertexCount; k++)
				{
					Int3 vertex = triangleMeshNode.GetVertex(k);
					Int3 vertex2 = triangleMeshNode.GetVertex((k + 1) % vertexCount);
					if (Math.Abs(vertex[num5] - num9) >= 2 || Math.Abs(vertex2[num5] - num9) >= 2)
					{
						continue;
					}
					int num10 = Math.Min(vertex[i], vertex2[i]);
					int num11 = Math.Max(vertex[i], vertex2[i]);
					if (num10 == num11)
					{
						continue;
					}
					foreach (TriangleMeshNode triangleMeshNode2 in nodes2)
					{
						int vertexCount2 = triangleMeshNode2.GetVertexCount();
						for (int m = 0; m < vertexCount2; m++)
						{
							Int3 vertex3 = triangleMeshNode2.GetVertex(m);
							Int3 vertex4 = triangleMeshNode2.GetVertex((m + 1) % vertexCount);
							if (Math.Abs(vertex3[num5] - num9) < 2 && Math.Abs(vertex4[num5] - num9) < 2)
							{
								int num12 = Math.Min(vertex3[i], vertex4[i]);
								int num13 = Math.Max(vertex3[i], vertex4[i]);
								if (num12 != num13 && num11 > num12 && num10 < num13 && ((vertex == vertex3 && vertex2 == vertex4) || (vertex == vertex4 && vertex2 == vertex3) || VectorMath.SqrDistanceSegmentSegment((Vector3)vertex, (Vector3)vertex2, (Vector3)vertex3, (Vector3)vertex4) < walkableClimb * walkableClimb))
								{
									uint costMagnitude = (uint)(triangleMeshNode.position - triangleMeshNode2.position).costMagnitude;
									triangleMeshNode.AddConnection(triangleMeshNode2, costMagnitude);
									triangleMeshNode2.AddConnection(triangleMeshNode, costMagnitude);
								}
							}
						}
					}
				}
			}
		}

		public void StartBatchTileUpdate()
		{
			if (batchTileUpdate)
			{
				throw new InvalidOperationException("Calling StartBatchLoad when batching is already enabled");
			}
			batchTileUpdate = true;
		}

		public void EndBatchTileUpdate()
		{
			if (!batchTileUpdate)
			{
				throw new InvalidOperationException("Calling EndBatchLoad when batching not enabled");
			}
			batchTileUpdate = false;
			int num = tileXCount;
			int num2 = tileZCount;
			for (int i = 0; i < num2; i++)
			{
				for (int j = 0; j < num; j++)
				{
					tiles[j + i * tileXCount].flag = false;
				}
			}
			for (int k = 0; k < batchUpdatedTiles.Count; k++)
			{
				tiles[batchUpdatedTiles[k]].flag = true;
			}
			for (int l = 0; l < num2; l++)
			{
				for (int m = 0; m < num; m++)
				{
					if (m < num - 1 && (tiles[m + l * tileXCount].flag || tiles[m + 1 + l * tileXCount].flag) && tiles[m + l * tileXCount] != tiles[m + 1 + l * tileXCount])
					{
						ConnectTiles(tiles[m + l * tileXCount], tiles[m + 1 + l * tileXCount]);
					}
					if (l < num2 - 1 && (tiles[m + l * tileXCount].flag || tiles[m + (l + 1) * tileXCount].flag) && tiles[m + l * tileXCount] != tiles[m + (l + 1) * tileXCount])
					{
						ConnectTiles(tiles[m + l * tileXCount], tiles[m + (l + 1) * tileXCount]);
					}
				}
			}
			batchUpdatedTiles.Clear();
		}

		public void ReplaceTile(int x, int z, Int3[] verts, int[] tris, bool worldSpace)
		{
			ReplaceTile(x, z, 1, 1, verts, tris, worldSpace);
		}

		public void ReplaceTile(int x, int z, int w, int d, Int3[] verts, int[] tris, bool worldSpace)
		{
			if (x + w > tileXCount || z + d > tileZCount || x < 0 || z < 0)
			{
				throw new ArgumentException("Tile is placed at an out of bounds position or extends out of the graph bounds (" + x + ", " + z + " [" + w + ", " + d + "] " + tileXCount + " " + tileZCount + ")");
			}
			if (w < 1 || d < 1)
			{
				throw new ArgumentException("width and depth must be greater or equal to 1. Was " + w + ", " + d);
			}
			for (int i = z; i < z + d; i++)
			{
				for (int j = x; j < x + w; j++)
				{
					NavmeshTile navmeshTile = tiles[j + i * tileXCount];
					if (navmeshTile == null)
					{
						continue;
					}
					RemoveConnectionsFromTile(navmeshTile);
					for (int k = 0; k < navmeshTile.nodes.Length; k++)
					{
						navmeshTile.nodes[k].Destroy();
					}
					for (int l = navmeshTile.z; l < navmeshTile.z + navmeshTile.d; l++)
					{
						for (int m = navmeshTile.x; m < navmeshTile.x + navmeshTile.w; m++)
						{
							NavmeshTile navmeshTile2 = tiles[m + l * tileXCount];
							if (navmeshTile2 == null || navmeshTile2 != navmeshTile)
							{
								throw new Exception("This should not happen");
							}
							if (l < z || l >= z + d || m < x || m >= x + w)
							{
								tiles[m + l * tileXCount] = NewEmptyTile(m, l);
								if (batchTileUpdate)
								{
									batchUpdatedTiles.Add(m + l * tileXCount);
								}
							}
							else
							{
								tiles[m + l * tileXCount] = null;
							}
						}
					}
				}
			}
			NavmeshTile navmeshTile3 = new NavmeshTile();
			navmeshTile3.x = x;
			navmeshTile3.z = z;
			navmeshTile3.w = w;
			navmeshTile3.d = d;
			navmeshTile3.tris = tris;
			navmeshTile3.verts = verts;
			navmeshTile3.bbTree = new BBTree();
			if (navmeshTile3.tris.Length % 3 != 0)
			{
				throw new ArgumentException("Triangle array's length must be a multiple of 3 (tris)");
			}
			if (navmeshTile3.verts.Length > 65535)
			{
				throw new ArgumentException("Too many vertices per tile (more than 65535)");
			}
			if (!worldSpace)
			{
				if (!Mathf.Approximately((float)(x * tileSizeX) * cellSize * 1000f, (float)Math.Round((float)(x * tileSizeX) * cellSize * 1000f)))
				{
					UnityEngine.Debug.LogWarning("Possible numerical imprecision. Consider adjusting tileSize and/or cellSize");
				}
				if (!Mathf.Approximately((float)(z * tileSizeZ) * cellSize * 1000f, (float)Math.Round((float)(z * tileSizeZ) * cellSize * 1000f)))
				{
					UnityEngine.Debug.LogWarning("Possible numerical imprecision. Consider adjusting tileSize and/or cellSize");
				}
				Int3 @int = (Int3)(new Vector3((float)(x * tileSizeX) * cellSize, 0f, (float)(z * tileSizeZ) * cellSize) + forcedBounds.min);
				for (int n = 0; n < verts.Length; n++)
				{
					verts[n] += @int;
				}
			}
			TriangleMeshNode[] array = (navmeshTile3.nodes = new TriangleMeshNode[navmeshTile3.tris.Length / 3]);
			int num = AstarPath.active.astarData.graphs.Length;
			TriangleMeshNode.SetNavmeshHolder(num, navmeshTile3);
			int num2 = x + z * tileXCount;
			num2 <<= 20;
			for (int num3 = 0; num3 < array.Length; num3++)
			{
				TriangleMeshNode triangleMeshNode = (array[num3] = new TriangleMeshNode(active));
				triangleMeshNode.GraphIndex = (uint)num;
				triangleMeshNode.v0 = navmeshTile3.tris[num3 * 3] | num2;
				triangleMeshNode.v1 = navmeshTile3.tris[num3 * 3 + 1] | num2;
				triangleMeshNode.v2 = navmeshTile3.tris[num3 * 3 + 2] | num2;
				if (!VectorMath.IsClockwiseXZ(triangleMeshNode.GetVertex(0), triangleMeshNode.GetVertex(1), triangleMeshNode.GetVertex(2)))
				{
					int v = triangleMeshNode.v0;
					triangleMeshNode.v0 = triangleMeshNode.v2;
					triangleMeshNode.v2 = v;
				}
				triangleMeshNode.Walkable = true;
				triangleMeshNode.Penalty = initialPenalty;
				triangleMeshNode.UpdatePositionFromVertices();
			}
			navmeshTile3.bbTree.RebuildFrom(array);
			CreateNodeConnections(navmeshTile3.nodes);
			for (int num4 = z; num4 < z + d; num4++)
			{
				for (int num5 = x; num5 < x + w; num5++)
				{
					tiles[num5 + num4 * tileXCount] = navmeshTile3;
				}
			}
			if (batchTileUpdate)
			{
				batchUpdatedTiles.Add(x + z * tileXCount);
			}
			else
			{
				ConnectTileWithNeighbours(navmeshTile3);
			}
			TriangleMeshNode.SetNavmeshHolder(num, null);
			num = AstarPath.active.astarData.GetGraphIndex(this);
			for (int num6 = 0; num6 < array.Length; num6++)
			{
				array[num6].GraphIndex = (uint)num;
			}
		}

		private void CollectTreeMeshes(Terrain terrain, List<ExtraMesh> extraMeshes)
		{
			TerrainData terrainData = terrain.terrainData;
			for (int i = 0; i < terrainData.treeInstances.Length; i++)
			{
				TreeInstance treeInstance = terrainData.treeInstances[i];
				TreePrototype treePrototype = terrainData.treePrototypes[treeInstance.prototypeIndex];
				if (treePrototype.prefab == null)
				{
					continue;
				}
				Collider component = treePrototype.prefab.GetComponent<Collider>();
				if (component == null)
				{
					Bounds b = new Bounds(terrain.transform.position + Vector3.Scale(treeInstance.position, terrainData.size), new Vector3(treeInstance.widthScale, treeInstance.heightScale, treeInstance.widthScale));
					Matrix4x4 matrix4x = Matrix4x4.TRS(terrain.transform.position + Vector3.Scale(treeInstance.position, terrainData.size), Quaternion.identity, new Vector3(treeInstance.widthScale, treeInstance.heightScale, treeInstance.widthScale) * 0.5f);
					ExtraMesh item = new ExtraMesh(BoxColliderVerts, BoxColliderTris, b, matrix4x);
					extraMeshes.Add(item);
					continue;
				}
				Vector3 pos = terrain.transform.position + Vector3.Scale(treeInstance.position, terrainData.size);
				ExtraMesh item2 = RasterizeCollider(component, Matrix4x4.TRS(s: new Vector3(treeInstance.widthScale, treeInstance.heightScale, treeInstance.widthScale), pos: pos, q: Quaternion.identity));
				if (item2.vertices != null)
				{
					item2.RecalculateBounds();
					extraMeshes.Add(item2);
				}
			}
		}

		private void CollectTerrainMeshes(Bounds bounds, bool rasterizeTrees, List<ExtraMesh> extraMeshes)
		{
			Terrain[] array = UnityEngine.Object.FindObjectsOfType(typeof(Terrain)) as Terrain[];
			if (array.Length <= 0)
			{
				return;
			}
			for (int i = 0; i < array.Length; i++)
			{
				TerrainData terrainData = array[i].terrainData;
				if (terrainData == null)
				{
					continue;
				}
				Vector3 position = array[i].GetPosition();
				Vector3 center = position + terrainData.size * 0.5f;
				Bounds b = new Bounds(center, terrainData.size);
				if (!b.Intersects(bounds))
				{
					continue;
				}
				float[,] heights = terrainData.GetHeights(0, 0, terrainData.heightmapWidth, terrainData.heightmapHeight);
				terrainSampleSize = Math.Max(terrainSampleSize, 1);
				int heightmapWidth = terrainData.heightmapWidth;
				int heightmapHeight = terrainData.heightmapHeight;
				int num = (terrainData.heightmapWidth + terrainSampleSize - 1) / terrainSampleSize + 1;
				int num2 = (terrainData.heightmapHeight + terrainSampleSize - 1) / terrainSampleSize + 1;
				Vector3[] array2 = new Vector3[num * num2];
				Vector3 heightmapScale = terrainData.heightmapScale;
				float y = terrainData.size.y;
				int num3 = 0;
				for (int j = 0; j < num2; j++)
				{
					int num4 = 0;
					for (int k = 0; k < num; k++)
					{
						int num5 = Math.Min(num4, heightmapWidth - 1);
						int num6 = Math.Min(num3, heightmapHeight - 1);
						array2[j * num + k] = new Vector3((float)num6 * heightmapScale.x, heights[num5, num6] * y, (float)num5 * heightmapScale.z) + position;
						num4 += terrainSampleSize;
					}
					num3 += terrainSampleSize;
				}
				int[] array3 = new int[(num - 1) * (num2 - 1) * 2 * 3];
				int num7 = 0;
				for (int l = 0; l < num2 - 1; l++)
				{
					for (int m = 0; m < num - 1; m++)
					{
						array3[num7] = l * num + m;
						array3[num7 + 1] = l * num + m + 1;
						array3[num7 + 2] = (l + 1) * num + m + 1;
						num7 += 3;
						array3[num7] = l * num + m;
						array3[num7 + 1] = (l + 1) * num + m + 1;
						array3[num7 + 2] = (l + 1) * num + m;
						num7 += 3;
					}
				}
				extraMeshes.Add(new ExtraMesh(array2, array3, b));
				if (rasterizeTrees)
				{
					CollectTreeMeshes(array[i], extraMeshes);
				}
			}
		}

		private void CollectColliderMeshes(Bounds bounds, List<ExtraMesh> extraMeshes)
		{
			Collider[] array = UnityEngine.Object.FindObjectsOfType(typeof(Collider)) as Collider[];
			if ((tagMask != null && tagMask.Count > 0) || (int)mask != 0)
			{
				foreach (Collider collider in array)
				{
					if ((((1 << collider.gameObject.layer) & (int)mask) != 0 || tagMask.Contains(collider.tag)) && collider.enabled && !collider.isTrigger && collider.bounds.Intersects(bounds))
					{
						ExtraMesh item = RasterizeCollider(collider);
						if (item.vertices != null)
						{
							extraMeshes.Add(item);
						}
					}
				}
			}
			capsuleCache.Clear();
		}

		private bool CollectMeshes(out List<ExtraMesh> extraMeshes, Bounds bounds)
		{
			extraMeshes = new List<ExtraMesh>();
			if (rasterizeMeshes)
			{
				GetSceneMeshes(bounds, tagMask, mask, extraMeshes);
			}
			GetRecastMeshObjs(bounds, extraMeshes);
			if (rasterizeTerrain)
			{
				CollectTerrainMeshes(bounds, rasterizeTrees, extraMeshes);
			}
			if (rasterizeColliders)
			{
				CollectColliderMeshes(bounds, extraMeshes);
			}
			if (extraMeshes.Count == 0)
			{
				UnityEngine.Debug.LogWarning("No MeshFilters were found contained in the layers specified by the 'mask' variables");
				return false;
			}
			return true;
		}

		private ExtraMesh RasterizeCollider(Collider col)
		{
			return RasterizeCollider(col, col.transform.localToWorldMatrix);
		}

		private ExtraMesh RasterizeCollider(Collider col, Matrix4x4 localToWorldMatrix)
		{
			if (col is BoxCollider)
			{
				BoxCollider boxCollider = col as BoxCollider;
				Matrix4x4 matrix4x = Matrix4x4.TRS(boxCollider.center, Quaternion.identity, boxCollider.size * 0.5f);
				matrix4x = localToWorldMatrix * matrix4x;
				Bounds bounds = boxCollider.bounds;
				return new ExtraMesh(BoxColliderVerts, BoxColliderTris, bounds, matrix4x);
			}
			if (col is SphereCollider || col is CapsuleCollider)
			{
				SphereCollider sphereCollider = col as SphereCollider;
				CapsuleCollider capsuleCollider = col as CapsuleCollider;
				float num = ((!(sphereCollider != null)) ? capsuleCollider.radius : sphereCollider.radius);
				float num2 = ((!(sphereCollider != null)) ? (capsuleCollider.height * 0.5f / num - 1f) : 0f);
				Matrix4x4 matrix4x2 = Matrix4x4.TRS((!(sphereCollider != null)) ? capsuleCollider.center : sphereCollider.center, Quaternion.identity, Vector3.one * num);
				matrix4x2 = localToWorldMatrix * matrix4x2;
				int num3 = Mathf.Max(4, Mathf.RoundToInt(colliderRasterizeDetail * Mathf.Sqrt(matrix4x2.MultiplyVector(Vector3.one).magnitude)));
				if (num3 > 100)
				{
					UnityEngine.Debug.LogWarning("Very large detail for some collider meshes. Consider decreasing Collider Rasterize Detail (RecastGraph)");
				}
				int num4 = num3;
				CapsuleCache capsuleCache = null;
				for (int i = 0; i < this.capsuleCache.Count; i++)
				{
					CapsuleCache capsuleCache2 = this.capsuleCache[i];
					if (capsuleCache2.rows == num3 && Mathf.Approximately(capsuleCache2.height, num2))
					{
						capsuleCache = capsuleCache2;
					}
				}
				Vector3[] array;
				if (capsuleCache == null)
				{
					array = new Vector3[num3 * num4 + 2];
					List<int> list = new List<int>();
					array[array.Length - 1] = Vector3.up;
					for (int j = 0; j < num3; j++)
					{
						for (int k = 0; k < num4; k++)
						{
							array[k + j * num4] = new Vector3(Mathf.Cos((float)k * (float)Math.PI * 2f / (float)num4) * Mathf.Sin((float)j * (float)Math.PI / (float)(num3 - 1)), Mathf.Cos((float)j * (float)Math.PI / (float)(num3 - 1)) + ((j >= num3 / 2) ? (0f - num2) : num2), Mathf.Sin((float)k * (float)Math.PI * 2f / (float)num4) * Mathf.Sin((float)j * (float)Math.PI / (float)(num3 - 1)));
						}
					}
					array[array.Length - 2] = Vector3.down;
					int num5 = 0;
					int num6 = num4 - 1;
					while (num5 < num4)
					{
						list.Add(array.Length - 1);
						list.Add(0 * num4 + num6);
						list.Add(0 * num4 + num5);
						num6 = num5++;
					}
					for (int l = 1; l < num3; l++)
					{
						int num7 = 0;
						int num8 = num4 - 1;
						while (num7 < num4)
						{
							list.Add(l * num4 + num7);
							list.Add(l * num4 + num8);
							list.Add((l - 1) * num4 + num7);
							list.Add((l - 1) * num4 + num8);
							list.Add((l - 1) * num4 + num7);
							list.Add(l * num4 + num8);
							num8 = num7++;
						}
					}
					int num9 = 0;
					int num10 = num4 - 1;
					while (num9 < num4)
					{
						list.Add(array.Length - 2);
						list.Add((num3 - 1) * num4 + num10);
						list.Add((num3 - 1) * num4 + num9);
						num10 = num9++;
					}
					capsuleCache = new CapsuleCache();
					capsuleCache.rows = num3;
					capsuleCache.height = num2;
					capsuleCache.verts = array;
					capsuleCache.tris = list.ToArray();
					this.capsuleCache.Add(capsuleCache);
				}
				array = capsuleCache.verts;
				int[] tris = capsuleCache.tris;
				Bounds bounds2 = col.bounds;
				return new ExtraMesh(array, tris, bounds2, matrix4x2);
			}
			if (col is MeshCollider)
			{
				MeshCollider meshCollider = col as MeshCollider;
				if (meshCollider.sharedMesh != null)
				{
					return new ExtraMesh(meshCollider.sharedMesh.vertices, meshCollider.sharedMesh.triangles, meshCollider.bounds, localToWorldMatrix);
				}
			}
			return default(ExtraMesh);
		}

		public bool Linecast(Vector3 origin, Vector3 end)
		{
			return Linecast(origin, end, GetNearest(origin, NNConstraint.None).node);
		}

		public bool Linecast(Vector3 origin, Vector3 end, GraphNode hint, out GraphHitInfo hit)
		{
			return NavMeshGraph.Linecast(this, origin, end, hint, out hit, null);
		}

		public bool Linecast(Vector3 origin, Vector3 end, GraphNode hint)
		{
			GraphHitInfo hit;
			return NavMeshGraph.Linecast(this, origin, end, hint, out hit, null);
		}

		public bool Linecast(Vector3 tmp_origin, Vector3 tmp_end, GraphNode hint, out GraphHitInfo hit, List<GraphNode> trace)
		{
			return NavMeshGraph.Linecast(this, tmp_origin, tmp_end, hint, out hit, trace);
		}

		public override void OnDrawGizmos(bool drawNodes)
		{
			if (!drawNodes)
			{
				return;
			}
			Gizmos.color = Color.white;
			Gizmos.DrawWireCube(forcedBounds.center, forcedBounds.size);
			PathHandler debugData = AstarPath.active.debugPathData;
			GraphNodeDelegateCancelable del = delegate(GraphNode _node)
			{
				TriangleMeshNode triangleMeshNode = _node as TriangleMeshNode;
				if (AstarPath.active.showSearchTree && debugData != null)
				{
					bool flag = NavGraph.InSearchTree(triangleMeshNode, AstarPath.active.debugPath);
					if (flag && showNodeConnections)
					{
						PathNode pathNode = debugData.GetPathNode(triangleMeshNode);
						if (pathNode.parent != null)
						{
							Gizmos.color = NodeColor(triangleMeshNode, debugData);
							Gizmos.DrawLine((Vector3)triangleMeshNode.position, (Vector3)debugData.GetPathNode(triangleMeshNode).parent.node.position);
						}
					}
					if (showMeshOutline)
					{
						Gizmos.color = ((!triangleMeshNode.Walkable) ? AstarColor.UnwalkableNode : NodeColor(triangleMeshNode, debugData));
						if (!flag)
						{
							Gizmos.color *= new Color(1f, 1f, 1f, 0.1f);
						}
						Gizmos.DrawLine((Vector3)triangleMeshNode.GetVertex(0), (Vector3)triangleMeshNode.GetVertex(1));
						Gizmos.DrawLine((Vector3)triangleMeshNode.GetVertex(1), (Vector3)triangleMeshNode.GetVertex(2));
						Gizmos.DrawLine((Vector3)triangleMeshNode.GetVertex(2), (Vector3)triangleMeshNode.GetVertex(0));
					}
				}
				else
				{
					if (showNodeConnections)
					{
						Gizmos.color = NodeColor(triangleMeshNode, null);
						for (int i = 0; i < triangleMeshNode.connections.Length; i++)
						{
							Gizmos.DrawLine((Vector3)triangleMeshNode.position, Vector3.Lerp((Vector3)triangleMeshNode.connections[i].position, (Vector3)triangleMeshNode.position, 0.4f));
						}
					}
					if (showMeshOutline)
					{
						Gizmos.color = ((!triangleMeshNode.Walkable) ? AstarColor.UnwalkableNode : NodeColor(triangleMeshNode, debugData));
						Gizmos.DrawLine((Vector3)triangleMeshNode.GetVertex(0), (Vector3)triangleMeshNode.GetVertex(1));
						Gizmos.DrawLine((Vector3)triangleMeshNode.GetVertex(1), (Vector3)triangleMeshNode.GetVertex(2));
						Gizmos.DrawLine((Vector3)triangleMeshNode.GetVertex(2), (Vector3)triangleMeshNode.GetVertex(0));
					}
				}
				return true;
			};
			GetNodes(del);
		}

		public override void SerializeExtraInfo(GraphSerializationContext ctx)
		{
			BinaryWriter writer = ctx.writer;
			if (tiles == null)
			{
				writer.Write(-1);
				return;
			}
			writer.Write(tileXCount);
			writer.Write(tileZCount);
			for (int i = 0; i < tileZCount; i++)
			{
				for (int j = 0; j < tileXCount; j++)
				{
					NavmeshTile navmeshTile = tiles[j + i * tileXCount];
					if (navmeshTile == null)
					{
						throw new Exception("NULL Tile");
					}
					writer.Write(navmeshTile.x);
					writer.Write(navmeshTile.z);
					if (navmeshTile.x == j && navmeshTile.z == i)
					{
						writer.Write(navmeshTile.w);
						writer.Write(navmeshTile.d);
						writer.Write(navmeshTile.tris.Length);
						for (int k = 0; k < navmeshTile.tris.Length; k++)
						{
							writer.Write(navmeshTile.tris[k]);
						}
						writer.Write(navmeshTile.verts.Length);
						for (int l = 0; l < navmeshTile.verts.Length; l++)
						{
							writer.Write(navmeshTile.verts[l].x);
							writer.Write(navmeshTile.verts[l].y);
							writer.Write(navmeshTile.verts[l].z);
						}
						writer.Write(navmeshTile.nodes.Length);
						for (int m = 0; m < navmeshTile.nodes.Length; m++)
						{
							navmeshTile.nodes[m].SerializeNode(ctx);
						}
					}
				}
			}
		}

		public override void DeserializeExtraInfo(GraphSerializationContext ctx)
		{
			BinaryReader reader = ctx.reader;
			tileXCount = reader.ReadInt32();
			if (tileXCount < 0)
			{
				return;
			}
			tileZCount = reader.ReadInt32();
			tiles = new NavmeshTile[tileXCount * tileZCount];
			TriangleMeshNode.SetNavmeshHolder((int)ctx.graphIndex, this);
			for (int i = 0; i < tileZCount; i++)
			{
				for (int j = 0; j < tileXCount; j++)
				{
					int num = j + i * tileXCount;
					int num2 = reader.ReadInt32();
					if (num2 < 0)
					{
						throw new Exception("Invalid tile coordinates (x < 0)");
					}
					int num3 = reader.ReadInt32();
					if (num3 < 0)
					{
						throw new Exception("Invalid tile coordinates (z < 0)");
					}
					if (num2 != j || num3 != i)
					{
						tiles[num] = tiles[num3 * tileXCount + num2];
						continue;
					}
					NavmeshTile navmeshTile = new NavmeshTile();
					navmeshTile.x = num2;
					navmeshTile.z = num3;
					navmeshTile.w = reader.ReadInt32();
					navmeshTile.d = reader.ReadInt32();
					navmeshTile.bbTree = new BBTree();
					tiles[num] = navmeshTile;
					int num4 = reader.ReadInt32();
					if (num4 % 3 != 0)
					{
						throw new Exception("Corrupt data. Triangle indices count must be divisable by 3. Got " + num4);
					}
					navmeshTile.tris = new int[num4];
					for (int k = 0; k < navmeshTile.tris.Length; k++)
					{
						navmeshTile.tris[k] = reader.ReadInt32();
					}
					navmeshTile.verts = new Int3[reader.ReadInt32()];
					for (int l = 0; l < navmeshTile.verts.Length; l++)
					{
						navmeshTile.verts[l] = new Int3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
					}
					int num5 = reader.ReadInt32();
					navmeshTile.nodes = new TriangleMeshNode[num5];
					num <<= 20;
					for (int m = 0; m < navmeshTile.nodes.Length; m++)
					{
						TriangleMeshNode triangleMeshNode = new TriangleMeshNode(active);
						navmeshTile.nodes[m] = triangleMeshNode;
						triangleMeshNode.DeserializeNode(ctx);
						triangleMeshNode.v0 = navmeshTile.tris[m * 3] | num;
						triangleMeshNode.v1 = navmeshTile.tris[m * 3 + 1] | num;
						triangleMeshNode.v2 = navmeshTile.tris[m * 3 + 2] | num;
						triangleMeshNode.UpdatePositionFromVertices();
					}
					navmeshTile.bbTree.RebuildFrom(navmeshTile.nodes);
				}
			}
		}
	}
}
