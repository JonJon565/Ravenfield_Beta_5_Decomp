using System.Collections;
using UnityEngine;

public class ReflectionProber : MonoBehaviour
{
	public static ReflectionProber instance;

	public ReflectionProbe normalProbe;

	public ReflectionProbe nightVisionProbe;

	private Vector3 enabledBounds = new Vector3(9999999f, 9999999f, 9999999f);

	private Vector3 disabledBounds = new Vector3(0f, 0f, 0f);

	private void Awake()
	{
		instance = this;
	}

	public void SetupProbes()
	{
		StartCoroutine(SetupProbesCoroutine());
	}

	private IEnumerator SetupProbesCoroutine()
	{
		normalProbe.RenderProbe();
		yield return new WaitForEndOfFrame();
		TimeOfDay.instance.ApplyNightvision();
		nightVisionProbe.RenderProbe();
		yield return new WaitForEndOfFrame();
		TimeOfDay.instance.ResetAtmosphere();
	}

	public void SwitchToNightVision()
	{
		normalProbe.size = disabledBounds;
		nightVisionProbe.size = enabledBounds;
	}

	public void Reset()
	{
		normalProbe.size = enabledBounds;
		nightVisionProbe.size = disabledBounds;
	}
}
