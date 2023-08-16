using UnityEngine;

public class ThrowableWeapon : Weapon
{
	public override void Unholster()
	{
		base.Unholster();
		if (ammo == 0)
		{
			ReloadDone();
		}
	}

	public override void Fire(Vector3 direction, bool useMuzzleDirection)
	{
		if (CanFire())
		{
			lastFired = Time.time;
			if (animator != null)
			{
				animator.SetTrigger("throw");
			}
			else
			{
				Shoot(direction, useMuzzleDirection);
			}
		}
		holdingFire = true;
	}

	public void SpawnThrowable()
	{
		Shoot(Vector3.zero, true);
		Reload();
	}

	public override bool CanBeAimed()
	{
		return base.CanBeAimed() && HasLoadedAmmo();
	}
}
