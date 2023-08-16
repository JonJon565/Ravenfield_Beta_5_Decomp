using System;
using UnityEngine;

public class CommandMapData : MonoBehaviour
{
	[Serializable]
	public class LevelConnection
	{
		public LevelMiniature l1;

		public LevelMiniature l2;
	}

	[NonSerialized]
	public LevelMiniature[] levels;

	public LevelConnection[] levelConnections;

	public float nodeRadius = 0.05f;

	public float connectionWidth = 0.01f;

	private void Awake()
	{
		levels = UnityEngine.Object.FindObjectsOfType<LevelMiniature>();
	}
}
