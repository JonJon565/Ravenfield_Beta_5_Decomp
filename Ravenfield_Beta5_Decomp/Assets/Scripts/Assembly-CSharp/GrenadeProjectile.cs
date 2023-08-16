using UnityEngine;

public class GrenadeProjectile : Projectile
{
	private const int LAYER_MASK = 4097;

	private const float CLEANUP_TIME = 10f;

	private const float ROTATION_SPEED_MAGNITUDE = 400f;

	public ExplodingProjectile.ExplosionConfiguration explosionConfiguration;

	public Renderer[] renderers;

	public float radius = 0.1f;

	public float bounciness = 0.5f;

	public float bounceDrag = 0.2f;

	private Vector3 rotationAxis;

	private float angularSpeed;

	protected override void Start()
	{
		velocity = base.transform.forward * configuration.speed;
		rotationAxis = Random.insideUnitSphere.normalized;
		angularSpeed = 400f;
		Invoke("Explode", configuration.lifetime);
	}

	protected override void Update()
	{
		velocity += Physics.gravity * Time.deltaTime;
		Vector3 vector = velocity * Time.deltaTime;
		Ray ray = new Ray(base.transform.position, vector);
		RaycastHit hitInfo;
		if (Physics.SphereCast(ray, radius, out hitInfo, vector.magnitude * 2f, 4097))
		{
			base.transform.position = hitInfo.point + hitInfo.normal * (radius + 0.01f);
			Vector3 vector2 = Vector3.Project(velocity, hitInfo.normal);
			velocity -= vector2 * (bounciness + 1f);
			Vector3 vector3 = velocity * bounceDrag;
			velocity -= vector3;
			rotationAxis = base.transform.worldToLocalMatrix.MultiplyVector((Vector3.Cross(vector3, Vector3.up) + rotationAxis).normalized);
			angularSpeed = (0f - vector3.magnitude) * 400f;
		}
		else
		{
			base.transform.position += vector;
		}
		base.transform.Rotate(rotationAxis, angularSpeed * Time.deltaTime);
	}

	protected virtual void Explode()
	{
		if (ActorManager.Explode(base.transform.position, explosionConfiguration) && !source.aiControlled)
		{
			IngameUi.Hit();
		}
		base.transform.rotation = Quaternion.LookRotation(Vector3.up);
		Ray ray = new Ray(base.transform.position, Vector3.down);
		RaycastHit hitInfo;
		if (Physics.Raycast(ray, out hitInfo, 1f, 1))
		{
			DecalManager.AddDecal(hitInfo.point, hitInfo.normal, Random.Range(1f, 2f), DecalManager.DecalType.Impact);
		}
		base.enabled = false;
		Renderer[] array = renderers;
		foreach (Renderer renderer in array)
		{
			renderer.enabled = false;
		}
		GetComponent<ParticleSystem>().Play(true);
		AudioSource component = GetComponent<AudioSource>();
		component.pitch = Random.Range(0.9f, 1.1f);
		component.Play();
		Invoke("Cleanup", 10f);
	}

	private void Cleanup()
	{
		Object.Destroy(base.gameObject);
	}
}
