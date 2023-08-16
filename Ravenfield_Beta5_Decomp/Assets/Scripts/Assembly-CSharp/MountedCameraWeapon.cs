using UnityEngine;

public class MountedCameraWeapon : MountedWeapon
{
	private Camera camera;

	public float maxPitch = 60f;

	public float minPitch = -15f;

	protected override void Awake()
	{
		base.Awake();
		camera = GetComponentInChildren<Camera>();
		camera.enabled = false;
		camera.depth = 50f;
	}

	protected override void Update()
	{
		base.Update();
		if (user != null)
		{
			base.transform.rotation = Quaternion.LookRotation(user.controller.FacingDirection(), Vector3.up);
			Vector3 localEulerAngles = base.transform.localEulerAngles;
			float value = Mathf.DeltaAngle(0f, localEulerAngles.x);
			localEulerAngles.x = Mathf.Clamp(value, minPitch, maxPitch);
			base.transform.localEulerAngles = localEulerAngles;
			base.transform.rotation = Quaternion.LookRotation(base.transform.forward, Vector3.up);
		}
	}

	public override void SetAiming(bool aiming)
	{
		base.SetAiming(aiming);
		if (!user.aiControlled)
		{
			camera.enabled = aiming;
		}
	}

	public override void Holster()
	{
		base.Holster();
		if (!user.aiControlled)
		{
			camera.enabled = false;
		}
	}
}
