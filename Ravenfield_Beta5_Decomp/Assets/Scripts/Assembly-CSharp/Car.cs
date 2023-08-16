using System;
using UnityEngine;

public class Car : Vehicle
{
	[Serializable]
	public class WheelConfiguration
	{
		public WheelCollider collider;

		public bool motor;

		public bool steer;
	}

	private const float STEER_RATE = 5f;

	private const float MIN_BRAKE_RPM = 10f;

	private const float BRAKE_TORQUE = 300f;

	private const float CAN_TURN_TOWARDS_DISTANCE_BIAS = 1f;

	public float extraStability = 0.5f;

	public float maxTorque = 300f;

	public float maxSteer = 40f;

	public float wheelSteerMultiplier = 3f;

	public float turningRadius = 5f;

	private float steerAngle;

	public Transform steeringWheel;

	public WheelConfiguration[] wheels;

	private float enginePitch;

	protected override void Awake()
	{
		base.Awake();
		rigidbody.centerOfMass += Vector3.down * extraStability;
		WheelConfiguration[] array = wheels;
		foreach (WheelConfiguration wheelConfiguration in array)
		{
			wheelConfiguration.collider.motorTorque = 0f;
			wheelConfiguration.collider.brakeTorque = 120f;
		}
	}

	protected override void DriverEntered()
	{
		base.DriverEntered();
		WheelConfiguration[] array = wheels;
		foreach (WheelConfiguration wheelConfiguration in array)
		{
			wheelConfiguration.collider.brakeTorque = 0f;
		}
		audio.Play();
		enginePitch = 0f;
		audio.pitch = 0f;
	}

	protected override void DriverExited()
	{
		WheelConfiguration[] array = wheels;
		foreach (WheelConfiguration wheelConfiguration in array)
		{
			wheelConfiguration.collider.motorTorque = 0f;
			wheelConfiguration.collider.brakeTorque = 120f;
		}
	}

	private void UpdateVisuals(WheelConfiguration wheel)
	{
		WheelCollider collider = wheel.collider;
		Transform child = collider.transform.GetChild(0);
		Vector3 pos;
		Quaternion quat;
		collider.GetWorldPose(out pos, out quat);
		child.transform.position = pos;
		child.transform.rotation = quat;
	}

	private void Update()
	{
		Vector3 localEulerAngles = steeringWheel.localEulerAngles;
		localEulerAngles.z = steerAngle * wheelSteerMultiplier;
		steeringWheel.localEulerAngles = localEulerAngles;
		float target = ((!HasDriver()) ? 0f : 0.5f);
		if (HasDriver() && !burning)
		{
			Vector2 vector = Vehicle.Clamp2(Driver().controller.CarInput());
			float num = 0f;
			WheelConfiguration[] array = wheels;
			foreach (WheelConfiguration wheelConfiguration in array)
			{
				num += wheelConfiguration.collider.rpm;
			}
			num /= (float)wheels.Length;
			float target2 = vector.x * maxSteer;
			steerAngle = Mathf.MoveTowards(steerAngle, target2, 5f * maxSteer * Time.deltaTime);
			WheelConfiguration[] array2 = wheels;
			foreach (WheelConfiguration wheelConfiguration2 in array2)
			{
				if (wheelConfiguration2.motor)
				{
					wheelConfiguration2.collider.motorTorque = vector.y * maxTorque;
					if ((vector.y < 0f && wheelConfiguration2.collider.rpm > 10f) || (vector.y > 0f && wheelConfiguration2.collider.rpm < -10f))
					{
						wheelConfiguration2.collider.brakeTorque = 300f;
						target = 0.5f;
					}
					else
					{
						wheelConfiguration2.collider.brakeTorque = 0f;
						target = ((!(vector.y > 0f)) ? (0.5f + Mathf.Abs(vector.y) * 0.2f) : (0.5f + Mathf.Abs(vector.y) * 0.6f));
					}
				}
				if (wheelConfiguration2.steer)
				{
					wheelConfiguration2.collider.steerAngle = steerAngle;
				}
			}
		}
		enginePitch = Mathf.MoveTowards(enginePitch, target, Time.deltaTime);
		audio.pitch = enginePitch;
		if (audio.isPlaying && enginePitch == 0f)
		{
			audio.Stop();
		}
	}

	public bool CanTurnTowards(Vector3 deltaPosition)
	{
		if (deltaPosition.magnitude < 4f)
		{
			return true;
		}
		float num = 2f * turningRadius * Vector3.Cross(deltaPosition.normalized, base.transform.forward).magnitude;
		return deltaPosition.magnitude > num + 1f;
	}

	private void LateUpdate()
	{
		WheelConfiguration[] array = wheels;
		foreach (WheelConfiguration wheel in array)
		{
			UpdateVisuals(wheel);
		}
	}

	public override void Die()
	{
		base.Die();
		WheelConfiguration[] array = wheels;
		foreach (WheelConfiguration wheelConfiguration in array)
		{
			wheelConfiguration.collider.gameObject.SetActive(false);
		}
		audio.Stop();
	}
}
