using UnityEngine;
using UnityEngine.UI;

public class ScoreUi : MonoBehaviour
{
	public static ScoreUi instance;

	public Text blueScoreText;

	public Text redScoreText;

	public Text blueFlagsText;

	public Text redFlagsText;

	public Text victoryText;

	public Image blueBar;

	public Image redBar;

	public Image intercept;

	public Image victoryScreen;

	private Canvas canvas;

	private int blueScore;

	private int redScore;

	private int blueFlags;

	private int redFlags;

	private Color blue;

	private Color red;

	private Action bluePulse = new Action(0.5f);

	private Action redPulse = new Action(0.5f);

	private bool gameEnded;

	public static void AddScore(int blue, int red)
	{
		instance.blueScore += blue * ScoreMultiplier(instance.blueFlags);
		instance.redScore += red * ScoreMultiplier(instance.redFlags);
		if (blue > 0)
		{
			instance.bluePulse.Start();
		}
		if (red > 0)
		{
			instance.redPulse.Start();
		}
		instance.UpdateUi();
		if (!instance.gameEnded)
		{
			if (instance.blueScore >= instance.redScore + GameManager.instance.victoryPoints)
			{
				Win(true);
			}
			else if (instance.redScore >= instance.blueScore + GameManager.instance.victoryPoints)
			{
				Win(false);
			}
		}
	}

	public static void AddFlag(int blue, int red)
	{
		instance.blueFlags += blue;
		instance.redFlags += red;
		instance.UpdateUi();
		if (!instance.gameEnded && GameManager.instance.ElapsedGameTime() > 1f)
		{
			if (!ActorManager.HasSpawnPoint(0))
			{
				Win(false);
			}
			else if (!ActorManager.HasSpawnPoint(1))
			{
				Win(true);
			}
		}
	}

	public static void Win(bool blue)
	{
		if (!instance.gameEnded)
		{
			instance.gameEnded = true;
			instance.victoryScreen.gameObject.SetActive(true);
			Color color = ((!blue) ? instance.red : instance.blue);
			color.a = 0.8f;
			instance.victoryScreen.color = color;
			if (blue)
			{
				instance.victoryText.text = "BLUE TEAM IS";
			}
			else
			{
				instance.victoryText.text = "RED TEAM IS";
			}
			instance.Invoke("HideVictoryScreen", 5f);
		}
	}

	private void HideVictoryScreen()
	{
		victoryScreen.gameObject.SetActive(false);
	}

	public static int ScoreMultiplier(int flags)
	{
		return flags;
	}

	private void Awake()
	{
		instance = this;
		blueScore = 0;
		redScore = 0;
		blueFlags = 0;
		redFlags = 0;
		blue = blueBar.color;
		red = redBar.color;
		canvas = GetComponent<Canvas>();
		victoryScreen.gameObject.SetActive(false);
		UpdateUi();
	}

	private void UpdateUi()
	{
		blueScoreText.text = blueScore.ToString();
		redScoreText.text = redScore.ToString();
		blueFlagsText.text = blueFlags.ToString();
		redFlagsText.text = redFlags.ToString();
		bool flag = blueScore + redScore >= GameManager.instance.victoryPoints;
		intercept.enabled = flag;
		if (!flag)
		{
			float x = (float)blueScore / (float)GameManager.instance.victoryPoints;
			float x2 = 1f - (float)redScore / (float)GameManager.instance.victoryPoints;
			blueBar.rectTransform.anchorMax = new Vector2(x, 1f);
			redBar.rectTransform.anchorMin = new Vector2(x2, 0f);
		}
		else
		{
			float x3 = Mathf.Clamp01((float)(blueScore - redScore + GameManager.instance.victoryPoints) / (float)(2 * GameManager.instance.victoryPoints));
			blueBar.rectTransform.anchorMax = new Vector2(x3, 1f);
			redBar.rectTransform.anchorMin = new Vector2(x3, 0f);
			intercept.rectTransform.anchorMin = new Vector2(x3, 0f);
			intercept.rectTransform.anchorMax = new Vector2(x3, 1f);
		}
	}

	private void Update()
	{
		if (!bluePulse.Done())
		{
			blueBar.color = Color.Lerp(Color.white, blue, bluePulse.Ratio());
		}
		if (!redPulse.Done())
		{
			redBar.color = Color.Lerp(Color.white, red, redPulse.Ratio());
		}
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			HideVictoryScreen();
		}
		if (Input.GetKeyDown(KeyCode.Home))
		{
			canvas.enabled = !canvas.enabled;
		}
	}
}
