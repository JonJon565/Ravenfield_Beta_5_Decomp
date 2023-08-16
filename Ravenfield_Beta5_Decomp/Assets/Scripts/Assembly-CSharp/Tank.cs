using System.Collections;
using UnityEngine;

public class Tank : Vehicle
{
	private const float DEFAULT_WHEEL_HEIGHT = 0.743f;

	private const float DEFAULT_TRACK_HEIGHT = 0.7f;

	private const float WHEEL_DISTANCE = 4.13f;

	public WheelCollider[] wheelCollidersLeft;

	public WheelCollider[] wheelCollidersRight;

	public Transform[] autoDestroyOnKill;

	public Transform tracksLeft;

	public Transform tracksRight;

	public Joint towerJoint;

	public float maxTorque = 7000f;

	public float driveStiffness = 3f;

	public float extraStability = 1f;

	public UvOffset trackOffsetLeft;

	public UvOffset trackOffsetRight;

	public float trackUvSpeedMultiplier = 1f;

	public Rigidbody towerRigidbody;

	public OwnerIndicator ownerIndicator;

	private float enginePitch;

	protected override void Awake()
	{
		base.Awake();
		rigidbody.centerOfMass += Vector3.down * extraStability;
	}

	private void Update()
	{
		UpdateTrack(tracksLeft, wheelCollidersLeft, trackOffsetLeft);
		UpdateTrack(tracksRight, wheelCollidersRight, trackOffsetRight);
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!dead)
		{
			UpdateMovement();
		}
	}

	protected override void DriverEntered()
	{
		base.DriverEntered();
		ownerIndicator.SetOwner(Driver().team);
	}

	protected override void DriverExited()
	{
		base.DriverExited();
		ownerIndicator.SetOwner(-1);
	}

	private void UpdateMovement()
	{
		bool flag = true;
		float motorTorque = 0f;
		float motorTorque2 = 0f;
		float brakeTorque = 3f;
		WheelFrictionCurve forwardFriction = wheelCollidersLeft[0].forwardFriction;
		forwardFriction.stiffness = 1f;
		float target = 0f;
		if (HasDriver() && !burning)
		{
			Vector2 vector = Driver().controller.CarInput();
			if (vector != Vector2.zero)
			{
				flag = false;
				motorTorque = Mathf.Clamp(vector.y * maxTorque + vector.x * maxTorque, 0f - maxTorque, maxTorque);
				motorTorque2 = Mathf.Clamp(vector.y * maxTorque - vector.x * maxTorque, 0f - maxTorque, maxTorque);
				forwardFriction.stiffness = driveStiffness;
				brakeTorque = 0f;
			}
			target = Mathf.Clamp01(0.7f + Mathf.Abs(vector.x) + Mathf.Abs(vector.y));
		}
		enginePitch = Mathf.MoveTowards(enginePitch, target, 0.4f * Time.fixedDeltaTime);
		audio.pitch = enginePitch;
		WheelCollider[] array = wheelCollidersLeft;
		foreach (WheelCollider wheelCollider in array)
		{
			wheelCollider.motorTorque = motorTorque;
			wheelCollider.brakeTorque = brakeTorque;
			wheelCollider.forwardFriction = forwardFriction;
		}
		WheelCollider[] array2 = wheelCollidersRight;
		foreach (WheelCollider wheelCollider2 in array2)
		{
			wheelCollider2.motorTorque = motorTorque2;
			wheelCollider2.brakeTorque = brakeTorque;
			wheelCollider2.forwardFriction = forwardFriction;
		}
	}

	private void UpdateTrack(Transform track, WheelCollider[] wheels, UvOffset offset)
	{
		Vector3 pos;
		Quaternion quat;
		wheels[0].GetWorldPose(out pos, out quat);
		Vector3 pos2;
		wheels[1].GetWorldPose(out pos2, out quat);
		float num = (wheels[0].rpm * wheels[0].radius + wheels[1].rpm * wheels[1].radius) / 2f;
		float num2 = base.transform.worldToLocalMatrix.MultiplyPoint(pos).y - 0.743f;
		float num3 = base.transform.worldToLocalMatrix.MultiplyPoint(pos2).y - 0.743f;
		Vector3 localPosition = track.transform.localPosition;
		localPosition.y = (num2 + num3) / 2f + 0.7f;
		track.transform.localPosition = localPosition;
		track.transform.localEulerAngles = new Vector3(-90f - Mathf.Atan2(num2 - num3, 4.13f) * 57.29578f, 0f, 0f);
		offset.IncrementOffset(Vector2.right * num * trackUvSpeedMultiplier * Time.deltaTime);
	}

	public override void Die()
	{
		base.Die();
		Transform[] array = autoDestroyOnKill;
		foreach (Transform transform in array)
		{
			Object.Destroy(transform.gameObject);
		}
		WheelCollider[] array2 = wheelCollidersRight;
		foreach (WheelCollider wheelCollider in array2)
		{
			wheelCollider.motorTorque = 0f;
			wheelCollider.brakeTorque = 100f;
		}
		WheelCollider[] array3 = wheelCollidersLeft;
		foreach (WheelCollider wheelCollider2 in array3)
		{
			wheelCollider2.motorTorque = 0f;
			wheelCollider2.brakeTorque = 100f;
		}
	}

	protected override void Explode()
	{
		base.Explode();
		Object.Destroy(towerJoint);
		StartCoroutine(ApplyExplosionForce());
	}

	private IEnumerator ApplyExplosionForce()
	{
		yield return new WaitForFixedUpdate();
		rigidbody.AddForce(Vector3.up * 3f + Random.insideUnitSphere * 2f, ForceMode.VelocityChange);
		towerRigidbody.AddForce(Vector3.up * 25f + Random.insideUnitSphere * 6f, ForceMode.VelocityChange);
		towerRigidbody.AddTorque(Random.insideUnitSphere * 10f, ForceMode.VelocityChange);
	}
}
