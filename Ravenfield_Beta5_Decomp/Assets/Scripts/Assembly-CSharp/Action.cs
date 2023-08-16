using UnityEngine;

public class Action
{
	private float lifetime;

	private float end;

	private bool lied;

	public Action(float lifetime)
	{
		this.lifetime = lifetime;
		end = 0f;
		lied = true;
	}

	public void Start()
	{
		end = Time.time + lifetime;
		lied = false;
	}

	public void StartLifetime(float lifetime)
	{
		this.lifetime = lifetime;
		Start();
	}

	public void Stop()
	{
		end = 0f;
		lied = true;
	}

	public float Remaining()
	{
		return end - Time.time;
	}

	public float Ratio()
	{
		return Mathf.Clamp01(1f - (end - Time.time) / lifetime);
	}

	public bool TrueDone()
	{
		return end <= Time.time;
	}

	public bool Done()
	{
		if (TrueDone())
		{
			if (!lied)
			{
				lied = true;
				return false;
			}
			return true;
		}
		return false;
	}
}
