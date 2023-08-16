using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityStandardAssets.Characters.FirstPerson;

public class IngameMenuUi : MonoBehaviour
{
	public static IngameMenuUi instance;

	public AudioMixer mixer;

	private Canvas canvas;

	public static void Show()
	{
		instance.canvas.enabled = true;
		MouseLook.paused = true;
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		Time.timeScale = 0f;
		instance.mixer.SetFloat("pitch", Time.timeScale);
	}

	public static void Hide()
	{
		instance.canvas.enabled = false;
		MouseLook.paused = false;
		Time.timeScale = 1f;
		Time.fixedDeltaTime = Time.timeScale / 60f;
		instance.mixer.SetFloat("pitch", Time.timeScale);
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	public static bool IsOpen()
	{
		return instance.canvas.enabled;
	}

	private void Awake()
	{
		instance = this;
		canvas = GetComponent<Canvas>();
		canvas.enabled = false;
		Hide();
	}

	public void Resume()
	{
		Hide();
	}

	public void Options()
	{
		OptionsUi.Show();
	}

	public void Menu()
	{
		MouseLook.paused = false;
		SceneManager.LoadScene(1);
	}

	public void Quit()
	{
		Application.Quit();
	}

	private void Update()
	{
		if (!Input.GetKeyDown(KeyCode.Escape))
		{
			return;
		}
		if (canvas.enabled)
		{
			Hide();
			if (OptionsUi.IsOpen())
			{
				OptionsUi.SaveAndClose();
			}
		}
		else
		{
			Show();
		}
	}
}
