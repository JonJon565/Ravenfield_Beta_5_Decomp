using UnityEngine;

public class BloodParticle : MonoBehaviour
{
	private const int LAYER_MASK = 1;

	private const float LIFETIME = 5f;

	private float expires;

	public Vector3 velocity;

	public int team;

	private void Start()
	{
		expires = Time.time + 5f;
	}

	private void Update()
	{
		if (Time.time > expires)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		velocity += Physics.gravity * Time.deltaTime;
		Vector3 vector = velocity * Time.deltaTime;
		Ray ray = new Ray(base.transform.position, vector.normalized);
		RaycastHit hitInfo;
		if (Physics.Raycast(ray, out hitInfo, vector.magnitude, 1))
		{
			DecalManager.AddDecal(hitInfo.point, hitInfo.normal, Random.Range(0.7f, 2.5f), (team == 0) ? DecalManager.DecalType.BloodBlue : DecalManager.DecalType.BloodRed);
			Object.Destroy(base.gameObject);
		}
		else
		{
			base.transform.position += vector;
		}
	}
}
