using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperWrench : Wrench
{
	public Material goldMaterial;

	public ParticleSystem cashParticles;

	protected override void ShootMeleeRay()
	{
		bool flag = false;
		Ray ray = new Ray(configuration.muzzle.position, configuration.muzzle.forward);
		RaycastHit hitInfo;
		if (Physics.SphereCast(ray, radius, out hitInfo, range))
		{
			Rigidbody attachedRigidbody = hitInfo.collider.attachedRigidbody;
			if (attachedRigidbody != null && !attachedRigidbody.isKinematic)
			{
				attachedRigidbody.MovePosition(attachedRigidbody.position + Vector3.up * 0.1f);
				attachedRigidbody.AddForceAtPosition((base.transform.forward + 0.05f * Vector3.up) * force * 0.02f, hitInfo.point, ForceMode.VelocityChange);
				attachedRigidbody.drag = 0.2f;
				attachedRigidbody.angularDrag = 0.2f;
				audio.PlayOneShot(hitSound);
				flag = true;
				HitSomething(hitInfo.collider);
			}
		}
		if (!flag)
		{
			base.ShootMeleeRay();
		}
	}

	protected override void HitSomething(Collider c)
	{
		Transform t = FindHighestParent(c.transform);
		cashParticles.Play();
		StartCoroutine(ApplyMaterial(t));
	}

	private IEnumerator ApplyMaterial(Transform t)
	{
		yield return new WaitForFixedUpdate();
		List<Renderer> renderers = new List<Renderer>();
		renderers.AddRange(t.GetComponentsInChildren<MeshRenderer>());
		renderers.AddRange(t.GetComponentsInChildren<SkinnedMeshRenderer>());
		foreach (Renderer r in renderers)
		{
			Material[] materials = r.materials;
			for (int i = 0; i < materials.Length; i++)
			{
				materials[i] = goldMaterial;
			}
			r.materials = materials;
		}
	}

	private Transform FindHighestParent(Transform t)
	{
		while (t.parent != null)
		{
			t = t.parent;
		}
		return t;
	}
}
