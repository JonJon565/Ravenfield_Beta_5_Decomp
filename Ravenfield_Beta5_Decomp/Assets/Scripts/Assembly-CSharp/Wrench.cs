using UnityEngine;

public class Wrench : MeleeWeapon
{
	protected const int VEHICLE_HIT_MASK = 4096;

	public float repairHealth = 50f;

	public AudioClip repairSound;

	protected override void ShootMeleeRay()
	{
		Ray ray = new Ray(configuration.muzzle.position, configuration.muzzle.forward);
		RaycastHit hitInfo;
		if (Physics.SphereCast(ray, radius, out hitInfo, range, 4096))
		{
			Vehicle componentInParent = hitInfo.collider.GetComponentInParent<Vehicle>();
			bool flag = componentInParent.Repair(repairHealth);
			HitSomething(hitInfo.collider);
			if (flag)
			{
				audio.PlayOneShot(repairSound);
			}
			if (!user.aiControlled)
			{
				IngameUi.instance.FlashVehicleBar(componentInParent.GetHealthRatio());
			}
		}
		base.ShootMeleeRay();
	}
}
