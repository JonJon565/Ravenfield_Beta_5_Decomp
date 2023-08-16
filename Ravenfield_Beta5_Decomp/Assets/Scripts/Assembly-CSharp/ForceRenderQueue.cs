using UnityEngine;

[ExecuteInEditMode]
public class ForceRenderQueue : MonoBehaviour
{
	public int queue;

	private void OnEnable()
	{
		GetComponent<Renderer>().sharedMaterial.renderQueue = queue;
	}
}
