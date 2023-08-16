using System;
using System.Collections;
using UnityEngine;

public class Actor : Hurtable
{
	public enum TargetType
	{
		Infantry = 0,
		InfantryGroup = 1,
		Unarmored = 2,
		Armored = 3,
		Air = 4
	}

	private const float MOVEMENT_LERP_SPEED = 5f;

	private const float TURN_MOVEMENT_FORWARD_SLERP = 2f;

	private const float CLAMP_FACING_ANGLE = 50f;

	private const float OFFSET_RETURN_SPEED = 2f;

	private const float GETUP_HEIGHT_PADDING = 0.5f;

	private const float PROJECT_TO_GROUND_THRESHOLD = 1.5f;

	private const float PROJECT_TO_GROUND_RADIUS = 0.3f;

	private const float AUTO_MOVE_FALL_PROJECT_DISTANCE = 4f;

	private const float RAGDOLL_DRIVE_SPRING = 700f;

	private const float RAGDOLL_DRIVE_DAMPING = 3f;

	private const float RAGDOLL_DEAD_DRIVE_SPRING = 50f;

	private const float RAGDOLL_DEAD_DRIVE_DAMPING = 1f;

	private const float BALANCE_GAIN_PER_SECOND = 10f;

	public const float CULL_ANIMATOR_DISTANCE = 300f;

	public const float LQ_CAMERA_DISTANCE = 12000f;

	public const float LQ_UPDATE_RATE = 0.2f;

	private const int AIM_ANIMATION_LAYER = 1;

	private const int CROUCH_ANIMATION_LAYER = 2;

	private const int RAGDOLL_ANIMATION_LAYER = 3;

	private const float SWIM_SPEED = 30f;

	public ActorController controller;

	public ActiveRaggy ragdoll;

	public Animator animator;

	public bool autoMoveActor;

	public Rigidbody hipRigidbody;

	public Rigidbody headRigidbody;

	[NonSerialized]
	public float deathTimestamp;

	[NonSerialized]
	public float health = 100f;

	[NonSerialized]
	public float balance = 100f;

	[NonSerialized]
	public bool dead;

	[NonSerialized]
	public bool fallenOver;

	private Collider[] hitboxColliders;

	private ActorIk ik;

	private Vector2 movement;

	private Vector3 parentOffset = Vector3.zero;

	private Transform originalParent;

	private Action fallAction = new Action(3f);

	private Action stopFallAction = new Action(0.5f);

	private Action getupAction = new Action(2f);

	private Action highlightAction = new Action(4f);

	private Action hurtAction = new Action(0.6f);

	private bool wasCrouching;

	[NonSerialized]
	public bool inWater;

	[NonSerialized]
	public Weapon activeWeapon;

	[NonSerialized]
	public Weapon[] weapons = new Weapon[5];

	private int activeWeaponSlot;

	public int[] spareAmmo = new int[5];

	private bool wasFiring;

	private bool aiming;

	private Action aimingAction = new Action(0.2f);

	[NonSerialized]
	public bool hasAmmoBox;

	[NonSerialized]
	public int ammoBoxSlot;

	[NonSerialized]
	public bool hasMedipack;

	[NonSerialized]
	public int medipackSlot;

	[NonSerialized]
	public bool aiControlled;

	[NonSerialized]
	public bool needsResupply;

	[NonSerialized]
	public Seat seat;

	private Action cannotEnterVehicleAction = new Action(1f);

	[NonSerialized]
	public SkinnedMeshRenderer skinnedRenderer;

	[NonSerialized]
	public SkinnedMeshRenderer skinnedRendererRagdoll;

	private bool lqUpdate;

	private float lastUpdate;

	private float nextLqUpdateTime;

	[NonSerialized]
	public float lqUpdatePhase;

	private Rigidbody rigidbody;

	private static Vector3 removePitchEuler = new Vector3(0f, 1f, 1f);

	private static Vector3 removeY = new Vector3(1f, 0f, 1f);

	public virtual void Awake()
	{
		animator.enabled = false;
		ragdoll.ragdollObject.SetActive(true);
		skinnedRenderer = animator.GetComponentInChildren<SkinnedMeshRenderer>();
		skinnedRendererRagdoll = ragdoll.ragdollObject.GetComponentInChildren<SkinnedMeshRenderer>();
		ragdoll.ragdollObject.SetActive(false);
		ik = animator.GetComponent<ActorIk>();
		parentOffset = base.transform.localPosition;
		originalParent = base.transform.parent;
		aiControlled = controller.GetType() == typeof(AiActorController);
		dead = true;
		rigidbody = GetComponent<Rigidbody>();
		hitboxColliders = ragdoll.animatedObject.GetComponentsInChildren<Collider>();
	}

	public void Start()
	{
		ActorManager.Register(this);
		lastUpdate = Time.time;
	}

	public bool IsAiming()
	{
		return aiming;
	}

	public void SpawnAt(Vector3 position)
	{
		if (autoMoveActor)
		{
			base.transform.position = position;
		}
		SpawnLoadoutWeapons();
		if (seat != null)
		{
			seat.OccupantLeft();
			seat = null;
		}
		ik.turnBody = true;
		ik.weight = 1f;
		fallenOver = false;
		animator.enabled = true;
		animator.SetLayerWeight(3, 0f);
		animator.SetTrigger("reset");
		ragdoll.SetDrive(700f, 3f);
		balance = 100f;
		health = 100f;
		dead = false;
		ragdoll.InstantAnimate();
		controller.EnableInput();
		controller.SpawnAt(position);
		needsResupply = false;
		animator.SetBool("dead", false);
		animator.SetBool("seated", false);
		if (!aiControlled)
		{
			IngameUi.instance.Show();
			IngameUi.instance.SetHealth(Mathf.Max(0f, health));
		}
		ActorManager.SetAlive(this);
	}

	private void SpawnLoadoutWeapons()
	{
		hasAmmoBox = false;
		hasMedipack = false;
		WeaponManager.LoadoutSet loadout = controller.GetLoadout();
		SpawnWeapon(loadout.primary, 0);
		SpawnWeapon(loadout.secondary, 1);
		SpawnWeapon(loadout.gear1, 2);
		SpawnWeapon(loadout.gear2, 3);
		SpawnWeapon(loadout.gear3, 4);
		SwitchToFirstAvailableWeapon();
	}

	private void SwitchToFirstAvailableWeapon()
	{
		for (int i = 0; i < 5; i++)
		{
			if (HasWeaponInSlot(i))
			{
				activeWeaponSlot = i;
				Unholster(weapons[i]);
				break;
			}
		}
	}

	private void SpawnWeapon(WeaponManager.WeaponEntry entry, int slotNumber)
	{
		if (entry == null)
		{
			weapons[slotNumber] = null;
			return;
		}
		Weapon component = UnityEngine.Object.Instantiate(entry.prefab).GetComponent<Weapon>();
		component.gameObject.name = entry.name;
		if (aiControlled)
		{
			UnityEngine.Object.Destroy(component.animator);
			component.thirdPersonTransform.localEulerAngles = new Vector3(0f, 0f, -90f);
			component.thirdPersonTransform.localPosition = component.thirdPersonOffset;
			component.thirdPersonTransform.localScale = new Vector3(component.thirdPersonScale, component.thirdPersonScale, component.thirdPersonScale);
			component.CullFpsObjects();
		}
		else
		{
			component.AssignFpAudioMix();
		}
		component.FindRenderers(aiControlled);
		component.Equip(this);
		component.transform.parent = controller.WeaponParent();
		component.transform.localPosition = Vector3.zero;
		component.transform.localRotation = Quaternion.identity;
		component.slot = slotNumber;
		component.ammo = component.configuration.ammo;
		weapons[slotNumber] = component;
		spareAmmo[slotNumber] = component.configuration.spareAmmo;
		if (!component.IsToggleable())
		{
			component.gameObject.SetActive(false);
			if (entry.name == "AMMO BAG")
			{
				hasAmmoBox = true;
				ammoBoxSlot = slotNumber;
			}
			if (entry.name == "MEDIPACK")
			{
				hasMedipack = true;
				medipackSlot = slotNumber;
			}
		}
	}

	public void EmoteHail()
	{
		if (!dead && !ragdoll.IsRagdoll() && animator.enabled)
		{
			animator.SetTrigger("hail");
		}
	}

	public void EmoteRegroup()
	{
		if (!dead && !ragdoll.IsRagdoll() && animator.enabled)
		{
			animator.SetTrigger("regroup");
		}
	}

	public void EmoteMove()
	{
		if (!dead && !ragdoll.IsRagdoll() && animator.enabled)
		{
			animator.SetTrigger("move");
		}
	}

	public void EmoteHalt()
	{
		if (!dead && !ragdoll.IsRagdoll() && animator.enabled)
		{
			animator.SetTrigger("halt");
		}
	}

	protected virtual void FixedUpdate()
	{
		if (!ragdoll.ragdollObject.activeInHierarchy)
		{
			return;
		}
		if (inWater)
		{
			hipRigidbody.AddForce(-Physics.gravity * 3f, ForceMode.Acceleration);
			hipRigidbody.drag = 8f;
			hipRigidbody.angularDrag = 8f;
			if (!dead)
			{
				headRigidbody.AddForce(-Physics.gravity * 3.5f, ForceMode.Acceleration);
				headRigidbody.angularDrag = 3f;
				headRigidbody.drag = 10f;
			}
		}
		else
		{
			hipRigidbody.drag = 0f;
			headRigidbody.drag = 0f;
			hipRigidbody.angularDrag = 0.05f;
			headRigidbody.angularDrag = 0.05f;
		}
		if (!dead && WaterLevel.InWater(CenterPosition()))
		{
			Vector3 vector = controller.SwimInput() * 30f;
			hipRigidbody.AddForce(-Physics.gravity * 2f + vector * 0.2f, ForceMode.Acceleration);
			headRigidbody.AddForce(-Physics.gravity * 2f + vector * 0.8f, ForceMode.Acceleration);
		}
	}

	public virtual void Update()
	{
		Vector3 position = CenterPosition();
		position.y += 0.5f;
		inWater = WaterLevel.InWater(position);
		if (dead)
		{
			return;
		}
		if (inWater && !fallenOver)
		{
			if (IsSeated())
			{
				LeaveSeat();
			}
			FallOver();
		}
		if (!hurtAction.Done() && !fallenOver && !dead)
		{
			float num = hurtAction.Ratio();
			if (num < 0.2f)
			{
				ik.weight = 0.5f - 2.5f * num + 0.5f;
			}
			else
			{
				ik.weight = 0.625f * (num - 0.2f) + 0.5f;
			}
		}
		if (!getupAction.Done())
		{
			UpdateGetup();
		}
		bool flag = lqUpdate;
		lqUpdate = IsLowQuality();
		float dt = Time.time - lastUpdate;
		if (!lqUpdate || Time.time >= nextLqUpdateTime)
		{
			if (!fallenOver)
			{
				UpdateFacing();
				if (!IsSeated())
				{
					UpdateMovement(dt);
				}
			}
			else
			{
				UpdateRagdollStates();
			}
			nextLqUpdateTime = (float)Mathf.CeilToInt(Time.time / 0.2f) * 0.2f + lqUpdatePhase;
			lastUpdate = Time.time;
		}
		if (activeWeapon != null)
		{
			if (lqUpdate && !flag)
			{
				activeWeapon.Hide();
			}
			else if (!lqUpdate && flag)
			{
				activeWeapon.Show();
			}
		}
		if (activeWeapon != null)
		{
			UpdateWeapon();
		}
		animator.SetBool("seated", IsSeated());
		balance = Mathf.Min(balance + Time.deltaTime * 10f, 100f);
	}

	private void UpdateWeapon()
	{
		bool flag = !fallenOver && controller.Fire() && (!IsSeated() || seat.CanUseWeapon() || seat.HasMountedWeapon());
		if (flag)
		{
			activeWeapon.Fire(controller.FacingDirection(), controller.UseMuzzleDirection());
		}
		else if (wasFiring)
		{
			activeWeapon.StopFire();
		}
		wasFiring = flag;
		aiming = (controller.Aiming() || !aimingAction.TrueDone()) && !fallenOver && activeWeapon != null && activeWeapon.CanBeAimed();
		if (!activeWeapon.aiming && aiming)
		{
			activeWeapon.SetAiming(true);
			aimingAction.Start();
		}
		else if (activeWeapon.aiming && !aiming)
		{
			activeWeapon.SetAiming(false);
		}
		if (controller.Reload() && !activeWeapon.AmmoFull() && activeWeapon.HasSpareAmmo())
		{
			activeWeapon.Reload();
		}
	}

	private void UpdateGetup()
	{
		float num = getupAction.Ratio();
		animator.SetLayerWeight(3, Mathf.Clamp01(2f * (1f - num)));
		ik.weight = num;
		if (num > 0.5f)
		{
			controller.EnableInput();
		}
		if (getupAction.TrueDone())
		{
			fallenOver = false;
			controller.EndRagdoll();
			if (HasUnholsteredWeapon())
			{
				activeWeapon.gameObject.SetActive(true);
			}
		}
	}

	public float GetupProgress()
	{
		if (ragdoll.IsRagdoll())
		{
			return 0f;
		}
		return getupAction.Ratio();
	}

	private void UpdateFacing()
	{
		Vector3 vector = controller.FacingDirection();
		Quaternion to = Quaternion.Euler(Vector3.Scale(Quaternion.LookRotation(vector, Vector3.up).eulerAngles, removePitchEuler));
		if (!IsSeated())
		{
			float f = Mathf.DeltaAngle(base.transform.eulerAngles.y, to.eulerAngles.y);
			if (Mathf.Abs(f) > 50f)
			{
				base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, to, Mathf.Abs(f) - 50f);
				base.transform.eulerAngles.Scale(new Vector3(0f, 1f, 1f));
			}
		}
		Vector3 normalized = Vector3.Cross(Vector3.up, vector).normalized;
		if (ControllingVehicle())
		{
			ik.aimPoint = base.transform.position + vector * 100f;
		}
		else
		{
			ik.aimPoint = base.transform.position + vector * 100f + normalized * 55f;
		}
		animator.SetFloat("lean", controller.Lean());
	}

	private void UpdateMovement(float dt)
	{
		if (!controller.OnGround())
		{
			return;
		}
		Vector3 vector = controller.Velocity();
		Vector3 vector2 = Vector3.Scale(vector, removeY);
		bool flag = vector2.magnitude > 0.1f;
		animator.SetBool("moving", flag);
		animator.SetBool("sprinting", controller.IsSprinting());
		if (!IngameMenuUi.IsOpen())
		{
			bool flag2 = controller.Crouch();
			if (flag2 && !wasCrouching)
			{
				animator.SetLayerWeight(2, 1f);
				controller.StartCrouch();
			}
			else if (!flag2 && wasCrouching)
			{
				if (!controller.EndCrouch())
				{
					flag2 = true;
				}
				else
				{
					animator.SetLayerWeight(2, 0f);
				}
			}
			animator.SetBool("crouched", flag2);
			wasCrouching = flag2;
		}
		if (flag)
		{
			bool flag3 = Vector3.Dot(vector, base.transform.forward) < 0f;
			Vector3 vector3 = base.transform.worldToLocalMatrix.MultiplyVector(vector2);
			Vector2 b = new Vector2(vector3.x, vector3.z);
			Quaternion b2;
			if (!flag3)
			{
				b2 = Quaternion.LookRotation(vector2);
			}
			else
			{
				b.x = 0f - b.x;
				b2 = Quaternion.LookRotation(-vector2);
			}
			movement = Vector2.Lerp(movement, b, 5f * dt);
			animator.SetFloat("movement x", movement.x);
			animator.SetFloat("movement y", movement.y);
			rigidbody.MoveRotation(Quaternion.Slerp(base.transform.rotation, b2, dt * 2f));
		}
		Vector3 vector4 = rigidbody.position + vector * dt;
		if (controller.ProjectToGround())
		{
			Ray ray = new Ray(vector4 + Vector3.up * 1.5f, Vector3.down);
			RaycastHit hitInfo;
			if (Physics.SphereCast(ray, 0.3f, out hitInfo, 15f, 1))
			{
				if (hitInfo.distance > 4f)
				{
					FallOver();
				}
				else
				{
					vector4.y = hitInfo.point.y;
				}
			}
			else
			{
				FallOver();
			}
		}
		if (autoMoveActor)
		{
			rigidbody.position = vector4;
		}
		UpdateOffset(dt);
	}

	private void UpdateOffset(float dt)
	{
		if (base.transform.parent != null)
		{
			base.transform.localPosition = Vector3.MoveTowards(base.transform.localPosition, parentOffset, 2f * dt);
		}
	}

	private void UpdateRagdollStates()
	{
		bool flag = Mathf.Abs(ragdoll.Velocity().magnitude) > 0.6f;
		animator.SetBool("falling", flag);
		animator.SetBool("onBack", ragdoll.OnBack());
		animator.SetBool("swim", inWater);
		animator.SetBool("swim forward", inWater && controller.SwimInput() != Vector3.zero);
		if (fallAction.TrueDone() && !flag && ragdoll.IsRagdoll() && !inWater)
		{
			if (stopFallAction.TrueDone())
			{
				GetUp();
			}
		}
		else
		{
			stopFallAction.Start();
		}
	}

	public void KnockOver(Vector3 force)
	{
		if (!ragdoll.IsRagdoll())
		{
			FallOver();
			ApplyRigidbodyForce(force);
		}
	}

	public void ApplyRigidbodyForce(Vector3 force)
	{
		ragdoll.MainRigidbody().AddForce(force, ForceMode.Impulse);
	}

	public void FallOver()
	{
		if (IsSeated())
		{
			LeaveSeat();
		}
		animator.SetBool("ragdolled", true);
		fallenOver = true;
		ragdoll.Ragdoll(controller.Velocity());
		controller.DisableInput();
		controller.StartRagdoll();
		ik.weight = 0f;
		animator.SetLayerWeight(3, 1f);
		fallAction.Start();
		getupAction.Stop();
		if (HasUnholsteredWeapon())
		{
			activeWeapon.SetAiming(false);
			activeWeapon.gameObject.SetActive(false);
		}
	}

	private void GetUp()
	{
		ragdoll.Animate();
		controller.GettingUp();
		getupAction.Start();
		animator.SetBool("ragdolled", false);
	}

	private void InstantGetUp()
	{
		ragdoll.InstantAnimate();
		controller.GettingUp();
		controller.EnableInput();
		animator.SetLayerWeight(3, 0f);
		ik.weight = 1f;
		fallenOver = false;
		controller.EndRagdoll();
		if (HasUnholsteredWeapon())
		{
			activeWeapon.gameObject.SetActive(true);
		}
	}

	private void Die(Vector3 impactForce)
	{
		Vector3 point = Position();
		animator.SetBool("dead", true);
		for (int i = 0; i < 5; i++)
		{
			if (HasWeaponInSlot(i))
			{
				DropWeapon(weapons[i]);
			}
		}
		if (IsSeated())
		{
			LeaveSeat();
		}
		deathTimestamp = Time.time;
		fallAction.Stop();
		getupAction.Stop();
		controller.Die();
		controller.DisableInput();
		animator.enabled = false;
		ragdoll.SetDrive(50f, 1f);
		ragdoll.Ragdoll(controller.Velocity());
		ApplyRigidbodyForce(impactForce);
		dead = true;
		if (!aiControlled)
		{
			IngameUi.instance.Hide();
		}
		ActorManager.SetDead(this);
		PathfindingManager.RegisterDeath(point);
		ScoreUi.AddScore((team == 1) ? 1 : 0, (team == 0) ? 1 : 0);
	}

	public virtual Vector3 Position()
	{
		if (ragdoll.IsRagdoll())
		{
			return ragdoll.Position();
		}
		return base.transform.position;
	}

	public virtual Vector3 CenterPosition()
	{
		if (ragdoll.IsRagdoll())
		{
			return ragdoll.HumanBoneTransform(HumanBodyBones.Spine).position;
		}
		return ragdoll.HumanBoneTransformAnimated(HumanBodyBones.Spine).position;
	}

	public virtual Vector3 Velocity()
	{
		if (IsSeated())
		{
			return seat.vehicle.Velocity();
		}
		if (ragdoll.IsRagdoll())
		{
			return ragdoll.Velocity();
		}
		return controller.Velocity();
	}

	public bool StandingStill()
	{
		return Velocity().sqrMagnitude < 0.1f;
	}

	public override bool Damage(float healthDamage, float balanceDamage, bool piercing, Vector3 point, Vector3 direction, Vector3 impactForce)
	{
		bool flag = IsSeated() && seat.enclosed;
		if (!piercing && flag)
		{
			return false;
		}
		if (dead)
		{
			return false;
		}
		controller.ReceivedDamage(healthDamage, balanceDamage, point, direction, impactForce);
		health -= healthDamage;
		if (!flag)
		{
			balance = Mathf.Max(balance - balanceDamage, -100f);
		}
		int num = Mathf.CeilToInt(healthDamage / 10f);
		for (int i = 0; i < num; i++)
		{
			DecalManager.CreateBloodDrop(point, Vector3.ClampMagnitude(impactForce * 0.1f, 5f), team);
		}
		if (health <= 0f)
		{
			Die(impactForce);
		}
		else if (ragdoll.IsRagdoll())
		{
			ApplyRigidbodyForce(impactForce);
		}
		else if (balance < 0f)
		{
			KnockOver(Vector3.up * 100f + impactForce);
		}
		else
		{
			Hurt(UnityEngine.Random.Range(-2f, 2f));
		}
		if (!aiControlled)
		{
			IngameUi.instance.SetHealth(Mathf.Max(0f, health));
			float intensity = Mathf.Clamp01(0.3f + (1f - health / 100f));
			IngameUi.instance.ShowVignette(intensity, 6f);
		}
		return true;
	}

	public virtual void ApplyRecoil(Vector3 impulse)
	{
		controller.ApplyRecoil(impulse);
	}

	public Vector3 WeaponMuzzlePosition()
	{
		return activeWeapon.MuzzlePosition();
	}

	private void Unholster(Weapon weapon)
	{
		weapon.gameObject.SetActive(true);
		activeWeapon = weapon;
		activeWeapon.Unholster();
		controller.SwitchedToWeapon(activeWeapon);
		if (lqUpdate)
		{
			activeWeapon.Hide();
		}
		if (!aiControlled)
		{
			IngameUi.instance.SetWeapon(weapon);
			UpdateAmmoUi();
		}
	}

	private void HolsterActiveWeapon()
	{
		if (!(activeWeapon == null))
		{
			activeWeapon.Holster();
			activeWeapon = null;
		}
	}

	private void DropWeapon(Weapon weapon)
	{
		weapon.transform.parent = null;
		weapon.Drop();
		if (weapon == activeWeapon)
		{
			activeWeapon = null;
		}
	}

	public bool HasWeaponInSlot(int i)
	{
		return weapons[i] != null;
	}

	public bool HasUnholsteredWeapon()
	{
		return activeWeapon != null;
	}

	public void Highlight()
	{
		highlightAction.Start();
	}

	public bool IsHighlighted()
	{
		return !highlightAction.TrueDone();
	}

	public bool IsSeated()
	{
		return seat != null;
	}

	public bool IsPassenger()
	{
		return IsSeated() && seat.type != 0 && seat.type != Seat.Type.Pilot;
	}

	public bool IsDriver()
	{
		return IsSeated() && (seat.type == Seat.Type.Driver || seat.type == Seat.Type.Pilot);
	}

	public bool CanEnterSeat()
	{
		return !IsSeated() && cannotEnterVehicleAction.TrueDone();
	}

	public bool EnterSeat(Seat seat)
	{
		if (fallenOver)
		{
			InstantGetUp();
		}
		if (seat.vehicle.dead || seat.IsOccupied())
		{
			return false;
		}
		animator.SetInteger("seated type", (int)seat.animation);
		seat.SetOccupant(this);
		base.transform.parent = seat.transform;
		base.transform.localPosition = Vector3.zero;
		base.transform.localRotation = Quaternion.identity;
		controller.StartSeated(seat);
		this.seat = seat;
		animator.SetLayerWeight(2, 0f);
		if (!seat.CanUseWeapon())
		{
			animator.SetLayerWeight(1, 0f);
			HolsterActiveWeapon();
			ik.turnBody = false;
		}
		if (seat.HasMountedWeapon())
		{
			Unholster(seat.weapon);
		}
		Collider[] array = hitboxColliders;
		foreach (Collider collider in array)
		{
			collider.gameObject.layer = 16;
		}
		return true;
	}

	public void LeaveSeat()
	{
		Vector3 vector = seat.transform.position + seat.transform.localToWorldMatrix.MultiplyVector(seat.exitOffset);
		Vector3 forward = seat.transform.forward;
		Vehicle vehicle = seat.vehicle;
		if (seat.HasMountedWeapon() && activeWeapon == seat.weapon)
		{
			HolsterActiveWeapon();
		}
		seat.OccupantLeft();
		seat = null;
		Quaternion quaternion = Quaternion.LookRotation(Vector3.Scale(forward, removeY), Vector3.up);
		controller.EndSeated(vector, quaternion);
		base.transform.parent = originalParent;
		if (originalParent != null)
		{
			base.transform.localRotation = Quaternion.identity;
		}
		rigidbody.position = vector;
		rigidbody.rotation = quaternion;
		animator.SetLayerWeight(1, 1f);
		ik.turnBody = true;
		if (activeWeapon == null)
		{
			SwitchToFirstAvailableWeapon();
		}
		cannotEnterVehicleAction.Start();
		StartCoroutine(ReactivateCollisionsWith(vehicle));
	}

	private IEnumerator ReactivateCollisionsWith(Vehicle vehicle)
	{
		yield return new WaitForSeconds(0.5f);
		bool reenteredThatVehicle = IsSeated() && seat.vehicle == vehicle;
		if (vehicle != null && !reenteredThatVehicle)
		{
			Collider[] array = hitboxColliders;
			foreach (Collider collider in array)
			{
				collider.gameObject.layer = 8;
			}
		}
	}

	public void NextWeapon()
	{
		if (dead || fallenOver || (IsSeated() && !seat.CanUseWeapon()))
		{
			return;
		}
		for (int i = 1; i < 4; i++)
		{
			int num = (activeWeaponSlot + i) % 5;
			if (weapons[num] != null && !weapons[num].IsToggleable())
			{
				SwitchWeapon(num);
				break;
			}
		}
	}

	public void PreviousWeapon()
	{
		if (dead || fallenOver || (IsSeated() && !seat.CanUseWeapon()))
		{
			return;
		}
		for (int i = 1; i < 4; i++)
		{
			int num = (activeWeaponSlot - i + 5) % 5;
			if (weapons[num] != null)
			{
				SwitchWeapon(num);
				break;
			}
		}
	}

	public void SwitchWeapon(int slot)
	{
		if (dead || fallenOver || (IsSeated() && !seat.CanUseWeapon()))
		{
			return;
		}
		Weapon weapon = weapons[slot];
		if (!(weapon != null))
		{
			return;
		}
		if (weapon.IsToggleable())
		{
			((ToggleableItem)weapon).Toggle();
		}
		else if (weapon != activeWeapon)
		{
			if (activeWeapon != null)
			{
				HolsterActiveWeapon();
			}
			activeWeaponSlot = slot;
			Unholster(weapon);
		}
	}

	public void SwitchSeat(int targetSeat)
	{
		if (IsSeated())
		{
			Vehicle vehicle = seat.vehicle;
			if (targetSeat >= 0 && targetSeat < vehicle.seats.Length && !vehicle.seats[targetSeat].IsOccupied())
			{
				LeaveSeat();
				EnterSeat(vehicle.seats[targetSeat]);
			}
		}
	}

	public void Hurt(float x)
	{
		if (!fallenOver && !dead)
		{
			animator.SetFloat("hurt x", x);
			animator.SetTrigger("hurt");
			hurtAction.Start();
		}
	}

	public void SetTeam(int team)
	{
		base.team = team;
		Color color = ColorScheme.TeamColor(base.team);
		skinnedRenderer.material.color = color;
		skinnedRendererRagdoll.material.color = color;
	}

	private bool ControllingVehicle()
	{
		return IsSeated() && !seat.CanUseWeapon();
	}

	public int RemainingSpareAmmoFor(Weapon weapon)
	{
		for (int i = 0; i < 5; i++)
		{
			if (weapon == weapons[i])
			{
				return spareAmmo[i];
			}
		}
		return 0;
	}

	public void UpdateAmmoUi()
	{
		IngameUi.instance.SetAmmoText(activeWeapon.ammo, activeWeapon.GetSpareAmmo());
	}

	public void UpdateHealthUi()
	{
		IngameUi.instance.SetHealth(health);
	}

	public int RemoveSpareAmmo(int howmuch, int slot)
	{
		if (slot != -1)
		{
			int num = spareAmmo[slot];
			int num2 = Mathf.Max(0, num - howmuch);
			spareAmmo[slot] = num2;
			if (!aiControlled)
			{
				UpdateAmmoUi();
			}
			return num - num2;
		}
		return 0;
	}

	public void AmmoChanged()
	{
		if (activeWeapon != null && activeWeapon.AllowsResupply() && RemainingSpareAmmoFor(activeWeapon) <= activeWeapon.configuration.spareAmmo / 2)
		{
			needsResupply = true;
		}
		if (!aiControlled)
		{
			UpdateAmmoUi();
		}
	}

	public void ResupplyAmmo()
	{
		bool flag = false;
		needsResupply = false;
		for (int i = 0; i < 5; i++)
		{
			Weapon weapon = weapons[i];
			if (!(weapon == null) && weapon.AllowsResupply())
			{
				int num = spareAmmo[i];
				spareAmmo[i] = Mathf.Min(weapon.configuration.spareAmmo, spareAmmo[i] + weapon.configuration.resupplyNumber);
				flag |= num != spareAmmo[i];
				if (spareAmmo[i] <= weapon.configuration.spareAmmo / 2)
				{
					needsResupply = true;
				}
			}
		}
		if (flag)
		{
			AmmoChanged();
			if (!aiControlled)
			{
				IngameUi.instance.Resupply();
			}
		}
	}

	public bool ResupplyHealth()
	{
		if (dead)
		{
			return false;
		}
		float num = health;
		health = Mathf.Min(health + 30f, 100f);
		if (!aiControlled && num != health)
		{
			UpdateHealthUi();
			IngameUi.instance.Heal();
		}
		return num != health;
	}

	public bool IsLowQuality()
	{
		if (!aiControlled)
		{
			return false;
		}
		if (fallenOver)
		{
			return !skinnedRendererRagdoll.isVisible || Vector3.Distance(base.transform.position, Camera.main.transform.position) > 12000f / Camera.main.fieldOfView;
		}
		return !skinnedRenderer.isVisible || Vector3.Distance(base.transform.position, Camera.main.transform.position) > 12000f / Camera.main.fieldOfView;
	}

	public virtual TargetType GetTargetType()
	{
		if (IsSeated())
		{
			return seat.vehicle.targetType;
		}
		if (controller.IsGroupedUp())
		{
			return TargetType.InfantryGroup;
		}
		return TargetType.Infantry;
	}
}
