using UnityEngine;

public class DetailObjectQuality : MonoBehaviour
{
	public static DetailObjectQuality instance;

	private float maxDensity;

	private float maxDistance;

	private void Awake()
	{
		instance = this;
		Terrain component = GetComponent<Terrain>();
		maxDensity = component.detailObjectDensity;
		maxDistance = component.detailObjectDistance;
		ApplyQuality();
	}

	public void ApplyQuality()
	{
		Terrain component = GetComponent<Terrain>();
		int qualityLevel = QualitySettings.GetQualityLevel();
		float num = Mathf.Clamp01(OptionsUi.GetOptions().vegetationDensity);
		component.drawTreesAndFoliage = num >= 0.01f;
		component.detailObjectDistance = OptionsUi.GetOptions().vegetationDistance * maxDistance;
		component.detailObjectDensity = num * maxDensity;
	}
}
