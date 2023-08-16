using UnityEngine;
using UnityEngine.UI;

public class CameraScroll : MonoBehaviour
{
	public Slider speedSlider;

	public float moveSpeed = 0.5f;

	private void Start()
	{
		speedSlider.onValueChanged.AddListener(delegate
		{
			ChangeSpeed();
		});
	}

	private void Update()
	{
		base.transform.Translate(Vector3.left * (Time.deltaTime * moveSpeed));
		if (base.transform.position.x > 112f)
		{
			base.transform.position = new Vector3(0f, base.transform.position.y, base.transform.position.z);
		}
	}

	private void ChangeSpeed()
	{
		moveSpeed = speedSlider.value;
	}
}
