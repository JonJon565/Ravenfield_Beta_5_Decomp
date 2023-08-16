using System;
using UnityEngine;

public class JavelinMissile : Rocket
{
	private const float TURN_SPEED = 300f;

	private const float TARGET_ALTITUDE = 200f;

	private const float ACCELERATION = 110f;

	private const float ACCURATE_ACCELERATION = 160f;

	private const float DIVE_DISTANCE = 50f;

	private const float VELOCITY_COMPENSATION = 0.3f;

	public float ejectSpeed = 10f;

	[NonSerialized]
	public Vector3 targetPoint;

	[NonSerialized]
	public Transform target;

	private bool diving;

	private bool thrustEnabled;

	private bool missing;

	private Action thrustStartAction = new Action(0.5f);

	private Action cannotMissDiveAction = new Action(1f);

	private Action inaccurateDiveAction = new Action(3f);

	public float damage = 800f;

	public float divingDamage = 1500f;

	public AudioClip flightSound;

	protected override void Start()
	{
		base.Start();
		velocity = base.transform.forward * ejectSpeed + source.Velocity() * 0.9f;
		thrustStartAction.Start();
		inaccurateDiveAction.Start();
		light.enabled = false;
		trailParticles.Stop(true);
	}

	protected override void Update()
	{
		if (thrustStartAction.TrueDone())
		{
			if (!thrustEnabled)
			{
				light.enabled = true;
				trailParticles.Play(true);
				thrustEnabled = true;
				audioSource.PlayOneShot(flightSound);
			}
			Vector3 vector = ((!(target == null)) ? target.position : targetPoint);
			Vector3 rhs = vector - base.transform.position;
			Vector3 vector2 = Vector3.zero;
			if (!diving)
			{
				rhs.y = 0f;
				float value = 200f - base.transform.position.y;
				vector2 = (rhs.normalized + Vector3.up * Mathf.Clamp(value, 0f, 3f)).normalized * configuration.speed;
				if (rhs.magnitude < 50f)
				{
					StartDiving();
				}
			}
			else if (!missing)
			{
				vector2 = (rhs.normalized - velocity.normalized * 0.3f).normalized * configuration.speed;
				if (target != null && cannotMissDiveAction.Done() && Vector3.Dot(velocity, rhs) < 0f)
				{
					missing = true;
				}
			}
			if (!missing)
			{
				float num = ((!diving || !inaccurateDiveAction.TrueDone()) ? 110f : 160f);
				velocity = Vector3.MoveTowards(velocity, vector2, 110f * Time.deltaTime);
			}
			else
			{
				velocity += Physics.gravity * Time.deltaTime;
			}
			bool flag = diving && Vector3.Dot(base.transform.forward, Vector3.down) > 0.8f;
			configuration.damage = ((!flag) ? damage : divingDamage);
			base.transform.rotation = Quaternion.RotateTowards(base.transform.rotation, Quaternion.LookRotation(velocity), 300f * Time.deltaTime);
		}
		base.Update();
	}

	public void ForceDirectMode()
	{
		StartDiving();
	}

	private void StartDiving()
	{
		diving = true;
		cannotMissDiveAction.Start();
	}
}
