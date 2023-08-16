using System;
using UnityEngine;
using UnityEngine.UI;

public class LoadoutUi : MonoBehaviour
{
	private const float LOADOUT_ITEM_PADDING = 10f;

	public static LoadoutUi instance;

	public RectTransform minimapContainer;

	public RectTransform loadoutContainer;

	public RectTransform primaryContainer;

	public RectTransform secondaryContainer;

	public RectTransform gearContainer;

	public RectTransform inspectorContainer;

	public GameObject arsenalSmallButtonPrefab;

	public GameObject arsenalLargeButtonPrefab;

	public Image inspectorImage;

	public RectTransform primaryButton;

	public RectTransform secondaryButton;

	public RectTransform gear1Button;

	public RectTransform gear2Button;

	public RectTransform gear3Button;

	public RectTransform largeGear2Button;

	public RectTransform scrollIndicator;

	public Sprite nothingSprite;

	private bool hasBeenOpen;

	private string targetSlot = string.Empty;

	private WeaponManager.WeaponEntry selectedWeaponEntry;

	[NonSerialized]
	public WeaponManager.LoadoutSet loadout;

	private Canvas uiCanvas;

	private void Awake()
	{
		instance = this;
		uiCanvas = GetComponent<Canvas>();
	}

	private void LoadDefaultLoadout()
	{
		loadout = new WeaponManager.LoadoutSet();
		loadout.primary = DefaultSlotEntry("primary");
		loadout.secondary = DefaultSlotEntry("secondary");
		loadout.gear1 = DefaultSlotEntry("gear1");
		loadout.gear2 = DefaultSlotEntry("gear2");
		loadout.gear3 = DefaultSlotEntry("gear3");
	}

	private WeaponManager.WeaponEntry DefaultSlotEntry(string slot)
	{
		if (PlayerPrefs.HasKey(slot))
		{
			int @int = PlayerPrefs.GetInt(slot);
			if (@int == -1)
			{
				return null;
			}
			return WeaponManager.instance.weapons[@int];
		}
		if (slot == "primary")
		{
			return WeaponManager.instance.weapons[0];
		}
		return null;
	}

	private void Start()
	{
		Show();
		SetupGameUi();
		Hide();
		hasBeenOpen = false;
	}

	private void SetupGameUi()
	{
		SetupLoadout();
		LoadDefaultLoadout();
		UpdateLoadout();
	}

	private void Update()
	{
		if (scrollIndicator.gameObject.activeInHierarchy)
		{
			scrollIndicator.anchoredPosition = new Vector2(10f, (1f + Mathf.Sin(Time.time * 3f)) * 10f);
		}
	}

	private void SetupLoadout()
	{
		float num = gearContainer.rect.width / gearContainer.GetChild(0).GetComponent<AspectRatioFitter>().aspectRatio;
		float num2 = num + 10f;
		foreach (WeaponManager.WeaponEntry item in WeaponManager.GetWeaponEntriesOfSlot(WeaponManager.WeaponSlot.Primary))
		{
			if (!item.hidden)
			{
				AddArsenalButton(item, num2, primaryContainer);
				num2 += num + 10f;
			}
		}
		Vector2 sizeDelta = primaryContainer.sizeDelta;
		sizeDelta.y = num2 + 10f;
		primaryContainer.sizeDelta = sizeDelta;
		num2 = num + 10f;
		foreach (WeaponManager.WeaponEntry item2 in WeaponManager.GetWeaponEntriesOfSlot(WeaponManager.WeaponSlot.Secondary))
		{
			if (!item2.hidden)
			{
				AddArsenalButton(item2, num2, secondaryContainer);
				num2 += num + 10f;
			}
		}
		sizeDelta = secondaryContainer.sizeDelta;
		sizeDelta.y = num2 + 10f;
		secondaryContainer.sizeDelta = sizeDelta;
		num2 = num + 10f;
		foreach (WeaponManager.WeaponEntry item3 in WeaponManager.GetWeaponEntriesOfSlot(WeaponManager.WeaponSlot.Gear))
		{
			if (!item3.hidden)
			{
				AddArsenalButton(item3, num2, gearContainer);
				num2 += num + 10f;
			}
		}
		foreach (WeaponManager.WeaponEntry item4 in WeaponManager.GetWeaponEntriesOfSlot(WeaponManager.WeaponSlot.LargeGear))
		{
			if (!item4.hidden)
			{
				AddArsenalButton(item4, num2, gearContainer);
				num2 += num + 10f;
			}
		}
		sizeDelta = gearContainer.sizeDelta;
		sizeDelta.y = num2 + 10f;
		gearContainer.sizeDelta = sizeDelta;
		scrollIndicator.gameObject.SetActive(false);
	}

	private void AddArsenalButton(WeaponManager.WeaponEntry entry, float padding, RectTransform parent)
	{
		GameObject original = ((!IsLargeEntry(entry)) ? arsenalSmallButtonPrefab : arsenalLargeButtonPrefab);
		RectTransform component = UnityEngine.Object.Instantiate(original).GetComponent<RectTransform>();
		component.GetComponentInChildren<Text>().text = entry.name;
		component.Find("Image").GetComponent<Image>().sprite = entry.image;
		Button component2 = component.GetComponent<Button>();
		component2.onClick.AddListener(delegate
		{
			SelectWeaponEntry(entry);
		});
		component.SetParent(parent, false);
		component.localPosition = new Vector3(0f, 0f - padding, 0f);
	}

	private void SelectWeaponEntry(WeaponManager.WeaponEntry entry)
	{
		if (entry == null)
		{
			inspectorImage.sprite = nothingSprite;
		}
		else
		{
			inspectorImage.sprite = entry.image;
		}
		selectedWeaponEntry = entry;
	}

	public void SelectWeaponEntryNothing()
	{
		SelectWeaponEntry(null);
	}

	public void FinalizeSelection()
	{
		scrollIndicator.gameObject.SetActive(false);
		string slot = targetSlot;
		switch (targetSlot)
		{
		case "primary":
			loadout.primary = selectedWeaponEntry;
			break;
		case "secondary":
			loadout.secondary = selectedWeaponEntry;
			break;
		case "gear1":
			if (IsLargeEntry(selectedWeaponEntry))
			{
				if (loadout.gear2 != null && !IsLargeEntry(loadout.gear2))
				{
					loadout.gear1 = loadout.gear2;
				}
				else if (loadout.gear3 != null && !IsLargeEntry(loadout.gear3))
				{
					loadout.gear1 = loadout.gear3;
				}
				else
				{
					loadout.gear1 = null;
				}
				slot = "gear2";
				loadout.gear2 = selectedWeaponEntry;
				loadout.gear3 = null;
			}
			else
			{
				loadout.gear1 = selectedWeaponEntry;
			}
			break;
		case "gear2":
			if (IsLargeEntry(selectedWeaponEntry))
			{
				loadout.gear3 = null;
				loadout.gear2 = selectedWeaponEntry;
			}
			else
			{
				loadout.gear2 = selectedWeaponEntry;
			}
			break;
		case "gear3":
			if (IsLargeEntry(selectedWeaponEntry))
			{
				loadout.gear3 = null;
				loadout.gear2 = selectedWeaponEntry;
				slot = "gear2";
			}
			else
			{
				loadout.gear3 = selectedWeaponEntry;
			}
			break;
		}
		SaveDefaultSlotEntry(slot, selectedWeaponEntry);
		primaryContainer.gameObject.SetActive(false);
		secondaryContainer.gameObject.SetActive(false);
		gearContainer.gameObject.SetActive(false);
		inspectorContainer.gameObject.SetActive(false);
		loadoutContainer.gameObject.SetActive(true);
		minimapContainer.gameObject.SetActive(true);
		UpdateLoadout();
		targetSlot = string.Empty;
	}

	private void SaveDefaultSlotEntry(string slot, WeaponManager.WeaponEntry entry)
	{
		int value = ((entry != null) ? WeaponManager.instance.weapons.IndexOf(entry) : (-1));
		PlayerPrefs.SetInt(slot, value);
		PlayerPrefs.Save();
		if (slot == "gear2" && IsLargeEntry(entry))
		{
			SaveDefaultSlotEntry("gear3", null);
		}
	}

	private bool IsLargeEntry(WeaponManager.WeaponEntry entry)
	{
		return entry != null && (entry.slot == WeaponManager.WeaponSlot.LargeGear || entry.slot == WeaponManager.WeaponSlot.Primary);
	}

	private void UpdateLoadout()
	{
		UpdateLoadoutButton(primaryButton, loadout.primary);
		UpdateLoadoutButton(secondaryButton, loadout.secondary);
		UpdateLoadoutButton(gear1Button, loadout.gear1);
		if (IsLargeEntry(loadout.gear2))
		{
			largeGear2Button.gameObject.SetActive(true);
			gear2Button.gameObject.SetActive(false);
			gear3Button.gameObject.SetActive(false);
			UpdateLoadoutButton(largeGear2Button, loadout.gear2);
		}
		else
		{
			largeGear2Button.gameObject.SetActive(false);
			gear2Button.gameObject.SetActive(true);
			gear3Button.gameObject.SetActive(true);
			UpdateLoadoutButton(gear2Button, loadout.gear2);
			UpdateLoadoutButton(gear3Button, loadout.gear3);
		}
	}

	private void UpdateLoadoutButton(RectTransform button, WeaponManager.WeaponEntry entry)
	{
		if (entry == null)
		{
			button.Find("Image").GetComponent<Image>().sprite = nothingSprite;
			button.GetComponentInChildren<Text>().text = "Nothing";
		}
		else
		{
			button.Find("Image").GetComponent<Image>().sprite = entry.image;
			button.GetComponentInChildren<Text>().text = entry.name;
		}
	}

	public void OpenArsenal(string slot)
	{
		targetSlot = slot;
		loadoutContainer.gameObject.SetActive(false);
		minimapContainer.gameObject.SetActive(false);
		inspectorContainer.gameObject.SetActive(true);
		switch (slot)
		{
		case "primary":
			ActivateContainer(primaryContainer);
			SelectWeaponEntry(loadout.primary);
			break;
		case "secondary":
			ActivateContainer(secondaryContainer);
			SelectWeaponEntry(loadout.secondary);
			break;
		case "gear1":
			ActivateContainer(gearContainer);
			SelectWeaponEntry(loadout.gear1);
			break;
		case "gear2":
			ActivateContainer(gearContainer);
			SelectWeaponEntry(loadout.gear2);
			break;
		case "gear3":
			ActivateContainer(gearContainer);
			SelectWeaponEntry(loadout.gear3);
			break;
		}
	}

	private void ActivateContainer(RectTransform container)
	{
		container.gameObject.SetActive(true);
		if (container.sizeDelta.y > (float)Screen.height)
		{
			scrollIndicator.gameObject.SetActive(true);
		}
	}

	public void OnScrolled(Vector2 value)
	{
		if (value.y < 0.95f)
		{
			scrollIndicator.gameObject.SetActive(false);
		}
	}

	private void ShowCanvas()
	{
		hasBeenOpen = true;
		if (!uiCanvas.enabled)
		{
			MinimapUi.PinToLoadoutScreen();
			uiCanvas.enabled = true;
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
			loadoutContainer.gameObject.SetActive(true);
			minimapContainer.gameObject.SetActive(true);
			primaryContainer.gameObject.SetActive(false);
			secondaryContainer.gameObject.SetActive(false);
			gearContainer.gameObject.SetActive(false);
			inspectorContainer.gameObject.SetActive(false);
		}
	}

	private void HideCanvas()
	{
		if (targetSlot != string.Empty)
		{
			FinalizeSelection();
		}
		MinimapUi.PinToIngameScreen();
		uiCanvas.enabled = false;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	public void OnDeployClick()
	{
		FpsActorController.instance.CloseLoadout();
	}

	public static bool IsOpen()
	{
		return instance.uiCanvas.enabled;
	}

	public static void Show()
	{
		instance.ShowCanvas();
	}

	public static void Hide()
	{
		instance.HideCanvas();
	}

	public static bool HasBeenOpen()
	{
		return instance.hasBeenOpen;
	}
}
