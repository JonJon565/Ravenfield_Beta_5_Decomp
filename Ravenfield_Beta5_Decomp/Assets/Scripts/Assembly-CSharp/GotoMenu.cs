using UnityEngine;
using UnityEngine.SceneManagement;

public class GotoMenu : MonoBehaviour
{
	public float duration = 10f;

	private Action nextSceneAction = new Action(1f);

	private void Start()
	{
		nextSceneAction.StartLifetime(duration);
	}

	private void Update()
	{
		if ((Input.anyKeyDown && CanSkip()) || nextSceneAction.TrueDone())
		{
			GotoNextScene();
		}
	}

	private void GotoNextScene()
	{
		PlayerPrefs.SetInt("SeenIntro", 1);
		PlayerPrefs.Save();
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
	}

	private bool CanSkip()
	{
		return PlayerPrefs.HasKey("SeenIntro");
	}
}
