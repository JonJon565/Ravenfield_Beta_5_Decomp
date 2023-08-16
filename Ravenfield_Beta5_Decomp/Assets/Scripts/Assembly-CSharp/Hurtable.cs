using UnityEngine;

public class Hurtable : MonoBehaviour
{
	public int team;

	public virtual bool Damage(float healthDamage, float balanceDamage, bool piercing, Vector3 point, Vector3 direction, Vector3 impactForce)
	{
		return false;
	}
}
