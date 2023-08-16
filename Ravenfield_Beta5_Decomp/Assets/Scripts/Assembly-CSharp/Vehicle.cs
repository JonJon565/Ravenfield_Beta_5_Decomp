using System;
using UnityEngine;

public class Vehicle : MonoBehaviour
{
	private const float HEAVY_DAMAGE_THRESHOLD = 900f;

	private const int RAM_MASK = 256;

	private const float EXPLODE_TIME = 0.3f;

	private const float CLEANUP_TIME = 15f;

	public const int LAYER = 12;

	private const float AUTO_DAMAGE_START_TIME = 50f;

	private const float AUTO_DAMAGE_PERIOD = 2f;

	private const float AUTO_DAMAGE_PERCENT = 0.07f;

	private const float RAM_MIN_SPEED = 3f;

	private static RaycastHit[] ramResults = new RaycastHit[16];

	public Actor.TargetType targetType = Actor.TargetType.Unarmored;

	public Seat[] seats;

	[NonSerialized]
	public int ownerTeam = -1;

	[NonSerialized]
	public int seatsClaimedByBots;

	[NonSerialized]
	public bool claimedByPlayer;

	[NonSerialized]
	public bool stuck;

	private Action takingFireAction = new Action(20f);

	public float maxHealth = 1000f;

	public float crashDamageSpeedThrehshold = 2f;

	public float crashDamageMultiplier;

	public float spotChanceMultiplier = 3f;

	public float burnTime;

	public bool crashSkipsBurn;

	public bool directJavelinPath;

	public bool exitWhenTakingFire;

	private float health;

	[NonSerialized]
	public bool dead;

	[NonSerialized]
	public Rigidbody rigidbody;

	private VehicleSpawner spawner;

	public ParticleSystem damageParticles;

	public ParticleSystem burnParticles;

	public ParticleSystem deathParticles;

	public AudioSource fireAlarm;

	public Transform blockSensor;

	protected AudioSource audio;

	public AudioSource explosionSound;

	public AudioSource impactAudio;

	public AudioSource heavyDamageAudio;

	public Texture blip;

	public Vector2 avoidanceSize = Vector2.one;

	public float pathingRadius;

	public Vector3 ramSize = Vector3.one;

	public Vector3 ramOffset = Vector3.zero;

	private float avoidanceCoarseRadius;

	private bool reportedFirstDriver;

	[NonSerialized]
	public Collider[] colliders;

	private Vector3 blockSensorOrigin;

	private Action cannotRamAction = new Action(0.5f);

	private Action crashDamageCooldown = new Action(0.2f);

	private Action drainClaimAction = new Action(10f);

	[NonSerialized]
	public bool burning;

	private int stopBurningRepairs;

	public bool HasDriver()
	{
		return seats[0].IsOccupied();
	}

	public Actor Driver()
	{
		return seats[0].occupant;
	}

	public void MarkTakingFire()
	{
		takingFireAction.Start();
	}

	protected virtual void Awake()
	{
		rigidbody = GetComponent<Rigidbody>();
		audio = GetComponent<AudioSource>();
		ActorManager.RegisterVehicle(this);
		health = maxHealth;
		colliders = GetComponentsInChildren<Collider>();
		if (HasBlockSensor())
		{
			blockSensorOrigin = blockSensor.transform.localPosition;
		}
		cannotRamAction.Start();
		avoidanceCoarseRadius = avoidanceSize.magnitude;
	}

	private void CheckRam()
	{
		Vector3 vector = rigidbody.velocity * Time.fixedDeltaTime;
		int num = Physics.BoxCastNonAlloc(base.transform.localToWorldMatrix.MultiplyPoint(ramOffset), ramSize, vector.normalized, ramResults, base.transform.rotation, vector.magnitude, 256);
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit = ramResults[i];
			Hitbox component = raycastHit.collider.GetComponent<Hitbox>();
			if (component.RigidbodyHit(rigidbody, raycastHit.point) && HasDriver() && !Driver().aiControlled)
			{
				IngameUi.Hit();
			}
		}
	}

	protected virtual void FixedUpdate()
	{
		if (rigidbody.velocity.magnitude < 3f)
		{
			cannotRamAction.Start();
		}
		if (cannotRamAction.TrueDone())
		{
			CheckRam();
		}
		if (burning && !dead)
		{
			burnTime -= Time.deltaTime;
			if (burnTime < 0f)
			{
				Die();
			}
		}
		if (drainClaimAction.TrueDone() && seatsClaimedByBots > 0)
		{
			DropSeatClaim();
			drainClaimAction.Start();
		}
	}

	public void OccupantEntered(Seat seat)
	{
		if (seat == seats[0])
		{
			DriverEntered();
		}
		if (!seat.occupant.aiControlled)
		{
			claimedByPlayer = true;
			ownerTeam = seat.occupant.team;
			if (burning && fireAlarm != null)
			{
				fireAlarm.Play();
			}
		}
		CancelInvoke("AutoDamage");
	}

	public void ClaimSeat()
	{
		seatsClaimedByBots = Mathf.Min(seatsClaimedByBots + 1, seats.Length);
		drainClaimAction.Start();
	}

	public void DropSeatClaim()
	{
		seatsClaimedByBots = Mathf.Max(seatsClaimedByBots - 1, 0);
	}

	public bool HasUnclaimedSeats()
	{
		return seatsClaimedByBots < seats.Length;
	}

	public void OccupantLeft(Seat seat, Actor leaver)
	{
		if (seat == seats[0])
		{
			DriverExited();
		}
		if (!leaver.aiControlled)
		{
			claimedByPlayer = false;
			if (fireAlarm != null)
			{
				fireAlarm.Stop();
			}
		}
		if (IsEmpty())
		{
			InvokeRepeating("AutoDamage", 50f, 2f);
			ownerTeam = -1;
		}
	}

	private void AutoDamage()
	{
		Damage(maxHealth * 0.07f);
	}

	protected virtual void DriverEntered()
	{
		if (!reportedFirstDriver)
		{
			spawner.FirstDriverEntered(this);
			reportedFirstDriver = true;
		}
	}

	protected virtual void DriverExited()
	{
	}

	public void Damage(float amount)
	{
		health = Mathf.Clamp(health - amount, 0f, maxHealth);
		if (amount > 900f)
		{
			HeavyDamage();
		}
		if (health <= 0f && !dead && !burning)
		{
			StartBurning();
		}
		if (health < 0.5f * maxHealth)
		{
			damageParticles.Play();
		}
	}

	protected virtual void StartBurning()
	{
		burning = true;
		stopBurningRepairs = 3;
		if (burnParticles != null)
		{
			burnParticles.Play();
		}
		if (fireAlarm != null && claimedByPlayer)
		{
			fireAlarm.Play();
		}
	}

	private void StopBurning()
	{
		burning = false;
		if (burnParticles != null)
		{
			burnParticles.Stop();
		}
		if (fireAlarm != null)
		{
			fireAlarm.Stop();
		}
	}

	public bool Repair(float amount)
	{
		if (dead)
		{
			return false;
		}
		if (burning)
		{
			stopBurningRepairs--;
			if (stopBurningRepairs == 0)
			{
				StopBurning();
			}
		}
		bool result = health < maxHealth;
		health = Mathf.Min(health + amount, maxHealth);
		if (health >= 0.5f * maxHealth)
		{
			damageParticles.Stop();
		}
		CancelInvoke("AutoDamage");
		InvokeRepeating("AutoDamage", 50f, 2f);
		return result;
	}

	public virtual void Die()
	{
		dead = true;
		if (fireAlarm != null)
		{
			fireAlarm.Stop();
		}
		if (spawner != null)
		{
			spawner.VehicleDied(this);
		}
		ActorManager.DropVehicle(this);
		Seat[] array = seats;
		foreach (Seat seat in array)
		{
			if (seat.IsOccupied())
			{
				Actor occupant = seat.occupant;
				occupant.LeaveSeat();
				if (seat.enclosed)
				{
					occupant.Damage(200f, 200f, true, base.transform.position, Vector3.forward, Vector3.up * 10f);
				}
				else
				{
					occupant.Damage(0f, 200f, true, base.transform.position, Vector3.forward, Vector3.up * 10f);
				}
			}
			seat.gameObject.SetActive(false);
		}
		rigidbody.WakeUp();
		base.enabled = false;
		Invoke("Cleanup", 15f);
		Invoke("Explode", 0.3f);
	}

	private void OnCollisionEnter(Collision c)
	{
		float num = Mathf.Abs(Vector3.Dot(c.relativeVelocity, c.contacts[0].normal));
		if (crashDamageCooldown.TrueDone() && num > crashDamageSpeedThrehshold && c.collider.gameObject.layer != 8 && c.collider.gameObject.layer != 10)
		{
			float amount = (num - crashDamageSpeedThrehshold) * crashDamageMultiplier;
			Damage(amount);
			crashDamageCooldown.Start();
			impactAudio.transform.position = c.contacts[0].point;
			impactAudio.pitch *= UnityEngine.Random.Range(0.9f, 1.1f);
			impactAudio.Play();
			if (burning && crashSkipsBurn && !dead)
			{
				Die();
			}
		}
	}

	protected virtual void Explode()
	{
		rigidbody.WakeUp();
		rigidbody.AddForce((UnityEngine.Random.insideUnitSphere + Vector3.up) * 2000f, ForceMode.Impulse);
		rigidbody.AddTorque(UnityEngine.Random.insideUnitSphere * 500f, ForceMode.Impulse);
		deathParticles.Play();
		audio.Stop();
		audio.pitch = 1f;
		audio.volume = 1f;
		explosionSound.Play();
	}

	private void Cleanup()
	{
		UnityEngine.Object.Destroy(base.gameObject);
	}

	public Vector3 Velocity()
	{
		return rigidbody.velocity;
	}

	public Vector3 LocalVelocity()
	{
		return base.transform.worldToLocalMatrix.MultiplyVector(Velocity());
	}

	public void SetSpawner(VehicleSpawner spawner)
	{
		this.spawner = spawner;
	}

	protected static Vector2 Clamp2(Vector2 v)
	{
		return new Vector2(Mathf.Clamp(v.x, -1f, 1f), Mathf.Clamp(v.y, -1f, 1f));
	}

	protected static Vector4 Clamp4(Vector4 v)
	{
		return new Vector4(Mathf.Clamp(v.x, -1f, 1f), Mathf.Clamp(v.y, -1f, 1f), Mathf.Clamp(v.z, -1f, 1f), Mathf.Clamp(v.w, -1f, 1f));
	}

	public Seat GetEmptySeat()
	{
		Seat[] array = seats;
		foreach (Seat seat in array)
		{
			if (!seat.IsOccupied())
			{
				return seat;
			}
		}
		return null;
	}

	public int EmptySeats()
	{
		int num = 0;
		Seat[] array = seats;
		foreach (Seat seat in array)
		{
			if (!seat.IsOccupied())
			{
				num++;
			}
		}
		return num;
	}

	public bool IsFull()
	{
		Seat[] array = seats;
		foreach (Seat seat in array)
		{
			if (!seat.IsOccupied())
			{
				return false;
			}
		}
		return true;
	}

	public bool IsEmpty()
	{
		Seat[] array = seats;
		foreach (Seat seat in array)
		{
			if (seat.IsOccupied())
			{
				return false;
			}
		}
		return true;
	}

	public bool HasBlockSensor()
	{
		return blockSensor != null;
	}

	public int BlockTest(Collider[] outColliders, float extrapolationTime, int mask)
	{
		float num = Mathf.Max(0.1f, LocalVelocity().z * extrapolationTime);
		Vector3 v = blockSensorOrigin;
		v.z += num / 2f;
		Vector3 localScale = blockSensor.localScale;
		localScale.z = num;
		Vector3 vector = base.transform.localToWorldMatrix.MultiplyPoint(v);
		blockSensor.transform.position = vector;
		blockSensor.transform.localScale = localScale;
		return Physics.OverlapBoxNonAlloc(vector, localScale / 2f, outColliders, blockSensor.rotation, mask);
	}

	public bool CoarseLineOverlap(Vector3 origin, Vector3 target, float lineRadius = 0f)
	{
		Vector3 point = SMath.LineSegmentVsPointClosest(origin, target, base.transform.position);
		return IsCoarseOverlapping(point, lineRadius);
	}

	public bool IsCoarseOverlapping(Vector3 point, float lineRadius = 0f)
	{
		return Vector3.Distance(base.transform.position, point) < avoidanceCoarseRadius + lineRadius;
	}

	public bool IsStill()
	{
		return rigidbody.velocity.magnitude < 0.2f;
	}

	public virtual bool ShouldBeAvoided()
	{
		return IsStill();
	}

	public float GetHealthRatio()
	{
		return health / maxHealth;
	}

	protected virtual void HeavyDamage()
	{
		if (heavyDamageAudio != null && claimedByPlayer)
		{
			heavyDamageAudio.Play();
			FpsActorController.instance.Deafen();
			FpsActorController.instance.fpParent.ApplyScreenshake(20f, 3);
		}
	}

	public bool AiShouldEnter()
	{
		return !stuck && !IsFull() && !burning && !dead && HasUnclaimedSeats() && takingFireAction.TrueDone() && !WaterLevel.InWater(base.transform.position);
	}

	private void OnGUI()
	{
		if (ActorManager.instance.debug && !dead && Camera.main != null)
		{
			float num = Vector3.Dot(base.transform.position - Camera.main.transform.position, Camera.main.transform.forward);
			if (num > 1f && num < 100f)
			{
				Vector3 vector = Camera.main.WorldToScreenPoint(base.transform.position + Vector3.up * ramSize.y * 4f);
				GUI.skin.label.alignment = TextAnchor.UpperCenter;
				GUI.Label(new Rect(vector.x - 100f, (float)Screen.height - vector.y, 200f, 50f), "AI Claimed seats: " + seatsClaimedByBots + "/" + seats.Length);
				GUI.Label(new Rect(vector.x - 100f, (float)Screen.height - vector.y + 20f, 200f, 50f), "Stuck: " + stuck);
				GUI.Label(new Rect(vector.x - 100f, (float)Screen.height - vector.y + 40f, 200f, 50f), "Taking fire: " + !takingFireAction.TrueDone());
				GUI.Label(new Rect(vector.x - 100f, (float)Screen.height - vector.y + 60f, 200f, 50f), "AI should enter? " + AiShouldEnter());
			}
		}
	}
}
