using UnityEngine;

public class UvOffset : MonoBehaviour
{
	private Material material;

	private void Awake()
	{
		material = GetComponent<Renderer>().materials[0];
	}

	public void SetOffset(Vector2 offset)
	{
		material.mainTextureOffset = offset;
	}

	public void IncrementOffset(Vector2 increment)
	{
		material.mainTextureOffset += increment;
	}
}
