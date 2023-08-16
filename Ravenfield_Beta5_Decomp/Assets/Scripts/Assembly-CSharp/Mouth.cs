using System.Collections;
using UnityEngine;

public class Mouth : MonoBehaviour
{
	private const float PHONEME_TIME = 0.1f;

	private const float WORD_PAUSE_TIME = 0.3f;

	private const int MIN_PHONEMES_PER_WORD = 4;

	private const int MAX_PHONEMES_PER_WORD = 8;

	private int phonemesPerWord = 4;

	private int lastPhoneme;

	private Material material;

	private void Awake()
	{
		material = GetComponent<Renderer>().material;
		AutoTalk();
	}

	public void AutoTalk()
	{
		StopAllCoroutines();
		StartCoroutine(AutoUpdateCoroutine());
	}

	private IEnumerator AutoUpdateCoroutine()
	{
		while (true)
		{
			phonemesPerWord = Random.Range(4, 8);
			for (int i = 0; i < phonemesPerWord; i++)
			{
				RandomPhoneme();
				yield return new WaitForSeconds(0.1f);
			}
			Idle();
			yield return new WaitForSeconds(0.3f);
		}
	}

	private void RandomPhoneme()
	{
		int num = (lastPhoneme = (lastPhoneme + Random.Range(1, 4)) % 4);
		material.mainTextureOffset = new Vector2(0f, 0.25f * (float)num);
	}

	private void Idle()
	{
		material.mainTextureOffset = new Vector2(0f, 0.75f);
	}

	public void ForceIdle()
	{
		StopAllCoroutines();
		Idle();
	}
}
