using UnityEngine;

public class TankTurret : MountedWeapon
{
	private const float MAX_TURN_DELTA = 5f;

	public Camera camera;

	public ConfigurableJoint towerJoint;

	public HingeJoint cannonJoint;

	public Renderer cannonRenderer;

	private Rigidbody rigidbody;

	protected override void Awake()
	{
		base.Awake();
		rigidbody = GetComponent<Rigidbody>();
	}

	protected override void Update()
	{
		base.Update();
		if (!(towerJoint == null))
		{
			JointSpring spring = cannonJoint.spring;
			Vector3 eulerAngles = towerJoint.targetRotation.eulerAngles;
			Vector2 input = GetInput();
			eulerAngles.z = Mathf.Clamp(eulerAngles.z - input.x, eulerAngles.z - 5f, eulerAngles.z + 5f);
			spring.targetPosition = Mathf.Clamp(Mathf.Clamp(spring.targetPosition - input.y, spring.targetPosition - 5f, spring.targetPosition + 5f), cannonJoint.limits.min, cannonJoint.limits.max);
			towerJoint.targetRotation = Quaternion.Euler(eulerAngles);
			cannonJoint.spring = spring;
		}
	}

	public override void Unholster()
	{
		base.Unholster();
		if (!user.aiControlled)
		{
			FpsActorController.instance.DisableCameras();
			camera.enabled = true;
		}
	}

	public override void Holster()
	{
		base.Holster();
		camera.enabled = false;
		if (!user.aiControlled)
		{
			FpsActorController.instance.EnableCameras();
		}
	}

	private Vector2 GetInput()
	{
		if (user == null)
		{
			return Vector2.zero;
		}
		if (!user.aiControlled)
		{
			return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y") * (float)((!OptionsUi.GetOptions().mouseInvert) ? 1 : (-1))) * OptionsUi.GetOptions().mouseSensitivity * 4f;
		}
		Vector3 vector = configuration.muzzle.worldToLocalMatrix.MultiplyVector(user.controller.FacingDirection());
		return new Vector2(vector.x * 3f, vector.y * 3f);
	}

	protected override Projectile SpawnProjectile(Vector3 direction)
	{
		rigidbody.AddForceAtPosition(-configuration.muzzle.forward * configuration.kickback + Random.insideUnitSphere * configuration.randomKick, configuration.muzzle.position, ForceMode.Impulse);
		return base.SpawnProjectile(direction);
	}
}
