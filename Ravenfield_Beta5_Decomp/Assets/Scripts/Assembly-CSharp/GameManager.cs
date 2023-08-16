using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
	public const int MENU_LEVEL_INDEX = 1;

	public static GameManager instance;

	[NonSerialized]
	public bool ingame;

	[NonSerialized]
	public bool spectating;

	public GameObject ingameUiPrefab;

	public GameObject playerPrefab;

	public bool reverseMode;

	public bool assaultMode;

	public bool nightMode;

	public bool noVehicles;

	public int victoryPoints = 200;

	public AudioMixerGroup fpMixerGroup;

	public GameObject spectatorCameraPrefab;

	private float gameStartTime;

	private void Awake()
	{
		instance = this;
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		SceneManager.sceneLoaded += OnLevelLoaded;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= OnLevelLoaded;
	}

	private void Start()
	{
	}

	private void OnLevelLoaded(Scene scene, LoadSceneMode mode)
	{
		OnLevelIndexLoaded(scene.buildIndex);
	}

	private void OnLevelIndexLoaded(int levelIndex)
	{
		if (IngameLevel(levelIndex))
		{
			StartGame();
		}
		else
		{
			ingame = false;
		}
	}

	private bool IngameLevel(int level)
	{
		return level > 1;
	}

	private void StartGame()
	{
		ingame = true;
		UnityEngine.Object.Instantiate(ingameUiPrefab);
		UnityEngine.Object.Instantiate(playerPrefab, new Vector3(0f, 1000f, 0f), Quaternion.identity);
		ActorManager.instance.StartGame();
		CoverManager.instance.StartGame();
		DecalManager.instance.StartGame();
		gameStartTime = Time.time;
		Invoke("OpenPlayerLoadout", 1f);
	}

	private void OpenPlayerLoadout()
	{
		if (Input.GetKey(KeyCode.S))
		{
			UnityEngine.Object.Instantiate(spectatorCameraPrefab, SceneryCamera.instance.transform.position, SceneryCamera.instance.transform.rotation);
			FpsActorController.instance.DisableCameras();
			FpsActorController.instance.DisableAudioListener();
			SceneryCamera.instance.camera.enabled = false;
			spectating = true;
		}
		else
		{
			FpsActorController.instance.OpenLoadoutWhileDead();
		}
	}

	public float ElapsedGameTime()
	{
		return Time.time - gameStartTime;
	}
}
