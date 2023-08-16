using System;
using System.Collections;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;

namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof(AudioSource))]
	[RequireComponent(typeof(CharacterController))]
	public class FirstPersonController : MonoBehaviour
	{
		private const int HIT_LAYER_MASK = -1793;

		[NonSerialized]
		public bool inputEnabled = true;

		[SerializeField]
		private bool m_IsWalking;

		[SerializeField]
		private float m_WalkSpeed;

		[SerializeField]
		private float m_RunSpeed;

		[Range(0f, 1f)]
		[SerializeField]
		private float m_RunstepLenghten;

		[SerializeField]
		private float m_JumpSpeed;

		[SerializeField]
		private float m_StickToGroundForce;

		[SerializeField]
		private float m_GravityMultiplier;

		[SerializeField]
		private MouseLook m_MouseLook;

		[SerializeField]
		private bool m_UseFovKick;

		[SerializeField]
		private FOVKick m_FovKick = new FOVKick();

		[SerializeField]
		private bool m_UseHeadBob;

		[SerializeField]
		private CurveControlledBob m_HeadBob = new CurveControlledBob();

		[SerializeField]
		private LerpControlledBob m_JumpBob = new LerpControlledBob();

		[SerializeField]
		private float m_StepInterval;

		[SerializeField]
		private AudioClip[] m_FootstepSounds;

		[SerializeField]
		private AudioClip m_JumpSound;

		[SerializeField]
		private AudioClip m_LandSound;

		public bool sprinting;

		public Transform cameraParent;

		public Camera m_Camera;

		private bool m_Jump;

		private float m_YRotation;

		private Vector2 m_Input;

		private Vector3 m_MoveDir = Vector3.zero;

		private CharacterController m_CharacterController;

		private CollisionFlags m_CollisionFlags;

		private bool m_PreviouslyGrounded;

		private Vector3 m_OriginalCameraPosition;

		private float m_StepCycle;

		private float m_NextStep;

		private bool m_Jumping;

		private AudioSource m_AudioSource;

		public float StepCycle()
		{
			return m_StepCycle / m_StepInterval;
		}

		public void SetMouseSensitivityMultiplier(float m, bool invertY)
		{
			m_MouseLook.sensitivityMultiplier = m;
			m_MouseLook.invertY = invertY;
		}

		public Vector3 Velocity()
		{
			return m_CharacterController.velocity;
		}

		public void ResetVelocity()
		{
			m_MoveDir = Vector3.zero;
		}

		public void SetMouseEnabled(bool enabled)
		{
			m_MouseLook.enabled = enabled;
		}

		public bool OnGround()
		{
			return m_CharacterController.isGrounded;
		}

		public void EnableCharacterController()
		{
			m_CharacterController.enabled = true;
		}

		public void DisableCharacterController()
		{
			m_CharacterController.enabled = false;
		}

		private void Start()
		{
			m_CharacterController = GetComponent<CharacterController>();
			m_OriginalCameraPosition = cameraParent.localPosition;
			m_FovKick.Setup(m_Camera);
			m_HeadBob.Setup(m_Camera, m_StepInterval);
			m_StepCycle = 0f;
			m_NextStep = m_StepCycle / 2f;
			m_Jumping = false;
			m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(base.transform, cameraParent);
		}

		private void Update()
		{
			RotateView();
			if (!m_Jump)
			{
				m_Jump = CrossPlatformInputManager.GetButtonDown("Jump") && inputEnabled;
			}
			if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
			{
				StartCoroutine(m_JumpBob.DoBobCycle());
				PlayLandingSound();
				m_MoveDir.y = 0f;
				m_Jumping = false;
			}
			if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
			{
				m_MoveDir.y = 0f;
			}
			m_PreviouslyGrounded = m_CharacterController.isGrounded;
		}

		private void PlayLandingSound()
		{
			if (!m_CharacterController.enabled)
			{
				m_AudioSource.clip = m_LandSound;
				m_AudioSource.Play();
				m_NextStep = m_StepCycle + 0.5f;
			}
		}

		private void FixedUpdate()
		{
			float speed;
			GetInput(out speed);
			Vector3 forward = cameraParent.forward;
			forward.y = 0f;
			forward = forward.normalized;
			Vector3 normalized = Vector3.Cross(Vector3.up, forward).normalized;
			Vector3 vector = forward * m_Input.y + normalized * m_Input.x;
			RaycastHit hitInfo;
			Physics.SphereCast(base.transform.position, m_CharacterController.radius, Vector3.down, out hitInfo, m_CharacterController.height / 2f, -1793);
			vector = Vector3.ProjectOnPlane(vector, hitInfo.normal).normalized;
			m_MoveDir.x = vector.x * speed;
			m_MoveDir.z = vector.z * speed;
			if (m_CharacterController.isGrounded)
			{
				m_MoveDir.y = 0f - m_StickToGroundForce;
				if (m_Jump)
				{
					m_MoveDir.y = m_JumpSpeed;
					PlayJumpSound();
					m_Jump = false;
					m_Jumping = true;
				}
			}
			else
			{
				m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
			}
			if (m_CharacterController.enabled)
			{
				m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);
			}
			ProgressStepCycle(speed);
			UpdateCameraPosition(speed);
		}

		private void PlayJumpSound()
		{
			if (!m_CharacterController.enabled)
			{
				m_AudioSource.clip = m_JumpSound;
				m_AudioSource.Play();
			}
		}

		private void ProgressStepCycle(float speed)
		{
			if (m_CharacterController.velocity.sqrMagnitude > 0f && (m_Input.x != 0f || m_Input.y != 0f))
			{
				m_StepCycle += (m_CharacterController.velocity.magnitude + speed * ((!m_IsWalking) ? m_RunstepLenghten : 1f)) * Time.fixedDeltaTime;
			}
			if (m_StepCycle > m_NextStep)
			{
				m_NextStep = m_StepCycle + m_StepInterval;
				PlayFootStepAudio();
			}
		}

		private void PlayFootStepAudio()
		{
			if (m_CharacterController.isGrounded && m_CharacterController.enabled)
			{
				int num = UnityEngine.Random.Range(1, m_FootstepSounds.Length);
				m_AudioSource.clip = m_FootstepSounds[num];
				m_AudioSource.PlayOneShot(m_AudioSource.clip);
				m_FootstepSounds[num] = m_FootstepSounds[0];
				m_FootstepSounds[0] = m_AudioSource.clip;
			}
		}

		private void UpdateCameraPosition(float speed)
		{
			if (m_UseHeadBob)
			{
				Vector3 localPosition;
				if (m_CharacterController.velocity.magnitude > 0f && m_CharacterController.isGrounded)
				{
					cameraParent.localPosition = m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude + speed * ((!m_IsWalking) ? m_RunstepLenghten : 1f));
					localPosition = cameraParent.localPosition;
					localPosition.y = cameraParent.localPosition.y - m_JumpBob.Offset();
				}
				else
				{
					localPosition = cameraParent.localPosition;
					localPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
				}
				cameraParent.localPosition = localPosition;
			}
		}

		private void GetInput(out float speed)
		{
			float axis = CrossPlatformInputManager.GetAxis("Horizontal");
			float axis2 = CrossPlatformInputManager.GetAxis("Vertical");
			bool isWalking = m_IsWalking;
			m_IsWalking = !sprinting;
			speed = ((!m_IsWalking) ? m_RunSpeed : m_WalkSpeed);
			if (inputEnabled)
			{
				m_Input = new Vector2(axis, axis2);
			}
			else
			{
				m_Input = Vector2.zero;
			}
			if (m_Input.sqrMagnitude > 1f)
			{
				m_Input.Normalize();
			}
			if (m_IsWalking != isWalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0f)
			{
				StopAllCoroutines();
				IEnumerator routine;
				if (!m_IsWalking)
				{
					IEnumerator enumerator = m_FovKick.FOVKickUp();
					routine = enumerator;
				}
				else
				{
					routine = m_FovKick.FOVKickDown();
				}
				StartCoroutine(routine);
			}
		}

		private void RotateView()
		{
			m_MouseLook.LookRotation(base.transform, cameraParent);
		}

		private void OnControllerColliderHit(ControllerColliderHit hit)
		{
			Rigidbody attachedRigidbody = hit.collider.attachedRigidbody;
			if (m_CollisionFlags != CollisionFlags.Below && !(attachedRigidbody == null) && !attachedRigidbody.isKinematic)
			{
				attachedRigidbody.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
			}
		}
	}
}
