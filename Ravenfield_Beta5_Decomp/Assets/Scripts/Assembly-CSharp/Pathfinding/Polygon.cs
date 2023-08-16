using System;
using System.Collections.Generic;
using Pathfinding.Util;
using UnityEngine;

namespace Pathfinding
{
	public static class Polygon
	{
		[Obsolete("Use VectorMath.SignedTriangleAreaTimes2XZ instead")]
		public static long TriangleArea2(Int3 a, Int3 b, Int3 c)
		{
			return VectorMath.SignedTriangleAreaTimes2XZ(a, b, c);
		}

		[Obsolete("Use VectorMath.SignedTriangleAreaTimes2XZ instead")]
		public static float TriangleArea2(Vector3 a, Vector3 b, Vector3 c)
		{
			return VectorMath.SignedTriangleAreaTimes2XZ(a, b, c);
		}

		[Obsolete("Use TriangleArea2 instead to avoid confusion regarding the factor 2")]
		public static long TriangleArea(Int3 a, Int3 b, Int3 c)
		{
			return TriangleArea2(a, b, c);
		}

		[Obsolete("Use TriangleArea2 instead to avoid confusion regarding the factor 2")]
		public static float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
		{
			return TriangleArea2(a, b, c);
		}

		[Obsolete("Use ContainsPointXZ instead")]
		public static bool ContainsPoint(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
		{
			return ContainsPointXZ(a, b, c, p);
		}

		public static bool ContainsPointXZ(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
		{
			return VectorMath.IsClockwiseMarginXZ(a, b, p) && VectorMath.IsClockwiseMarginXZ(b, c, p) && VectorMath.IsClockwiseMarginXZ(c, a, p);
		}

		[Obsolete("Use ContainsPointXZ instead")]
		public static bool ContainsPoint(Int3 a, Int3 b, Int3 c, Int3 p)
		{
			return ContainsPointXZ(a, b, c, p);
		}

		public static bool ContainsPointXZ(Int3 a, Int3 b, Int3 c, Int3 p)
		{
			return VectorMath.IsClockwiseOrColinearXZ(a, b, p) && VectorMath.IsClockwiseOrColinearXZ(b, c, p) && VectorMath.IsClockwiseOrColinearXZ(c, a, p);
		}

		public static bool ContainsPoint(Int2 a, Int2 b, Int2 c, Int2 p)
		{
			return VectorMath.IsClockwiseOrColinear(a, b, p) && VectorMath.IsClockwiseOrColinear(b, c, p) && VectorMath.IsClockwiseOrColinear(c, a, p);
		}

		[Obsolete("Use ContainsPointXZ instead")]
		public static bool ContainsPoint(Vector3[] polyPoints, Vector3 p)
		{
			return ContainsPointXZ(polyPoints, p);
		}

		public static bool ContainsPoint(Vector2[] polyPoints, Vector2 p)
		{
			int num = polyPoints.Length - 1;
			bool flag = false;
			int num2 = 0;
			while (num2 < polyPoints.Length)
			{
				if (((polyPoints[num2].y <= p.y && p.y < polyPoints[num].y) || (polyPoints[num].y <= p.y && p.y < polyPoints[num2].y)) && p.x < (polyPoints[num].x - polyPoints[num2].x) * (p.y - polyPoints[num2].y) / (polyPoints[num].y - polyPoints[num2].y) + polyPoints[num2].x)
				{
					flag = !flag;
				}
				num = num2++;
			}
			return flag;
		}

		public static bool ContainsPointXZ(Vector3[] polyPoints, Vector3 p)
		{
			int num = polyPoints.Length - 1;
			bool flag = false;
			int num2 = 0;
			while (num2 < polyPoints.Length)
			{
				if (((polyPoints[num2].z <= p.z && p.z < polyPoints[num].z) || (polyPoints[num].z <= p.z && p.z < polyPoints[num2].z)) && p.x < (polyPoints[num].x - polyPoints[num2].x) * (p.z - polyPoints[num2].z) / (polyPoints[num].z - polyPoints[num2].z) + polyPoints[num2].x)
				{
					flag = !flag;
				}
				num = num2++;
			}
			return flag;
		}

		[Obsolete("Use VectorMath.RightXZ instead. Note that it now uses a left handed coordinate system (same as Unity)")]
		public static bool LeftNotColinear(Vector3 a, Vector3 b, Vector3 p)
		{
			return VectorMath.RightXZ(a, b, p);
		}

		[Obsolete("Use VectorMath.RightOrColinearXZ instead. Note that it now uses a left handed coordinate system (same as Unity)")]
		public static bool Left(Vector3 a, Vector3 b, Vector3 p)
		{
			return VectorMath.RightOrColinearXZ(a, b, p);
		}

		[Obsolete("Use VectorMath.RightOrColinear instead. Note that it now uses a left handed coordinate system (same as Unity)")]
		public static bool Left(Vector2 a, Vector2 b, Vector2 p)
		{
			return VectorMath.RightOrColinear(a, b, p);
		}

		[Obsolete("Use VectorMath.RightOrColinearXZ instead. Note that it now uses a left handed coordinate system (same as Unity)")]
		public static bool Left(Int3 a, Int3 b, Int3 p)
		{
			return VectorMath.RightOrColinearXZ(a, b, p);
		}

		[Obsolete("Use VectorMath.RightXZ instead. Note that it now uses a left handed coordinate system (same as Unity)")]
		public static bool LeftNotColinear(Int3 a, Int3 b, Int3 p)
		{
			return VectorMath.RightXZ(a, b, p);
		}

		[Obsolete("Use VectorMath.RightOrColinear instead. Note that it now uses a left handed coordinate system (same as Unity)")]
		public static bool Left(Int2 a, Int2 b, Int2 p)
		{
			return VectorMath.RightOrColinear(a, b, p);
		}

		[Obsolete("Use VectorMath.IsClockwiseMarginXZ instead")]
		public static bool IsClockwiseMargin(Vector3 a, Vector3 b, Vector3 c)
		{
			return VectorMath.IsClockwiseMarginXZ(a, b, c);
		}

		[Obsolete("Use VectorMath.IsClockwiseXZ instead")]
		public static bool IsClockwise(Vector3 a, Vector3 b, Vector3 c)
		{
			return VectorMath.IsClockwiseXZ(a, b, c);
		}

		[Obsolete("Use VectorMath.IsClockwiseXZ instead")]
		public static bool IsClockwise(Int3 a, Int3 b, Int3 c)
		{
			return VectorMath.IsClockwiseXZ(a, b, c);
		}

		[Obsolete("Use VectorMath.IsClockwiseOrColinearXZ instead")]
		public static bool IsClockwiseMargin(Int3 a, Int3 b, Int3 c)
		{
			return VectorMath.IsClockwiseOrColinearXZ(a, b, c);
		}

		[Obsolete("Use VectorMath.IsClockwiseOrColinear instead")]
		public static bool IsClockwiseMargin(Int2 a, Int2 b, Int2 c)
		{
			return VectorMath.IsClockwiseOrColinear(a, b, c);
		}

		[Obsolete("Use VectorMath.IsColinearXZ instead")]
		public static bool IsColinear(Int3 a, Int3 b, Int3 c)
		{
			return VectorMath.IsColinearXZ(a, b, c);
		}

		[Obsolete("Use VectorMath.IsColinearAlmostXZ instead")]
		public static bool IsColinearAlmost(Int3 a, Int3 b, Int3 c)
		{
			return VectorMath.IsColinearAlmostXZ(a, b, c);
		}

		[Obsolete("Use VectorMath.IsColinearXZ instead")]
		public static bool IsColinear(Vector3 a, Vector3 b, Vector3 c)
		{
			return VectorMath.IsColinearXZ(a, b, c);
		}

		[Obsolete("Marked for removal since it is not used by any part of the A* Pathfinding Project")]
		public static bool IntersectsUnclamped(Vector3 a, Vector3 b, Vector3 a2, Vector3 b2)
		{
			return VectorMath.RightOrColinearXZ(a, b, a2) != VectorMath.RightOrColinearXZ(a, b, b2);
		}

		[Obsolete("Use VectorMath.SegmentsIntersect instead")]
		public static bool Intersects(Int2 start1, Int2 end1, Int2 start2, Int2 end2)
		{
			return VectorMath.SegmentsIntersect(start1, end1, start2, end2);
		}

		[Obsolete("Use VectorMath.SegmentsIntersectXZ instead")]
		public static bool Intersects(Int3 start1, Int3 end1, Int3 start2, Int3 end2)
		{
			return VectorMath.SegmentsIntersectXZ(start1, end1, start2, end2);
		}

		[Obsolete("Use VectorMath.SegmentsIntersectXZ instead")]
		public static bool Intersects(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2)
		{
			return VectorMath.SegmentsIntersectXZ(start1, end1, start2, end2);
		}

		[Obsolete("Use VectorMath.LineDirIntersectionPointXZ instead")]
		public static Vector3 IntersectionPointOptimized(Vector3 start1, Vector3 dir1, Vector3 start2, Vector3 dir2)
		{
			return VectorMath.LineDirIntersectionPointXZ(start1, dir1, start2, dir2);
		}

		[Obsolete("Use VectorMath.LineDirIntersectionPointXZ instead")]
		public static Vector3 IntersectionPointOptimized(Vector3 start1, Vector3 dir1, Vector3 start2, Vector3 dir2, out bool intersects)
		{
			return VectorMath.LineDirIntersectionPointXZ(start1, dir1, start2, dir2, out intersects);
		}

		[Obsolete("Use VectorMath.RaySegmentIntersectXZ instead")]
		public static bool IntersectionFactorRaySegment(Int3 start1, Int3 end1, Int3 start2, Int3 end2)
		{
			return VectorMath.RaySegmentIntersectXZ(start1, end1, start2, end2);
		}

		[Obsolete("Use VectorMath.LineIntersectionFactorXZ instead")]
		public static bool IntersectionFactor(Int3 start1, Int3 end1, Int3 start2, Int3 end2, out float factor1, out float factor2)
		{
			return VectorMath.LineIntersectionFactorXZ(start1, end1, start2, end2, out factor1, out factor2);
		}

		[Obsolete("Use VectorMath.LineIntersectionFactorXZ instead")]
		public static bool IntersectionFactor(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2, out float factor1, out float factor2)
		{
			return VectorMath.LineIntersectionFactorXZ(start1, end1, start2, end2, out factor1, out factor2);
		}

		[Obsolete("Use VectorMath.LineRayIntersectionFactorXZ instead")]
		public static float IntersectionFactorRay(Int3 start1, Int3 end1, Int3 start2, Int3 end2)
		{
			return VectorMath.LineRayIntersectionFactorXZ(start1, end1, start2, end2);
		}

		[Obsolete("Use VectorMath.LineIntersectionFactorXZ instead")]
		public static float IntersectionFactor(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2)
		{
			return VectorMath.LineIntersectionFactorXZ(start1, end1, start2, end2);
		}

		[Obsolete("Use VectorMath.LineIntersectionPointXZ instead")]
		public static Vector3 IntersectionPoint(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2)
		{
			return VectorMath.LineIntersectionPointXZ(start1, end1, start2, end2);
		}

		[Obsolete("Use VectorMath.LineIntersectionPointXZ instead")]
		public static Vector3 IntersectionPoint(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2, out bool intersects)
		{
			return VectorMath.LineIntersectionPointXZ(start1, end1, start2, end2, out intersects);
		}

		[Obsolete("Use VectorMath.LineIntersectionPoint instead")]
		public static Vector2 IntersectionPoint(Vector2 start1, Vector2 end1, Vector2 start2, Vector2 end2)
		{
			return VectorMath.LineIntersectionPoint(start1, end1, start2, end2);
		}

		[Obsolete("Use VectorMath.LineIntersectionPoint instead")]
		public static Vector2 IntersectionPoint(Vector2 start1, Vector2 end1, Vector2 start2, Vector2 end2, out bool intersects)
		{
			return VectorMath.LineIntersectionPoint(start1, end1, start2, end2, out intersects);
		}

		[Obsolete("Use VectorMath.SegmentIntersectionPointXZ instead")]
		public static Vector3 SegmentIntersectionPoint(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2, out bool intersects)
		{
			return VectorMath.SegmentIntersectionPointXZ(start1, end1, start2, end2, out intersects);
		}

		[Obsolete("Use ConvexHullXZ instead")]
		public static Vector3[] ConvexHull(Vector3[] points)
		{
			return ConvexHullXZ(points);
		}

		public static Vector3[] ConvexHullXZ(Vector3[] points)
		{
			if (points.Length == 0)
			{
				return new Vector3[0];
			}
			List<Vector3> list = ListPool<Vector3>.Claim();
			int num = 0;
			for (int i = 1; i < points.Length; i++)
			{
				if (points[i].x < points[num].x)
				{
					num = i;
				}
			}
			int num2 = num;
			int num3 = 0;
			do
			{
				list.Add(points[num]);
				int num4 = 0;
				for (int j = 0; j < points.Length; j++)
				{
					if (num4 == num || !VectorMath.RightOrColinearXZ(points[num], points[num4], points[j]))
					{
						num4 = j;
					}
				}
				num = num4;
				num3++;
				if (num3 > 10000)
				{
					Debug.LogWarning("Infinite Loop in Convex Hull Calculation");
					break;
				}
			}
			while (num != num2);
			Vector3[] result = list.ToArray();
			ListPool<Vector3>.Release(list);
			return result;
		}

		[Obsolete("Use VectorMath.SegmentIntersectsBounds instead")]
		public static bool LineIntersectsBounds(Bounds bounds, Vector3 a, Vector3 b)
		{
			return VectorMath.SegmentIntersectsBounds(bounds, a, b);
		}

		public static Vector3[] Subdivide(Vector3[] path, int subdivisions)
		{
			subdivisions = ((subdivisions >= 0) ? subdivisions : 0);
			if (subdivisions == 0)
			{
				return path;
			}
			Vector3[] array = new Vector3[(path.Length - 1) * (int)Mathf.Pow(2f, subdivisions) + 1];
			int num = 0;
			for (int i = 0; i < path.Length - 1; i++)
			{
				float num2 = 1f / Mathf.Pow(2f, subdivisions);
				for (float num3 = 0f; num3 < 1f; num3 += num2)
				{
					array[num] = Vector3.Lerp(path[i], path[i + 1], Mathf.SmoothStep(0f, 1f, num3));
					num++;
				}
			}
			array[num] = path[path.Length - 1];
			return array;
		}

		[Obsolete("Scheduled for removal since it is not used by any part of the A* Pathfinding Project")]
		public static Vector3 ClosestPointOnTriangle(Vector3[] triangle, Vector3 point)
		{
			return ClosestPointOnTriangle(triangle[0], triangle[1], triangle[2], point);
		}

		public static Vector3 ClosestPointOnTriangle(Vector3 tr0, Vector3 tr1, Vector3 tr2, Vector3 point)
		{
			Vector3 lhs = tr0 - point;
			Vector3 vector = tr1 - tr0;
			Vector3 vector2 = tr2 - tr0;
			float sqrMagnitude = vector.sqrMagnitude;
			float num = Vector3.Dot(vector, vector2);
			float sqrMagnitude2 = vector2.sqrMagnitude;
			float num2 = Vector3.Dot(lhs, vector);
			float num3 = Vector3.Dot(lhs, vector2);
			float num4 = sqrMagnitude * sqrMagnitude2 - num * num;
			float num5 = num * num3 - sqrMagnitude2 * num2;
			float num6 = num * num2 - sqrMagnitude * num3;
			if (num5 + num6 <= num4)
			{
				if (num5 < 0f)
				{
					if (num6 < 0f)
					{
						if (num2 < 0f)
						{
							num6 = 0f;
							num5 = ((!(0f - num2 >= sqrMagnitude)) ? ((0f - num2) / sqrMagnitude) : 1f);
						}
						else
						{
							num5 = 0f;
							num6 = ((num3 >= 0f) ? 0f : ((!(0f - num3 >= sqrMagnitude2)) ? ((0f - num3) / sqrMagnitude2) : 1f));
						}
					}
					else
					{
						num5 = 0f;
						num6 = ((num3 >= 0f) ? 0f : ((!(0f - num3 >= sqrMagnitude2)) ? ((0f - num3) / sqrMagnitude2) : 1f));
					}
				}
				else if (num6 < 0f)
				{
					num6 = 0f;
					num5 = ((num2 >= 0f) ? 0f : ((!(0f - num2 >= sqrMagnitude)) ? ((0f - num2) / sqrMagnitude) : 1f));
				}
				else
				{
					float num7 = 1f / num4;
					num5 *= num7;
					num6 *= num7;
				}
			}
			else if (num5 < 0f)
			{
				float num8 = num + num2;
				float num9 = sqrMagnitude2 + num3;
				if (num9 > num8)
				{
					float num10 = num9 - num8;
					float num11 = sqrMagnitude - 2f * num + sqrMagnitude2;
					if (num10 >= num11)
					{
						num5 = 1f;
						num6 = 0f;
					}
					else
					{
						num5 = num10 / num11;
						num6 = 1f - num5;
					}
				}
				else
				{
					num5 = 0f;
					num6 = ((num9 <= 0f) ? 1f : ((!(num3 >= 0f)) ? ((0f - num3) / sqrMagnitude2) : 0f));
				}
			}
			else if (num6 < 0f)
			{
				float num8 = num + num3;
				float num9 = sqrMagnitude + num2;
				if (num9 > num8)
				{
					float num10 = num9 - num8;
					float num11 = sqrMagnitude - 2f * num + sqrMagnitude2;
					if (num10 >= num11)
					{
						num6 = 1f;
						num5 = 0f;
					}
					else
					{
						num6 = num10 / num11;
						num5 = 1f - num6;
					}
				}
				else
				{
					num6 = 0f;
					num5 = ((num9 <= 0f) ? 1f : ((!(num2 >= 0f)) ? ((0f - num2) / sqrMagnitude) : 0f));
				}
			}
			else
			{
				float num10 = sqrMagnitude2 + num3 - num - num2;
				if (num10 <= 0f)
				{
					num5 = 0f;
					num6 = 1f;
				}
				else
				{
					float num11 = sqrMagnitude - 2f * num + sqrMagnitude2;
					if (num10 >= num11)
					{
						num5 = 1f;
						num6 = 0f;
					}
					else
					{
						num5 = num10 / num11;
						num6 = 1f - num5;
					}
				}
			}
			return tr0 + num5 * vector + num6 * vector2;
		}

		[Obsolete("Use VectorMath.SqrDistanceSegmentSegment instead")]
		public static float DistanceSegmentSegment3D(Vector3 s1, Vector3 e1, Vector3 s2, Vector3 e2)
		{
			return VectorMath.SqrDistanceSegmentSegment(s1, e1, s2, e2);
		}
	}
}
