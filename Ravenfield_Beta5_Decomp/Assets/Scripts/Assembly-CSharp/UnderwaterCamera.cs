using UnityEngine;

public class UnderwaterCamera : MonoBehaviour
{
	private Camera camera;

	private Texture2D whiteTexture;

	private void Awake()
	{
		camera = GetComponent<Camera>();
		whiteTexture = new Texture2D(1, 1);
	}

	private void OnGUI()
	{
		if (camera.enabled && WaterLevel.InWater(base.transform.position))
		{
			GUI.color = new Color(0.2f, 0.2f, 0.5f, 0.9f);
			GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture);
		}
	}
}
