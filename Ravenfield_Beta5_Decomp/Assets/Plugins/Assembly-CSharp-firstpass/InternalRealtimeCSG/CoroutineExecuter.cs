using System.Collections;
using UnityEngine;

namespace InternalRealtimeCSG
{
	[ExecuteInEditMode]
	internal sealed class CoroutineExecuter : MonoBehaviour
	{
		private static MonoBehaviour Singleton;

		private void Awake()
		{
			Singleton = this;
		}

		private void OnDestroy()
		{
			Singleton = null;
		}

		public static MonoBehaviour GetSingleton()
		{
			if (!Singleton || Singleton == null)
			{
				GameObject gameObject = new GameObject();
				gameObject.hideFlags = HideFlags.HideAndDontSave;
				Singleton = gameObject.AddComponent<CoroutineExecuter>();
			}
			return Singleton;
		}

		public new static Coroutine StartCoroutine(IEnumerator coroutine)
		{
			if (!Singleton || Singleton == null)
			{
				Singleton = GetSingleton();
				if (!Singleton || Singleton == null)
				{
					Debug.LogWarning("Tried to execute coroutine but CoroutineExecuter was not initialized.");
					return null;
				}
			}
			Singleton.StopAllCoroutines();
			return Singleton.StartCoroutine(coroutine);
		}
	}
}
