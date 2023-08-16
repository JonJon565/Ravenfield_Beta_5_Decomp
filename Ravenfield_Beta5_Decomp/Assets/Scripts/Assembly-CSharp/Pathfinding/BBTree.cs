using System;
using UnityEngine;

namespace Pathfinding
{
	public class BBTree
	{
		private struct BBTreeBox
		{
			public IntRect rect;

			public MeshNode node;

			public int left;

			public int right;

			public bool IsLeaf
			{
				get
				{
					return node != null;
				}
			}

			public BBTreeBox(IntRect rect)
			{
				node = null;
				this.rect = rect;
				left = (right = -1);
			}

			public BBTreeBox(MeshNode node)
			{
				this.node = node;
				Int3 vertex = node.GetVertex(0);
				Int2 @int = new Int2(vertex.x, vertex.z);
				Int2 int2 = @int;
				for (int i = 1; i < node.GetVertexCount(); i++)
				{
					Int3 vertex2 = node.GetVertex(i);
					@int.x = Math.Min(@int.x, vertex2.x);
					@int.y = Math.Min(@int.y, vertex2.z);
					int2.x = Math.Max(int2.x, vertex2.x);
					int2.y = Math.Max(int2.y, vertex2.z);
				}
				rect = new IntRect(@int.x, @int.y, int2.x, int2.y);
				left = (right = -1);
			}

			public bool Contains(Vector3 p)
			{
				Int3 @int = (Int3)p;
				return rect.Contains(@int.x, @int.z);
			}
		}

		private BBTreeBox[] arr = new BBTreeBox[6];

		private int count;

		public Rect Size
		{
			get
			{
				if (count == 0)
				{
					return new Rect(0f, 0f, 0f, 0f);
				}
				IntRect rect = arr[0].rect;
				return Rect.MinMaxRect((float)rect.xmin * 0.001f, (float)rect.ymin * 0.001f, (float)rect.xmax * 0.001f, (float)rect.ymax * 0.001f);
			}
		}

		public void Clear()
		{
			count = 0;
		}

		private void EnsureCapacity(int c)
		{
			if (arr.Length < c)
			{
				BBTreeBox[] array = new BBTreeBox[Math.Max(c, (int)((float)arr.Length * 1.5f))];
				for (int i = 0; i < count; i++)
				{
					array[i] = arr[i];
				}
				arr = array;
			}
		}

		private int GetBox(MeshNode node)
		{
			if (count >= arr.Length)
			{
				EnsureCapacity(count + 1);
			}
			arr[count] = new BBTreeBox(node);
			count++;
			return count - 1;
		}

		private int GetBox(IntRect rect)
		{
			if (count >= arr.Length)
			{
				EnsureCapacity(count + 1);
			}
			arr[count] = new BBTreeBox(rect);
			count++;
			return count - 1;
		}

		public void RebuildFrom(MeshNode[] nodes)
		{
			Clear();
			if (nodes.Length == 0)
			{
				return;
			}
			if (nodes.Length == 1)
			{
				GetBox(nodes[0]);
				return;
			}
			EnsureCapacity(Mathf.CeilToInt((float)nodes.Length * 2.1f));
			MeshNode[] array = new MeshNode[nodes.Length];
			for (int i = 0; i < nodes.Length; i++)
			{
				array[i] = nodes[i];
			}
			RebuildFromInternal(array, 0, nodes.Length, false);
		}

		private static int SplitByX(MeshNode[] nodes, int from, int to, int divider)
		{
			int num = to;
			for (int i = from; i < num; i++)
			{
				if (nodes[i].position.x > divider)
				{
					num--;
					MeshNode meshNode = nodes[num];
					nodes[num] = nodes[i];
					nodes[i] = meshNode;
					i--;
				}
			}
			return num;
		}

		private static int SplitByZ(MeshNode[] nodes, int from, int to, int divider)
		{
			int num = to;
			for (int i = from; i < num; i++)
			{
				if (nodes[i].position.z > divider)
				{
					num--;
					MeshNode meshNode = nodes[num];
					nodes[num] = nodes[i];
					nodes[i] = meshNode;
					i--;
				}
			}
			return num;
		}

		private int RebuildFromInternal(MeshNode[] nodes, int from, int to, bool odd)
		{
			if (to - from <= 0)
			{
				throw new ArgumentException();
			}
			if (to - from == 1)
			{
				return GetBox(nodes[from]);
			}
			IntRect rect = NodeBounds(nodes, from, to);
			int box = GetBox(rect);
			if (to - from == 2)
			{
				arr[box].left = GetBox(nodes[from]);
				arr[box].right = GetBox(nodes[from + 1]);
				return box;
			}
			int num;
			if (odd)
			{
				int divider = (rect.xmin + rect.xmax) / 2;
				num = SplitByX(nodes, from, to, divider);
			}
			else
			{
				int divider2 = (rect.ymin + rect.ymax) / 2;
				num = SplitByZ(nodes, from, to, divider2);
			}
			if (num == from || num == to)
			{
				if (!odd)
				{
					int divider3 = (rect.xmin + rect.xmax) / 2;
					num = SplitByX(nodes, from, to, divider3);
				}
				else
				{
					int divider4 = (rect.ymin + rect.ymax) / 2;
					num = SplitByZ(nodes, from, to, divider4);
				}
				if (num == from || num == to)
				{
					num = (from + to) / 2;
				}
			}
			arr[box].left = RebuildFromInternal(nodes, from, num, !odd);
			arr[box].right = RebuildFromInternal(nodes, num, to, !odd);
			return box;
		}

		private static IntRect NodeBounds(MeshNode[] nodes, int from, int to)
		{
			if (to - from <= 0)
			{
				throw new ArgumentException();
			}
			Int3 vertex = nodes[from].GetVertex(0);
			Int2 @int = new Int2(vertex.x, vertex.z);
			Int2 int2 = @int;
			for (int i = from; i < to; i++)
			{
				MeshNode meshNode = nodes[i];
				int vertexCount = meshNode.GetVertexCount();
				for (int j = 0; j < vertexCount; j++)
				{
					Int3 vertex2 = meshNode.GetVertex(j);
					@int.x = Math.Min(@int.x, vertex2.x);
					@int.y = Math.Min(@int.y, vertex2.z);
					int2.x = Math.Max(int2.x, vertex2.x);
					int2.y = Math.Max(int2.y, vertex2.z);
				}
			}
			return new IntRect(@int.x, @int.y, int2.x, int2.y);
		}

		public void Insert(MeshNode node)
		{
			int box = GetBox(node);
			if (box == 0)
			{
				return;
			}
			BBTreeBox bBTreeBox = arr[box];
			int num = 0;
			BBTreeBox bBTreeBox2;
			while (true)
			{
				bBTreeBox2 = arr[num];
				bBTreeBox2.rect = ExpandToContain(bBTreeBox2.rect, bBTreeBox.rect);
				if (bBTreeBox2.node != null)
				{
					break;
				}
				arr[num] = bBTreeBox2;
				int num2 = ExpansionRequired(arr[bBTreeBox2.left].rect, bBTreeBox.rect);
				int num3 = ExpansionRequired(arr[bBTreeBox2.right].rect, bBTreeBox.rect);
				num = ((num2 >= num3) ? ((num3 >= num2) ? ((RectArea(arr[bBTreeBox2.left].rect) >= RectArea(arr[bBTreeBox2.right].rect)) ? bBTreeBox2.right : bBTreeBox2.left) : bBTreeBox2.right) : bBTreeBox2.left);
			}
			bBTreeBox2.left = box;
			int box2 = GetBox(bBTreeBox2.node);
			bBTreeBox2.right = box2;
			bBTreeBox2.node = null;
			arr[num] = bBTreeBox2;
		}

		public NNInfo Query(Vector3 p, NNConstraint constraint)
		{
			if (count == 0)
			{
				return new NNInfo(null);
			}
			NNInfo nnInfo = default(NNInfo);
			SearchBox(0, p, constraint, ref nnInfo);
			nnInfo.UpdateInfo();
			return nnInfo;
		}

		public NNInfo QueryCircle(Vector3 p, float radius, NNConstraint constraint)
		{
			if (count == 0)
			{
				return new NNInfo(null);
			}
			NNInfo nnInfo = new NNInfo(null);
			SearchBoxCircle(0, p, radius, constraint, ref nnInfo);
			nnInfo.UpdateInfo();
			return nnInfo;
		}

		public NNInfo QueryClosest(Vector3 p, NNConstraint constraint, out float distance)
		{
			distance = float.PositiveInfinity;
			return QueryClosest(p, constraint, ref distance, new NNInfo(null));
		}

		public NNInfo QueryClosestXZ(Vector3 p, NNConstraint constraint, ref float distance, NNInfo previous)
		{
			if (count == 0)
			{
				return previous;
			}
			SearchBoxClosestXZ(0, p, ref distance, constraint, ref previous);
			return previous;
		}

		private void SearchBoxClosestXZ(int boxi, Vector3 p, ref float closestDist, NNConstraint constraint, ref NNInfo nnInfo)
		{
			BBTreeBox bBTreeBox = arr[boxi];
			if (bBTreeBox.node != null)
			{
				Vector3 constClampedPosition = bBTreeBox.node.ClosestPointOnNodeXZ(p);
				if (constraint == null || constraint.Suitable(bBTreeBox.node))
				{
					float num = (constClampedPosition.x - p.x) * (constClampedPosition.x - p.x) + (constClampedPosition.z - p.z) * (constClampedPosition.z - p.z);
					if (nnInfo.constrainedNode == null)
					{
						nnInfo.constrainedNode = bBTreeBox.node;
						nnInfo.constClampedPosition = constClampedPosition;
						closestDist = (float)Math.Sqrt(num);
					}
					else if (num < closestDist * closestDist)
					{
						nnInfo.constrainedNode = bBTreeBox.node;
						nnInfo.constClampedPosition = constClampedPosition;
						closestDist = (float)Math.Sqrt(num);
					}
				}
			}
			else
			{
				if (RectIntersectsCircle(arr[bBTreeBox.left].rect, p, closestDist))
				{
					SearchBoxClosestXZ(bBTreeBox.left, p, ref closestDist, constraint, ref nnInfo);
				}
				if (RectIntersectsCircle(arr[bBTreeBox.right].rect, p, closestDist))
				{
					SearchBoxClosestXZ(bBTreeBox.right, p, ref closestDist, constraint, ref nnInfo);
				}
			}
		}

		public NNInfo QueryClosest(Vector3 p, NNConstraint constraint, ref float distance, NNInfo previous)
		{
			if (count == 0)
			{
				return previous;
			}
			SearchBoxClosest(0, p, ref distance, constraint, ref previous);
			return previous;
		}

		private void SearchBoxClosest(int boxi, Vector3 p, ref float closestDist, NNConstraint constraint, ref NNInfo nnInfo)
		{
			BBTreeBox bBTreeBox = arr[boxi];
			if (bBTreeBox.node != null)
			{
				if (!NodeIntersectsCircle(bBTreeBox.node, p, closestDist))
				{
					return;
				}
				Vector3 vector = bBTreeBox.node.ClosestPointOnNode(p);
				if (constraint == null || constraint.Suitable(bBTreeBox.node))
				{
					float sqrMagnitude = (vector - p).sqrMagnitude;
					if (nnInfo.constrainedNode == null)
					{
						nnInfo.constrainedNode = bBTreeBox.node;
						nnInfo.constClampedPosition = vector;
						closestDist = (float)Math.Sqrt(sqrMagnitude);
					}
					else if (sqrMagnitude < closestDist * closestDist)
					{
						nnInfo.constrainedNode = bBTreeBox.node;
						nnInfo.constClampedPosition = vector;
						closestDist = (float)Math.Sqrt(sqrMagnitude);
					}
				}
			}
			else
			{
				if (RectIntersectsCircle(arr[bBTreeBox.left].rect, p, closestDist))
				{
					SearchBoxClosest(bBTreeBox.left, p, ref closestDist, constraint, ref nnInfo);
				}
				if (RectIntersectsCircle(arr[bBTreeBox.right].rect, p, closestDist))
				{
					SearchBoxClosest(bBTreeBox.right, p, ref closestDist, constraint, ref nnInfo);
				}
			}
		}

		public MeshNode QueryInside(Vector3 p, NNConstraint constraint)
		{
			return (count == 0) ? null : SearchBoxInside(0, p, constraint);
		}

		private MeshNode SearchBoxInside(int boxi, Vector3 p, NNConstraint constraint)
		{
			BBTreeBox bBTreeBox = arr[boxi];
			if (bBTreeBox.node != null)
			{
				if (bBTreeBox.node.ContainsPoint((Int3)p) && (constraint == null || constraint.Suitable(bBTreeBox.node)))
				{
					return bBTreeBox.node;
				}
			}
			else
			{
				if (arr[bBTreeBox.left].Contains(p))
				{
					MeshNode meshNode = SearchBoxInside(bBTreeBox.left, p, constraint);
					if (meshNode != null)
					{
						return meshNode;
					}
				}
				if (arr[bBTreeBox.right].Contains(p))
				{
					MeshNode meshNode = SearchBoxInside(bBTreeBox.right, p, constraint);
					if (meshNode != null)
					{
						return meshNode;
					}
				}
			}
			return null;
		}

		private void SearchBoxCircle(int boxi, Vector3 p, float radius, NNConstraint constraint, ref NNInfo nnInfo)
		{
			BBTreeBox bBTreeBox = arr[boxi];
			if (bBTreeBox.node != null)
			{
				if (!NodeIntersectsCircle(bBTreeBox.node, p, radius))
				{
					return;
				}
				Vector3 vector = bBTreeBox.node.ClosestPointOnNode(p);
				float sqrMagnitude = (vector - p).sqrMagnitude;
				if (nnInfo.node == null)
				{
					nnInfo.node = bBTreeBox.node;
					nnInfo.clampedPosition = vector;
				}
				else if (sqrMagnitude < (nnInfo.clampedPosition - p).sqrMagnitude)
				{
					nnInfo.node = bBTreeBox.node;
					nnInfo.clampedPosition = vector;
				}
				if (constraint == null || constraint.Suitable(bBTreeBox.node))
				{
					if (nnInfo.constrainedNode == null)
					{
						nnInfo.constrainedNode = bBTreeBox.node;
						nnInfo.constClampedPosition = vector;
					}
					else if (sqrMagnitude < (nnInfo.constClampedPosition - p).sqrMagnitude)
					{
						nnInfo.constrainedNode = bBTreeBox.node;
						nnInfo.constClampedPosition = vector;
					}
				}
			}
			else
			{
				if (RectIntersectsCircle(arr[bBTreeBox.left].rect, p, radius))
				{
					SearchBoxCircle(bBTreeBox.left, p, radius, constraint, ref nnInfo);
				}
				if (RectIntersectsCircle(arr[bBTreeBox.right].rect, p, radius))
				{
					SearchBoxCircle(bBTreeBox.right, p, radius, constraint, ref nnInfo);
				}
			}
		}

		private void SearchBox(int boxi, Vector3 p, NNConstraint constraint, ref NNInfo nnInfo)
		{
			BBTreeBox bBTreeBox = arr[boxi];
			if (bBTreeBox.node != null)
			{
				if (!bBTreeBox.node.ContainsPoint((Int3)p))
				{
					return;
				}
				if (nnInfo.node == null)
				{
					nnInfo.node = bBTreeBox.node;
				}
				else if (Mathf.Abs(((Vector3)bBTreeBox.node.position).y - p.y) < Mathf.Abs(((Vector3)nnInfo.node.position).y - p.y))
				{
					nnInfo.node = bBTreeBox.node;
				}
				if (constraint.Suitable(bBTreeBox.node))
				{
					if (nnInfo.constrainedNode == null)
					{
						nnInfo.constrainedNode = bBTreeBox.node;
					}
					else if (Mathf.Abs((float)bBTreeBox.node.position.y - p.y) < Mathf.Abs((float)nnInfo.constrainedNode.position.y - p.y))
					{
						nnInfo.constrainedNode = bBTreeBox.node;
					}
				}
			}
			else
			{
				if (arr[bBTreeBox.left].Contains(p))
				{
					SearchBox(bBTreeBox.left, p, constraint, ref nnInfo);
				}
				if (arr[bBTreeBox.right].Contains(p))
				{
					SearchBox(bBTreeBox.right, p, constraint, ref nnInfo);
				}
			}
		}

		public void OnDrawGizmos()
		{
			Gizmos.color = new Color(1f, 1f, 1f, 0.5f);
			if (count != 0)
			{
				OnDrawGizmos(0, 0);
			}
		}

		private void OnDrawGizmos(int boxi, int depth)
		{
			BBTreeBox bBTreeBox = arr[boxi];
			Vector3 vector = (Vector3)new Int3(bBTreeBox.rect.xmin, 0, bBTreeBox.rect.ymin);
			Vector3 vector2 = (Vector3)new Int3(bBTreeBox.rect.xmax, 0, bBTreeBox.rect.ymax);
			Vector3 vector3 = (vector + vector2) * 0.5f;
			Vector3 vector4 = (vector2 - vector3) * 2f;
			vector4 = new Vector3(vector4.x, 1f, vector4.z);
			vector3.y += depth * 2;
			Gizmos.color = AstarMath.IntToColor(depth, 1f);
			Gizmos.DrawCube(vector3, vector4);
			if (bBTreeBox.node == null)
			{
				OnDrawGizmos(bBTreeBox.left, depth + 1);
				OnDrawGizmos(bBTreeBox.right, depth + 1);
			}
		}

		private static bool NodeIntersectsCircle(MeshNode node, Vector3 p, float radius)
		{
			if (float.IsPositiveInfinity(radius))
			{
				return true;
			}
			return (p - node.ClosestPointOnNode(p)).sqrMagnitude < radius * radius;
		}

		private static bool RectIntersectsCircle(IntRect r, Vector3 p, float radius)
		{
			if (float.IsPositiveInfinity(radius))
			{
				return true;
			}
			Vector3 vector = p;
			p.x = Math.Max(p.x, (float)r.xmin * 0.001f);
			p.x = Math.Min(p.x, (float)r.xmax * 0.001f);
			p.z = Math.Max(p.z, (float)r.ymin * 0.001f);
			p.z = Math.Min(p.z, (float)r.ymax * 0.001f);
			return (p.x - vector.x) * (p.x - vector.x) + (p.z - vector.z) * (p.z - vector.z) < radius * radius;
		}

		private static int ExpansionRequired(IntRect r, IntRect r2)
		{
			int num = Math.Min(r.xmin, r2.xmin);
			int num2 = Math.Max(r.xmax, r2.xmax);
			int num3 = Math.Min(r.ymin, r2.ymin);
			int num4 = Math.Max(r.ymax, r2.ymax);
			return (num2 - num) * (num4 - num3) - RectArea(r);
		}

		private static IntRect ExpandToContain(IntRect r, IntRect r2)
		{
			return IntRect.Union(r, r2);
		}

		private static int RectArea(IntRect r)
		{
			return r.Width * r.Height;
		}
	}
}
