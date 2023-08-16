using UnityEngine;

public abstract class ActorController : MonoBehaviour
{
	public Actor actor;

	public abstract Vector3 FacingDirection();

	public abstract bool UseMuzzleDirection();

	public abstract Vector3 Velocity();

	public abstract bool OnGround();

	public abstract bool Fire();

	public abstract bool ProjectToGround();

	public abstract Vector3 SwimInput();

	public abstract Vector2 BoatInput();

	public abstract Vector2 CarInput();

	public abstract Vector4 HelicopterInput();

	public abstract float Lean();

	public abstract bool Crouch();

	public abstract bool Aiming();

	public abstract bool Reload();

	public abstract bool IsSprinting();

	public abstract SpawnPoint SelectedSpawnPoint();

	public abstract Transform WeaponParent();

	public abstract WeaponManager.LoadoutSet GetLoadout();

	public abstract bool IsGroupedUp();

	public abstract void SwitchedToWeapon(Weapon weapon);

	public abstract void ReceivedDamage(float damage, float balanceDamage, Vector3 point, Vector3 direction, Vector3 force);

	public abstract void DisableInput();

	public abstract void EnableInput();

	public abstract void StartSeated(Seat seat);

	public abstract void EndSeated(Vector3 exitPosition, Quaternion flatFacing);

	public abstract void StartRagdoll();

	public abstract void GettingUp();

	public abstract void EndRagdoll();

	public abstract void Die();

	public abstract void SpawnAt(Vector3 position);

	public abstract void ApplyRecoil(Vector3 impulse);

	public abstract void StartCrouch();

	public abstract bool EndCrouch();
}
