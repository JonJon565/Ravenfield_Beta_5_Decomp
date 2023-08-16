using System;
using UnityEngine;

namespace CellBlockUtils
{
	[Serializable]
	public struct Coordinate
	{
		public static Coordinate Zero = new Coordinate(0, 0, 0);

		public int x;

		public int y;

		public int z;

		public Coordinate(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}

		public Coordinate(Vector3 vector)
		{
			x = Mathf.RoundToInt(vector.x);
			y = Mathf.RoundToInt(vector.y);
			z = Mathf.RoundToInt(vector.z);
		}

		public Vector3 Vector3()
		{
			return new Vector3(x, y, z);
		}

		public static int ManhattanDistance(Coordinate c1, Coordinate c2)
		{
			return Mathf.Abs(c1.x - c2.x) + Mathf.Abs(c1.y - c2.y) + Mathf.Abs(c1.z - c2.z);
		}

		public static float Distance(Coordinate c1, Coordinate c2)
		{
			return UnityEngine.Vector3.Distance(c1.Vector3(), c2.Vector3());
		}

		public override bool Equals(object obj)
		{
			if (obj.GetType() != typeof(Coordinate))
			{
				return false;
			}
			Coordinate coordinate = (Coordinate)obj;
			return x == coordinate.x && y == coordinate.y && z == coordinate.z;
		}

		public override int GetHashCode()
		{
			return x ^ y ^ z;
		}

		public static Coordinate operator +(Coordinate c1, Coordinate c2)
		{
			return new Coordinate(c1.x + c2.x, c1.y + c2.y, c1.z + c2.z);
		}

		public static Coordinate operator -(Coordinate c1, Coordinate c2)
		{
			return new Coordinate(c1.x - c2.x, c1.y - c2.y, c1.z - c2.z);
		}

		public static Coordinate operator *(Coordinate c1, float m)
		{
			return new Coordinate(Mathf.RoundToInt((float)c1.x * m), Mathf.RoundToInt((float)c1.y * m), Mathf.RoundToInt((float)c1.z * m));
		}

		public static bool operator ==(Coordinate c1, Coordinate c2)
		{
			return c1.x == c2.x && c1.y == c2.y && c1.z == c2.z;
		}

		public static bool operator !=(Coordinate c1, Coordinate c2)
		{
			return !(c1 == c2);
		}
	}
}
