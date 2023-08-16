using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
	[Serializable]
	public class Configuration
	{
		public float speed = 300f;

		public float impactForce = 200f;

		public float lifetime = 2f;

		public float damage = 70f;

		public float balanceDamage = 60f;

		public float impactDecalSize = 0.2f;

		public bool piercing;

		public bool makesFlybySound;

		public float flybyPitch = 1f;

		public float dropoffEnd = 300f;

		public AnimationCurve damageDropOff;
	}

	private const float PASS_PLAYER_MAX_SOUND_DISTANCE = 15f;

	private const int LEVEL_LAYER = 0;

	private const int RAGDOLL_LAYER = 10;

	private const int HIT_MASK = -2049;

	private const float PIERCING_RANGE = 2f;

	public Configuration configuration;

	protected Vector3 velocity = Vector3.zero;

	protected float expireTime;

	[NonSerialized]
	public Actor source;

	private bool travellingTowardsPlayer;

	private float travelDistance;

	protected virtual void Start()
	{
		velocity = base.transform.forward * configuration.speed;
		expireTime = Time.time + configuration.lifetime;
		ActorManager.RegisterProjectile(this);
	}

	protected virtual void Update()
	{
		if (Time.time > expireTime)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		Vector3 position = base.transform.position;
		travelDistance += configuration.speed * Time.deltaTime;
		velocity += Physics.gravity * Time.deltaTime;
		Vector3 delta = velocity * Time.deltaTime;
		Travel(delta);
		if (!configuration.makesFlybySound)
		{
			return;
		}
		Vector3 vector = ActorManager.instance.player.Position();
		Vector3 lhs = base.transform.position - vector;
		bool flag = travellingTowardsPlayer;
		travellingTowardsPlayer = Vector3.Dot(lhs, velocity) < 0f;
		if (!travellingTowardsPlayer && flag)
		{
			Vector3 vector2 = SMath.LineVsPointClosest(position, base.transform.position, vector);
			if (Vector3.Distance(vector2, vector) < 15f)
			{
				FpsActorController.instance.BulletFlyby(vector2, UnityEngine.Random.Range(configuration.flybyPitch, 0.9f * configuration.flybyPitch));
			}
		}
	}

	protected virtual void Travel(Vector3 delta)
	{
		Ray ray = new Ray(base.transform.position, delta.normalized);
		bool flag = true;
		RaycastHit hitInfo;
		if (Physics.Raycast(ray, out hitInfo, delta.magnitude * 2f, -2049) && Hit(ray, hitInfo))
		{
			flag = false;
			if (hitInfo.collider.gameObject.layer == 0)
			{
				SpawnDecal(hitInfo);
			}
		}
		if (flag)
		{
			base.transform.position += delta;
		}
	}

	protected virtual bool Hit(Ray ray, RaycastHit hitInfo)
	{
		if (hitInfo.collider.CompareTag("Piercable"))
		{
			Collider collider = hitInfo.collider;
			collider.enabled = false;
			Ray ray2 = new Ray(hitInfo.point, ray.direction);
			RaycastHit hitInfo2;
			if (Physics.Raycast(ray2, out hitInfo2, 2f, -2049))
			{
				hitInfo = hitInfo2;
			}
			collider.enabled = true;
		}
		if (Hitbox.IsHitboxLayer(hitInfo.collider.gameObject.layer))
		{
			Hitbox component = hitInfo.collider.GetComponent<Hitbox>();
			if (component.parent == source)
			{
				base.transform.position = hitInfo.point + velocity.normalized * 0.2f;
			}
			else if (component.ProjectileHit(this, hitInfo.point) && !source.aiControlled)
			{
				IngameUi.Hit();
			}
		}
		Rigidbody attachedRigidbody = hitInfo.collider.attachedRigidbody;
		if (attachedRigidbody != null)
		{
			attachedRigidbody.AddForceAtPosition(velocity.normalized * configuration.impactForce, hitInfo.point, ForceMode.Impulse);
		}
		if (configuration.makesFlybySound && travellingTowardsPlayer && Vector3.Distance(hitInfo.point, ActorManager.instance.player.Position()) < 15f)
		{
			FpsActorController.instance.BulletFlyby(hitInfo.point, configuration.flybyPitch);
		}
		UnityEngine.Object.Destroy(base.gameObject);
		return true;
	}

	protected virtual void SpawnDecal(RaycastHit hitInfo)
	{
		DecalManager.AddDecal(hitInfo.point, hitInfo.normal, configuration.impactDecalSize, DecalManager.DecalType.Impact);
	}

	public virtual float Damage()
	{
		return DamageDropOff() * configuration.damage;
	}

	public virtual float BalanceDamage()
	{
		return DamageDropOff() * configuration.balanceDamage;
	}

	private float DamageDropOff()
	{
		return configuration.damageDropOff.Evaluate(travelDistance / configuration.dropoffEnd);
	}
}
