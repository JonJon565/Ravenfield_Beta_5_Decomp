using System;
using System.Collections.Generic;
using Pathfinding.ClipperLib;
using Pathfinding.Poly2Tri;
using Pathfinding.Voxels;
using UnityEngine;

namespace Pathfinding.Util
{
	public class TileHandler
	{
		public class TileType
		{
			private Int3[] verts;

			private int[] tris;

			private Int3 offset;

			private int lastYOffset;

			private int lastRotation;

			private int width;

			private int depth;

			private static readonly int[] Rotations = new int[16]
			{
				1, 0, 0, 1, 0, 1, -1, 0, -1, 0,
				0, -1, 0, -1, 1, 0
			};

			public int Width
			{
				get
				{
					return width;
				}
			}

			public int Depth
			{
				get
				{
					return depth;
				}
			}

			public TileType(Int3[] sourceVerts, int[] sourceTris, Int3 tileSize, Int3 centerOffset, int width = 1, int depth = 1)
			{
				if (sourceVerts == null)
				{
					throw new ArgumentNullException("sourceVerts");
				}
				if (sourceTris == null)
				{
					throw new ArgumentNullException("sourceTris");
				}
				tris = new int[sourceTris.Length];
				for (int i = 0; i < tris.Length; i++)
				{
					tris[i] = sourceTris[i];
				}
				verts = new Int3[sourceVerts.Length];
				for (int j = 0; j < sourceVerts.Length; j++)
				{
					verts[j] = sourceVerts[j] + centerOffset;
				}
				offset = tileSize / 2f;
				offset.x *= width;
				offset.z *= depth;
				offset.y = 0;
				for (int k = 0; k < sourceVerts.Length; k++)
				{
					verts[k] += offset;
				}
				lastRotation = 0;
				lastYOffset = 0;
				this.width = width;
				this.depth = depth;
			}

			public TileType(Mesh source, Int3 tileSize, Int3 centerOffset, int width = 1, int depth = 1)
			{
				if (source == null)
				{
					throw new ArgumentNullException("source");
				}
				Vector3[] vertices = source.vertices;
				tris = source.triangles;
				verts = new Int3[vertices.Length];
				for (int i = 0; i < vertices.Length; i++)
				{
					verts[i] = (Int3)vertices[i] + centerOffset;
				}
				offset = tileSize / 2f;
				offset.x *= width;
				offset.z *= depth;
				offset.y = 0;
				for (int j = 0; j < vertices.Length; j++)
				{
					verts[j] += offset;
				}
				lastRotation = 0;
				lastYOffset = 0;
				this.width = width;
				this.depth = depth;
			}

			public void Load(out Int3[] verts, out int[] tris, int rotation, int yoffset)
			{
				rotation = (rotation % 4 + 4) % 4;
				int num = rotation;
				rotation = (rotation - lastRotation % 4 + 4) % 4;
				lastRotation = num;
				verts = this.verts;
				int num2 = yoffset - lastYOffset;
				lastYOffset = yoffset;
				if (rotation != 0 || num2 != 0)
				{
					for (int i = 0; i < verts.Length; i++)
					{
						Int3 @int = verts[i] - offset;
						Int3 int2 = @int;
						int2.y += num2;
						int2.x = @int.x * Rotations[rotation * 4] + @int.z * Rotations[rotation * 4 + 1];
						int2.z = @int.x * Rotations[rotation * 4 + 2] + @int.z * Rotations[rotation * 4 + 3];
						verts[i] = int2 + offset;
					}
				}
				tris = this.tris;
			}
		}

		[Flags]
		public enum CutMode
		{
			CutAll = 1,
			CutDual = 2,
			CutExtra = 4
		}

		private const int CUT_ALL = 0;

		private const int CUT_DUAL = 1;

		private const int CUT_BREAK = 2;

		private RecastGraph _graph;

		private List<TileType> tileTypes = new List<TileType>();

		private Clipper clipper;

		private int[] cached_int_array = new int[32];

		private Dictionary<Int3, int> cached_Int3_int_dict = new Dictionary<Int3, int>();

		private Dictionary<Int2, int> cached_Int2_int_dict = new Dictionary<Int2, int>();

		private TileType[] activeTileTypes;

		private int[] activeTileRotations;

		private int[] activeTileOffsets;

		private bool[] reloadedInBatch;

		private bool isBatching;

		public RecastGraph graph
		{
			get
			{
				return _graph;
			}
		}

		public TileHandler(RecastGraph graph)
		{
			if (graph == null)
			{
				throw new ArgumentNullException("graph");
			}
			if (graph.GetTiles() == null)
			{
				throw new ArgumentException("graph has no tiles. Please scan the graph before creating a TileHandler");
			}
			activeTileTypes = new TileType[graph.tileXCount * graph.tileZCount];
			activeTileRotations = new int[activeTileTypes.Length];
			activeTileOffsets = new int[activeTileTypes.Length];
			reloadedInBatch = new bool[activeTileTypes.Length];
			_graph = graph;
		}

		public int GetActiveRotation(Int2 p)
		{
			return activeTileRotations[p.x + p.y * _graph.tileXCount];
		}

		public TileType GetTileType(int index)
		{
			return tileTypes[index];
		}

		public int GetTileTypeCount()
		{
			return tileTypes.Count;
		}

		public TileType RegisterTileType(Mesh source, Int3 centerOffset, int width = 1, int depth = 1)
		{
			TileType tileType = new TileType(source, new Int3(graph.tileSizeX, 1, graph.tileSizeZ) * (1000f * graph.cellSize), centerOffset, width, depth);
			tileTypes.Add(tileType);
			return tileType;
		}

		public void CreateTileTypesFromGraph()
		{
			RecastGraph.NavmeshTile[] tiles = graph.GetTiles();
			if (tiles == null || tiles.Length != graph.tileXCount * graph.tileZCount)
			{
				throw new InvalidOperationException("Graph tiles are invalid (either null or number of tiles is not equal to width*depth of the graph");
			}
			for (int i = 0; i < graph.tileZCount; i++)
			{
				for (int j = 0; j < graph.tileXCount; j++)
				{
					RecastGraph.NavmeshTile navmeshTile = tiles[j + i * graph.tileXCount];
					Int3 @int = (Int3)graph.GetTileBounds(j, i).min;
					Int3 tileSize = new Int3(graph.tileSizeX, 1, graph.tileSizeZ) * (1000f * graph.cellSize);
					@int += new Int3(tileSize.x * navmeshTile.w / 2, 0, tileSize.z * navmeshTile.d / 2);
					@int = -@int;
					TileType tileType = new TileType(navmeshTile.verts, navmeshTile.tris, tileSize, @int, navmeshTile.w, navmeshTile.d);
					tileTypes.Add(tileType);
					int num = j + i * graph.tileXCount;
					activeTileTypes[num] = tileType;
					activeTileRotations[num] = 0;
					activeTileOffsets[num] = 0;
				}
			}
		}

		public bool StartBatchLoad()
		{
			if (isBatching)
			{
				return false;
			}
			isBatching = true;
			AstarPath.active.AddWorkItem(new AstarPath.AstarWorkItem(delegate
			{
				graph.StartBatchTileUpdate();
				return true;
			}));
			return true;
		}

		public void EndBatchLoad()
		{
			if (!isBatching)
			{
				throw new Exception("Ending batching when batching has not been started");
			}
			for (int i = 0; i < reloadedInBatch.Length; i++)
			{
				reloadedInBatch[i] = false;
			}
			isBatching = false;
			AstarPath.active.AddWorkItem(new AstarPath.AstarWorkItem(delegate
			{
				graph.EndBatchTileUpdate();
				return true;
			}));
		}

		private void CutPoly(Int3[] verts, int[] tris, ref Int3[] outVertsArr, ref int[] outTrisArr, out int outVCount, out int outTCount, Int3[] extraShape, Int3 cuttingOffset, Bounds realBounds, CutMode mode = CutMode.CutAll | CutMode.CutDual, int perturbate = 0)
		{
			//Discarded unreachable code: IL_100e
			if (verts.Length == 0 || tris.Length == 0)
			{
				outVCount = 0;
				outTCount = 0;
				outTrisArr = new int[0];
				outVertsArr = new Int3[0];
				return;
			}
			List<IntPoint> list = null;
			if (extraShape == null && (mode & CutMode.CutExtra) != 0)
			{
				throw new Exception("extraShape is null and the CutMode specifies that it should be used. Cannot use null shape.");
			}
			if ((mode & CutMode.CutExtra) != 0)
			{
				list = new List<IntPoint>(extraShape.Length);
				for (int i = 0; i < extraShape.Length; i++)
				{
					list.Add(new IntPoint(extraShape[i].x + cuttingOffset.x, extraShape[i].z + cuttingOffset.z));
				}
			}
			List<IntPoint> list2 = new List<IntPoint>(5);
			Dictionary<TriangulationPoint, int> dictionary = new Dictionary<TriangulationPoint, int>();
			List<PolygonPoint> list3 = new List<PolygonPoint>();
			IntRect b = new IntRect(verts[0].x, verts[0].z, verts[0].x, verts[0].z);
			for (int j = 0; j < verts.Length; j++)
			{
				b = b.ExpandToContain(verts[j].x, verts[j].z);
			}
			List<Int3> list4 = ListPool<Int3>.Claim(verts.Length * 2);
			List<int> list5 = ListPool<int>.Claim(tris.Length);
			PolyTree polyTree = new PolyTree();
			List<List<IntPoint>> list6 = new List<List<IntPoint>>();
			Stack<Pathfinding.Poly2Tri.Polygon> stack = new Stack<Pathfinding.Poly2Tri.Polygon>();
			clipper = clipper ?? new Clipper();
			clipper.ReverseSolution = true;
			clipper.StrictlySimple = true;
			List<NavmeshCut> list7 = ((mode != CutMode.CutExtra) ? NavmeshCut.GetAllInRange(realBounds) : ListPool<NavmeshCut>.Claim());
			List<int> list8 = ListPool<int>.Claim();
			List<IntRect> list9 = ListPool<IntRect>.Claim();
			List<Int2> list10 = ListPool<Int2>.Claim();
			List<List<IntPoint>> list11 = new List<List<IntPoint>>();
			List<bool> list12 = ListPool<bool>.Claim();
			List<bool> list13 = ListPool<bool>.Claim();
			if (perturbate > 10)
			{
				Debug.LogError("Too many perturbations aborting : " + mode);
				Debug.Break();
				outVCount = verts.Length;
				outTCount = tris.Length;
				outTrisArr = tris;
				outVertsArr = verts;
				return;
			}
			System.Random random = null;
			if (perturbate > 0)
			{
				random = new System.Random();
			}
			for (int k = 0; k < list7.Count; k++)
			{
				Bounds bounds = list7[k].GetBounds();
				Int3 @int = (Int3)bounds.min + cuttingOffset;
				Int3 int2 = (Int3)bounds.max + cuttingOffset;
				IntRect a = new IntRect(@int.x, @int.z, int2.x, int2.z);
				if (!IntRect.Intersects(a, b))
				{
					continue;
				}
				Int2 int3 = new Int2(0, 0);
				if (perturbate > 0)
				{
					int3.x = random.Next() % 6 * perturbate - 3 * perturbate;
					if (int3.x >= 0)
					{
						int3.x++;
					}
					int3.y = random.Next() % 6 * perturbate - 3 * perturbate;
					if (int3.y >= 0)
					{
						int3.y++;
					}
				}
				int count = list11.Count;
				list7[k].GetContour(list11);
				for (int l = count; l < list11.Count; l++)
				{
					List<IntPoint> list14 = list11[l];
					if (list14.Count == 0)
					{
						Debug.LogError("Zero Length Contour");
						list9.Add(default(IntRect));
						list10.Add(new Int2(0, 0));
						continue;
					}
					IntRect item = new IntRect((int)list14[0].X + cuttingOffset.x, (int)list14[0].Y + cuttingOffset.y, (int)list14[0].X + cuttingOffset.x, (int)list14[0].Y + cuttingOffset.y);
					for (int m = 0; m < list14.Count; m++)
					{
						IntPoint value = list14[m];
						value.X += cuttingOffset.x;
						value.Y += cuttingOffset.z;
						if (perturbate > 0)
						{
							value.X += int3.x;
							value.Y += int3.y;
						}
						list14[m] = value;
						item = item.ExpandToContain((int)value.X, (int)value.Y);
					}
					list10.Add(new Int2(@int.y, int2.y));
					list9.Add(item);
					list12.Add(list7[k].isDual);
					list13.Add(list7[k].cutsAddedGeom);
				}
			}
			List<NavmeshAdd> allInRange = NavmeshAdd.GetAllInRange(realBounds);
			Int3[] vbuffer = verts;
			int[] tbuffer = tris;
			int num = -1;
			int num2 = -3;
			Int3[] array = null;
			Int3[] array2 = null;
			Int3 int4 = Int3.zero;
			if (allInRange.Count > 0)
			{
				array = new Int3[7];
				array2 = new Int3[7];
				int4 = (Int3)realBounds.extents;
			}
			while (true)
			{
				num2 += 3;
				while (num2 >= tbuffer.Length)
				{
					num++;
					num2 = 0;
					if (num >= allInRange.Count)
					{
						vbuffer = null;
						break;
					}
					if (vbuffer == verts)
					{
						vbuffer = null;
					}
					allInRange[num].GetMesh(cuttingOffset, ref vbuffer, out tbuffer);
				}
				if (vbuffer == null)
				{
					break;
				}
				Int3 int5 = vbuffer[tbuffer[num2]];
				Int3 int6 = vbuffer[tbuffer[num2 + 1]];
				Int3 int7 = vbuffer[tbuffer[num2 + 2]];
				IntRect a2 = new IntRect(int5.x, int5.z, int5.x, int5.z).ExpandToContain(int6.x, int6.z).ExpandToContain(int7.x, int7.z);
				int num3 = Math.Min(int5.y, Math.Min(int6.y, int7.y));
				int num4 = Math.Max(int5.y, Math.Max(int6.y, int7.y));
				list8.Clear();
				bool flag = false;
				for (int n = 0; n < list11.Count; n++)
				{
					int x = list10[n].x;
					int y = list10[n].y;
					if (IntRect.Intersects(a2, list9[n]) && y >= num3 && x <= num4 && (list13[n] || num == -1))
					{
						Int3 int8 = int5;
						int8.y = x;
						Int3 int9 = int5;
						int9.y = y;
						list8.Add(n);
						flag |= list12[n];
					}
				}
				if (list8.Count == 0 && (mode & CutMode.CutExtra) == 0 && (mode & CutMode.CutAll) != 0 && num == -1)
				{
					list5.Add(list4.Count);
					list5.Add(list4.Count + 1);
					list5.Add(list4.Count + 2);
					list4.Add(int5);
					list4.Add(int6);
					list4.Add(int7);
					continue;
				}
				list2.Clear();
				if (num == -1)
				{
					list2.Add(new IntPoint(int5.x, int5.z));
					list2.Add(new IntPoint(int6.x, int6.z));
					list2.Add(new IntPoint(int7.x, int7.z));
				}
				else
				{
					array[0] = int5;
					array[1] = int6;
					array[2] = int7;
					int num5 = Utility.ClipPolygon(array, 3, array2, 1, 0, 0);
					if (num5 == 0)
					{
						continue;
					}
					num5 = Utility.ClipPolygon(array2, num5, array, -1, 2 * int4.x, 0);
					if (num5 == 0)
					{
						continue;
					}
					num5 = Utility.ClipPolygon(array, num5, array2, 1, 0, 2);
					if (num5 == 0)
					{
						continue;
					}
					num5 = Utility.ClipPolygon(array2, num5, array, -1, 2 * int4.z, 2);
					if (num5 == 0)
					{
						continue;
					}
					for (int num6 = 0; num6 < num5; num6++)
					{
						list2.Add(new IntPoint(array[num6].x, array[num6].z));
					}
				}
				dictionary.Clear();
				Int3 int10 = int6 - int5;
				Int3 int11 = int7 - int5;
				Int3 int12 = int10;
				Int3 int13 = int11;
				int12.y = 0;
				int13.y = 0;
				for (int num7 = 0; num7 < 16; num7++)
				{
					if ((((int)mode >> num7) & 1) == 0)
					{
						continue;
					}
					if (1 << num7 == 1)
					{
						clipper.Clear();
						clipper.AddPolygon(list2, PolyType.ptSubject);
						for (int num8 = 0; num8 < list8.Count; num8++)
						{
							clipper.AddPolygon(list11[list8[num8]], PolyType.ptClip);
						}
						polyTree.Clear();
						clipper.Execute(ClipType.ctDifference, polyTree, PolyFillType.pftEvenOdd, PolyFillType.pftNonZero);
					}
					else if (1 << num7 == 2)
					{
						if (!flag)
						{
							continue;
						}
						clipper.Clear();
						clipper.AddPolygon(list2, PolyType.ptSubject);
						for (int num9 = 0; num9 < list8.Count; num9++)
						{
							if (list12[list8[num9]])
							{
								clipper.AddPolygon(list11[list8[num9]], PolyType.ptClip);
							}
						}
						list6.Clear();
						clipper.Execute(ClipType.ctIntersection, list6, PolyFillType.pftEvenOdd, PolyFillType.pftNonZero);
						clipper.Clear();
						for (int num10 = 0; num10 < list6.Count; num10++)
						{
							clipper.AddPolygon(list6[num10], Clipper.Orientation(list6[num10]) ? PolyType.ptClip : PolyType.ptSubject);
						}
						for (int num11 = 0; num11 < list8.Count; num11++)
						{
							if (!list12[list8[num11]])
							{
								clipper.AddPolygon(list11[list8[num11]], PolyType.ptClip);
							}
						}
						polyTree.Clear();
						clipper.Execute(ClipType.ctDifference, polyTree, PolyFillType.pftEvenOdd, PolyFillType.pftNonZero);
					}
					else if (1 << num7 == 4)
					{
						clipper.Clear();
						clipper.AddPolygon(list2, PolyType.ptSubject);
						clipper.AddPolygon(list, PolyType.ptClip);
						polyTree.Clear();
						clipper.Execute(ClipType.ctIntersection, polyTree, PolyFillType.pftEvenOdd, PolyFillType.pftNonZero);
					}
					for (int num12 = 0; num12 < polyTree.ChildCount; num12++)
					{
						PolyNode polyNode = polyTree.Childs[num12];
						List<IntPoint> contour = polyNode.Contour;
						List<PolyNode> childs = polyNode.Childs;
						if (childs.Count == 0 && contour.Count == 3 && num == -1)
						{
							for (int num13 = 0; num13 < contour.Count; num13++)
							{
								Int3 item2 = new Int3((int)contour[num13].X, 0, (int)contour[num13].Y);
								double num14 = (double)(int6.z - int7.z) * (double)(int5.x - int7.x) + (double)(int7.x - int6.x) * (double)(int5.z - int7.z);
								if (num14 == 0.0)
								{
									Debug.LogWarning("Degenerate triangle");
									continue;
								}
								double num15 = ((double)(int6.z - int7.z) * (double)(item2.x - int7.x) + (double)(int7.x - int6.x) * (double)(item2.z - int7.z)) / num14;
								double num16 = ((double)(int7.z - int5.z) * (double)(item2.x - int7.x) + (double)(int5.x - int7.x) * (double)(item2.z - int7.z)) / num14;
								item2.y = (int)Math.Round(num15 * (double)int5.y + num16 * (double)int6.y + (1.0 - num15 - num16) * (double)int7.y);
								list5.Add(list4.Count);
								list4.Add(item2);
							}
							continue;
						}
						Pathfinding.Poly2Tri.Polygon polygon = null;
						int num17 = -1;
						for (List<IntPoint> list15 = contour; list15 != null; list15 = ((num17 >= childs.Count) ? null : childs[num17].Contour))
						{
							list3.Clear();
							for (int num18 = 0; num18 < list15.Count; num18++)
							{
								PolygonPoint polygonPoint = new PolygonPoint(list15[num18].X, list15[num18].Y);
								list3.Add(polygonPoint);
								Int3 item3 = new Int3((int)list15[num18].X, 0, (int)list15[num18].Y);
								double num19 = (double)(int6.z - int7.z) * (double)(int5.x - int7.x) + (double)(int7.x - int6.x) * (double)(int5.z - int7.z);
								if (num19 == 0.0)
								{
									Debug.LogWarning("Degenerate triangle");
									continue;
								}
								double num20 = ((double)(int6.z - int7.z) * (double)(item3.x - int7.x) + (double)(int7.x - int6.x) * (double)(item3.z - int7.z)) / num19;
								double num21 = ((double)(int7.z - int5.z) * (double)(item3.x - int7.x) + (double)(int5.x - int7.x) * (double)(item3.z - int7.z)) / num19;
								item3.y = (int)Math.Round(num20 * (double)int5.y + num21 * (double)int6.y + (1.0 - num20 - num21) * (double)int7.y);
								dictionary[polygonPoint] = list4.Count;
								list4.Add(item3);
							}
							Pathfinding.Poly2Tri.Polygon polygon2 = null;
							if (stack.Count > 0)
							{
								polygon2 = stack.Pop();
								polygon2.AddPoints(list3);
							}
							else
							{
								polygon2 = new Pathfinding.Poly2Tri.Polygon(list3);
							}
							if (polygon == null)
							{
								polygon = polygon2;
							}
							else
							{
								polygon.AddHole(polygon2);
							}
							num17++;
						}
						try
						{
							P2T.Triangulate(polygon);
						}
						catch (PointOnEdgeException)
						{
							Debug.LogWarning(string.Concat("PointOnEdgeException, perturbating vertices slightly ( at ", num7, " in ", mode, ")"));
							CutPoly(verts, tris, ref outVertsArr, ref outTrisArr, out outVCount, out outTCount, extraShape, cuttingOffset, realBounds, mode, perturbate + 1);
							return;
						}
						for (int num22 = 0; num22 < polygon.Triangles.Count; num22++)
						{
							DelaunayTriangle delaunayTriangle = polygon.Triangles[num22];
							list5.Add(dictionary[delaunayTriangle.Points._0]);
							list5.Add(dictionary[delaunayTriangle.Points._1]);
							list5.Add(dictionary[delaunayTriangle.Points._2]);
						}
						if (polygon.Holes != null)
						{
							for (int num23 = 0; num23 < polygon.Holes.Count; num23++)
							{
								polygon.Holes[num23].Points.Clear();
								polygon.Holes[num23].ClearTriangles();
								if (polygon.Holes[num23].Holes != null)
								{
									polygon.Holes[num23].Holes.Clear();
								}
								stack.Push(polygon.Holes[num23]);
							}
						}
						polygon.ClearTriangles();
						if (polygon.Holes != null)
						{
							polygon.Holes.Clear();
						}
						polygon.Points.Clear();
						stack.Push(polygon);
					}
				}
			}
			Dictionary<Int3, int> dictionary2 = cached_Int3_int_dict;
			dictionary2.Clear();
			if (cached_int_array.Length < list4.Count)
			{
				cached_int_array = new int[Math.Max(cached_int_array.Length * 2, list4.Count)];
			}
			int[] array3 = cached_int_array;
			int num24 = 0;
			for (int num25 = 0; num25 < list4.Count; num25++)
			{
				int value2;
				if (!dictionary2.TryGetValue(list4[num25], out value2))
				{
					dictionary2.Add(list4[num25], num24);
					array3[num25] = num24;
					list4[num24] = list4[num25];
					num24++;
				}
				else
				{
					array3[num25] = value2;
				}
			}
			outTCount = list5.Count;
			if (outTrisArr == null || outTrisArr.Length < outTCount)
			{
				outTrisArr = new int[outTCount];
			}
			for (int num26 = 0; num26 < outTCount; num26++)
			{
				outTrisArr[num26] = array3[list5[num26]];
			}
			outVCount = num24;
			if (outVertsArr == null || outVertsArr.Length < outVCount)
			{
				outVertsArr = new Int3[outVCount];
			}
			for (int num27 = 0; num27 < outVCount; num27++)
			{
				outVertsArr[num27] = list4[num27];
			}
			for (int num28 = 0; num28 < list7.Count; num28++)
			{
				list7[num28].UsedForCut();
			}
			ListPool<Int3>.Release(list4);
			ListPool<int>.Release(list5);
			ListPool<int>.Release(list8);
			ListPool<Int2>.Release(list10);
			ListPool<bool>.Release(list12);
			ListPool<bool>.Release(list13);
			ListPool<IntRect>.Release(list9);
			ListPool<NavmeshCut>.Release(list7);
		}

		private void DelaunayRefinement(Int3[] verts, int[] tris, ref int vCount, ref int tCount, bool delaunay, bool colinear, Int3 worldOffset)
		{
			if (tCount % 3 != 0)
			{
				throw new Exception("Triangle array length must be a multiple of 3");
			}
			Dictionary<Int2, int> dictionary = cached_Int2_int_dict;
			dictionary.Clear();
			for (int i = 0; i < tCount; i += 3)
			{
				if (!VectorMath.IsClockwiseXZ(verts[tris[i]], verts[tris[i + 1]], verts[tris[i + 2]]))
				{
					int num = tris[i];
					tris[i] = tris[i + 2];
					tris[i + 2] = num;
				}
				dictionary[new Int2(tris[i], tris[i + 1])] = i + 2;
				dictionary[new Int2(tris[i + 1], tris[i + 2])] = i;
				dictionary[new Int2(tris[i + 2], tris[i])] = i + 1;
			}
			for (int j = 0; j < tCount; j += 3)
			{
				for (int k = 0; k < 3; k++)
				{
					int value;
					if (!dictionary.TryGetValue(new Int2(tris[j + (k + 1) % 3], tris[j + k % 3]), out value))
					{
						continue;
					}
					Int3 @int = verts[tris[j + (k + 2) % 3]];
					Int3 int2 = verts[tris[j + (k + 1) % 3]];
					Int3 int3 = verts[tris[j + (k + 3) % 3]];
					Int3 int4 = verts[tris[value]];
					@int.y = 0;
					int2.y = 0;
					int3.y = 0;
					int4.y = 0;
					bool flag = false;
					if (!VectorMath.RightOrColinearXZ(@int, int3, int4) || VectorMath.RightXZ(@int, int2, int4))
					{
						if (!colinear)
						{
							continue;
						}
						flag = true;
					}
					if (colinear && VectorMath.SqrDistancePointSegmentApproximate(@int, int4, int2) < 9f && !dictionary.ContainsKey(new Int2(tris[j + (k + 2) % 3], tris[j + (k + 1) % 3])) && !dictionary.ContainsKey(new Int2(tris[j + (k + 1) % 3], tris[value])))
					{
						tCount -= 3;
						int num2 = value / 3 * 3;
						tris[j + (k + 1) % 3] = tris[value];
						if (num2 != tCount)
						{
							tris[num2] = tris[tCount];
							tris[num2 + 1] = tris[tCount + 1];
							tris[num2 + 2] = tris[tCount + 2];
							dictionary[new Int2(tris[num2], tris[num2 + 1])] = num2 + 2;
							dictionary[new Int2(tris[num2 + 1], tris[num2 + 2])] = num2;
							dictionary[new Int2(tris[num2 + 2], tris[num2])] = num2 + 1;
							tris[tCount] = 0;
							tris[tCount + 1] = 0;
							tris[tCount + 2] = 0;
						}
						else
						{
							tCount += 3;
						}
						dictionary[new Int2(tris[j], tris[j + 1])] = j + 2;
						dictionary[new Int2(tris[j + 1], tris[j + 2])] = j;
						dictionary[new Int2(tris[j + 2], tris[j])] = j + 1;
					}
					else if (delaunay && !flag)
					{
						float num3 = Int3.Angle(int2 - @int, int3 - @int);
						float num4 = Int3.Angle(int2 - int4, int3 - int4);
						if (num4 > (float)Math.PI * 2f - 2f * num3)
						{
							tris[j + (k + 1) % 3] = tris[value];
							int num5 = value / 3 * 3;
							int num6 = value - num5;
							tris[num5 + (num6 - 1 + 3) % 3] = tris[j + (k + 2) % 3];
							dictionary[new Int2(tris[j], tris[j + 1])] = j + 2;
							dictionary[new Int2(tris[j + 1], tris[j + 2])] = j;
							dictionary[new Int2(tris[j + 2], tris[j])] = j + 1;
							dictionary[new Int2(tris[num5], tris[num5 + 1])] = num5 + 2;
							dictionary[new Int2(tris[num5 + 1], tris[num5 + 2])] = num5;
							dictionary[new Int2(tris[num5 + 2], tris[num5])] = num5 + 1;
						}
					}
				}
			}
		}

		private Vector3 Point2D2V3(TriangulationPoint p)
		{
			return new Vector3((float)p.X, 0f, (float)p.Y) * 0.001f;
		}

		private Int3 IntPoint2Int3(IntPoint p)
		{
			return new Int3((int)p.X, 0, (int)p.Y);
		}

		public void ClearTile(int x, int z)
		{
			if (!(AstarPath.active == null) && x >= 0 && z >= 0 && x < graph.tileXCount && z < graph.tileZCount)
			{
				AstarPath.active.AddWorkItem(new AstarPath.AstarWorkItem(delegate
				{
					graph.ReplaceTile(x, z, new Int3[0], new int[0], false);
					activeTileTypes[x + z * graph.tileXCount] = null;
					GraphModifier.TriggerEvent(GraphModifier.EventType.PostUpdate);
					AstarPath.active.QueueWorkItemFloodFill();
					return true;
				}));
			}
		}

		public void ReloadInBounds(Bounds b)
		{
			Int2 tileCoordinates = graph.GetTileCoordinates(b.min);
			Int2 tileCoordinates2 = graph.GetTileCoordinates(b.max);
			IntRect a = new IntRect(tileCoordinates.x, tileCoordinates.y, tileCoordinates2.x, tileCoordinates2.y);
			a = IntRect.Intersection(a, new IntRect(0, 0, graph.tileXCount - 1, graph.tileZCount - 1));
			if (!a.IsValid())
			{
				return;
			}
			for (int i = a.ymin; i <= a.ymax; i++)
			{
				for (int j = a.xmin; j <= a.xmax; j++)
				{
					ReloadTile(j, i);
				}
			}
		}

		public void ReloadTile(int x, int z)
		{
			if (x >= 0 && z >= 0 && x < graph.tileXCount && z < graph.tileZCount)
			{
				int num = x + z * graph.tileXCount;
				if (activeTileTypes[num] != null)
				{
					LoadTile(activeTileTypes[num], x, z, activeTileRotations[num], activeTileOffsets[num]);
				}
			}
		}

		public void CutShapeWithTile(int x, int z, Int3[] shape, ref Int3[] verts, ref int[] tris, out int vCount, out int tCount)
		{
			if (isBatching)
			{
				throw new Exception("Cannot cut with shape when batching. Please stop batching first.");
			}
			int num = x + z * graph.tileXCount;
			if (x < 0 || z < 0 || x >= graph.tileXCount || z >= graph.tileZCount || activeTileTypes[num] == null)
			{
				verts = new Int3[0];
				tris = new int[0];
				vCount = 0;
				tCount = 0;
				return;
			}
			Int3[] verts2;
			int[] tris2;
			activeTileTypes[num].Load(out verts2, out tris2, activeTileRotations[num], activeTileOffsets[num]);
			Bounds tileBounds = graph.GetTileBounds(x, z);
			Int3 @int = (Int3)tileBounds.min;
			@int = -@int;
			CutPoly(verts2, tris2, ref verts, ref tris, out vCount, out tCount, shape, @int, tileBounds, CutMode.CutExtra);
			for (int i = 0; i < verts.Length; i++)
			{
				verts[i] -= @int;
			}
		}

		protected static T[] ShrinkArray<T>(T[] arr, int newLength)
		{
			newLength = Math.Min(newLength, arr.Length);
			T[] array = new T[newLength];
			if (newLength % 4 == 0)
			{
				for (int i = 0; i < newLength; i += 4)
				{
					array[i] = arr[i];
					array[i + 1] = arr[i + 1];
					array[i + 2] = arr[i + 2];
					array[i + 3] = arr[i + 3];
				}
			}
			else if (newLength % 3 == 0)
			{
				for (int j = 0; j < newLength; j += 3)
				{
					array[j] = arr[j];
					array[j + 1] = arr[j + 1];
					array[j + 2] = arr[j + 2];
				}
			}
			else if (newLength % 2 == 0)
			{
				for (int k = 0; k < newLength; k += 2)
				{
					array[k] = arr[k];
					array[k + 1] = arr[k + 1];
				}
			}
			else
			{
				for (int l = 0; l < newLength; l++)
				{
					array[l] = arr[l];
				}
			}
			return array;
		}

		public void LoadTile(TileType tile, int x, int z, int rotation, int yoffset)
		{
			if (tile == null)
			{
				throw new ArgumentNullException("tile");
			}
			if (AstarPath.active == null)
			{
				return;
			}
			int index = x + z * graph.tileXCount;
			rotation %= 4;
			if (isBatching && reloadedInBatch[index] && activeTileOffsets[index] == yoffset && activeTileRotations[index] == rotation && activeTileTypes[index] == tile)
			{
				return;
			}
			reloadedInBatch[index] |= isBatching;
			activeTileOffsets[index] = yoffset;
			activeTileRotations[index] = rotation;
			activeTileTypes[index] = tile;
			AstarPath.active.AddWorkItem(new AstarPath.AstarWorkItem(delegate
			{
				if (activeTileOffsets[index] != yoffset || activeTileRotations[index] != rotation || activeTileTypes[index] != tile)
				{
					return true;
				}
				GraphModifier.TriggerEvent(GraphModifier.EventType.PreUpdate);
				Int3[] verts;
				int[] tris;
				tile.Load(out verts, out tris, rotation, yoffset);
				Bounds tileBounds = graph.GetTileBounds(x, z, tile.Width, tile.Depth);
				Int3 @int = (Int3)tileBounds.min;
				@int = -@int;
				Int3[] outVertsArr = null;
				int[] outTrisArr = null;
				int outVCount;
				int outTCount;
				CutPoly(verts, tris, ref outVertsArr, ref outTrisArr, out outVCount, out outTCount, null, @int, tileBounds);
				DelaunayRefinement(outVertsArr, outTrisArr, ref outVCount, ref outTCount, true, false, -@int);
				if (outTCount != outTrisArr.Length)
				{
					outTrisArr = ShrinkArray(outTrisArr, outTCount);
				}
				if (outVCount != outVertsArr.Length)
				{
					outVertsArr = ShrinkArray(outVertsArr, outVCount);
				}
				int w = ((rotation % 2 != 0) ? tile.Depth : tile.Width);
				int d = ((rotation % 2 != 0) ? tile.Width : tile.Depth);
				graph.ReplaceTile(x, z, w, d, outVertsArr, outTrisArr, false);
				GraphModifier.TriggerEvent(GraphModifier.EventType.PostUpdate);
				AstarPath.active.QueueWorkItemFloodFill();
				return true;
			}));
		}
	}
}
