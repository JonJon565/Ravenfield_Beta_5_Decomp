using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

[RequireComponent(typeof(Seeker))]
[HelpURL("http://arongranberg.com/astar/docs/class_a_i_lerp.php")]
[AddComponentMenu("Pathfinding/AI/AISimpleLerp (2D,3D generic)")]
public class AILerp : MonoBehaviour
{
	public float repathRate = 0.5f;

	public Transform target;

	public bool canSearch = true;

	public bool canMove = true;

	public float speed = 3f;

	public bool enableRotation = true;

	public bool rotationIn2D;

	public float rotationSpeed = 10f;

	public bool interpolatePathSwitches = true;

	public float switchPathInterpolationSpeed = 5f;

	protected Seeker seeker;

	protected Transform tr;

	protected float lastRepath = -9999f;

	protected ABPath path;

	protected int currentWaypointIndex;

	protected float distanceAlongSegment;

	protected bool canSearchAgain = true;

	protected Vector3 previousMovementOrigin;

	protected Vector3 previousMovementDirection;

	protected float previousMovementStartTime = -9999f;

	private bool startHasRun;

	public bool targetReached { get; private set; }

	protected virtual void Awake()
	{
		tr = base.transform;
		seeker = GetComponent<Seeker>();
		seeker.startEndModifier.adjustStartPoint = () => tr.position;
	}

	protected virtual void Start()
	{
		startHasRun = true;
		OnEnable();
	}

	protected virtual void OnEnable()
	{
		lastRepath = -9999f;
		canSearchAgain = true;
		if (startHasRun)
		{
			Seeker obj = seeker;
			obj.pathCallback = (OnPathDelegate)Delegate.Combine(obj.pathCallback, new OnPathDelegate(OnPathComplete));
			StartCoroutine(RepeatTrySearchPath());
		}
	}

	public void OnDisable()
	{
		if (seeker != null && !seeker.IsDone())
		{
			seeker.GetCurrentPath().Error();
		}
		if (path != null)
		{
			path.Release(this);
		}
		path = null;
		Seeker obj = seeker;
		obj.pathCallback = (OnPathDelegate)Delegate.Remove(obj.pathCallback, new OnPathDelegate(OnPathComplete));
	}

	protected IEnumerator RepeatTrySearchPath()
	{
		while (true)
		{
			float v = TrySearchPath();
			yield return new WaitForSeconds(v);
		}
	}

	public float TrySearchPath()
	{
		if (Time.time - lastRepath >= repathRate && canSearchAgain && canSearch && target != null)
		{
			SearchPath();
			return repathRate;
		}
		float num = repathRate - (Time.time - lastRepath);
		return (!(num < 0f)) ? num : 0f;
	}

	public virtual void SearchPath()
	{
		ForceSearchPath();
	}

	public virtual void ForceSearchPath()
	{
		if (target == null)
		{
			throw new InvalidOperationException("Target is null");
		}
		lastRepath = Time.time;
		Vector3 position = target.position;
		Vector3 start = GetFeetPosition();
		if (path != null && path.vectorPath.Count > 1)
		{
			start = path.vectorPath[currentWaypointIndex];
		}
		canSearchAgain = false;
		seeker.StartPath(start, position);
	}

	public virtual void OnTargetReached()
	{
	}

	public virtual void OnPathComplete(Path _p)
	{
		ABPath aBPath = _p as ABPath;
		if (aBPath == null)
		{
			throw new Exception("This function only handles ABPaths, do not use special path types");
		}
		canSearchAgain = true;
		aBPath.Claim(this);
		if (aBPath.error)
		{
			aBPath.Release(this);
			return;
		}
		if (interpolatePathSwitches)
		{
			ConfigurePathSwitchInterpolation();
		}
		if (path != null)
		{
			path.Release(this);
		}
		path = aBPath;
		if (path.vectorPath != null && path.vectorPath.Count == 1)
		{
			path.vectorPath.Insert(0, GetFeetPosition());
		}
		targetReached = false;
		ConfigureNewPath();
	}

	protected virtual void ConfigurePathSwitchInterpolation()
	{
		bool flag = path != null && path.vectorPath != null && path.vectorPath.Count > 1;
		bool flag2 = false;
		if (flag)
		{
			flag2 = currentWaypointIndex == path.vectorPath.Count - 1 && distanceAlongSegment >= (path.vectorPath[path.vectorPath.Count - 1] - path.vectorPath[path.vectorPath.Count - 2]).magnitude;
		}
		if (flag && !flag2)
		{
			List<Vector3> vectorPath = path.vectorPath;
			currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 1, vectorPath.Count - 1);
			Vector3 vector = vectorPath[currentWaypointIndex] - vectorPath[currentWaypointIndex - 1];
			float magnitude = vector.magnitude;
			float num = magnitude * Mathf.Clamp01(1f - distanceAlongSegment);
			for (int i = currentWaypointIndex; i < vectorPath.Count - 1; i++)
			{
				num += (vectorPath[i + 1] - vectorPath[i]).magnitude;
			}
			previousMovementOrigin = GetFeetPosition();
			previousMovementDirection = vector.normalized * num;
			previousMovementStartTime = Time.time;
		}
		else
		{
			previousMovementOrigin = Vector3.zero;
			previousMovementDirection = Vector3.zero;
			previousMovementStartTime = -9999f;
		}
	}

	public virtual Vector3 GetFeetPosition()
	{
		return tr.position;
	}

	protected virtual void ConfigureNewPath()
	{
		List<Vector3> vectorPath = path.vectorPath;
		Vector3 feetPosition = GetFeetPosition();
		float num = 0f;
		float num2 = float.PositiveInfinity;
		Vector3 vector = Vector3.zero;
		int num3 = 1;
		for (int i = 0; i < vectorPath.Count - 1; i++)
		{
			float value = VectorMath.ClosestPointOnLineFactor(vectorPath[i], vectorPath[i + 1], feetPosition);
			value = Mathf.Clamp01(value);
			Vector3 vector2 = Vector3.Lerp(vectorPath[i], vectorPath[i + 1], value);
			float sqrMagnitude = (feetPosition - vector2).sqrMagnitude;
			if (sqrMagnitude < num2)
			{
				num2 = sqrMagnitude;
				vector = vectorPath[i + 1] - vectorPath[i];
				num = value * vector.magnitude;
				num3 = i + 1;
			}
		}
		currentWaypointIndex = num3;
		distanceAlongSegment = num;
		if (interpolatePathSwitches && switchPathInterpolationSpeed > 0.01f)
		{
			float num4 = Mathf.Max(0f - Vector3.Dot(previousMovementDirection.normalized, vector.normalized), 0f);
			distanceAlongSegment -= speed * num4 * (1f / switchPathInterpolationSpeed);
		}
	}

	protected virtual void Update()
	{
		if (!canMove)
		{
			return;
		}
		Vector3 direction;
		Vector3 position = CalculateNextPosition(out direction);
		if (enableRotation && direction != Vector3.zero)
		{
			if (rotationIn2D)
			{
				float b = Mathf.Atan2(direction.x, 0f - direction.y) * 57.29578f + 180f;
				Vector3 eulerAngles = tr.eulerAngles;
				eulerAngles.z = Mathf.LerpAngle(eulerAngles.z, b, Time.deltaTime * rotationSpeed);
				tr.eulerAngles = eulerAngles;
			}
			else
			{
				Quaternion rotation = tr.rotation;
				Quaternion b2 = Quaternion.LookRotation(direction);
				tr.rotation = Quaternion.Slerp(rotation, b2, Time.deltaTime * rotationSpeed);
			}
		}
		tr.position = position;
	}

	protected virtual Vector3 CalculateNextPosition(out Vector3 direction)
	{
		if (path == null || path.vectorPath == null || path.vectorPath.Count == 0)
		{
			direction = Vector3.zero;
			return tr.position;
		}
		List<Vector3> vectorPath = path.vectorPath;
		currentWaypointIndex = Mathf.Clamp(currentWaypointIndex, 1, vectorPath.Count - 1);
		Vector3 vector = vectorPath[currentWaypointIndex] - vectorPath[currentWaypointIndex - 1];
		float num = vector.magnitude;
		distanceAlongSegment += Time.deltaTime * speed;
		if (distanceAlongSegment >= num && currentWaypointIndex < vectorPath.Count - 1)
		{
			float num2 = distanceAlongSegment - num;
			Vector3 vector2;
			float magnitude;
			while (true)
			{
				currentWaypointIndex++;
				vector2 = vectorPath[currentWaypointIndex] - vectorPath[currentWaypointIndex - 1];
				magnitude = vector2.magnitude;
				if (num2 <= magnitude || currentWaypointIndex == vectorPath.Count - 1)
				{
					break;
				}
				num2 -= magnitude;
			}
			vector = vector2;
			num = magnitude;
			distanceAlongSegment = num2;
		}
		if (distanceAlongSegment >= num && currentWaypointIndex == vectorPath.Count - 1)
		{
			if (!targetReached)
			{
				OnTargetReached();
			}
			targetReached = true;
		}
		Vector3 vector3 = vector * Mathf.Clamp01((!(num > 0f)) ? 1f : (distanceAlongSegment / num)) + vectorPath[currentWaypointIndex - 1];
		direction = vector;
		if (interpolatePathSwitches)
		{
			Vector3 a = previousMovementOrigin + Vector3.ClampMagnitude(previousMovementDirection, speed * (Time.time - previousMovementStartTime));
			return Vector3.Lerp(a, vector3, switchPathInterpolationSpeed * (Time.time - previousMovementStartTime));
		}
		return vector3;
	}
}
