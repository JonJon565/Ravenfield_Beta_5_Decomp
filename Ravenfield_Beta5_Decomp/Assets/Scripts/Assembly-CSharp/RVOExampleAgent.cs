using System.Collections.Generic;
using Pathfinding;
using Pathfinding.RVO;
using UnityEngine;

[HelpURL("http://arongranberg.com/astar/docs/class_r_v_o_example_agent.php")]
public class RVOExampleAgent : MonoBehaviour
{
	public float repathRate = 1f;

	private float nextRepath;

	private Vector3 target;

	private bool canSearchAgain = true;

	private RVOController controller;

	private Path path;

	private List<Vector3> vectorPath;

	private int wp;

	public float moveNextDist = 1f;

	private Seeker seeker;

	private MeshRenderer[] rends;

	public void Awake()
	{
		seeker = GetComponent<Seeker>();
	}

	public void Start()
	{
		SetTarget(-base.transform.position);
		controller = GetComponent<RVOController>();
	}

	public void SetTarget(Vector3 target)
	{
		this.target = target;
		RecalculatePath();
	}

	public void SetColor(Color col)
	{
		if (rends == null)
		{
			rends = GetComponentsInChildren<MeshRenderer>();
		}
		MeshRenderer[] array = rends;
		foreach (MeshRenderer meshRenderer in array)
		{
			Color color = meshRenderer.material.GetColor("_TintColor");
			AnimationCurve curve = AnimationCurve.Linear(0f, color.r, 1f, col.r);
			AnimationCurve curve2 = AnimationCurve.Linear(0f, color.g, 1f, col.g);
			AnimationCurve curve3 = AnimationCurve.Linear(0f, color.b, 1f, col.b);
			AnimationClip animationClip = new AnimationClip();
			animationClip.legacy = true;
			animationClip.SetCurve(string.Empty, typeof(Material), "_TintColor.r", curve);
			animationClip.SetCurve(string.Empty, typeof(Material), "_TintColor.g", curve2);
			animationClip.SetCurve(string.Empty, typeof(Material), "_TintColor.b", curve3);
			Animation animation = meshRenderer.gameObject.GetComponent<Animation>();
			if (animation == null)
			{
				animation = meshRenderer.gameObject.AddComponent<Animation>();
			}
			animationClip.wrapMode = WrapMode.Once;
			animation.AddClip(animationClip, "ColorAnim");
			animation.Play("ColorAnim");
		}
	}

	public void RecalculatePath()
	{
		canSearchAgain = false;
		nextRepath = Time.time + repathRate * (Random.value + 0.5f);
		seeker.StartPath(base.transform.position, target, OnPathComplete);
	}

	public void OnPathComplete(Path _p)
	{
		ABPath aBPath = _p as ABPath;
		canSearchAgain = true;
		if (path != null)
		{
			path.Release(this);
		}
		path = aBPath;
		aBPath.Claim(this);
		if (aBPath.error)
		{
			wp = 0;
			vectorPath = null;
			return;
		}
		Vector3 originalStartPoint = aBPath.originalStartPoint;
		Vector3 position = base.transform.position;
		originalStartPoint.y = position.y;
		float magnitude = (position - originalStartPoint).magnitude;
		wp = 0;
		vectorPath = aBPath.vectorPath;
		for (float num = 0f; num <= magnitude; num += moveNextDist * 0.6f)
		{
			wp--;
			Vector3 vector = originalStartPoint + (position - originalStartPoint) * num;
			Vector3 vector2;
			do
			{
				wp++;
				vector2 = vectorPath[wp];
				vector2.y = vector.y;
			}
			while ((vector - vector2).sqrMagnitude < moveNextDist * moveNextDist && wp != vectorPath.Count - 1);
		}
	}

	public void Update()
	{
		if (Time.time >= nextRepath && canSearchAgain)
		{
			RecalculatePath();
		}
		Vector3 vel = Vector3.zero;
		Vector3 position = base.transform.position;
		if (vectorPath != null && vectorPath.Count != 0)
		{
			Vector3 vector = vectorPath[wp];
			vector.y = position.y;
			while ((position - vector).sqrMagnitude < moveNextDist * moveNextDist && wp != vectorPath.Count - 1)
			{
				wp++;
				vector = vectorPath[wp];
				vector.y = position.y;
			}
			vel = vector - position;
			float magnitude = vel.magnitude;
			if (magnitude > 0f)
			{
				float num = Mathf.Min(magnitude, controller.maxSpeed);
				vel *= num / magnitude;
			}
		}
		controller.Move(vel);
	}
}
