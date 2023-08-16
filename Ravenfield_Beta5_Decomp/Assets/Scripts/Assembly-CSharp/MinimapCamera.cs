using System;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
	private const int RESOLUTION = 1024;

	public static MinimapCamera instance;

	[NonSerialized]
	public Camera camera;

	[NonSerialized]
	public RenderTexture minimapRenderTexture;

	private void Awake()
	{
		instance = this;
		camera = GetComponent<Camera>();
		minimapRenderTexture = new RenderTexture(1024, 1024, 16);
		camera.targetTexture = minimapRenderTexture;
	}

	private void Start()
	{
		Render();
	}

	private void Render()
	{
		bool fog = RenderSettings.fog;
		RenderSettings.fog = false;
		camera.Render();
		RenderSettings.fog = fog;
		camera.enabled = false;
	}

	public Texture Minimap()
	{
		return minimapRenderTexture;
	}
}
