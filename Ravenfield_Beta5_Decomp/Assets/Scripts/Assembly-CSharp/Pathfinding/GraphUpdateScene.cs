using UnityEngine;

namespace Pathfinding
{
	[AddComponentMenu("Pathfinding/GraphUpdateScene")]
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_graph_update_scene.php")]
	public class GraphUpdateScene : GraphModifier
	{
		public Vector3[] points;

		private Vector3[] convexPoints;

		[HideInInspector]
		public bool convex = true;

		[HideInInspector]
		public float minBoundsHeight = 1f;

		[HideInInspector]
		public int penaltyDelta;

		[HideInInspector]
		public bool modifyWalkability;

		[HideInInspector]
		public bool setWalkability;

		[HideInInspector]
		public bool applyOnStart = true;

		[HideInInspector]
		public bool applyOnScan = true;

		[HideInInspector]
		public bool useWorldSpace;

		[HideInInspector]
		public bool updatePhysics;

		[HideInInspector]
		public bool resetPenaltyOnPhysics = true;

		[HideInInspector]
		public bool updateErosion = true;

		[HideInInspector]
		public bool lockToY;

		[HideInInspector]
		public float lockToYValue;

		[HideInInspector]
		public bool modifyTag;

		[HideInInspector]
		public int setTag;

		private int setTagInvert;

		private bool firstApplied;

		public void Start()
		{
			if (!firstApplied && applyOnStart)
			{
				Apply();
			}
		}

		public override void OnPostScan()
		{
			if (applyOnScan)
			{
				Apply();
			}
		}

		public virtual void InvertSettings()
		{
			setWalkability = !setWalkability;
			penaltyDelta = -penaltyDelta;
			if (setTagInvert == 0)
			{
				setTagInvert = setTag;
				setTag = 0;
			}
			else
			{
				setTag = setTagInvert;
				setTagInvert = 0;
			}
		}

		public void RecalcConvex()
		{
			convexPoints = ((!convex) ? null : Polygon.ConvexHullXZ(points));
		}

		public void ToggleUseWorldSpace()
		{
			useWorldSpace = !useWorldSpace;
			if (points != null)
			{
				convexPoints = null;
				Matrix4x4 matrix4x = ((!useWorldSpace) ? base.transform.worldToLocalMatrix : base.transform.localToWorldMatrix);
				for (int i = 0; i < points.Length; i++)
				{
					points[i] = matrix4x.MultiplyPoint3x4(points[i]);
				}
			}
		}

		public void LockToY()
		{
			if (points != null)
			{
				for (int i = 0; i < points.Length; i++)
				{
					points[i].y = lockToYValue;
				}
			}
		}

		public void Apply(AstarPath active)
		{
			if (applyOnScan)
			{
				Apply();
			}
		}

		public Bounds GetBounds()
		{
			Bounds result;
			if (points == null || points.Length == 0)
			{
				Collider component = GetComponent<Collider>();
				Renderer component2 = GetComponent<Renderer>();
				if (component != null)
				{
					result = component.bounds;
				}
				else
				{
					if (!(component2 != null))
					{
						return new Bounds(Vector3.zero, Vector3.zero);
					}
					result = component2.bounds;
				}
			}
			else
			{
				Matrix4x4 matrix4x = Matrix4x4.identity;
				if (!useWorldSpace)
				{
					matrix4x = base.transform.localToWorldMatrix;
				}
				Vector3 vector = matrix4x.MultiplyPoint3x4(points[0]);
				Vector3 vector2 = matrix4x.MultiplyPoint3x4(points[0]);
				for (int i = 0; i < points.Length; i++)
				{
					Vector3 rhs = matrix4x.MultiplyPoint3x4(points[i]);
					vector = Vector3.Min(vector, rhs);
					vector2 = Vector3.Max(vector2, rhs);
				}
				result = new Bounds((vector + vector2) * 0.5f, vector2 - vector);
			}
			if (result.size.y < minBoundsHeight)
			{
				result.size = new Vector3(result.size.x, minBoundsHeight, result.size.z);
			}
			return result;
		}

		public void Apply()
		{
			if (AstarPath.active == null)
			{
				Debug.LogError("There is no AstarPath object in the scene");
				return;
			}
			GraphUpdateObject graphUpdateObject;
			if (points == null || points.Length == 0)
			{
				Collider component = GetComponent<Collider>();
				Renderer component2 = GetComponent<Renderer>();
				Bounds bounds;
				if (component != null)
				{
					bounds = component.bounds;
				}
				else
				{
					if (!(component2 != null))
					{
						Debug.LogWarning("Cannot apply GraphUpdateScene, no points defined and no renderer or collider attached");
						return;
					}
					bounds = component2.bounds;
				}
				if (bounds.size.y < minBoundsHeight)
				{
					bounds.size = new Vector3(bounds.size.x, minBoundsHeight, bounds.size.z);
				}
				graphUpdateObject = new GraphUpdateObject(bounds);
			}
			else
			{
				GraphUpdateShape graphUpdateShape = new GraphUpdateShape();
				graphUpdateShape.convex = convex;
				Vector3[] array = points;
				if (!useWorldSpace)
				{
					array = new Vector3[points.Length];
					Matrix4x4 localToWorldMatrix = base.transform.localToWorldMatrix;
					for (int i = 0; i < array.Length; i++)
					{
						array[i] = localToWorldMatrix.MultiplyPoint3x4(points[i]);
					}
				}
				graphUpdateShape.points = array;
				Bounds bounds2 = graphUpdateShape.GetBounds();
				if (bounds2.size.y < minBoundsHeight)
				{
					bounds2.size = new Vector3(bounds2.size.x, minBoundsHeight, bounds2.size.z);
				}
				graphUpdateObject = new GraphUpdateObject(bounds2);
				graphUpdateObject.shape = graphUpdateShape;
			}
			firstApplied = true;
			graphUpdateObject.modifyWalkability = modifyWalkability;
			graphUpdateObject.setWalkability = setWalkability;
			graphUpdateObject.addPenalty = penaltyDelta;
			graphUpdateObject.updatePhysics = updatePhysics;
			graphUpdateObject.updateErosion = updateErosion;
			graphUpdateObject.resetPenaltyOnPhysics = resetPenaltyOnPhysics;
			graphUpdateObject.modifyTag = modifyTag;
			graphUpdateObject.setTag = setTag;
			AstarPath.active.UpdateGraphs(graphUpdateObject);
		}

		public void OnDrawGizmos()
		{
			OnDrawGizmos(false);
		}

		public void OnDrawGizmosSelected()
		{
			OnDrawGizmos(true);
		}

		public void OnDrawGizmos(bool selected)
		{
			Color color = ((!selected) ? new Color(0.8901961f, 0.23921569f, 0.08627451f, 0.9f) : new Color(0.8901961f, 0.23921569f, 0.08627451f, 1f));
			if (selected)
			{
				Gizmos.color = Color.Lerp(color, new Color(1f, 1f, 1f, 0.2f), 0.9f);
				Bounds bounds = GetBounds();
				Gizmos.DrawCube(bounds.center, bounds.size);
				Gizmos.DrawWireCube(bounds.center, bounds.size);
			}
			if (points == null)
			{
				return;
			}
			if (convex)
			{
				color.a *= 0.5f;
			}
			Gizmos.color = color;
			Matrix4x4 matrix4x = ((!useWorldSpace) ? base.transform.localToWorldMatrix : Matrix4x4.identity);
			if (convex)
			{
				color.r -= 0.1f;
				color.g -= 0.2f;
				color.b -= 0.1f;
				Gizmos.color = color;
			}
			if (selected || !convex)
			{
				for (int i = 0; i < points.Length; i++)
				{
					Gizmos.DrawLine(matrix4x.MultiplyPoint3x4(points[i]), matrix4x.MultiplyPoint3x4(points[(i + 1) % points.Length]));
				}
			}
			if (convex)
			{
				if (convexPoints == null)
				{
					RecalcConvex();
				}
				Gizmos.color = ((!selected) ? new Color(0.8901961f, 0.23921569f, 0.08627451f, 0.9f) : new Color(0.8901961f, 0.23921569f, 0.08627451f, 1f));
				for (int j = 0; j < convexPoints.Length; j++)
				{
					Gizmos.DrawLine(matrix4x.MultiplyPoint3x4(convexPoints[j]), matrix4x.MultiplyPoint3x4(convexPoints[(j + 1) % convexPoints.Length]));
				}
			}
		}
	}
}
