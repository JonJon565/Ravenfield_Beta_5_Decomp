using System;
using UnityEngine;

public class SoundBank : MonoBehaviour
{
	public AudioClip[] clips;

	[NonSerialized]
	public AudioSource audioSource;

	private int lastIndex;

	public virtual void Start()
	{
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}
		lastIndex = UnityEngine.Random.Range(0, clips.Length);
		if (audioSource.playOnAwake)
		{
			audioSource.Stop();
			PlayRandom();
		}
	}

	public void PlayRandom()
	{
		lastIndex = (lastIndex + UnityEngine.Random.Range(1, clips.Length)) % clips.Length;
		audioSource.PlayOneShot(clips[lastIndex]);
	}
}
