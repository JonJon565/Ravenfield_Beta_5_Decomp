using UnityEngine;

public class Lifetime : MonoBehaviour
{
	public float lifetime = 1f;

	private Action lifetimeAction;

	private void Start()
	{
		lifetimeAction = new Action(lifetime);
		lifetimeAction.Start();
	}

	private void Update()
	{
		if (lifetimeAction.TrueDone())
		{
			Object.Destroy(base.gameObject);
		}
	}
}
