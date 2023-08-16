using UnityEngine;

public class Spring
{
	private float spring;

	private float drag;

	private float maxDelta;

	public Vector3 position;

	public Vector3 velocity;

	private Vector3 min;

	private Vector3 max;

	private int iterations;

	public Spring(float spring, float drag, Vector3 min, Vector3 max, int iterations)
	{
		this.spring = spring;
		this.drag = drag;
		position = Vector3.zero;
		velocity = Vector3.zero;
		this.min = min;
		this.max = max;
		maxDelta = maxDelta;
		this.iterations = iterations;
	}

	public void AddVelocity(Vector3 delta)
	{
		velocity += delta;
	}

	public void Update()
	{
		float num = Time.deltaTime / (float)iterations;
		for (int i = 0; i < iterations; i++)
		{
			velocity -= (position * spring + velocity * drag) * num;
			position = Vector3.Min(Vector3.Max(position + velocity * num, min), max);
		}
	}
}
