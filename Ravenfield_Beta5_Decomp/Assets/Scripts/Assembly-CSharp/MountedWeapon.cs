using UnityEngine;

public class MountedWeapon : Weapon
{
	private int spareAmmo;

	protected override void Awake()
	{
		base.Awake();
		spareAmmo = configuration.spareAmmo;
	}

	public override void Fire(Vector3 direction, bool useMuzzleDirection)
	{
		base.Fire(direction, true);
	}

	public override void Show()
	{
	}

	public override void Hide()
	{
	}

	protected override int RemoveSpareAmmo(int count)
	{
		if (HasInfiniteSpareAmmo())
		{
			return count;
		}
		int num = Mathf.Max(0, spareAmmo - count);
		int result = spareAmmo - num;
		spareAmmo = num;
		return result;
	}

	public override int GetSpareAmmo()
	{
		return spareAmmo;
	}

	public override void Holster()
	{
		unholstered = false;
		reloading = false;
		CancelInvoke();
	}

	public override void Unholster()
	{
		base.Unholster();
		if (!HasLoadedAmmo() && configuration.forceAutoReload)
		{
			Reload(true);
		}
	}
}
