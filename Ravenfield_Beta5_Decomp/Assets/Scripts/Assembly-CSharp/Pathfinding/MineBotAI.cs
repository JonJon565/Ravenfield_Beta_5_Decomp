using UnityEngine;

namespace Pathfinding
{
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_mine_bot_a_i.php")]
	[RequireComponent(typeof(Seeker))]
	public class MineBotAI : AIPath
	{
		public Animation anim;

		public float sleepVelocity = 0.4f;

		public float animationSpeed = 0.2f;

		public GameObject endOfPathEffect;

		protected Vector3 lastTarget;

		public new void Start()
		{
			anim["forward"].layer = 10;
			anim.Play("awake");
			anim.Play("forward");
			anim["awake"].wrapMode = WrapMode.Once;
			anim["awake"].speed = 0f;
			anim["awake"].normalizedTime = 1f;
			base.Start();
		}

		public override void OnTargetReached()
		{
			if (endOfPathEffect != null && Vector3.Distance(tr.position, lastTarget) > 1f)
			{
				Object.Instantiate(endOfPathEffect, tr.position, tr.rotation);
				lastTarget = tr.position;
			}
		}

		public override Vector3 GetFeetPosition()
		{
			return tr.position;
		}

		protected new void Update()
		{
			Vector3 direction;
			if (canMove)
			{
				Vector3 vel = CalculateVelocity(GetFeetPosition());
				RotateTowards(targetDirection);
				vel.y = 0f;
				if (!(vel.sqrMagnitude > sleepVelocity * sleepVelocity))
				{
					vel = Vector3.zero;
				}
				if (rvoController != null)
				{
					rvoController.Move(vel);
					direction = rvoController.velocity;
				}
				else if (controller != null)
				{
					controller.SimpleMove(vel);
					direction = controller.velocity;
				}
				else
				{
					Debug.LogWarning("No NavmeshController or CharacterController attached to GameObject");
					direction = Vector3.zero;
				}
			}
			else
			{
				direction = Vector3.zero;
			}
			Vector3 vector = tr.InverseTransformDirection(direction);
			vector.y = 0f;
			if (direction.sqrMagnitude <= sleepVelocity * sleepVelocity)
			{
				anim.Blend("forward", 0f, 0.2f);
				return;
			}
			anim.Blend("forward", 1f, 0.2f);
			AnimationState animationState = anim["forward"];
			float z = vector.z;
			animationState.speed = z * animationSpeed;
		}
	}
}
