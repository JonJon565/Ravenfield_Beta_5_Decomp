using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveRaggy : MonoBehaviour
{
	public enum SignedAxis
	{
		X = 0,
		XNegative = 1,
		Y = 2,
		YNegative = 3,
		Z = 4,
		ZNegative = 5
	}

	private enum State
	{
		Animate = 0,
		Ragdoll = 1,
		PreInterpolate = 2,
		Interpolate = 3
	}

	[Serializable]
	public class Configuration
	{
		public float limbScale = 1f;

		public SignedAxis alongAxis = SignedAxis.XNegative;

		public SignedAxis forwardAxis = SignedAxis.Z;

		public SignedAxis rightAxis = SignedAxis.Y;
	}

	private const float MAXIMUM_DRIVE_FORCE = 1E+13f;

	private const float MAX_PROJECTION_DISTANCE = 2f;

	private const float HALFPOINT = 0.70710677f;

	public GameObject animatedObject;

	public GameObject ragdollObject;

	public float interpolateDuration = 0.5f;

	public bool autoDisableColliders;

	public Configuration configuration;

	[SerializeField]
	[HideInInspector]
	private Animator animator;

	[SerializeField]
	[HideInInspector]
	private Renderer[] animatedRenderers;

	[HideInInspector]
	[SerializeField]
	private ConfigurableJoint[] joints;

	[SerializeField]
	[HideInInspector]
	private Rigidbody[] rigidbodies;

	[SerializeField]
	[HideInInspector]
	private List<Transform> ragdollTransforms;

	[HideInInspector]
	[SerializeField]
	private List<Transform> animatedTransforms;

	[HideInInspector]
	[SerializeField]
	private Collider[] animatedColliders;

	private Dictionary<ConfigurableJoint, Transform> jointTargets;

	private Dictionary<Transform, Transform> r2aTransforms;

	private Dictionary<Transform, Transform> a2rTransforms;

	private Dictionary<ConfigurableJoint, Quaternion> initialRotations;

	private Dictionary<Transform, Vector3> interpolateSourcePosition;

	private Dictionary<Transform, Quaternion> interpolateSourceRotation;

	private State state;

	private JointDrive jointDrive;

	private float interpolateStartTime;

	private void Awake()
	{
		InitializeDictionaries();
		jointDrive = default(JointDrive);
		jointDrive.mode = JointDriveMode.Position;
		jointDrive.maximumForce = 1E+13f;
		SetDrive(1000f, 3f);
		animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
		ragdollObject.SetActive(false);
	}

	public void ClearSerializedLists()
	{
		animatedRenderers = null;
		animator = null;
		joints = null;
		rigidbodies = null;
		jointTargets = null;
		r2aTransforms = null;
		a2rTransforms = null;
		ragdollTransforms = null;
	}

	public void SetupSerializedTransformLists()
	{
		animatedRenderers = animatedObject.GetComponentsInChildren<Renderer>();
		animator = animatedObject.GetComponent<Animator>();
		if (animator == null)
		{
			throw new Exception("No animator component found in Animated Object.");
		}
		if (!animator.isHuman)
		{
			throw new Exception("Animator is not a humanoid!");
		}
		Debug.Log("Serialized trnasform list");
		animatedTransforms = new List<Transform>(animatedObject.GetComponentsInChildren<Transform>());
		ragdollTransforms = new List<Transform>(ragdollObject.GetComponentsInChildren<Transform>());
		animatedTransforms.Remove(animatedObject.transform);
		ragdollTransforms.Remove(ragdollObject.transform);
	}

	public void SetupSerializedRagdollLists()
	{
		rigidbodies = ragdollObject.GetComponentsInChildren<Rigidbody>();
		joints = ragdollObject.GetComponentsInChildren<ConfigurableJoint>();
	}

	public void SetupSerializedColliderLists()
	{
		animatedColliders = animatedObject.GetComponentsInChildren<Collider>();
	}

	public void InitializeDictionaries()
	{
		r2aTransforms = new Dictionary<Transform, Transform>();
		a2rTransforms = new Dictionary<Transform, Transform>();
		for (int i = 0; i < ragdollTransforms.Count; i++)
		{
			r2aTransforms.Add(ragdollTransforms[i], animatedTransforms[i]);
			a2rTransforms.Add(animatedTransforms[i], ragdollTransforms[i]);
		}
		jointTargets = new Dictionary<ConfigurableJoint, Transform>();
		if (joints != null)
		{
			ConfigurableJoint[] array = joints;
			foreach (ConfigurableJoint configurableJoint in array)
			{
				Transform value = r2aTransforms[configurableJoint.transform];
				jointTargets.Add(configurableJoint, value);
			}
		}
	}

	private void InitializeJointRotations()
	{
		initialRotations = new Dictionary<ConfigurableJoint, Quaternion>(joints.Length);
		ConfigurableJoint[] array = joints;
		foreach (ConfigurableJoint configurableJoint in array)
		{
			Quaternion localRotation = configurableJoint.transform.localRotation;
			initialRotations.Add(configurableJoint, localRotation);
		}
	}

	public void SetDrive(float spring, float damping)
	{
		jointDrive.positionSpring = spring;
		jointDrive.positionDamper = damping;
		ConfigurableJoint[] array = joints;
		foreach (ConfigurableJoint configurableJoint in array)
		{
			configurableJoint.slerpDrive = jointDrive;
		}
	}

	public void Animate()
	{
		if (state != State.Ragdoll)
		{
			return;
		}
		state = State.PreInterpolate;
		interpolateSourcePosition = new Dictionary<Transform, Vector3>(ragdollTransforms.Count);
		interpolateSourceRotation = new Dictionary<Transform, Quaternion>(ragdollTransforms.Count);
		foreach (Transform ragdollTransform in ragdollTransforms)
		{
			interpolateSourcePosition.Add(ragdollTransform, ragdollTransform.position);
			interpolateSourceRotation.Add(ragdollTransform, ragdollTransform.rotation);
		}
		StartCoroutine(SyncAnimatorPosition());
	}

	public void InstantAnimate()
	{
		StopRagdoll();
	}

	private IEnumerator SyncAnimatorPosition()
	{
		yield return new WaitForFixedUpdate();
		Vector3 ragdollPelvisPosition = interpolateSourcePosition[HumanBoneTransform(HumanBodyBones.Hips)];
		Vector3 groundPosition = ragdollPelvisPosition;
		Ray groundRay = new Ray(ragdollPelvisPosition, Vector3.down);
		RaycastHit hitInfo;
		if (Physics.Raycast(groundRay, out hitInfo, 2f))
		{
			groundPosition = hitInfo.point;
		}
		base.transform.rotation = Quaternion.LookRotation(PlanarForward());
		base.transform.position = groundPosition;
		interpolateStartTime = Time.time;
		state = State.Interpolate;
	}

	public void Ragdoll(Vector3 velocity)
	{
		if (state == State.Ragdoll)
		{
			return;
		}
		if (autoDisableColliders)
		{
			Collider[] array = animatedColliders;
			foreach (Collider collider in array)
			{
				collider.enabled = false;
			}
		}
		Renderer[] array2 = animatedRenderers;
		foreach (Renderer renderer in array2)
		{
			renderer.enabled = false;
		}
		ragdollObject.SetActive(true);
		InitializeJointRotations();
		ragdollObject.transform.rotation = Quaternion.identity;
		foreach (Transform ragdollTransform in ragdollTransforms)
		{
			Transform transform = r2aTransforms[ragdollTransform];
			ragdollTransform.position = transform.position;
			ragdollTransform.rotation = transform.rotation;
			ragdollTransform.localScale = transform.localScale;
		}
		Rigidbody[] array3 = rigidbodies;
		foreach (Rigidbody rigidbody in array3)
		{
			rigidbody.velocity = velocity;
		}
		state = State.Ragdoll;
	}

	private void StopRagdoll()
	{
		if (autoDisableColliders)
		{
			Collider[] array = animatedColliders;
			foreach (Collider collider in array)
			{
				collider.enabled = true;
			}
		}
		Renderer[] array2 = animatedRenderers;
		foreach (Renderer renderer in array2)
		{
			renderer.enabled = true;
		}
		ragdollObject.SetActive(false);
		state = State.Animate;
	}

	public bool OnBack()
	{
		return Forward(HumanBoneTransform(HumanBodyBones.Spine)).y > 0f;
	}

	public bool Upright()
	{
		return (HumanBoneTransform(HumanBodyBones.Head).position - HumanBoneTransform(HumanBodyBones.Spine).position).normalized.y > 0.70710677f;
	}

	public Vector3 Velocity()
	{
		return rigidbodies[0].velocity;
	}

	public Rigidbody MainRigidbody()
	{
		return rigidbodies[0];
	}

	public Vector3 Position()
	{
		if (state == State.Ragdoll || state == State.PreInterpolate)
		{
			return HumanBoneTransform(HumanBodyBones.Hips).position;
		}
		return HumanBoneTransformAnimated(HumanBodyBones.Hips).position;
	}

	public Vector3 PlanarForward()
	{
		Vector3 vector = (Upright() ? Forward(HumanBoneTransform(HumanBodyBones.Spine)) : ((!OnBack()) ? Along(HumanBoneTransform(HumanBodyBones.Spine)) : (-Along(HumanBoneTransform(HumanBodyBones.Spine)))));
		vector.y = 0f;
		return vector.normalized;
	}

	public Vector3 Forward(Transform transform)
	{
		return TransformVector(configuration.forwardAxis, transform);
	}

	public Vector3 Along(Transform transform)
	{
		return TransformVector(configuration.alongAxis, transform);
	}

	public Vector3 Right(Transform transform)
	{
		return TransformVector(configuration.rightAxis, transform);
	}

	private Vector3 TransformVector(SignedAxis axis, Transform t)
	{
		switch (axis)
		{
		case SignedAxis.X:
			return t.right;
		case SignedAxis.XNegative:
			return -t.right;
		case SignedAxis.Y:
			return t.up;
		case SignedAxis.YNegative:
			return -t.up;
		case SignedAxis.Z:
			return t.forward;
		case SignedAxis.ZNegative:
			return -t.forward;
		default:
			return Vector3.zero;
		}
	}

	public Vector3 LocalForward()
	{
		return WorldVector(configuration.forwardAxis);
	}

	public Vector3 LocalAlong()
	{
		return WorldVector(configuration.alongAxis);
	}

	public Vector3 LocalRight()
	{
		return WorldVector(configuration.rightAxis);
	}

	private Vector3 WorldVector(SignedAxis axis)
	{
		switch (axis)
		{
		case SignedAxis.X:
			return Vector3.right;
		case SignedAxis.XNegative:
			return Vector3.left;
		case SignedAxis.Y:
			return Vector3.up;
		case SignedAxis.YNegative:
			return Vector3.down;
		case SignedAxis.Z:
			return Vector3.forward;
		case SignedAxis.ZNegative:
			return Vector3.back;
		default:
			return Vector3.zero;
		}
	}

	public int AlongDirectionIndex()
	{
		switch (configuration.alongAxis)
		{
		case SignedAxis.X:
			return 0;
		case SignedAxis.XNegative:
			return 0;
		case SignedAxis.Y:
			return 1;
		case SignedAxis.YNegative:
			return 1;
		case SignedAxis.Z:
			return 2;
		case SignedAxis.ZNegative:
			return 2;
		default:
			return 0;
		}
	}

	public bool IsRagdoll()
	{
		return state == State.Ragdoll;
	}

	public Transform HumanBoneTransformAnimated(HumanBodyBones bone)
	{
		return animator.GetBoneTransform(bone);
	}

	public Transform HumanBoneTransform(HumanBodyBones bone)
	{
		return a2rTransforms[HumanBoneTransformAnimated(bone)];
	}

	public Transform AnimatedToRagdoll(Transform transform)
	{
		return a2rTransforms[transform];
	}

	public Transform RagdollToAnimated(Transform transform)
	{
		return r2aTransforms[transform];
	}

	public Renderer[] AnimatedRenderers()
	{
		return animatedRenderers;
	}

	private void Update()
	{
		if (state == State.Ragdoll)
		{
			ConfigurableJoint[] array = joints;
			foreach (ConfigurableJoint configurableJoint in array)
			{
				Quaternion localRotation = jointTargets[configurableJoint].localRotation;
				configurableJoint.SetTargetRotationLocal(localRotation, initialRotations[configurableJoint]);
			}
		}
	}

	private void LateUpdate()
	{
		if (state != State.Interpolate)
		{
			return;
		}
		float num = Mathf.Clamp01((Time.time - interpolateStartTime) / interpolateDuration);
		foreach (Transform ragdollTransform in ragdollTransforms)
		{
			Transform transform = r2aTransforms[ragdollTransform];
			ragdollTransform.position = Vector3.Lerp(interpolateSourcePosition[ragdollTransform], transform.position, num);
			ragdollTransform.rotation = Quaternion.Slerp(interpolateSourceRotation[ragdollTransform], transform.rotation, num);
		}
		if (num == 1f)
		{
			StopRagdoll();
		}
	}
}
