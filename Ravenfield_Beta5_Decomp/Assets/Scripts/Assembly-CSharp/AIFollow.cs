using System;
using System.Collections;
using Pathfinding;
using UnityEngine;

[HelpURL("http://arongranberg.com/astar/docs/class_a_i_follow.php")]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Seeker))]
[AddComponentMenu("Pathfinding/AI/AIFollow (deprecated)")]
public class AIFollow : MonoBehaviour
{
	public Transform target;

	public float repathRate = 0.1f;

	public float pickNextWaypointDistance = 1f;

	public float targetReached = 0.2f;

	public float speed = 5f;

	public float rotationSpeed = 1f;

	public bool drawGizmos;

	public bool canSearch = true;

	public bool canMove = true;

	protected Seeker seeker;

	protected CharacterController controller;

	protected Transform tr;

	protected float lastPathSearch = -9999f;

	protected int pathIndex;

	protected Vector3[] path;

	public void Start()
	{
		seeker = GetComponent<Seeker>();
		controller = GetComponent<CharacterController>();
		tr = base.transform;
		Repath();
	}

	public void Reset()
	{
		path = null;
	}

	public void OnPathComplete(Path p)
	{
		StartCoroutine(WaitToRepath());
		if (p.error)
		{
			return;
		}
		path = p.vectorPath.ToArray();
		float num = float.PositiveInfinity;
		int num2 = 0;
		for (int i = 0; i < path.Length - 1; i++)
		{
			float num3 = VectorMath.SqrDistancePointSegment(path[i], path[i + 1], tr.position);
			if (num3 < num)
			{
				num2 = 0;
				num = num3;
				pathIndex = i + 1;
			}
			else if (num2 > 6)
			{
				break;
			}
		}
	}

	public IEnumerator WaitToRepath()
	{
		float timeLeft = repathRate - (Time.time - lastPathSearch);
		yield return new WaitForSeconds(timeLeft);
		Repath();
	}

	public void Stop()
	{
		canMove = false;
		canSearch = false;
	}

	public void Resume()
	{
		canMove = true;
		canSearch = true;
	}

	public virtual void Repath()
	{
		lastPathSearch = Time.time;
		if (seeker == null || target == null || !canSearch || !seeker.IsDone())
		{
			StartCoroutine(WaitToRepath());
			return;
		}
		Path p = ABPath.Construct(base.transform.position, target.position);
		seeker.StartPath(p, OnPathComplete);
	}

	public void PathToTarget(Vector3 targetPoint)
	{
		lastPathSearch = Time.time;
		if (!(seeker == null))
		{
			seeker.StartPath(base.transform.position, targetPoint, OnPathComplete);
		}
	}

	public virtual void ReachedEndOfPath()
	{
	}

	public void Update()
	{
		if (path == null || pathIndex >= path.Length || pathIndex < 0 || !canMove)
		{
			return;
		}
		Vector3 vector = path[pathIndex];
		vector.y = tr.position.y;
		while ((vector - tr.position).sqrMagnitude < pickNextWaypointDistance * pickNextWaypointDistance)
		{
			pathIndex++;
			if (pathIndex >= path.Length)
			{
				if ((vector - tr.position).sqrMagnitude < pickNextWaypointDistance * targetReached * (pickNextWaypointDistance * targetReached))
				{
					ReachedEndOfPath();
					return;
				}
				pathIndex--;
				break;
			}
			vector = path[pathIndex];
			vector.y = tr.position.y;
		}
		Vector3 forward = vector - tr.position;
		tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.LookRotation(forward), rotationSpeed * Time.deltaTime);
		tr.eulerAngles = new Vector3(0f, tr.eulerAngles.y, 0f);
		Vector3 forward2 = base.transform.forward;
		forward2 *= speed;
		forward2 *= Mathf.Clamp01(Vector3.Dot(forward.normalized, tr.forward));
		if (controller != null)
		{
			controller.SimpleMove(forward2);
		}
		else
		{
			base.transform.Translate(forward2 * Time.deltaTime, Space.World);
		}
	}

	public void OnDrawGizmos()
	{
		if (drawGizmos && path != null && pathIndex < path.Length && pathIndex >= 0)
		{
			Vector3 vector = path[pathIndex];
			vector.y = tr.position.y;
			Debug.DrawLine(base.transform.position, vector, Color.blue);
			float num = pickNextWaypointDistance;
			if (pathIndex == path.Length - 1)
			{
				num *= targetReached;
			}
			Vector3 start = vector + num * new Vector3(1f, 0f, 0f);
			for (float num2 = 0f; (double)num2 < Math.PI * 2.0; num2 += 0.1f)
			{
				Vector3 vector2 = vector + new Vector3((float)Math.Cos(num2) * num, 0f, (float)Math.Sin(num2) * num);
				Debug.DrawLine(start, vector2, Color.yellow);
				start = vector2;
			}
			Debug.DrawLine(start, vector + num * new Vector3(1f, 0f, 0f), Color.yellow);
		}
	}
}
