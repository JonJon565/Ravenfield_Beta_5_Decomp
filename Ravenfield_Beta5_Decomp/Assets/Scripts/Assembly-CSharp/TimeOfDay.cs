using System;
using System.Collections.Generic;
using UnityEngine;

public class TimeOfDay : MonoBehaviour
{
	[Serializable]
	public class Atmosphere
	{
		public Color sky;

		public Color equator;

		public Color ground;

		public float fogDensity;

		public Color fog;

		public Material skyboxMaterial;
	}

	private const float NIGHT_VISION_AMOUNT = 0.7f;

	private const float NIGHT_VISION_EXTRA_EXPOSURE = 1.1f;

	private const float NIGHT_VISION_LIGHT_BASE = 0.4f;

	private const float NIGHT_VISION_LIGHT_MULTIPLIER = 4f;

	public static TimeOfDay instance;

	public Atmosphere nightAtmosphere;

	private Atmosphere atmosphere;

	public bool testNight;

	private Light[] lights;

	private Dictionary<Light, float> lightIntensity;

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		if (GameManager.instance.nightMode)
		{
			ApplyNight();
		}
		else
		{
			ApplyDay();
		}
		lights = UnityEngine.Object.FindObjectsOfType<Light>();
		lightIntensity = new Dictionary<Light, float>(lights.Length);
		Light[] array = lights;
		foreach (Light light in array)
		{
			lightIntensity.Add(light, light.intensity);
		}
		ReflectionProber.instance.SetupProbes();
	}

	private void ApplyDay()
	{
		base.transform.Find("Day").gameObject.SetActive(true);
		base.transform.Find("Night").gameObject.SetActive(false);
		atmosphere = new Atmosphere();
		atmosphere.sky = RenderSettings.ambientSkyColor;
		atmosphere.equator = RenderSettings.ambientEquatorColor;
		atmosphere.ground = RenderSettings.ambientGroundColor;
		atmosphere.fog = RenderSettings.fogColor;
		atmosphere.fogDensity = RenderSettings.fogDensity;
		atmosphere.skyboxMaterial = RenderSettings.skybox;
		ApplyAtmosphere(atmosphere);
	}

	private void ApplyNight()
	{
		base.transform.Find("Day").gameObject.SetActive(false);
		base.transform.Find("Night").gameObject.SetActive(true);
		ApplyAtmosphere(nightAtmosphere);
	}

	private void ApplyAtmosphere(Atmosphere atmosphere)
	{
		this.atmosphere = atmosphere;
		RenderSettings.ambientSkyColor = atmosphere.sky;
		RenderSettings.ambientEquatorColor = atmosphere.equator;
		RenderSettings.ambientGroundColor = atmosphere.ground;
		RenderSettings.fogColor = atmosphere.fog;
		RenderSettings.fogDensity = atmosphere.fogDensity;
		RenderSettings.skybox = new Material(atmosphere.skyboxMaterial);
	}

	public void ApplyNightvision()
	{
		instance.BlendAtmosphereColor(Color.green, 0.7f, 1.1f);
		ReflectionProber.instance.SwitchToNightVision();
		Light[] array = lights;
		foreach (Light light in array)
		{
			light.intensity = lightIntensity[light] * 4f + 0.4f;
		}
	}

	private void BlendAtmosphereColor(Color target, float amount, float extraExposure)
	{
		RenderSettings.ambientSkyColor = (1f + extraExposure) * Color.Lerp(atmosphere.sky, target, amount);
		RenderSettings.ambientEquatorColor = (1f + extraExposure) * Color.Lerp(atmosphere.equator, target, amount);
		RenderSettings.ambientGroundColor = (1f + extraExposure) * Color.Lerp(atmosphere.ground, target, amount);
		RenderSettings.fogColor = Color.Lerp(atmosphere.fog, target, amount);
		RenderSettings.skybox.SetColor("_SkyTint", Color.Lerp(atmosphere.skyboxMaterial.GetColor("_SkyTint"), target, amount));
		RenderSettings.skybox.SetFloat("_Exposure", atmosphere.skyboxMaterial.GetFloat("_Exposure") + extraExposure);
	}

	public void ResetAtmosphere()
	{
		ApplyAtmosphere(atmosphere);
		ReflectionProber.instance.Reset();
		Light[] array = lights;
		foreach (Light light in array)
		{
			light.intensity = lightIntensity[light];
		}
	}
}
