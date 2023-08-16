using System.Collections.Generic;
using Pathfinding.Util;
using UnityEngine;

namespace Pathfinding
{
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_navmesh_add.php")]
	public class NavmeshAdd : MonoBehaviour
	{
		public enum MeshType
		{
			Rectangle = 0,
			CustomMesh = 1
		}

		private static List<NavmeshAdd> allCuts = new List<NavmeshAdd>();

		public MeshType type;

		public Mesh mesh;

		private Vector3[] verts;

		private int[] tris;

		public Vector2 rectangleSize = new Vector2(1f, 1f);

		public float meshScale = 1f;

		public Vector3 center;

		private Bounds bounds;

		public bool useRotation;

		protected Transform tr;

		public static readonly Color GizmoColor = new Color(0.36862746f, 0.9372549f, 0.14509805f);

		public Vector3 Center
		{
			get
			{
				return tr.position + ((!useRotation) ? center : tr.TransformPoint(center));
			}
		}

		private static void Add(NavmeshAdd obj)
		{
			allCuts.Add(obj);
		}

		private static void Remove(NavmeshAdd obj)
		{
			allCuts.Remove(obj);
		}

		public static List<NavmeshAdd> GetAllInRange(Bounds b)
		{
			List<NavmeshAdd> list = ListPool<NavmeshAdd>.Claim();
			for (int i = 0; i < allCuts.Count; i++)
			{
				if (allCuts[i].enabled && Intersects(b, allCuts[i].GetBounds()))
				{
					list.Add(allCuts[i]);
				}
			}
			return list;
		}

		private static bool Intersects(Bounds b1, Bounds b2)
		{
			Vector3 min = b1.min;
			Vector3 max = b1.max;
			Vector3 min2 = b2.min;
			Vector3 max2 = b2.max;
			return min.x <= max2.x && max.x >= min2.x && min.z <= max2.z && max.z >= min2.z;
		}

		public static List<NavmeshAdd> GetAll()
		{
			return allCuts;
		}

		public void Awake()
		{
			Add(this);
		}

		public void OnEnable()
		{
			tr = base.transform;
		}

		public void OnDestroy()
		{
			Remove(this);
		}

		[ContextMenu("Rebuild Mesh")]
		public void RebuildMesh()
		{
			if (type == MeshType.CustomMesh)
			{
				if (mesh == null)
				{
					verts = null;
					tris = null;
				}
				else
				{
					verts = mesh.vertices;
					tris = mesh.triangles;
				}
				return;
			}
			if (verts == null || verts.Length != 4 || tris == null || tris.Length != 6)
			{
				verts = new Vector3[4];
				tris = new int[6];
			}
			tris[0] = 0;
			tris[1] = 1;
			tris[2] = 2;
			tris[3] = 0;
			tris[4] = 2;
			tris[5] = 3;
			verts[0] = new Vector3((0f - rectangleSize.x) * 0.5f, 0f, (0f - rectangleSize.y) * 0.5f);
			verts[1] = new Vector3(rectangleSize.x * 0.5f, 0f, (0f - rectangleSize.y) * 0.5f);
			verts[2] = new Vector3(rectangleSize.x * 0.5f, 0f, rectangleSize.y * 0.5f);
			verts[3] = new Vector3((0f - rectangleSize.x) * 0.5f, 0f, rectangleSize.y * 0.5f);
		}

		public Bounds GetBounds()
		{
			switch (type)
			{
			case MeshType.Rectangle:
				if (useRotation)
				{
					Matrix4x4 matrix4x2 = Matrix4x4.TRS(tr.position, tr.rotation, Vector3.one);
					this.bounds = new Bounds(matrix4x2.MultiplyPoint3x4(center + new Vector3(0f - rectangleSize.x, 0f, 0f - rectangleSize.y) * 0.5f), Vector3.zero);
					this.bounds.Encapsulate(matrix4x2.MultiplyPoint3x4(center + new Vector3(rectangleSize.x, 0f, 0f - rectangleSize.y) * 0.5f));
					this.bounds.Encapsulate(matrix4x2.MultiplyPoint3x4(center + new Vector3(rectangleSize.x, 0f, rectangleSize.y) * 0.5f));
					this.bounds.Encapsulate(matrix4x2.MultiplyPoint3x4(center + new Vector3(0f - rectangleSize.x, 0f, rectangleSize.y) * 0.5f));
				}
				else
				{
					this.bounds = new Bounds(tr.position + center, new Vector3(rectangleSize.x, 0f, rectangleSize.y));
				}
				break;
			case MeshType.CustomMesh:
				if (!(mesh == null))
				{
					Bounds bounds = mesh.bounds;
					if (useRotation)
					{
						Matrix4x4 matrix4x = Matrix4x4.TRS(tr.position, tr.rotation, Vector3.one * meshScale);
						this.bounds = new Bounds(matrix4x.MultiplyPoint3x4(center + bounds.center), Vector3.zero);
						Vector3 max = bounds.max;
						Vector3 min = bounds.min;
						this.bounds.Encapsulate(matrix4x.MultiplyPoint3x4(center + new Vector3(max.x, min.y, max.z)));
						this.bounds.Encapsulate(matrix4x.MultiplyPoint3x4(center + new Vector3(min.x, min.y, max.z)));
						this.bounds.Encapsulate(matrix4x.MultiplyPoint3x4(center + new Vector3(min.x, max.y, min.z)));
						this.bounds.Encapsulate(matrix4x.MultiplyPoint3x4(center + new Vector3(max.x, max.y, min.z)));
					}
					else
					{
						Vector3 size = bounds.size * meshScale;
						this.bounds = new Bounds(base.transform.position + center + bounds.center * meshScale, size);
					}
				}
				break;
			}
			return this.bounds;
		}

		public void GetMesh(Int3 offset, ref Int3[] vbuffer, out int[] tbuffer)
		{
			if (verts == null)
			{
				RebuildMesh();
			}
			if (verts == null)
			{
				tbuffer = new int[0];
				return;
			}
			if (vbuffer == null || vbuffer.Length < verts.Length)
			{
				vbuffer = new Int3[verts.Length];
			}
			tbuffer = tris;
			if (useRotation)
			{
				Matrix4x4 matrix4x = Matrix4x4.TRS(tr.position + center, tr.rotation, tr.localScale * meshScale);
				for (int i = 0; i < verts.Length; i++)
				{
					vbuffer[i] = offset + (Int3)matrix4x.MultiplyPoint3x4(verts[i]);
				}
			}
			else
			{
				Vector3 vector = tr.position + center;
				for (int j = 0; j < verts.Length; j++)
				{
					vbuffer[j] = offset + (Int3)(vector + verts[j] * meshScale);
				}
			}
		}
	}
}
