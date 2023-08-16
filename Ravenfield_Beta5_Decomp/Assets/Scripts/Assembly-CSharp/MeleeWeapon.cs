using System.Collections;
using UnityEngine;

public class MeleeWeapon : Weapon
{
	private const int HIT_MASK = 66816;

	public float radius = 0.4f;

	public float range = 2f;

	public float swingTime = 0.15f;

	public float damage = 55f;

	public float balanceDamage = 150f;

	public float force = 100f;

	public AudioClip hitSound;

	protected override void Shoot(Vector3 direction, bool useMuzzleDirection)
	{
		base.Shoot(direction, useMuzzleDirection);
		StartCoroutine(Swing());
	}

	protected override Projectile SpawnProjectile(Vector3 direction)
	{
		return null;
	}

	private IEnumerator Swing()
	{
		yield return new WaitForSeconds(swingTime);
		ShootMeleeRay();
	}

	protected virtual void ShootMeleeRay()
	{
		Ray ray = new Ray(configuration.muzzle.position, configuration.muzzle.forward);
		RaycastHit hitInfo;
		if (Physics.SphereCast(ray, radius, out hitInfo, range, 66816))
		{
			Hitbox component = hitInfo.collider.GetComponent<Hitbox>();
			HitSomething(hitInfo.collider);
			audio.PlayOneShot(hitSound);
			if (component.parent.Damage(damage, balanceDamage, false, hitInfo.point, ray.direction, ray.direction * force))
			{
				IngameUi.Hit();
			}
		}
	}

	protected virtual void HitSomething(Collider c)
	{
	}
}
