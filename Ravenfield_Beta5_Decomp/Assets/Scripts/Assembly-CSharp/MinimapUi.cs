using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapUi : MonoBehaviour
{
	private const float MINIMAP_SCALE = 1.3f;

	public static MinimapUi instance;

	public RectTransform loadoutParent;

	public RectTransform ingameParent;

	public RawImage minimap;

	public GameObject minimapSpawnPointPrefab;

	public GameObject actorBlipPrefab;

	public Sprite spawnPointSprite;

	public Sprite spawnPointSelectedSprite;

	private Dictionary<SpawnPoint, Button> minimapSpawnPointButton;

	private SpawnPoint selectedSpawnPoint;

	private float minimapSize;

	private float minimapOpenness;

	private Vector2 minimapTargetAnchor;

	private void Awake()
	{
		instance = this;
		RectTransform rectTransform = minimap.rectTransform;
		float num = minimap.rectTransform.anchorMax.x - minimap.rectTransform.anchorMin.x;
		minimapSize = num * (float)Screen.width * 1.3f;
		minimapTargetAnchor = new Vector2(minimap.rectTransform.anchorMin.x, minimap.rectTransform.anchorMax.y);
	}

	private void Update()
	{
		float target = ((!Input.GetKey(KeyCode.M)) ? 0f : 1f);
		minimapOpenness = Mathf.MoveTowards(minimapOpenness, target, Time.deltaTime * 20f);
		ingameParent.anchorMin = new Vector2(0f, Mathf.Lerp(-1f, 0f, minimapOpenness));
		ingameParent.anchorMax = new Vector2(1f, Mathf.Lerp(0f, 1f, minimapOpenness));
	}

	private void Start()
	{
		SetupMinimap();
		UpdateSpawnPointButtons();
	}

	private void SetupMinimap()
	{
		MinimapCamera minimapCamera = Object.FindObjectOfType<MinimapCamera>();
		if (minimapCamera == null)
		{
			Debug.LogWarning("No minimap camera found!");
			return;
		}
		minimap.texture = minimapCamera.Minimap();
		minimapSpawnPointButton = new Dictionary<SpawnPoint, Button>();
		Camera component = minimapCamera.GetComponent<Camera>();
		SpawnPoint[] spawnPoints = ActorManager.instance.spawnPoints;
		foreach (SpawnPoint spawnPoint in spawnPoints)
		{
			Button component2 = Object.Instantiate(minimapSpawnPointPrefab).GetComponent<Button>();
			RectTransform rectTransform = (RectTransform)component2.transform;
			Vector3 vector = component.WorldToViewportPoint(spawnPoint.transform.position);
			SpawnPoint anonSpawnPoint = spawnPoint;
			component2.onClick.AddListener(delegate
			{
				SelectSpawnPoint(anonSpawnPoint);
			});
			rectTransform.SetParent(minimap.rectTransform);
			Vector2 anchorMax = (rectTransform.anchorMin = new Vector2(vector.x, vector.y));
			rectTransform.anchorMax = anchorMax;
			rectTransform.anchoredPosition = Vector2.zero;
			minimapSpawnPointButton.Add(spawnPoint, component2);
		}
	}

	private void SelectSpawnPoint(SpawnPoint spawnPoint)
	{
		if (selectedSpawnPoint != null)
		{
			RemoveSpawnButtonHighlight(minimapSpawnPointButton[selectedSpawnPoint]);
		}
		selectedSpawnPoint = spawnPoint;
		AddSpawnButtonHighlight(minimapSpawnPointButton[selectedSpawnPoint]);
	}

	public static SpawnPoint SelectedSpawnPoint()
	{
		if (LoadoutUi.IsOpen())
		{
			return null;
		}
		if (instance.selectedSpawnPoint == null)
		{
			return ActorManager.RandomFrontlineSpawnPointForTeam(FpsActorController.instance.actor.team);
		}
		if (instance.selectedSpawnPoint.owner != FpsActorController.instance.actor.team)
		{
			LoadoutUi.Show();
		}
		return instance.selectedSpawnPoint;
	}

	public static void UpdateSpawnPointButtons()
	{
		int num = 0;
		foreach (SpawnPoint key in instance.minimapSpawnPointButton.Keys)
		{
			int owner = key.owner;
			Button button = instance.minimapSpawnPointButton[key];
			ColorBlock colors = button.colors;
			Color color2 = (colors.normalColor = ColorScheme.TeamColor(owner));
			colors.highlightedColor = color2 + new Color(0.2f, 0.2f, 0.2f);
			colors.disabledColor = color2 * new Color(0.5f, 0.5f, 0.5f);
			colors.pressedColor = Color.white;
			button.colors = colors;
			button.interactable = owner == num;
		}
	}

	private void RemoveSpawnButtonHighlight(Button b)
	{
		b.image.sprite = spawnPointSprite;
	}

	private void AddSpawnButtonHighlight(Button b)
	{
		b.image.sprite = spawnPointSelectedSprite;
	}

	public static void PinToLoadoutScreen()
	{
		instance.minimap.rectTransform.SetParent(instance.loadoutParent, false);
	}

	public static void PinToIngameScreen()
	{
		instance.minimap.rectTransform.SetParent(instance.ingameParent, false);
	}

	public static void AddActorBlip(Actor actor)
	{
		ActorBlip component = ((GameObject)Object.Instantiate(instance.actorBlipPrefab, instance.minimap.rectTransform)).GetComponent<ActorBlip>();
		component.SetActor(actor, !actor.aiControlled);
	}
}
