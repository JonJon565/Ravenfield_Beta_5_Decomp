using System.Collections.Generic;
using UnityEngine;

public class MeanFilterVector3
{
	private int taps;

	private Queue<Vector3> queue;

	private Vector3 queueSum;

	public MeanFilterVector3(int taps)
	{
		this.taps = Mathf.Max(taps, 1);
		queue = new Queue<Vector3>(this.taps);
		queueSum = Vector3.zero;
		for (int i = 0; i < this.taps; i++)
		{
			queue.Enqueue(Vector3.zero);
		}
	}

	public Vector3 Tick(Vector3 input)
	{
		queueSum -= queue.Dequeue();
		queueSum += input;
		queue.Enqueue(input);
		return Value();
	}

	public Vector3 Value()
	{
		return queueSum / taps;
	}
}
