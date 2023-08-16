using UnityEngine;

public class MountedTurret : MountedWeapon
{
	private const float MAX_TURN_DELTA = 10f;

	public Camera camera;

	public Transform towerTransform;

	public Transform turretTransform;

	protected override void Update()
	{
		base.Update();
		if (user != null)
		{
			Vector2 vector = Vector2.ClampMagnitude(GetInput(), 10f);
			Vector3 localEulerAngles = towerTransform.localEulerAngles;
			localEulerAngles.z += vector.x;
			towerTransform.localEulerAngles = localEulerAngles;
			Vector3 localEulerAngles2 = turretTransform.localEulerAngles;
			localEulerAngles2.x = Mathf.Clamp(Mathf.DeltaAngle(0f, localEulerAngles2.x - vector.y), -40f, 15f);
			turretTransform.localEulerAngles = localEulerAngles2;
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
		return new Vector2(vector.x * 5f, vector.y * 5f);
	}
}
