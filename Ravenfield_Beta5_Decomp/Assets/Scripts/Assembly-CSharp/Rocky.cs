using UnityEngine;

public class Rocky : MonoBehaviour
{
	public GameObject rockPrefab;

	[Range(0f, 20f)]
	public float depth;

	[Range(0f, 10f)]
	public float depthOctaves;

	public float noiseAmplitude = 0.5f;

	public float decimateDistance = 1f;

	[Range(0f, 5f)]
	public int iterations;

	private void Start()
	{
	}

	private void Update()
	{
	}
}
