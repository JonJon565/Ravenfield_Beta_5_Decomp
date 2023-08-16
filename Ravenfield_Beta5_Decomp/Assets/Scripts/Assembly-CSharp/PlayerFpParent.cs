using System;
using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class PlayerFpParent : MonoBehaviour
{
	private const float CAMERA_HIP_FOV = 60f;

	private const float CAMERA_AIM_FOV = 45f;

	private const float FOV_SPEED = 150f;

	private const float POSITION_SPRING = 150f;

	private const float POSITION_DRAG = 10f;

	private const float MAX_POSITION_OFFSET = 0.2f;

	private const int POSITION_SPRING_ITERAIONS = 8;

	private const float ROTATION_SPRING = 70f;

	private const float ROTATION_DRAG = 6f;

	private const float MAX_ROTATION_OFFSET = 15f;

	private const float ROTATION_IMPULSE_GAIN = 100f;

	private const int ROTATION_SPRING_ITERAIONS = 8;

	private const float CAMERA_KICK_MULTIPLIER = 0.7f;

	private const float WEAPON_EXTRA_PITCH = 0.1f;

	private const float LEAN_MAX_HEAD_OFFSET = 0.4f;

	private const float LEAN_MAX_WEAPON_OFFSET_LEFT = 0.3f;

	private const float LEAN_MAX_WEAPON_OFFSET_RIGHT = 0.1f;

	private const float LEAN_MAX_CAMERA_ROLL_ANGLE = 3f;

	private const float LEAN_MAX_ROLL_ANGLE = 12f;

	private const float LEAN_HEAD_SPHERECAST_RADIUS = 0.3f;

	private const float SCREENSHAKE_WEAPON_SHAKE = 0.1f;

	private const float SCREENSHAKE_ITERATION_TIME = 0.17f;

	private const float WALK_OFFSET_MAGNITUDE_AIMING = 0.003f;

	private const float WALK_OFFSET_MAGNITUDE = 0.06f;

	private const float WALK_OFFSET_MAGNITUDE_CHANGE_SPEED = 0.06f;

	public static PlayerFpParent instance;

	public Camera fpCamera;

	private Transform fpCameraParent;

	private FirstPersonController fpsController;

	private Actor actor;

	private Vector3 localOrigin = Vector3.zero;

	private Spring positionSpring;

	private Spring rotationSpring;

	private Transform movementProbe;

	private Vector3 lastPosition = Vector3.zero;

	private Vector3 lastRotation = Vector3.zero;

	private Transform weaponParent;

	private Vector3 weaponParentOrigin;

	private Vector3 kickEuler = Vector3.zero;

	private Action fpKickAction = new Action(0.2f);

	private Action weaponSnapAction = new Action(0.5f);

	private float weaponSnapMagnitude = 0.5f;

	private float weaponSnapFrequency = 5f;

	[NonSerialized]
	public float lean;

	private float fovRatio;

	private float fovSpeed = 5f;

	private bool aiming;

	private bool screenshake;

	private Coroutine screenshakeCoroutine;

	private float verticalFov = 60f;

	private float normalFov = 60f;

	private float zoomFov = 45f;

	private float walkOffsetMagnitude;

	private float extraPitch;

	private void Awake()
	{
		instance = this;
		fpCamera.fieldOfView = 60f;
		movementProbe = base.transform.parent;
		positionSpring = new Spring(150f, 10f, -Vector3.one * 0.2f, Vector3.one * 0.2f, 8);
		rotationSpring = new Spring(70f, 6f, -Vector3.one * 15f, Vector3.one * 15f, 8);
		weaponParent = base.transform.GetChild(0);
		weaponParentOrigin = weaponParent.transform.localPosition;
		fpCameraParent = fpCamera.transform.parent;
		fpsController = GetComponentInParent<FirstPersonController>();
		actor = GetComponentInParent<FpsActorController>().actor;
		AudioListener.volume = 0.3f;
		SetupVerticalFov(OptionsUi.GetOptions().fieldOfView);
	}

	private void Start()
	{
		localOrigin = base.transform.localPosition;
		lastPosition = movementProbe.position;
		lastRotation = movementProbe.eulerAngles;
		SetAimFov(45f);
	}

	private void Update()
	{
		float num = fpsController.StepCycle() * (float)Math.PI;
		float target = 0f;
		if (!actor.IsSeated())
		{
			target = Mathf.Clamp01(actor.Velocity().sqrMagnitude / 60f) * ((!aiming) ? 0.06f : 0.003f);
		}
		walkOffsetMagnitude = Mathf.MoveTowards(walkOffsetMagnitude, target, Time.deltaTime * 0.06f);
		Vector3 vector = new Vector3(Mathf.Cos(num) * walkOffsetMagnitude, Mathf.Sin(num * 2f) * walkOffsetMagnitude * 0.7f, 0f);
		positionSpring.Update();
		rotationSpring.Update();
		Vector3 vector2 = base.transform.worldToLocalMatrix.MultiplyVector(movementProbe.position - lastPosition);
		Vector2 vector3 = new Vector2(Mathf.DeltaAngle(lastRotation.x, movementProbe.eulerAngles.x), Mathf.DeltaAngle(lastRotation.y, movementProbe.eulerAngles.y));
		lastPosition = movementProbe.position;
		lastRotation = movementProbe.eulerAngles;
		float num2 = 0f;
		if (!weaponSnapAction.TrueDone())
		{
			float num3 = 1f - weaponSnapAction.Ratio();
			num2 = num3 * Mathf.Sin(weaponSnapAction.Ratio() * (0.1f + num3) * weaponSnapFrequency) * weaponSnapMagnitude;
		}
		base.transform.localPosition = localOrigin + positionSpring.position + Vector3.down * num2 * 0.1f + vector;
		base.transform.localEulerAngles = rotationSpring.position + Vector3.left * num2 * 20f;
		rotationSpring.position += new Vector3(-0.1f * vector3.x + vector2.y * 5f, -0.1f * vector3.y, 0f);
		positionSpring.position += new Vector3(-0.0001f * vector3.y, 0.0001f * vector3.x, 0f);
		fovRatio = Mathf.MoveTowards(fovRatio, (!aiming) ? 0f : 1f, Time.deltaTime * fovSpeed);
		fpCamera.fieldOfView = Mathf.Lerp(normalFov, zoomFov, fovRatio);
	}

	private void LateUpdate()
	{
		Vector3 vector = Vector3.right * lean * 0.4f;
		Vector3 vector2 = fpCamera.transform.localToWorldMatrix.MultiplyVector(vector);
		Ray ray = new Ray(fpCamera.transform.parent.position, vector2.normalized);
		RaycastHit hitInfo;
		if (Physics.SphereCast(ray, 0.3f, out hitInfo, vector2.magnitude, 1))
		{
			vector = vector.normalized * hitInfo.distance;
		}
		fpCamera.transform.localPosition = vector;
		Vector3 localEulerAngles = Vector3.back * lean * 3f;
		Vector3 target = weaponParentOrigin;
		if (!fpKickAction.Done())
		{
			float num = Mathf.Sin(fpKickAction.Ratio() * (float)Math.PI);
			localEulerAngles += num * kickEuler;
		}
		fpCamera.transform.localEulerAngles = localEulerAngles;
		if (!aiming)
		{
			if (lean > 0f)
			{
				target += new Vector3(1f, -1f, 0f) * lean * 0.1f;
			}
			else
			{
				target += new Vector3(1f, 0.3f, 0f) * lean * 0.3f;
			}
		}
		Vector3 localPosition = Vector3.MoveTowards(weaponParent.transform.localPosition, target, 2f * Time.deltaTime);
		weaponParent.transform.localPosition = localPosition;
		extraPitch = Mathf.MoveTowards(extraPitch, (!aiming) ? 0.1f : 0f, Time.deltaTime);
		weaponParent.transform.localEulerAngles = Vector3.right * Mathf.DeltaAngle(0f, fpCameraParent.localEulerAngles.x) * extraPitch + Vector3.back * lean * 12f;
	}

	public void ApplyScreenshake(float magnitude, int iterations)
	{
		if (screenshake)
		{
			StopCoroutine(screenshakeCoroutine);
		}
		screenshakeCoroutine = StartCoroutine(Screenshake(magnitude, iterations));
	}

	private IEnumerator Screenshake(float magnitude, int iterations)
	{
		screenshake = true;
		for (int i = 0; i < iterations; i++)
		{
			float iterationMagnitude = magnitude * ((float)(iterations - i) / (float)iterations);
			positionSpring.AddVelocity(UnityEngine.Random.insideUnitSphere * iterationMagnitude * 0.1f);
			KickCamera(UnityEngine.Random.insideUnitSphere * iterationMagnitude);
			yield return new WaitForSeconds(0.17f);
		}
		screenshake = false;
	}

	public void ApplyRecoil(Vector3 impulse)
	{
		positionSpring.AddVelocity(impulse);
		Vector3 vector = new Vector3(impulse.z, impulse.x, 0f - impulse.x);
		float num = 0.1f + positionSpring.position.magnitude / 0.2f;
		rotationSpring.AddVelocity(vector * num * 100f);
		if (!screenshake)
		{
			KickCamera(vector);
		}
	}

	public void ApplyWeaponSnap(float magnitude, float duration, float frequency)
	{
		weaponSnapAction.StartLifetime(duration);
		weaponSnapFrequency = frequency;
		weaponSnapMagnitude = magnitude;
	}

	public void Aim()
	{
		aiming = true;
	}

	public void StopAim()
	{
		aiming = false;
	}

	public void SetupVerticalFov(float hFov)
	{
		float num = (float)Screen.width / (float)Screen.height;
		verticalFov = 114.59156f * Mathf.Atan(Mathf.Tan(hFov / 2f * ((float)Math.PI / 180f)) / num);
		fpCamera.fieldOfView = verticalFov;
		normalFov = verticalFov;
	}

	public void SetAimFov(float zoom)
	{
		SetFov(verticalFov, zoom);
	}

	public void SetFov(float normal, float zoom)
	{
		normalFov = normal;
		zoomFov = zoom;
	}

	public void KickCamera(Vector3 kick)
	{
		fpKickAction.Start();
		kickEuler = kick * 0.7f;
	}
}
