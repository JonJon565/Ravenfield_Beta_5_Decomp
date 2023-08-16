using UnityEngine;

namespace Pathfinding
{
	public class GraphUpdateShape
	{
		private Vector3[] _points;

		private Vector3[] _convexPoints;

		private bool _convex;

		public Vector3[] points
		{
			get
			{
				return _points;
			}
			set
			{
				_points = value;
				if (convex)
				{
					CalculateConvexHull();
				}
			}
		}

		public bool convex
		{
			get
			{
				return _convex;
			}
			set
			{
				if (_convex != value && value)
				{
					_convex = value;
					CalculateConvexHull();
				}
				else
				{
					_convex = value;
				}
			}
		}

		private void CalculateConvexHull()
		{
			if (points == null)
			{
				_convexPoints = null;
				return;
			}
			_convexPoints = Polygon.ConvexHullXZ(points);
			for (int i = 0; i < _convexPoints.Length; i++)
			{
				Debug.DrawLine(_convexPoints[i], _convexPoints[(i + 1) % _convexPoints.Length], Color.green);
			}
		}

		public Bounds GetBounds()
		{
			if (points == null || points.Length == 0)
			{
				return default(Bounds);
			}
			Vector3 vector = points[0];
			Vector3 vector2 = points[0];
			for (int i = 0; i < points.Length; i++)
			{
				vector = Vector3.Min(vector, points[i]);
				vector2 = Vector3.Max(vector2, points[i]);
			}
			return new Bounds((vector + vector2) * 0.5f, vector2 - vector);
		}

		public bool Contains(GraphNode node)
		{
			return Contains((Vector3)node.position);
		}

		public bool Contains(Vector3 point)
		{
			if (convex)
			{
				if (_convexPoints == null)
				{
					return false;
				}
				int i = 0;
				int num = _convexPoints.Length - 1;
				for (; i < _convexPoints.Length; i++)
				{
					if (VectorMath.RightOrColinearXZ(_convexPoints[i], _convexPoints[num], point))
					{
						return false;
					}
					num = i;
				}
				return true;
			}
			return _points != null && Polygon.ContainsPointXZ(_points, point);
		}
	}
}
