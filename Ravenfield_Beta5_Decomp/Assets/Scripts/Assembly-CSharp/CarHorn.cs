using UnityEngine;

public class CarHorn : MountedWeapon
{
	protected override void Shoot(Vector3 direction, bool useMuzzleDirection)
	{
		if (configuration.loud)
		{
			user.Highlight();
		}
		audio.Play();
		lastFired = Time.time;
	}
}
