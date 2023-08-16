using RealtimeCSG;
using UnityEngine;

namespace InternalRealtimeCSG
{
	[DisallowMultipleComponent]
	[SelectionBase]
	[ExecuteInEditMode]
	public sealed class CSGModelExported : MonoBehaviour
	{
		[HideInInspector]
		[SerializeField]
		public CSGModel containedModel;
	}
}
