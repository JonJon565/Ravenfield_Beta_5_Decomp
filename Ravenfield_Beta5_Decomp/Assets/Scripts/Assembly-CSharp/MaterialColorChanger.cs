using UnityEngine;

[ExecuteInEditMode]
public class MaterialColorChanger : MonoBehaviour
{
	public Material material;

	public Color color;

	private void OnEnable()
	{
		material.color = color;
	}
}
