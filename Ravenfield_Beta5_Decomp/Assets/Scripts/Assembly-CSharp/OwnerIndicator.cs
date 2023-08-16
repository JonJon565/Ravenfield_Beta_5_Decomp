using UnityEngine;

public class OwnerIndicator : MonoBehaviour
{
	private Renderer renderer;

	private void Start()
	{
		renderer = GetComponent<QualitySwitcher>().activeObject.GetComponent<Renderer>();
		SetOwner(-1);
	}

	public void SetOwner(int team)
	{
		if (team == -1)
		{
			renderer.enabled = false;
			return;
		}
		renderer.enabled = true;
		renderer.material.color = Color.Lerp(ColorScheme.TeamColor(team), Color.black, 0.2f);
	}
}
