using System.Collections.Generic;
using UnityEngine;

public class MeanFilter
{
	private int taps;

	private Queue<float> queue;

	private float queueSum;

	public MeanFilter(int taps)
	{
		this.taps = Mathf.Max(taps, 1);
		queue = new Queue<float>(this.taps);
		queueSum = 0f;
		for (int i = 0; i < this.taps; i++)
		{
			queue.Enqueue(0f);
		}
	}

	public float Tick(float input)
	{
		queueSum -= queue.Dequeue();
		queueSum += input;
		queue.Enqueue(input);
		return Value();
	}

	public float Value()
	{
		return queueSum / (float)taps;
	}
}
