using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsUi : MonoBehaviour
{
	public class Options
	{
		public const int HELICOPTER_TYPE_BATTLEFIELD = 0;

		public const int HELICOPTER_TYPE_ARMA = 1;

		public const int HELICOPTER_TYPE_CUSTOM = 2;

		public const int DIFFICULTY_EASY = 0;

		public const int DIFFICULTY_NORMAL = 1;

		public const int DIFFICULTY_HARD = 2;

		public float mouseSensitivity;

		public float sniperMultiplier;

		public float helicopterSensitivity;

		public float vegetationDensity;

		public float vegetationDistance;

		public float masterVolume;

		public float fieldOfView;

		public bool mouseInvert;

		public bool hitmarkers;

		public bool heliInvertPitch;

		public bool heliInvertYaw;

		public bool heliInvertRoll;

		public bool heliInvertThrottle;

		public bool autoReload;

		public bool toggleAim;

		public bool toggleCrouch;

		public int helicopterType;

		public int difficulty;

		public static Options Load()
		{
			Options options = new Options();
			options.mouseSensitivity = Mathf.Max(0.05f, PlayerPrefs.GetFloat("mouse sensitivity", 0.5f));
			options.sniperMultiplier = Mathf.Max(0.05f, PlayerPrefs.GetFloat("sniper multiplier", 0.3f));
			options.mouseInvert = PlayerPrefs.GetInt("mouse invert", 0) == 1;
			options.helicopterType = PlayerPrefs.GetInt("helicopter type 2", 1);
			options.helicopterSensitivity = PlayerPrefs.GetFloat("helicopter sensitivity 2", 0.5f);
			options.heliInvertPitch = PlayerPrefs.GetInt("helicopter invert pitch 2", 0) == 1;
			options.heliInvertYaw = PlayerPrefs.GetInt("helicopter invert yaw", 0) == 1;
			options.heliInvertRoll = PlayerPrefs.GetInt("helicopter invert roll", 0) == 1;
			options.heliInvertThrottle = PlayerPrefs.GetInt("helicopter invert throttle", 1) == 1;
			options.hitmarkers = PlayerPrefs.GetInt("hitmarkers2", 1) == 1;
			options.autoReload = PlayerPrefs.GetInt("auto reload", 0) == 1;
			options.difficulty = PlayerPrefs.GetInt("difficulty", 1);
			if (IsFastQuality())
			{
				options.vegetationDensity = Mathf.Clamp01(PlayerPrefs.GetFloat("fast vegetation density", 0f));
			}
			else
			{
				options.vegetationDensity = Mathf.Clamp01(PlayerPrefs.GetFloat("vegetation density", 0.5f));
			}
			options.vegetationDistance = Mathf.Clamp01(PlayerPrefs.GetFloat("vegetation distance", 0.7f));
			options.masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("master volume", 1f));
			options.toggleAim = PlayerPrefs.GetInt("toggle aim", 0) == 1;
			options.toggleCrouch = PlayerPrefs.GetInt("toggle crouch", 0) == 1;
			options.fieldOfView = PlayerPrefs.GetFloat("field of view", 90f);
			return options;
		}

		public void Save()
		{
			PlayerPrefs.SetFloat("mouse sensitivity", Mathf.Max(0.05f, instance.mouseSensitivity.value));
			PlayerPrefs.SetFloat("sniper multiplier", Mathf.Max(0.05f, instance.sniperMultiplier.value));
			PlayerPrefs.SetInt("mouse invert", instance.mouseInvert.isOn ? 1 : 0);
			PlayerPrefs.SetInt("helicopter type 2", instance.helicopterType.value);
			PlayerPrefs.SetFloat("helicopter sensitivity 2", instance.helicopterSensitivity.value);
			PlayerPrefs.SetInt("helicopter invert pitch 2", instance.heliInvertPitch.isOn ? 1 : 0);
			PlayerPrefs.SetInt("helicopter invert yaw", instance.heliInvertYaw.isOn ? 1 : 0);
			PlayerPrefs.SetInt("helicopter invert roll", instance.heliInvertRoll.isOn ? 1 : 0);
			PlayerPrefs.SetInt("helicopter invert throttle", instance.heliInvertThrottle.isOn ? 1 : 0);
			PlayerPrefs.SetInt("hitmarkers2", instance.hitmarkers.isOn ? 1 : 0);
			PlayerPrefs.SetInt("auto reload", instance.autoReload.isOn ? 1 : 0);
			PlayerPrefs.SetInt("difficulty", instance.difficulty.value);
			if (IsFastQuality())
			{
				PlayerPrefs.SetFloat("fast vegetation density", instance.vegetationDensity.value);
			}
			else
			{
				PlayerPrefs.SetFloat("vegetation density", instance.vegetationDensity.value);
			}
			PlayerPrefs.SetFloat("vegetation distance", instance.vegetationDistance.value);
			PlayerPrefs.SetFloat("master volume", instance.masterVolume.value);
			PlayerPrefs.SetInt("toggle aim", instance.toggleAim.isOn ? 1 : 0);
			PlayerPrefs.SetInt("toggle crouch", instance.toggleCrouch.isOn ? 1 : 0);
			PlayerPrefs.SetFloat("field of view", instance.fieldOfView.value);
			PlayerPrefs.Save();
		}
	}

	public static OptionsUi instance;

	private static Options options;

	private Canvas canvas;

	public AudioMixer audioMixer;

	public RawImage hitmarkerEffect;

	public Text fieldOfViewLabel;

	private AudioSource hitmarkerAudio;

	public Slider mouseSensitivity;

	public Slider sniperMultiplier;

	public Slider helicopterSensitivity;

	public Slider vegetationDensity;

	public Slider vegetationDistance;

	public Slider masterVolume;

	public Slider fieldOfView;

	public Toggle mouseInvert;

	public Toggle heliInvertPitch;

	public Toggle heliInvertYaw;

	public Toggle heliInvertRoll;

	public Toggle heliInvertThrottle;

	public Toggle hitmarkers;

	public Toggle autoReload;

	public Toggle toggleAim;

	public Toggle toggleCrouch;

	public Dropdown helicopterType;

	public Dropdown difficulty;

	public static bool IsFastQuality()
	{
		return QualitySettings.GetQualityLevel() <= 1;
	}

	public static void Show()
	{
		if (instance != null)
		{
			instance.canvas.enabled = true;
		}
	}

	public static void Hide()
	{
		if (instance != null)
		{
			instance.canvas.enabled = false;
		}
	}

	public static bool IsOpen()
	{
		return instance != null && instance.canvas.enabled;
	}

	public static void SaveAndClose()
	{
		if (instance != null)
		{
			instance.Save();
		}
	}

	public static Options GetOptions()
	{
		if (options == null)
		{
			options = Options.Load();
		}
		return options;
	}

	private void Awake()
	{
		if (instance != null)
		{
			Object.Destroy(instance.gameObject);
		}
		instance = this;
		Object.DontDestroyOnLoad(base.gameObject);
		canvas = GetComponent<Canvas>();
		hitmarkerAudio = hitmarkerEffect.GetComponent<AudioSource>();
		Load();
		Hide();
	}

	private void Start()
	{
		ApplyOptions();
	}

	private void Load()
	{
		Show();
		options = Options.Load();
		mouseSensitivity.value = options.mouseSensitivity;
		sniperMultiplier.value = options.sniperMultiplier;
		mouseInvert.isOn = options.mouseInvert;
		helicopterType.value = options.helicopterType;
		helicopterSensitivity.value = options.helicopterSensitivity;
		heliInvertPitch.isOn = options.heliInvertPitch;
		heliInvertYaw.isOn = options.heliInvertYaw;
		heliInvertRoll.isOn = options.heliInvertRoll;
		heliInvertThrottle.isOn = options.heliInvertThrottle;
		hitmarkers.isOn = options.hitmarkers;
		autoReload.isOn = options.autoReload;
		difficulty.value = options.difficulty;
		vegetationDensity.value = options.vegetationDensity;
		vegetationDistance.value = options.vegetationDistance;
		masterVolume.value = options.masterVolume;
		toggleAim.isOn = options.toggleAim;
		toggleCrouch.isOn = options.toggleCrouch;
		fieldOfView.value = options.fieldOfView;
		ApplyOptions();
	}

	private void Update()
	{
		vegetationDistance.interactable = vegetationDensity.value >= 0.01f;
	}

	public void Cancel()
	{
		Load();
		Hide();
	}

	public void Save()
	{
		options.Save();
		Load();
		Hide();
	}

	private void ApplyOptions()
	{
		if (DetailObjectQuality.instance != null)
		{
			DetailObjectQuality.instance.ApplyQuality();
		}
		float num = GetOptions().masterVolume;
		float value = 0f - (Mathf.Pow(80f, 1f - num) - 1f);
		audioMixer.SetFloat("volume", value);
	}

	public void ToggleHitmarker()
	{
		if (hitmarkers.isOn)
		{
			CancelInvoke();
			StartCoroutine(Hitmarker());
		}
	}

	private IEnumerator Hitmarker()
	{
		hitmarkerEffect.enabled = true;
		hitmarkerAudio.Play();
		yield return new WaitForSecondsRealtime(0.2f);
		hitmarkerEffect.enabled = false;
	}

	public void UpdateFieldOfView()
	{
		string text = Mathf.RoundToInt(fieldOfView.value).ToString();
		fieldOfViewLabel.text = text;
		if (PlayerFpParent.instance != null)
		{
			PlayerFpParent.instance.SetupVerticalFov(fieldOfView.value);
		}
	}
}
