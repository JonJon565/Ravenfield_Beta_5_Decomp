using System;
using UnityEngine;

public class ActorIk : MonoBehaviour
{
	private Animator animator;

	[NonSerialized]
	public Vector3 aimPoint = Vector3.zero;

	[NonSerialized]
	public bool turnBody = true;

	[NonSerialized]
	public float weight;

	[NonSerialized]
	public bool bypass;

	private void Awake()
	{
		animator = GetComponent<Animator>();
	}

	private void OnAnimatorIK()
	{
		try
		{
			animator.SetLookAtPosition(aimPoint);
			if (turnBody)
			{
				animator.SetLookAtWeight(weight, weight, weight);
			}
			else
			{
				animator.SetLookAtWeight(weight, 0f, weight);
			}
		}
		catch (Exception)
		{
		}
	}
}
