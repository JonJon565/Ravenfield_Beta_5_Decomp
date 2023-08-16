using System;
using System.IO;
using UnityEngine;

public class WeaponPhotoShoot : MonoBehaviour
{
	public bool large;

	[Range(0f, 1f)]
	public float sizeMultiplier = 1f;

	private Camera camera;

	public Camera pass2Camera;

	private RenderTexture renderTexture;

	public Material material;

	public Renderer pass2Quad;

	public Transform fallbackTarget;

	private void Start()
	{
		camera = GetComponent<Camera>();
		Render();
	}

	private void Render()
	{
		this.renderTexture = new RenderTexture(2048, 2048, 0);
		Transform transform = fallbackTarget;
		Weapon weapon = UnityEngine.Object.FindObjectOfType<Weapon>();
		if (weapon != null)
		{
			transform = weapon.transform;
		}
		transform.position = Vector3.zero;
		try
		{
			transform.Find("Arms").GetComponent<Renderer>().enabled = false;
		}
		catch (Exception)
		{
		}
		if (transform == null)
		{
			Debug.LogError("No weapon target");
			return;
		}
		Animator component = transform.GetComponent<Animator>();
		if (component != null)
		{
			component.enabled = false;
		}
		Bounds bounds = default(Bounds);
		Renderer[] componentsInChildren = transform.GetComponentsInChildren<Renderer>();
		foreach (Renderer renderer in componentsInChildren)
		{
			if (renderer.enabled)
			{
				Material[] array = new Material[renderer.materials.Length];
				for (int j = 0; j < renderer.materials.Length; j++)
				{
					array[j] = material;
				}
				renderer.materials = array;
				bounds.SetMinMax(Vector3.Min(bounds.min, renderer.bounds.min), Vector3.Max(bounds.max, renderer.bounds.max));
			}
		}
		Vector3 center = bounds.center;
		center.x = -10f;
		camera.transform.position = center;
		Debug.Log(bounds.extents.z);
		camera.orthographicSize = bounds.extents.z + (0.1f + bounds.extents.z * (5f - 5f * sizeMultiplier));
		camera.aspect = 1f;
		camera.targetTexture = this.renderTexture;
		pass2Quad.material.mainTexture = this.renderTexture;
		camera.Render();
		camera.enabled = false;
		RenderTexture renderTexture;
		if (large)
		{
			renderTexture = new RenderTexture(1760, 608, 0);
			pass2Camera.orthographicSize *= 0.5f;
		}
		else
		{
			renderTexture = new RenderTexture(840, 608, 0);
			pass2Camera.orthographicSize *= 0.85f;
		}
		pass2Camera.targetTexture = renderTexture;
		pass2Camera.Render();
		pass2Camera.enabled = false;
		Graphics.SetRenderTarget(this.renderTexture);
		Graphics.SetRenderTarget(null);
		pass2Quad.material.mainTexture = renderTexture;
		RenderTexture.active = renderTexture;
		Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height);
		texture2D.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
		texture2D.Apply();
		Debug.Log("Writing stream");
		byte[] array2 = new byte[4] { 1, 2, 3, 4 };
		File.WriteAllBytes("weapon_render.png", texture2D.EncodeToPNG());
		Debug.Log("Done!");
	}
}
