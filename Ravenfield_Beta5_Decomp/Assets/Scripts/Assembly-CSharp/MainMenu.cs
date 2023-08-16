using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
	private const string NEWS_URL = "https://docs.google.com/document/export?format=txt&id=12lhDOtsf3FUBGm-UGuNdVV1_3qju_ODtQvuuS1f7xp0";

	private const string GREENLIGHT_URL = "https://docs.google.com/document/export?format=txt&id=10slFia3_pfrp9G0Sna3K77uuhw-7E441DbVmuOsx3fc";

	private const string GREENLIGHT_STEAM_PREFIX = "steam://url/CommunityFilePage/";

	private const string GREENLIGHT_WEB_PREFIX = "http://steamcommunity.com/sharedfiles/filedetails/?id=";

	public static string greenlightId = string.Empty;

	public static bool displayedNews;

	public GameObject menuContent;

	public GameObject newsContent;

	public GameObject greenlightContent;

	public GameObject greenlightButton;

	public Text newsText;

	public Toggle assaultModeToggle;

	public Toggle reverseModeToggle;

	public Toggle nightModeToggle;

	public Toggle noVehiclesToggle;

	public InputField victoryScoreInput;

	public InputField numberActorsInput;

	public InputField respawnTimeInput;

	public Slider botBalanceSlider;

	private bool greenlightActivated;

	private string greenlightWebUrl = "https://twitter.com/SteelRaven7";

	private string greenlightSteamUrl = "https://twitter.com/SteelRaven7";

	private void Start()
	{
		menuContent.SetActive(false);
		newsContent.SetActive(true);
		if (displayedNews)
		{
			if (!string.IsNullOrEmpty(greenlightId))
			{
				ActivateGreenlight();
			}
			ShowMenu();
		}
		else
		{
			StartCoroutine(LoadNews());
			StartCoroutine(LoadGreenlight());
			displayedNews = true;
		}
	}

	public void StartLevel(string levelName)
	{
		SaveGameSettings();
		Application.LoadLevel(levelName);
	}

	private void SaveGameSettings()
	{
		GameManager.instance.assaultMode = assaultModeToggle.isOn;
		GameManager.instance.reverseMode = reverseModeToggle.isOn;
		GameManager.instance.nightMode = nightModeToggle.isOn;
		GameManager.instance.noVehicles = noVehiclesToggle.isOn;
		int result;
		if (int.TryParse(victoryScoreInput.text, out result))
		{
			GameManager.instance.victoryPoints = result;
		}
		int result2;
		if (int.TryParse(numberActorsInput.text, out result2))
		{
			int num = Mathf.RoundToInt(botBalanceSlider.value * (float)result2);
			ActorManager.instance.team0Bots = result2 - num;
			ActorManager.instance.team1Bots = num;
		}
		int result3;
		if (int.TryParse(respawnTimeInput.text, out result3))
		{
			ActorManager.instance.spawnTime = result3;
		}
	}

	private void Update()
	{
		if (!newsContent.activeInHierarchy && !greenlightContent.activeInHierarchy)
		{
			menuContent.SetActive(!OptionsUi.IsOpen());
		}
		float value = botBalanceSlider.value;
		Color color = ((!(value > 0.5f)) ? Color.Lerp(Color.blue, Color.white, value * 2f) : Color.Lerp(Color.white, Color.red, (value - 0.5f) * 2f));
		ColorBlock colors = botBalanceSlider.colors;
		colors.normalColor = color;
		colors.highlightedColor = color;
		colors.pressedColor = color;
		botBalanceSlider.colors = colors;
	}

	public void OpenOptions()
	{
		OptionsUi.Show();
	}

	public void CloseNews()
	{
		newsContent.SetActive(false);
		if (greenlightActivated)
		{
			OpenGreenlightWindow();
		}
		else
		{
			ShowMenu();
		}
	}

	public void ShowMenu()
	{
		newsContent.SetActive(false);
		greenlightContent.SetActive(false);
		menuContent.SetActive(true);
	}

	private IEnumerator LoadNews()
	{
		WWW newsRequest = new WWW("https://docs.google.com/document/export?format=txt&id=12lhDOtsf3FUBGm-UGuNdVV1_3qju_ODtQvuuS1f7xp0");
		yield return newsRequest;
		if (newsRequest.error == null)
		{
			ParseNews(newsRequest.text);
		}
		else
		{
			WriteNews("Could not load news: \n\r" + newsRequest.error);
		}
	}

	private IEnumerator LoadGreenlight()
	{
		WWW newsRequest = new WWW("https://docs.google.com/document/export?format=txt&id=10slFia3_pfrp9G0Sna3K77uuhw-7E441DbVmuOsx3fc");
		yield return newsRequest;
		if (newsRequest.error == null)
		{
			ParseGreenlightUrl(newsRequest.text);
		}
		Debug.Log("Greenlight activated? " + greenlightActivated);
	}

	private void ParseNews(string html)
	{
		try
		{
			WriteNews(html);
		}
		catch (Exception ex)
		{
			WriteNews("Could not load news: \n\r" + ex.Message);
		}
	}

	private void ParseGreenlightUrl(string html)
	{
		if (html.Length > 1 && html.Length < 20)
		{
			if (html[0].ToString() == "\ufeff")
			{
				html = html.Substring(1);
			}
			int result;
			if (int.TryParse(html, out result))
			{
				greenlightId = html;
				ActivateGreenlight();
			}
		}
		else
		{
			greenlightActivated = false;
		}
	}

	private void ActivateGreenlight()
	{
		greenlightWebUrl = "http://steamcommunity.com/sharedfiles/filedetails/?id=" + greenlightId;
		greenlightSteamUrl = "steam://url/CommunityFilePage/" + greenlightId;
		Debug.Log(greenlightSteamUrl);
		Debug.Log(greenlightWebUrl);
		greenlightActivated = true;
		greenlightButton.SetActive(true);
	}

	private void WriteNews(string text)
	{
		newsText.text = "\n\r" + text + "\n\r";
		newsText.rectTransform.anchoredPosition = newsText.rectTransform.anchoredPosition + Vector2.right * 3f;
	}

	public void OpenTwitter()
	{
		Application.OpenURL("http://twitter.com/SteelRaven7");
	}

	public void OpenGreenlightWeb()
	{
		Application.OpenURL(greenlightWebUrl);
	}

	public void OpenGreenlightSteam()
	{
		Application.OpenURL(greenlightSteamUrl);
	}

	public void OpenGreenlightWindow()
	{
		newsContent.SetActive(false);
		greenlightContent.SetActive(true);
		menuContent.SetActive(false);
	}

	public void Quit()
	{
		Application.Quit();
	}
}
