public class ShellLoadedWeapon : Weapon
{
	public float aiReloadTimePerShell = 0.4f;

	private void NewShell()
	{
		ammo++;
		user.RemoveSpareAmmo(1, slot);
		if (ammo >= configuration.ammo || !HasSpareAmmo())
		{
			if (animator != null)
			{
				animator.SetTrigger("reloadDone");
			}
			Invoke("ReloadDone", configuration.reloadTime);
		}
	}

	public override void Reload(bool overrideHolstered)
	{
		if ((unholstered || overrideHolstered) && !reloading)
		{
			if (animator != null)
			{
				animator.ResetTrigger("reloadDone");
				base.Reload();
				CancelInvoke("ReloadDone");
			}
			else
			{
				configuration.reloadTime = aiReloadTimePerShell * (float)(configuration.ammo - ammo);
				base.Reload();
			}
		}
	}
}
