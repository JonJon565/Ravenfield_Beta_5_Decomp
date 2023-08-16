using UnityEngine;

public class UvAutoScroll : UvOffset
{
	public Vector2 speed;

	private void Update()
	{
		IncrementOffset(speed * Time.deltaTime);
	}
}
