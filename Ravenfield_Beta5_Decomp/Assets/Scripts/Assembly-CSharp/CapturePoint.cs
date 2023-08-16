using System;
using System.Collections.Generic;
using UnityEngine;

public class CapturePoint : SpawnPoint
{
	private const float UPDATE_RATE = 1f;

	private const float CAPTURE_RATE_PER_PERSON = 0.05f;

	private const int HQ_QUALITY_LEVEL = 5;

	private const float CONTESTED_SPAWNPOINT_SAFE_DOT = 0.8f;

	public Transform contestedSpawnpointContainer;

	private Vector3[] contestedSpawnpointFlatDirection;

	private bool[] contestedSpawnpointIsSafe;

	public float captureRange = 10f;

	public bool canBeCaptured = true;

	public Transform flagParent;

	public GameObject lqFlag;

	public GameObject hqFlag;

	private float control = 1f;

	private int pendingOwner;

	private Renderer flagRenderer;

	private bool isContested;

	private Action unsafeAction = new Action(10f);

	private bool playerWasInRadius;

	protected override void Awake()
	{
		base.Awake();
		if (contestedSpawnpointContainer != null)
		{
			int childCount = contestedSpawnpointContainer.childCount;
			contestedSpawnpointIsSafe = new bool[childCount];
			contestedSpawnpointFlatDirection = new Vector3[childCount];
			for (int i = 0; i < childCount; i++)
			{
				contestedSpawnpointFlatDirection[i] = (contestedSpawnpointContainer.GetChild(i).transform.position - base.transform.position).ToGround().normalized;
			}
			Renderer[] componentsInChildren = contestedSpawnpointContainer.GetComponentsInChildren<Renderer>();
			Renderer[] array = componentsInChildren;
			foreach (Renderer renderer in array)
			{
				renderer.enabled = false;
			}
			ClearContestedSpawnpointSafeFlags();
		}
		if (QualitySettings.GetQualityLevel() >= 5)
		{
			lqFlag.SetActive(false);
			hqFlag.SetActive(true);
			flagRenderer = hqFlag.GetComponent<Renderer>();
		}
		else
		{
			lqFlag.SetActive(true);
			hqFlag.SetActive(false);
			flagRenderer = lqFlag.GetComponent<Renderer>();
		}
	}

	private void Start()
	{
		if (GameManager.instance.reverseMode)
		{
			if (owner == 0)
			{
				owner = 1;
			}
			else if (owner == 1)
			{
				owner = 0;
			}
		}
		SetOwner(owner);
		if (owner == -1)
		{
			if (GameManager.instance.assaultMode)
			{
				SetOwner(1);
			}
			else
			{
				control = 0f;
			}
		}
		InvokeRepeating("UpdateOwner", 1f, 1f);
	}

	private void Update()
	{
		Vector3 localPosition = flagParent.localPosition;
		localPosition.y = 1.2f + 4.8f * control;
		flagParent.localPosition = Vector3.Lerp(flagParent.localPosition, localPosition, 3f * Time.deltaTime);
	}

	private void UpdateOwner()
	{
		if (!canBeCaptured)
		{
			return;
		}
		int num = owner;
		bool flag = false;
		List<Actor> list = ActorManager.AliveActorsInRange(base.transform.position, captureRange);
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		isContested = false;
		if (contestedSpawnpointContainer != null)
		{
			ClearContestedSpawnpointSafeFlags();
		}
		foreach (Actor item in list)
		{
			if (dictionary.ContainsKey(item.team))
			{
				Dictionary<int, int> dictionary2;
				Dictionary<int, int> dictionary3 = (dictionary2 = dictionary);
				int team;
				int key = (team = item.team);
				team = dictionary2[team];
				dictionary3[key] = team + 1;
			}
			else
			{
				dictionary.Add(item.team, 1);
			}
			if (item.team != owner)
			{
				isContested = true;
				if (contestedSpawnpointContainer != null)
				{
					UpdateContestedSpawnpointSafeFlags(item);
				}
			}
			if (!item.aiControlled)
			{
				flag = true;
			}
		}
		int num2 = -1;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < 2; i++)
		{
			if (dictionary.ContainsKey(i) && dictionary[i] > num4)
			{
				num2 = i;
				num3 = num4;
				num4 = dictionary[i];
			}
		}
		int num5 = num4 - num3;
		if (num2 != -1)
		{
			if (num2 != pendingOwner)
			{
				control -= (float)num5 * 0.05f;
				if (control <= 0f)
				{
					SetOwner(num2);
					control = 0.01f;
				}
			}
			else
			{
				control = Mathf.Clamp01(control + (float)num5 * 0.05f);
				if (control == 1f && owner != pendingOwner)
				{
					SetOwner(pendingOwner);
				}
			}
		}
		if (isContested)
		{
			unsafeAction.Start();
		}
		if (flag && !playerWasInRadius)
		{
			IngameUi.instance.ShowFlagIndicator();
		}
		else if (!flag && playerWasInRadius)
		{
			IngameUi.instance.HideFlagIndicator();
		}
		if (flag)
		{
			IngameUi.instance.SetFlagIndicator(control, owner);
		}
		flagRenderer.enabled = control > 0f;
		playerWasInRadius = flag;
	}

	private void ClearContestedSpawnpointSafeFlags()
	{
		for (int i = 0; i < contestedSpawnpointIsSafe.Length; i++)
		{
			contestedSpawnpointIsSafe[i] = true;
		}
	}

	private void UpdateContestedSpawnpointSafeFlags(Actor attacker)
	{
		Vector3 normalized = (attacker.Position() - base.transform.position).ToGround().normalized;
		for (int i = 0; i < contestedSpawnpointIsSafe.Length; i++)
		{
			if (contestedSpawnpointIsSafe[i])
			{
				float num = Vector3.Dot(normalized, contestedSpawnpointFlatDirection[i]);
				contestedSpawnpointIsSafe[i] = num < 0.8f;
			}
		}
	}

	public override bool IsSafe()
	{
		return unsafeAction.TrueDone();
	}

	private void SetOwner(int team)
	{
		int num = 0;
		int num2 = 0;
		switch (team)
		{
		case 0:
			num2++;
			break;
		case 1:
			num++;
			break;
		}
		if (team != owner)
		{
			if (owner == 0)
			{
				num2--;
			}
			else if (owner == 1)
			{
				num--;
			}
		}
		owner = team;
		pendingOwner = team;
		flagRenderer.material.color = Color.Lerp(ColorScheme.TeamColor(team), Color.black, 0.2f);
		ScoreUi.AddFlag(num2, num);
		try
		{
			MinimapUi.UpdateSpawnPointButtons();
		}
		catch (Exception)
		{
		}
	}

	public override float GotoRadius()
	{
		return captureRange * 0.9f;
	}

	public override Vector3 GetSpawnPosition()
	{
		if (isContested && contestedSpawnpointContainer != null)
		{
			return GetSafeSpawnPosition();
		}
		return base.GetSpawnPosition();
	}

	private Vector3 GetSafeSpawnPosition()
	{
		int childCount = contestedSpawnpointContainer.childCount;
		if (childCount == 0)
		{
			return base.GetSpawnPosition();
		}
		int num = UnityEngine.Random.Range(0, childCount);
		for (int i = 0; i < childCount; i++)
		{
			int num2 = (num + i) % childCount;
			if (contestedSpawnpointIsSafe[num2])
			{
				return contestedSpawnpointContainer.GetChild(num2).position;
			}
		}
		return contestedSpawnpointContainer.GetChild(num).position;
	}
}
