using System.Collections;
using UnityEngine;

public class Eyes : MonoBehaviour
{
	private const float AUTO_MIN_OPEN_TIME = 2f;

	private const float AUTO_MAX_OPEN_TIME = 8f;

	private const float BLINK_TIME = 0.2f;

	private Material material;

	private void Awake()
	{
		material = GetComponent<MeshRenderer>().material;
		AutoUpdateEye();
	}

	public void AutoUpdateEye()
	{
		StopAllCoroutines();
		StartCoroutine(AutoUpdateCoroutine());
	}

	private IEnumerator AutoUpdateCoroutine()
	{
		while (true)
		{
			OpenEye();
			yield return new WaitForSeconds(Random.Range(2f, 8f));
			CloseEye();
			yield return new WaitForSeconds(0.2f);
		}
	}

	private void OpenEye()
	{
		material.mainTextureOffset = new Vector2(0f, 0f);
	}

	private void CloseEye()
	{
		material.mainTextureOffset = new Vector2(0.5f, 0f);
	}

	public void ForceOpenEye()
	{
		StopAllCoroutines();
		OpenEye();
	}

	public void ForceCloseEye()
	{
		StopAllCoroutines();
		CloseEye();
	}
}
