using System;
using UnityEngine;

public class Boat : Vehicle
{
	public float floatAcceleration = 10f;

	public float floatDepth = 0.5f;

	public float speed = 5f;

	public float turnSpeed = 5f;

	public float stability = 1f;

	public Transform[] floatingSamplers;

	[NonSerialized]
	public bool inWater;

	private float audioPitch = 1f;

	protected override void Awake()
	{
		base.Awake();
		rigidbody.centerOfMass = Vector3.down * stability;
	}

	protected override void DriverEntered()
	{
		base.DriverEntered();
		audio.Play();
		audio.pitch = 0f;
		audioPitch = 0f;
	}

	protected override void DriverExited()
	{
		base.DriverExited();
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();
		int num = 0;
		Transform[] array = floatingSamplers;
		foreach (Transform transform in array)
		{
			if (WaterLevel.InWater(transform.position))
			{
				float num2 = Mathf.Clamp01(WaterLevel.Depth(transform.position) / floatDepth) / (float)floatingSamplers.Length;
				rigidbody.AddForceAtPosition(Vector3.up * floatAcceleration * num2, transform.position + Vector3.up, ForceMode.Acceleration);
				num++;
			}
		}
		inWater = num >= 3;
		float target = ((!HasDriver()) ? 0f : 0.7f);
		if (inWater && HasDriver())
		{
			Vector2 vector = Driver().controller.BoatInput();
			if (vector.y < 0f)
			{
				vector.y *= 0.15f;
			}
			rigidbody.AddForce(base.transform.forward.ToGround().normalized * speed * vector.y, ForceMode.Acceleration);
			rigidbody.AddRelativeTorque(base.transform.up * turnSpeed * vector.x, ForceMode.Acceleration);
			target = 1f + Mathf.Clamp01(Mathf.Abs(vector.y) + Mathf.Abs(vector.x) * 0.5f);
		}
		audioPitch = Mathf.MoveTowards(audioPitch, target, Time.fixedDeltaTime);
		audio.pitch = audioPitch;
		if (audio.isPlaying && audioPitch == 0f)
		{
			audio.Stop();
		}
	}
}
