using System;
using UnityEngine;

public class QualitySwitcher : MonoBehaviour
{
	public int hqLevel = 5;

	public GameObject hqObject;

	public GameObject lqObject;

	[NonSerialized]
	public GameObject activeObject;

	private void Awake()
	{
		bool flag = QualitySettings.GetQualityLevel() >= hqLevel;
		hqObject.SetActive(flag);
		if (lqObject != null)
		{
			lqObject.SetActive(!flag);
		}
		if (flag)
		{
			activeObject = hqObject;
		}
		else
		{
			activeObject = lqObject;
		}
	}
}
