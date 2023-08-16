using UnityEngine;

namespace Pathfinding.Voxels
{
	public class Utility
	{
		public static Color[] colors = new Color[7]
		{
			Color.green,
			Color.blue,
			Color.red,
			Color.yellow,
			Color.cyan,
			Color.white,
			Color.black
		};

		public static float lastStartTime;

		public static float lastAdditiveTimerStart;

		public static float additiveTimer;

		private static float[] clipPolygonCache = new float[21];

		private static int[] clipPolygonIntCache = new int[21];

		public static Color GetColor(int i)
		{
			while (i >= colors.Length)
			{
				i -= colors.Length;
			}
			while (i < 0)
			{
				i += colors.Length;
			}
			return colors[i];
		}

		public static int Bit(int a, int b)
		{
			return (a & (1 << b)) >> b;
		}

		public static Color IntToColor(int i, float a)
		{
			int num = Bit(i, 1) + Bit(i, 3) * 2 + 1;
			int num2 = Bit(i, 2) + Bit(i, 4) * 2 + 1;
			int num3 = Bit(i, 0) + Bit(i, 5) * 2 + 1;
			return new Color((float)num * 0.25f, (float)num2 * 0.25f, (float)num3 * 0.25f, a);
		}

		public static float TriangleArea2(Vector3 a, Vector3 b, Vector3 c)
		{
			return Mathf.Abs(a.x * b.z + b.x * c.z + c.x * a.z - a.x * c.z - c.x * b.z - b.x * a.z);
		}

		public static float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
		{
			return (b.x - a.x) * (c.z - a.z) - (c.x - a.x) * (b.z - a.z);
		}

		public static float Min(float a, float b, float c)
		{
			a = ((!(a < b)) ? b : a);
			return (!(a < c)) ? c : a;
		}

		public static float Max(float a, float b, float c)
		{
			a = ((!(a > b)) ? b : a);
			return (!(a > c)) ? c : a;
		}

		public static int Max(int a, int b, int c, int d)
		{
			a = ((a <= b) ? b : a);
			a = ((a <= c) ? c : a);
			return (a <= d) ? d : a;
		}

		public static int Min(int a, int b, int c, int d)
		{
			a = ((a >= b) ? b : a);
			a = ((a >= c) ? c : a);
			return (a >= d) ? d : a;
		}

		public static float Max(float a, float b, float c, float d)
		{
			a = ((!(a > b)) ? b : a);
			a = ((!(a > c)) ? c : a);
			return (!(a > d)) ? d : a;
		}

		public static float Min(float a, float b, float c, float d)
		{
			a = ((!(a < b)) ? b : a);
			a = ((!(a < c)) ? c : a);
			return (!(a < d)) ? d : a;
		}

		public static string ToMillis(float v)
		{
			return (v * 1000f).ToString("0");
		}

		public static void StartTimer()
		{
			lastStartTime = Time.realtimeSinceStartup;
		}

		public static void EndTimer(string label)
		{
			Debug.Log(label + ", process took " + ToMillis(Time.realtimeSinceStartup - lastStartTime) + "ms to complete");
		}

		public static void StartTimerAdditive(bool reset)
		{
			if (reset)
			{
				additiveTimer = 0f;
			}
			lastAdditiveTimerStart = Time.realtimeSinceStartup;
		}

		public static void EndTimerAdditive(string label, bool log)
		{
			additiveTimer += Time.realtimeSinceStartup - lastAdditiveTimerStart;
			if (log)
			{
				Debug.Log(label + ", process took " + ToMillis(additiveTimer) + "ms to complete");
			}
			lastAdditiveTimerStart = Time.realtimeSinceStartup;
		}

		public static void CopyVector(float[] a, int i, Vector3 v)
		{
			a[i] = v.x;
			a[i + 1] = v.y;
			a[i + 2] = v.z;
		}

		public static int ClipPoly(float[] vIn, int n, float[] vOut, float pnx, float pnz, float pd)
		{
			float[] array = clipPolygonCache;
			for (int i = 0; i < n; i++)
			{
				array[i] = pnx * vIn[i * 3] + pnz * vIn[i * 3 + 2] + pd;
			}
			int num = 0;
			int j = 0;
			int num2 = n - 1;
			for (; j < n; j++)
			{
				bool flag = array[num2] >= 0f;
				bool flag2 = array[j] >= 0f;
				if (flag != flag2)
				{
					float num3 = array[num2] / (array[num2] - array[j]);
					vOut[num * 3] = vIn[num2 * 3] + (vIn[j * 3] - vIn[num2 * 3]) * num3;
					vOut[num * 3 + 1] = vIn[num2 * 3 + 1] + (vIn[j * 3 + 1] - vIn[num2 * 3 + 1]) * num3;
					vOut[num * 3 + 2] = vIn[num2 * 3 + 2] + (vIn[j * 3 + 2] - vIn[num2 * 3 + 2]) * num3;
					num++;
				}
				if (flag2)
				{
					vOut[num * 3] = vIn[j * 3];
					vOut[num * 3 + 1] = vIn[j * 3 + 1];
					vOut[num * 3 + 2] = vIn[j * 3 + 2];
					num++;
				}
				num2 = j;
			}
			return num;
		}

		public static int ClipPolygon(float[] vIn, int n, float[] vOut, float multi, float offset, int axis)
		{
			float[] array = clipPolygonCache;
			for (int i = 0; i < n; i++)
			{
				array[i] = multi * vIn[i * 3 + axis] + offset;
			}
			int num = 0;
			int j = 0;
			int num2 = n - 1;
			for (; j < n; j++)
			{
				bool flag = array[num2] >= 0f;
				bool flag2 = array[j] >= 0f;
				if (flag != flag2)
				{
					int num3 = num * 3;
					int num4 = j * 3;
					int num5 = num2 * 3;
					float num6 = array[num2] / (array[num2] - array[j]);
					vOut[num3] = vIn[num5] + (vIn[num4] - vIn[num5]) * num6;
					vOut[num3 + 1] = vIn[num5 + 1] + (vIn[num4 + 1] - vIn[num5 + 1]) * num6;
					vOut[num3 + 2] = vIn[num5 + 2] + (vIn[num4 + 2] - vIn[num5 + 2]) * num6;
					num++;
				}
				if (flag2)
				{
					int num7 = num * 3;
					int num8 = j * 3;
					vOut[num7] = vIn[num8];
					vOut[num7 + 1] = vIn[num8 + 1];
					vOut[num7 + 2] = vIn[num8 + 2];
					num++;
				}
				num2 = j;
			}
			return num;
		}

		public static int ClipPolygonY(float[] vIn, int n, float[] vOut, float multi, float offset, int axis)
		{
			float[] array = clipPolygonCache;
			for (int i = 0; i < n; i++)
			{
				array[i] = multi * vIn[i * 3 + axis] + offset;
			}
			int num = 0;
			int j = 0;
			int num2 = n - 1;
			for (; j < n; j++)
			{
				bool flag = array[num2] >= 0f;
				bool flag2 = array[j] >= 0f;
				if (flag != flag2)
				{
					vOut[num * 3 + 1] = vIn[num2 * 3 + 1] + (vIn[j * 3 + 1] - vIn[num2 * 3 + 1]) * (array[num2] / (array[num2] - array[j]));
					num++;
				}
				if (flag2)
				{
					vOut[num * 3 + 1] = vIn[j * 3 + 1];
					num++;
				}
				num2 = j;
			}
			return num;
		}

		public static int ClipPolygon(Vector3[] vIn, int n, Vector3[] vOut, float multi, float offset, int axis)
		{
			float[] array = clipPolygonCache;
			for (int i = 0; i < n; i++)
			{
				array[i] = multi * vIn[i][axis] + offset;
			}
			int num = 0;
			int j = 0;
			int num2 = n - 1;
			for (; j < n; j++)
			{
				bool flag = array[num2] >= 0f;
				bool flag2 = array[j] >= 0f;
				if (flag != flag2)
				{
					float num3 = array[num2] / (array[num2] - array[j]);
					vOut[num] = vIn[num2] + (vIn[j] - vIn[num2]) * num3;
					num++;
				}
				if (flag2)
				{
					vOut[num] = vIn[j];
					num++;
				}
				num2 = j;
			}
			return num;
		}

		public static int ClipPolygon(Int3[] vIn, int n, Int3[] vOut, int multi, int offset, int axis)
		{
			int[] array = clipPolygonIntCache;
			for (int i = 0; i < n; i++)
			{
				array[i] = multi * vIn[i][axis] + offset;
			}
			int num = 0;
			int j = 0;
			int num2 = n - 1;
			for (; j < n; j++)
			{
				bool flag = array[num2] >= 0;
				bool flag2 = array[j] >= 0;
				if (flag != flag2)
				{
					double num3 = (double)array[num2] / (double)(array[num2] - array[j]);
					vOut[num] = vIn[num2] + (vIn[j] - vIn[num2]) * num3;
					num++;
				}
				if (flag2)
				{
					vOut[num] = vIn[j];
					num++;
				}
				num2 = j;
			}
			return num;
		}

		public static bool IntersectXAxis(out Vector3 intersection, Vector3 start1, Vector3 dir1, float x)
		{
			float x2 = dir1.x;
			if (x2 == 0f)
			{
				intersection = Vector3.zero;
				return false;
			}
			float num = x - start1.x;
			float value = num / x2;
			value = Mathf.Clamp01(value);
			intersection = start1 + dir1 * value;
			return true;
		}

		public static bool IntersectZAxis(out Vector3 intersection, Vector3 start1, Vector3 dir1, float z)
		{
			float num = 0f - dir1.z;
			if (num == 0f)
			{
				intersection = Vector3.zero;
				return false;
			}
			float num2 = start1.z - z;
			float value = num2 / num;
			value = Mathf.Clamp01(value);
			intersection = start1 + dir1 * value;
			return true;
		}
	}
}
