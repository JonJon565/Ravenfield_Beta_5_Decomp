using UnityEngine;

namespace Pathfinding.Voxels
{
	public struct ExtraMesh
	{
		public MeshFilter original;

		public int area;

		public Vector3[] vertices;

		public int[] triangles;

		public Bounds bounds;

		public Matrix4x4 matrix;

		public ExtraMesh(Vector3[] v, int[] t, Bounds b)
		{
			matrix = Matrix4x4.identity;
			vertices = v;
			triangles = t;
			bounds = b;
			original = null;
			area = 0;
		}

		public ExtraMesh(Vector3[] v, int[] t, Bounds b, Matrix4x4 matrix)
		{
			this.matrix = matrix;
			vertices = v;
			triangles = t;
			bounds = b;
			original = null;
			area = 0;
		}

		public void RecalculateBounds()
		{
			Bounds bounds = new Bounds(matrix.MultiplyPoint3x4(vertices[0]), Vector3.zero);
			for (int i = 1; i < vertices.Length; i++)
			{
				bounds.Encapsulate(matrix.MultiplyPoint3x4(vertices[i]));
			}
			this.bounds = bounds;
		}
	}
}
