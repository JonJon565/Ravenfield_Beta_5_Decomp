using UnityEngine;

public class VertexRedToAlpha : MonoBehaviour
{
	private void Awake()
	{
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		Color[] colors = mesh.colors;
		for (int i = 0; i < mesh.colors.Length; i++)
		{
			colors[i].a = colors[i].r;
		}
		mesh.colors = colors;
	}
}
