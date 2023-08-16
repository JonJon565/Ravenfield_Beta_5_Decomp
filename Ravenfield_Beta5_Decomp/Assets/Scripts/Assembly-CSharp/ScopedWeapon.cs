using UnityEngine;

public class ScopedWeapon : Weapon
{
	public GameObject scope;

	private Action blackoutAction = new Action(0.3f);

	private Texture2D blackoutTexture;

	private bool showingScope;

	protected override void Awake()
	{
		base.Awake();
		blackoutTexture = new Texture2D(8, 8);
	}

	public override void FindRenderers(bool thirdPerson)
	{
		base.FindRenderers(thirdPerson);
		Renderer[] componentsInChildren = scope.GetComponentsInChildren<Renderer>();
		foreach (Renderer item in componentsInChildren)
		{
			renderers.Remove(item);
		}
		SetAiming(false);
	}

	public override void Unholster()
	{
		base.Unholster();
		SetAiming(false);
	}

	public override void SetAiming(bool aiming)
	{
		base.SetAiming(aiming);
		if (!HasActiveAnimator())
		{
			return;
		}
		if (aiming)
		{
			showingScope = false;
			blackoutAction.Start();
			return;
		}
		showingScope = false;
		blackoutAction.Stop();
		scope.SetActive(false);
		foreach (Renderer renderer in renderers)
		{
			renderer.enabled = true;
		}
	}

	protected override void Update()
	{
		base.Update();
		if (blackoutAction.TrueDone() || !(blackoutAction.Ratio() > 0.5f) || showingScope)
		{
			return;
		}
		showingScope = true;
		scope.SetActive(true);
		foreach (Renderer renderer in renderers)
		{
			renderer.enabled = false;
		}
	}

	private void OnGUI()
	{
		if (HasActiveAnimator() && !blackoutAction.TrueDone() && showingScope)
		{
			Color black = Color.black;
			black.a = Mathf.Clamp01(4f - 4f * blackoutAction.Ratio());
			GUI.color = black;
			GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), blackoutTexture);
		}
	}
}
