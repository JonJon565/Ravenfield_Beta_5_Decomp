using UnityEngine;

public class LevelTester : MonoBehaviour
{
	private void Awake()
	{
		if (GameManager.instance == null)
		{
			InstantiateManagers();
		}
	}

	private void InstantiateManagers()
	{
		GameObject original = Resources.Load("_Managers") as GameObject;
		Object.Instantiate(original);
	}
}
