using UnityEngine;
using UnityEngine.UI;

public class NightVision : ToggleableItem
{
	public Color blendColor;

	public RawImage blackOverlay;

	public AudioClip enableClip;

	public AudioClip disableClip;

	private new bool enabled;

	private Action toggleAction = new Action(0.5f);

	private Canvas canvas;

	protected override void Awake()
	{
		base.Awake();
		canvas = GetComponentInChildren<Canvas>();
	}

	public override void Toggle()
	{
		if (userIsPlayer && toggleAction.TrueDone())
		{
			enabled = !enabled;
			if (enabled)
			{
				TimeOfDay.instance.ApplyNightvision();
				toggleAction.Start();
				audio.PlayOneShot(enableClip);
				FpsActorController.instance.EnableNoise();
			}
			else
			{
				TimeOfDay.instance.ResetAtmosphere();
				toggleAction.Start();
				audio.PlayOneShot(disableClip);
				FpsActorController.instance.DisableNoise();
			}
		}
	}

	private void LateUpdate()
	{
		if (userIsPlayer)
		{
			canvas.transform.parent.position = Vector3.zero;
			canvas.transform.parent.rotation = Quaternion.identity;
			if (toggleAction.TrueDone())
			{
				blackOverlay.enabled = false;
				return;
			}
			blackOverlay.enabled = true;
			Color black = Color.black;
			black.a = Mathf.Clamp01((1f - toggleAction.Ratio()) * 2f);
			blackOverlay.color = black;
		}
	}

	public override void Equip(Actor user)
	{
		base.Equip(user);
		canvas.enabled = userIsPlayer;
	}

	private void OnDisable()
	{
		if (userIsPlayer && enabled)
		{
			TimeOfDay.instance.ResetAtmosphere();
			FpsActorController.instance.DisableNoise();
		}
	}
}
