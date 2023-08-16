using UnityEngine;

namespace RealtimeCSG
{
	[DisallowMultipleComponent]
	public abstract class CSGNode : MonoBehaviour
	{
		[HideInInspector]
		public float Version = 1f;
	}
}
