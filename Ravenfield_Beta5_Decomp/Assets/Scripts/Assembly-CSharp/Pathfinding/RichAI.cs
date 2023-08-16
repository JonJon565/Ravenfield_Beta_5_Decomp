using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding.RVO;
using UnityEngine;

namespace Pathfinding
{
	[RequireComponent(typeof(Seeker))]
	[AddComponentMenu("Pathfinding/AI/RichAI (3D, for navmesh)")]
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_rich_a_i.php")]
	public class RichAI : MonoBehaviour
	{
		public Transform target;

		public bool drawGizmos = true;

		public bool repeatedlySearchPaths;

		public float repathRate = 0.5f;

		public float maxSpeed = 1f;

		public float acceleration = 5f;

		public float slowdownTime = 0.5f;

		public float rotationSpeed = 360f;

		public float endReachedDistance = 0.01f;

		public float wallForce = 3f;

		public float wallDist = 1f;

		public Vector3 gravity = new Vector3(0f, -9.82f, 0f);

		public bool raycastingForGroundPlacement = true;

		public LayerMask groundMask = -1;

		public float centerOffset = 1f;

		public RichFunnel.FunnelSimplification funnelSimplification;

		public Animation anim;

		public bool preciseSlowdown = true;

		public bool slowWhenNotFacingTarget = true;

		private Vector3 velocity;

		protected RichPath rp;

		protected Seeker seeker;

		protected Transform tr;

		private CharacterController controller;

		private RVOController rvoController;

		private Vector3 lastTargetPoint;

		private Vector3 currentTargetDirection;

		protected bool waitingForPathCalc;

		protected bool canSearchPath;

		protected bool delayUpdatePath;

		protected bool traversingSpecialPath;

		protected bool lastCorner;

		private float distanceToWaypoint = 999f;

		protected List<Vector3> buffer = new List<Vector3>();

		protected List<Vector3> wallBuffer = new List<Vector3>();

		private bool startHasRun;

		protected float lastRepath = -9999f;

		private static float deltaTime;

		public static readonly Color GizmoColorRaycast = new Color(0.4627451f, 0.80784315f, 0.4392157f);

		public static readonly Color GizmoColorPath = new Color(0.03137255f, 26f / 85f, 0.7607843f);

		public Vector3 Velocity
		{
			get
			{
				return velocity;
			}
		}

		public bool TraversingSpecial
		{
			get
			{
				return traversingSpecialPath;
			}
		}

		public Vector3 TargetPoint
		{
			get
			{
				return lastTargetPoint;
			}
		}

		public bool ApproachingPartEndpoint
		{
			get
			{
				return lastCorner;
			}
		}

		public bool ApproachingPathEndpoint
		{
			get
			{
				return rp != null && ApproachingPartEndpoint && !rp.PartsLeft();
			}
		}

		public float DistanceToNextWaypoint
		{
			get
			{
				return distanceToWaypoint;
			}
		}

		private void Awake()
		{
			seeker = GetComponent<Seeker>();
			controller = GetComponent<CharacterController>();
			rvoController = GetComponent<RVOController>();
			if (rvoController != null)
			{
				rvoController.enableRotation = false;
			}
			tr = base.transform;
		}

		protected virtual void Start()
		{
			startHasRun = true;
			OnEnable();
		}

		protected virtual void OnEnable()
		{
			lastRepath = -9999f;
			waitingForPathCalc = false;
			canSearchPath = true;
			if (startHasRun)
			{
				Seeker obj = seeker;
				obj.pathCallback = (OnPathDelegate)Delegate.Combine(obj.pathCallback, new OnPathDelegate(OnPathComplete));
				StartCoroutine(SearchPaths());
			}
		}

		public void OnDisable()
		{
			if (seeker != null && !seeker.IsDone())
			{
				seeker.GetCurrentPath().Error();
			}
			Seeker obj = seeker;
			obj.pathCallback = (OnPathDelegate)Delegate.Remove(obj.pathCallback, new OnPathDelegate(OnPathComplete));
		}

		public virtual void UpdatePath()
		{
			canSearchPath = true;
			waitingForPathCalc = false;
			Path currentPath = seeker.GetCurrentPath();
			if (currentPath != null && !seeker.IsDone())
			{
				currentPath.Error();
				currentPath.Claim(this);
				currentPath.Release(this);
			}
			waitingForPathCalc = true;
			lastRepath = Time.time;
			seeker.StartPath(tr.position, target.position);
		}

		private IEnumerator SearchPaths()
		{
			while (true)
			{
				if (!repeatedlySearchPaths || waitingForPathCalc || !canSearchPath || Time.time - lastRepath < repathRate)
				{
					yield return null;
					continue;
				}
				UpdatePath();
				yield return null;
			}
		}

		private void OnPathComplete(Path p)
		{
			waitingForPathCalc = false;
			p.Claim(this);
			if (p.error)
			{
				p.Release(this);
				return;
			}
			if (traversingSpecialPath)
			{
				delayUpdatePath = true;
			}
			else
			{
				if (rp == null)
				{
					rp = new RichPath();
				}
				rp.Initialize(seeker, p, true, funnelSimplification);
			}
			p.Release(this);
		}

		private void NextPart()
		{
			rp.NextPart();
			lastCorner = false;
			if (!rp.PartsLeft())
			{
				OnTargetReached();
			}
		}

		protected virtual void OnTargetReached()
		{
		}

		protected virtual Vector3 UpdateTarget(RichFunnel fn)
		{
			buffer.Clear();
			Vector3 position = tr.position;
			bool requiresRepath;
			position = fn.Update(position, buffer, 2, out lastCorner, out requiresRepath);
			if (requiresRepath && !waitingForPathCalc)
			{
				UpdatePath();
			}
			return position;
		}

		protected virtual void Update()
		{
			deltaTime = Mathf.Min(Time.smoothDeltaTime * 2f, Time.deltaTime);
			if (rp != null)
			{
				RichPathPart currentPart = rp.GetCurrentPart();
				RichFunnel richFunnel = currentPart as RichFunnel;
				if (richFunnel != null)
				{
					Vector3 vector = UpdateTarget(richFunnel);
					if (Time.frameCount % 5 == 0 && wallForce > 0f && wallDist > 0f)
					{
						wallBuffer.Clear();
						richFunnel.FindWalls(wallBuffer, wallDist);
					}
					int num = 0;
					Vector3 vector2 = buffer[num];
					Vector3 lhs = vector2 - vector;
					lhs.y = 0f;
					if (Vector3.Dot(lhs, currentTargetDirection) < 0f && buffer.Count - num > 1)
					{
						num++;
						vector2 = buffer[num];
					}
					if (vector2 != lastTargetPoint)
					{
						currentTargetDirection = vector2 - vector;
						currentTargetDirection.y = 0f;
						currentTargetDirection.Normalize();
						lastTargetPoint = vector2;
					}
					lhs = vector2 - vector;
					lhs.y = 0f;
					float num2 = (distanceToWaypoint = lhs.magnitude);
					lhs = ((num2 != 0f) ? (lhs / num2) : Vector3.zero);
					Vector3 lhs2 = lhs;
					Vector3 vector3 = Vector3.zero;
					if (wallForce > 0f && wallDist > 0f)
					{
						float num3 = 0f;
						float num4 = 0f;
						for (int i = 0; i < wallBuffer.Count; i += 2)
						{
							Vector3 vector4 = VectorMath.ClosestPointOnSegment(wallBuffer[i], wallBuffer[i + 1], tr.position);
							float sqrMagnitude = (vector4 - vector).sqrMagnitude;
							if (!(sqrMagnitude > wallDist * wallDist))
							{
								Vector3 normalized = (wallBuffer[i + 1] - wallBuffer[i]).normalized;
								float num5 = Vector3.Dot(lhs, normalized) * (1f - Math.Max(0f, 2f * (sqrMagnitude / (wallDist * wallDist)) - 1f));
								if (num5 > 0f)
								{
									num4 = Math.Max(num4, num5);
								}
								else
								{
									num3 = Math.Max(num3, 0f - num5);
								}
							}
						}
						Vector3 vector5 = Vector3.Cross(Vector3.up, lhs);
						vector3 = vector5 * (num4 - num3);
					}
					bool flag = lastCorner && buffer.Count - num == 1;
					if (flag)
					{
						if (slowdownTime < 0.001f)
						{
							slowdownTime = 0.001f;
						}
						Vector3 vector6 = vector2 - vector;
						vector6.y = 0f;
						lhs = ((!preciseSlowdown) ? (2f * (vector6 - slowdownTime * velocity) / (slowdownTime * slowdownTime)) : ((6f * vector6 - 4f * slowdownTime * velocity) / (slowdownTime * slowdownTime)));
						lhs = Vector3.ClampMagnitude(lhs, acceleration);
						vector3 *= Math.Min(num2 / 0.5f, 1f);
						if (num2 < endReachedDistance)
						{
							NextPart();
						}
					}
					else
					{
						lhs *= acceleration;
					}
					velocity += (lhs + vector3 * wallForce) * deltaTime;
					if (slowWhenNotFacingTarget)
					{
						float a = (Vector3.Dot(lhs2, tr.forward) + 0.5f) * (2f / 3f);
						float a2 = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
						float y = velocity.y;
						velocity.y = 0f;
						float num6 = Mathf.Min(a2, maxSpeed * Mathf.Max(a, 0.2f));
						velocity = Vector3.Lerp(tr.forward * num6, velocity.normalized * num6, Mathf.Clamp((!flag) ? 0f : (num2 * 2f), 0.5f, 1f));
						velocity.y = y;
					}
					else
					{
						float num7 = Mathf.Sqrt(velocity.x * velocity.x + velocity.z * velocity.z);
						num7 = maxSpeed / num7;
						if (num7 < 1f)
						{
							velocity.x *= num7;
							velocity.z *= num7;
						}
					}
					if (flag)
					{
						Vector3 trotdir = Vector3.Lerp(velocity, currentTargetDirection, Math.Max(1f - num2 * 2f, 0f));
						RotateTowards(trotdir);
					}
					else
					{
						RotateTowards(velocity);
					}
					velocity += deltaTime * gravity;
					if (rvoController != null && rvoController.enabled)
					{
						tr.position = vector;
						rvoController.Move(velocity);
					}
					else if (controller != null && controller.enabled)
					{
						tr.position = vector;
						controller.Move(velocity * deltaTime);
					}
					else
					{
						float y2 = vector.y;
						vector += velocity * deltaTime;
						vector = RaycastPosition(vector, y2);
						tr.position = vector;
					}
				}
				else if (rvoController != null && rvoController.enabled)
				{
					rvoController.Move(Vector3.zero);
				}
				if (currentPart is RichSpecial && !traversingSpecialPath)
				{
					StartCoroutine(TraverseSpecial(currentPart as RichSpecial));
				}
			}
			else if (rvoController != null && rvoController.enabled)
			{
				rvoController.Move(Vector3.zero);
			}
			else if (!(controller != null) || !controller.enabled)
			{
				tr.position = RaycastPosition(tr.position, tr.position.y);
			}
		}

		private Vector3 RaycastPosition(Vector3 position, float lasty)
		{
			if (raycastingForGroundPlacement)
			{
				float num = Mathf.Max(centerOffset, lasty - position.y + centerOffset);
				RaycastHit hitInfo;
				if (Physics.Raycast(position + Vector3.up * num, Vector3.down, out hitInfo, num, groundMask) && hitInfo.distance < num)
				{
					position = hitInfo.point;
					velocity.y = 0f;
				}
			}
			return position;
		}

		private bool RotateTowards(Vector3 trotdir)
		{
			trotdir.y = 0f;
			if (trotdir != Vector3.zero)
			{
				Quaternion rotation = tr.rotation;
				Vector3 eulerAngles = Quaternion.LookRotation(trotdir).eulerAngles;
				Vector3 eulerAngles2 = rotation.eulerAngles;
				eulerAngles2.y = Mathf.MoveTowardsAngle(eulerAngles2.y, eulerAngles.y, rotationSpeed * deltaTime);
				tr.rotation = Quaternion.Euler(eulerAngles2);
				return Mathf.Abs(eulerAngles2.y - eulerAngles.y) < 5f;
			}
			return false;
		}

		public void OnDrawGizmos()
		{
			if (!drawGizmos)
			{
				return;
			}
			if (raycastingForGroundPlacement)
			{
				Gizmos.color = GizmoColorRaycast;
				Gizmos.DrawLine(base.transform.position, base.transform.position + Vector3.up * centerOffset);
				Gizmos.DrawLine(base.transform.position + Vector3.left * 0.1f, base.transform.position + Vector3.right * 0.1f);
				Gizmos.DrawLine(base.transform.position + Vector3.back * 0.1f, base.transform.position + Vector3.forward * 0.1f);
			}
			if (tr != null && buffer != null)
			{
				Gizmos.color = GizmoColorPath;
				Vector3 from = tr.position;
				for (int i = 0; i < buffer.Count; i++)
				{
					Gizmos.DrawLine(from, buffer[i]);
					from = buffer[i];
				}
			}
		}

		private IEnumerator TraverseSpecial(RichSpecial rs)
		{
			traversingSpecialPath = true;
			velocity = Vector3.zero;
			AnimationLink al = rs.nodeLink as AnimationLink;
			if (al == null)
			{
				Debug.LogError("Unhandled RichSpecial");
				yield break;
			}
			while (!RotateTowards(rs.first.forward))
			{
				yield return null;
			}
			tr.parent.position = tr.position;
			tr.parent.rotation = tr.rotation;
			tr.localPosition = Vector3.zero;
			tr.localRotation = Quaternion.identity;
			if (rs.reverse && al.reverseAnim)
			{
				anim[al.clip].speed = 0f - al.animSpeed;
				anim[al.clip].normalizedTime = 1f;
				anim.Play(al.clip);
				anim.Sample();
			}
			else
			{
				anim[al.clip].speed = al.animSpeed;
				anim.Rewind(al.clip);
				anim.Play(al.clip);
			}
			tr.parent.position -= tr.position - tr.parent.position;
			yield return new WaitForSeconds(Mathf.Abs(anim[al.clip].length / al.animSpeed));
			traversingSpecialPath = false;
			NextPart();
			if (delayUpdatePath)
			{
				delayUpdatePath = false;
				UpdatePath();
			}
		}
	}
}
