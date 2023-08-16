using System;
using UnityEngine;

public class SceneryCamera : MonoBehaviour
{
	public static SceneryCamera instance;

	[NonSerialized]
	public Camera camera;

	private int cullingMask;

	private void Awake()
	{
		instance = this;
		camera = GetComponent<Camera>();
		cullingMask = camera.cullingMask;
		camera.cullingMask = 0;
		camera.clearFlags = CameraClearFlags.Color;
	}

	private void Start()
	{
		Invoke("Render", 0.5f);
	}

	private void Render()
	{
		camera.cullingMask = cullingMask;
		camera.clearFlags = CameraClearFlags.Skybox;
	}
}
