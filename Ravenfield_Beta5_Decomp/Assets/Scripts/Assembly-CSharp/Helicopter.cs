using UnityEngine;

public class Helicopter : Vehicle
{
	private const float ROTOR_SPEED = 1000f;

	private const float ROTOR_SPEED_GAIN = 0.3f;

	private const float MAX_VOLUME = 0.5f;

	private const float BASE_ANGULAR_DRAG = 0.2f;

	private const float BASE_DRAG = 0.05f;

	private const float ANGULAR_DRAG_ALONG_WIND_GAIN = 0.01f;

	private const float DRAG_BROADSIDE_WIND_GAIN = 0.01f;

	private const float ALONG_WIND_LIFT = 0.03f;

	private const float MANOUVERABILITY_SCALE = 0.0069999998f;

	public Transform rotor;

	private Renderer solidRotor;

	private Renderer blurredRotor;

	public float rotorForce = 5f;

	public float manouverability = 1f;

	public float counterForceMultiplier = 0.3f;

	private float rotorSpeed;

	private bool isAirborne;

	private Vector3 randomBurningTorque = Vector3.zero;

	protected override void Awake()
	{
		base.Awake();
		solidRotor = rotor.GetComponent<Renderer>();
		blurredRotor = rotor.GetChild(0).GetComponent<Renderer>();
		rigidbody.maxAngularVelocity = 1.5f;
	}

	private void Update()
	{
		audio.volume = rotorSpeed * 0.5f;
		audio.pitch = rotorSpeed;
		if (HasDriver())
		{
			rotorSpeed = Mathf.Clamp01(rotorSpeed + Time.deltaTime * 0.3f);
			if (base.transform.up.y < 0f)
			{
				Damage(Time.deltaTime * 30f);
			}
		}
		else
		{
			rotorSpeed = Mathf.Clamp01(rotorSpeed - Time.deltaTime * 0.3f);
		}
		bool flag = rotorSpeed > 0.8f;
		solidRotor.enabled = !flag;
		blurredRotor.enabled = flag;
		rotor.Rotate(Vector3.forward * 1000f * rotorSpeed * Time.deltaTime);
		isAirborne = !Physics.Raycast(base.transform.position, Vector3.down, 3f);
	}

	protected override void DriverEntered()
	{
		base.DriverEntered();
	}

	protected override void DriverExited()
	{
		base.DriverExited();
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();
		Vector3 normalized = (base.transform.forward + 0.15f * base.transform.up).normalized;
		float num = Vector3.Dot(normalized, rigidbody.velocity);
		float magnitude = Vector3.Cross(normalized, rigidbody.velocity).magnitude;
		rigidbody.angularDrag = 0.2f + num * 0.01f;
		rigidbody.drag = 0.05f + magnitude * 0.01f;
		rigidbody.AddForce(base.transform.up * num * 0.03f, ForceMode.Acceleration);
		if (HasDriver())
		{
			Vector4 vector = Vehicle.Clamp4(Driver().controller.HelicopterInput()) * rotorSpeed;
			float y = vector.y;
			Vector3 vector2 = new Vector3(vector.w, vector.x, 0f - vector.z) * manouverability * 0.0069999998f;
			Vector3 normalized2 = (base.transform.up + base.transform.forward * 0.05f).normalized;
			Vector3 vector3 = Vector3.Project(normalized2, Vector3.up);
			normalized2 = (normalized2 - 0.05f * vector3).normalized;
			float t = Mathf.Clamp01(0f - Vector3.Dot(normalized2, rigidbody.velocity.normalized));
			float num2 = 1f + Mathf.Lerp(0f, counterForceMultiplier, t);
			if (burning)
			{
				rigidbody.AddForce(0.3f * normalized2 * (y * rotorForce * num2 - Physics.gravity.y - 0.5f), ForceMode.Acceleration);
				rigidbody.AddRelativeTorque(randomBurningTorque + 0.5f * vector2, ForceMode.VelocityChange);
			}
			else
			{
				rigidbody.AddForce(normalized2 * (y * rotorForce * num2 - Physics.gravity.y - 0.5f), ForceMode.Acceleration);
				rigidbody.AddRelativeTorque(vector2, ForceMode.VelocityChange);
			}
		}
	}

	protected override void StartBurning()
	{
		base.StartBurning();
		randomBurningTorque = Random.insideUnitSphere * 0.005f + Vector3.up * 0.025f;
	}

	public override void Die()
	{
		base.Die();
		rotor.gameObject.SetActive(false);
		audio.Stop();
	}

	public override bool ShouldBeAvoided()
	{
		return !isAirborne && base.ShouldBeAvoided();
	}
}
