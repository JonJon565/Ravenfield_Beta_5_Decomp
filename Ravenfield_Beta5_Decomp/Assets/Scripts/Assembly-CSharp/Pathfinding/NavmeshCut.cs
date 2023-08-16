using System;
using System.Collections.Generic;
using Pathfinding.ClipperLib;
using Pathfinding.Util;
using UnityEngine;

namespace Pathfinding
{
	[AddComponentMenu("Pathfinding/Navmesh/Navmesh Cut")]
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_navmesh_cut.php")]
	public class NavmeshCut : MonoBehaviour
	{
		public enum MeshType
		{
			Rectangle = 0,
			Circle = 1,
			CustomMesh = 2
		}

		private static List<NavmeshCut> allCuts = new List<NavmeshCut>();

		public MeshType type;

		public Mesh mesh;

		public Vector2 rectangleSize = new Vector2(1f, 1f);

		public float circleRadius = 1f;

		public int circleResolution = 6;

		public float height = 1f;

		public float meshScale = 1f;

		public Vector3 center;

		public float updateDistance = 0.4f;

		public bool isDual;

		public bool cutsAddedGeom = true;

		public float updateRotationDistance = 10f;

		public bool useRotation;

		private Vector3[][] contours;

		protected Transform tr;

		private Mesh lastMesh;

		private Vector3 lastPosition;

		private Quaternion lastRotation;

		private bool wasEnabled;

		private Bounds lastBounds;

		private static readonly Dictionary<Int2, int> edges = new Dictionary<Int2, int>();

		private static readonly Dictionary<int, int> pointers = new Dictionary<int, int>();

		public static readonly Color GizmoColor = new Color(0.14509805f, 0.72156864f, 0.9372549f);

		public Bounds LastBounds
		{
			get
			{
				return lastBounds;
			}
		}

		public static event Action<NavmeshCut> OnDestroyCallback;

		private static void AddCut(NavmeshCut obj)
		{
			allCuts.Add(obj);
		}

		private static void RemoveCut(NavmeshCut obj)
		{
			allCuts.Remove(obj);
		}

		public static List<NavmeshCut> GetAllInRange(Bounds b)
		{
			List<NavmeshCut> list = ListPool<NavmeshCut>.Claim();
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
			return min.x <= max2.x && max.x >= min2.x && min.y <= max2.y && max.y >= min2.y && min.z <= max2.z && max.z >= min2.z;
		}

		public static List<NavmeshCut> GetAll()
		{
			return allCuts;
		}

		public void Awake()
		{
			AddCut(this);
		}

		public void OnEnable()
		{
			tr = base.transform;
			lastPosition = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
			lastRotation = tr.rotation;
		}

		public void OnDestroy()
		{
			if (NavmeshCut.OnDestroyCallback != null)
			{
				NavmeshCut.OnDestroyCallback(this);
			}
			RemoveCut(this);
		}

		public void ForceUpdate()
		{
			lastPosition = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
		}

		public bool RequiresUpdate()
		{
			return wasEnabled != base.enabled || (wasEnabled && ((tr.position - lastPosition).sqrMagnitude > updateDistance * updateDistance || (useRotation && Quaternion.Angle(lastRotation, tr.rotation) > updateRotationDistance)));
		}

		public virtual void UsedForCut()
		{
		}

		public void NotifyUpdated()
		{
			wasEnabled = base.enabled;
			if (wasEnabled)
			{
				lastPosition = tr.position;
				lastBounds = GetBounds();
				if (useRotation)
				{
					lastRotation = tr.rotation;
				}
			}
		}

		private void CalculateMeshContour()
		{
			if (mesh == null)
			{
				return;
			}
			edges.Clear();
			pointers.Clear();
			Vector3[] vertices = mesh.vertices;
			int[] triangles = mesh.triangles;
			for (int i = 0; i < triangles.Length; i += 3)
			{
				if (VectorMath.IsClockwiseXZ(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]]))
				{
					int num = triangles[i];
					triangles[i] = triangles[i + 2];
					triangles[i + 2] = num;
				}
				edges[new Int2(triangles[i], triangles[i + 1])] = i;
				edges[new Int2(triangles[i + 1], triangles[i + 2])] = i;
				edges[new Int2(triangles[i + 2], triangles[i])] = i;
			}
			for (int j = 0; j < triangles.Length; j += 3)
			{
				for (int k = 0; k < 3; k++)
				{
					if (!edges.ContainsKey(new Int2(triangles[j + (k + 1) % 3], triangles[j + k % 3])))
					{
						pointers[triangles[j + k % 3]] = triangles[j + (k + 1) % 3];
					}
				}
			}
			List<Vector3[]> list = new List<Vector3[]>();
			List<Vector3> list2 = ListPool<Vector3>.Claim();
			for (int l = 0; l < vertices.Length; l++)
			{
				if (!pointers.ContainsKey(l))
				{
					continue;
				}
				list2.Clear();
				int num2 = l;
				do
				{
					int num3 = pointers[num2];
					if (num3 == -1)
					{
						break;
					}
					pointers[num2] = -1;
					list2.Add(vertices[num2]);
					num2 = num3;
					if (num2 == -1)
					{
						Debug.LogError("Invalid Mesh '" + mesh.name + " in " + base.gameObject.name);
						break;
					}
				}
				while (num2 != l);
				if (list2.Count > 0)
				{
					list.Add(list2.ToArray());
				}
			}
			ListPool<Vector3>.Release(list2);
			contours = list.ToArray();
		}

		public Bounds GetBounds()
		{
			Bounds result = default(Bounds);
			switch (type)
			{
			case MeshType.Rectangle:
				if (useRotation)
				{
					Matrix4x4 localToWorldMatrix2 = tr.localToWorldMatrix;
					result = new Bounds(localToWorldMatrix2.MultiplyPoint3x4(center + new Vector3(0f - rectangleSize.x, 0f - height, 0f - rectangleSize.y) * 0.5f), Vector3.zero);
					result.Encapsulate(localToWorldMatrix2.MultiplyPoint3x4(center + new Vector3(rectangleSize.x, 0f - height, 0f - rectangleSize.y) * 0.5f));
					result.Encapsulate(localToWorldMatrix2.MultiplyPoint3x4(center + new Vector3(rectangleSize.x, 0f - height, rectangleSize.y) * 0.5f));
					result.Encapsulate(localToWorldMatrix2.MultiplyPoint3x4(center + new Vector3(0f - rectangleSize.x, 0f - height, rectangleSize.y) * 0.5f));
					result.Encapsulate(localToWorldMatrix2.MultiplyPoint3x4(center + new Vector3(0f - rectangleSize.x, height, 0f - rectangleSize.y) * 0.5f));
					result.Encapsulate(localToWorldMatrix2.MultiplyPoint3x4(center + new Vector3(rectangleSize.x, height, 0f - rectangleSize.y) * 0.5f));
					result.Encapsulate(localToWorldMatrix2.MultiplyPoint3x4(center + new Vector3(rectangleSize.x, height, rectangleSize.y) * 0.5f));
					result.Encapsulate(localToWorldMatrix2.MultiplyPoint3x4(center + new Vector3(0f - rectangleSize.x, height, rectangleSize.y) * 0.5f));
				}
				else
				{
					result = new Bounds(tr.position + center, new Vector3(rectangleSize.x, height, rectangleSize.y));
				}
				break;
			case MeshType.Circle:
				result = ((!useRotation) ? new Bounds(base.transform.position + center, new Vector3(circleRadius * 2f, height, circleRadius * 2f)) : new Bounds(tr.localToWorldMatrix.MultiplyPoint3x4(center), new Vector3(circleRadius * 2f, height, circleRadius * 2f)));
				break;
			case MeshType.CustomMesh:
				if (!(mesh == null))
				{
					Bounds bounds = mesh.bounds;
					if (useRotation)
					{
						Matrix4x4 localToWorldMatrix = tr.localToWorldMatrix;
						bounds.center *= meshScale;
						bounds.size *= meshScale;
						result = new Bounds(localToWorldMatrix.MultiplyPoint3x4(center + bounds.center), Vector3.zero);
						Vector3 max = bounds.max;
						Vector3 min = bounds.min;
						result.Encapsulate(localToWorldMatrix.MultiplyPoint3x4(center + new Vector3(max.x, max.y, max.z)));
						result.Encapsulate(localToWorldMatrix.MultiplyPoint3x4(center + new Vector3(min.x, max.y, max.z)));
						result.Encapsulate(localToWorldMatrix.MultiplyPoint3x4(center + new Vector3(min.x, max.y, min.z)));
						result.Encapsulate(localToWorldMatrix.MultiplyPoint3x4(center + new Vector3(max.x, max.y, min.z)));
						result.Encapsulate(localToWorldMatrix.MultiplyPoint3x4(center + new Vector3(max.x, min.y, max.z)));
						result.Encapsulate(localToWorldMatrix.MultiplyPoint3x4(center + new Vector3(min.x, min.y, max.z)));
						result.Encapsulate(localToWorldMatrix.MultiplyPoint3x4(center + new Vector3(min.x, min.y, min.z)));
						result.Encapsulate(localToWorldMatrix.MultiplyPoint3x4(center + new Vector3(max.x, min.y, min.z)));
						Vector3 size = result.size;
						size.y = Mathf.Max(size.y, height * tr.lossyScale.y);
						result.size = size;
					}
					else
					{
						Vector3 size2 = bounds.size * meshScale;
						size2.y = Mathf.Max(size2.y, height);
						result = new Bounds(base.transform.position + center + bounds.center * meshScale, size2);
					}
				}
				break;
			default:
				throw new Exception("Invalid mesh type");
			}
			return result;
		}

		public void GetContour(List<List<IntPoint>> buffer)
		{
			if (circleResolution < 3)
			{
				circleResolution = 3;
			}
			Vector3 position = tr.position;
			switch (type)
			{
			case MeshType.Rectangle:
			{
				List<IntPoint> list = ListPool<IntPoint>.Claim();
				if (useRotation)
				{
					Matrix4x4 localToWorldMatrix2 = tr.localToWorldMatrix;
					list.Add(V3ToIntPoint(localToWorldMatrix2.MultiplyPoint3x4(center + new Vector3(0f - rectangleSize.x, 0f, 0f - rectangleSize.y) * 0.5f)));
					list.Add(V3ToIntPoint(localToWorldMatrix2.MultiplyPoint3x4(center + new Vector3(rectangleSize.x, 0f, 0f - rectangleSize.y) * 0.5f)));
					list.Add(V3ToIntPoint(localToWorldMatrix2.MultiplyPoint3x4(center + new Vector3(rectangleSize.x, 0f, rectangleSize.y) * 0.5f)));
					list.Add(V3ToIntPoint(localToWorldMatrix2.MultiplyPoint3x4(center + new Vector3(0f - rectangleSize.x, 0f, rectangleSize.y) * 0.5f)));
				}
				else
				{
					position += center;
					list.Add(V3ToIntPoint(position + new Vector3(0f - rectangleSize.x, 0f, 0f - rectangleSize.y) * 0.5f));
					list.Add(V3ToIntPoint(position + new Vector3(rectangleSize.x, 0f, 0f - rectangleSize.y) * 0.5f));
					list.Add(V3ToIntPoint(position + new Vector3(rectangleSize.x, 0f, rectangleSize.y) * 0.5f));
					list.Add(V3ToIntPoint(position + new Vector3(0f - rectangleSize.x, 0f, rectangleSize.y) * 0.5f));
				}
				buffer.Add(list);
				break;
			}
			case MeshType.Circle:
			{
				List<IntPoint> list = ListPool<IntPoint>.Claim(circleResolution);
				if (useRotation)
				{
					Matrix4x4 localToWorldMatrix3 = tr.localToWorldMatrix;
					for (int l = 0; l < circleResolution; l++)
					{
						list.Add(V3ToIntPoint(localToWorldMatrix3.MultiplyPoint3x4(center + new Vector3(Mathf.Cos((float)(l * 2) * (float)Math.PI / (float)circleResolution), 0f, Mathf.Sin((float)(l * 2) * (float)Math.PI / (float)circleResolution)) * circleRadius)));
					}
				}
				else
				{
					position += center;
					for (int m = 0; m < circleResolution; m++)
					{
						list.Add(V3ToIntPoint(position + new Vector3(Mathf.Cos((float)(m * 2) * (float)Math.PI / (float)circleResolution), 0f, Mathf.Sin((float)(m * 2) * (float)Math.PI / (float)circleResolution)) * circleRadius));
					}
				}
				buffer.Add(list);
				break;
			}
			case MeshType.CustomMesh:
			{
				if (mesh != lastMesh || contours == null)
				{
					CalculateMeshContour();
					lastMesh = mesh;
				}
				if (contours == null)
				{
					break;
				}
				position += center;
				bool flag = Vector3.Dot(tr.up, Vector3.up) < 0f;
				for (int i = 0; i < contours.Length; i++)
				{
					Vector3[] array = contours[i];
					List<IntPoint> list = ListPool<IntPoint>.Claim(array.Length);
					if (useRotation)
					{
						Matrix4x4 localToWorldMatrix = tr.localToWorldMatrix;
						for (int j = 0; j < array.Length; j++)
						{
							list.Add(V3ToIntPoint(localToWorldMatrix.MultiplyPoint3x4(center + array[j] * meshScale)));
						}
					}
					else
					{
						for (int k = 0; k < array.Length; k++)
						{
							list.Add(V3ToIntPoint(position + array[k] * meshScale));
						}
					}
					if (flag)
					{
						list.Reverse();
					}
					buffer.Add(list);
				}
				break;
			}
			}
		}

		public static IntPoint V3ToIntPoint(Vector3 p)
		{
			Int3 @int = (Int3)p;
			return new IntPoint(@int.x, @int.z);
		}

		public static Vector3 IntPointToV3(IntPoint p)
		{
			Int3 @int = new Int3((int)p.X, 0, (int)p.Y);
			return (Vector3)@int;
		}

		public void OnDrawGizmos()
		{
			if (tr == null)
			{
				tr = base.transform;
			}
			List<List<IntPoint>> list = ListPool<List<IntPoint>>.Claim();
			GetContour(list);
			Gizmos.color = GizmoColor;
			Bounds bounds = GetBounds();
			float y = bounds.min.y;
			Vector3 vector = Vector3.up * (bounds.max.y - y);
			for (int i = 0; i < list.Count; i++)
			{
				List<IntPoint> list2 = list[i];
				for (int j = 0; j < list2.Count; j++)
				{
					Vector3 vector2 = IntPointToV3(list2[j]);
					vector2.y = y;
					Vector3 vector3 = IntPointToV3(list2[(j + 1) % list2.Count]);
					vector3.y = y;
					Gizmos.DrawLine(vector2, vector3);
					Gizmos.DrawLine(vector2 + vector, vector3 + vector);
					Gizmos.DrawLine(vector2, vector2 + vector);
					Gizmos.DrawLine(vector3, vector3 + vector);
				}
			}
			ListPool<List<IntPoint>>.Release(list);
		}

		public void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.Lerp(GizmoColor, new Color(1f, 1f, 1f, 0.2f), 0.9f);
			Bounds bounds = GetBounds();
			Gizmos.DrawCube(bounds.center, bounds.size);
			Gizmos.DrawWireCube(bounds.center, bounds.size);
		}
	}
}
