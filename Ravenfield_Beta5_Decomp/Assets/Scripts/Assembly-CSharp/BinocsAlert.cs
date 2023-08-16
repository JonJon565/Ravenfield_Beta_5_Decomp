using UnityEngine;

public class BinocsAlert : MonoBehaviour
{
	public Color color;

	private Action lifetime = new Action(1.3f);

	private Material material;

	private void Start()
	{
		lifetime.Start();
		material = GetComponentInChildren<Renderer>().sharedMaterial;
	}

	private void Update()
	{
		if (lifetime.TrueDone())
		{
			Object.Destroy(base.gameObject);
		}
		material.SetColor("_TintColor", Color.Lerp(Color.black, color, Mathf.Clamp01(2f - 2f * lifetime.Ratio())));
	}
}
