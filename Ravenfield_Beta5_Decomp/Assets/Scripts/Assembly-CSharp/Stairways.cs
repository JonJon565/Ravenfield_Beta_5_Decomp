using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class Stairways : MonoBehaviour
{
	[HideInInspector]
	public Vector3 target;

	public bool singleAxis = true;

	public float targetStepHeight = 0.2f;

	public float width = 2f;

	public float depth = 1f;

	public bool leftEdge = true;

	public bool rightEdge = true;

	public float edgeWidth = 0.2f;
}
