using UnityEngine;

public class CommandRoomCamera : MonoBehaviour
{
	public CameraPosition defaultCameraPosition;

	public CameraPosition commandMapCameraPosition;

	public CameraPosition economyTableCameraPosition;

	private CameraPosition previousCameraPosition;

	private CameraPosition cameraPosition;

	private Camera camera;

	private Action changeCameraPositionAction = new Action(0.5f);

	private Vector2 rotationOffset = Vector2.zero;

	private void Awake()
	{
		cameraPosition = defaultCameraPosition;
		camera = GetComponent<Camera>();
	}

	private void Update()
	{
		Vector2 vector = Input.mousePosition;
		vector.x = Mathf.Clamp01(vector.x / (float)Screen.width);
		vector.y = Mathf.Clamp01(vector.y / (float)Screen.height);
		Vector3 eulerAngles = cameraPosition.transform.eulerAngles;
		float num = cameraPosition.turnHorizontal;
		float num2 = cameraPosition.turnVertical;
		base.transform.position = cameraPosition.transform.position;
		if (!changeCameraPositionAction.TrueDone())
		{
			float t = Mathf.SmoothStep(0f, 1f, changeCameraPositionAction.Ratio());
			eulerAngles = Quaternion.Slerp(previousCameraPosition.transform.rotation, cameraPosition.transform.rotation, t).eulerAngles;
			num = Mathf.Lerp(previousCameraPosition.turnHorizontal, cameraPosition.turnHorizontal, t);
			num2 = Mathf.Lerp(previousCameraPosition.turnVertical, cameraPosition.turnVertical, t);
			base.transform.position = Vector3.Lerp(previousCameraPosition.transform.position, cameraPosition.transform.position, t);
		}
		rotationOffset = Vector2.Lerp(b: new Vector2(vector.x - 0.5f, vector.y - 0.5f), a: rotationOffset, t: 5f * Time.deltaTime);
		Vector3 euler = eulerAngles + new Vector3((0f - num2) * rotationOffset.y, num * rotationOffset.x, 0f);
		base.transform.rotation = Quaternion.Euler(euler);
		if (Input.GetKeyDown(KeyCode.W))
		{
			ChangeCameraPosition(commandMapCameraPosition);
		}
		if (Input.GetKeyDown(KeyCode.S))
		{
			ChangeCameraPosition(defaultCameraPosition);
		}
		if (Input.GetKeyDown(KeyCode.D))
		{
			ChangeCameraPosition(economyTableCameraPosition);
		}
	}

	public void ChangeCameraPosition(CameraPosition newPosition)
	{
		previousCameraPosition = cameraPosition;
		cameraPosition = newPosition;
		changeCameraPositionAction.Start();
	}
}
