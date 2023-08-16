using UnityEngine;

namespace Pathfinding
{
	public abstract class GraphModifier : MonoBehaviour
	{
		public enum EventType
		{
			PostScan = 1,
			PreScan = 2,
			LatePostScan = 4,
			PreUpdate = 8,
			PostUpdate = 0x10,
			PostCacheLoad = 0x20
		}

		private static GraphModifier root;

		private GraphModifier prev;

		private GraphModifier next;

		public static void FindAllModifiers()
		{
			GraphModifier[] array = Object.FindObjectsOfType(typeof(GraphModifier)) as GraphModifier[];
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnEnable();
			}
		}

		public static void TriggerEvent(EventType type)
		{
			if (!Application.isPlaying)
			{
				FindAllModifiers();
			}
			GraphModifier graphModifier = root;
			switch (type)
			{
			default:
				return;
			case EventType.PreScan:
				while (graphModifier != null)
				{
					graphModifier.OnPreScan();
					graphModifier = graphModifier.next;
				}
				return;
			case EventType.PostScan:
				while (graphModifier != null)
				{
					graphModifier.OnPostScan();
					graphModifier = graphModifier.next;
				}
				return;
			case EventType.LatePostScan:
				while (graphModifier != null)
				{
					graphModifier.OnLatePostScan();
					graphModifier = graphModifier.next;
				}
				return;
			case EventType.PreUpdate:
				while (graphModifier != null)
				{
					graphModifier.OnGraphsPreUpdate();
					graphModifier = graphModifier.next;
				}
				return;
			case EventType.PostUpdate:
				while (graphModifier != null)
				{
					graphModifier.OnGraphsPostUpdate();
					graphModifier = graphModifier.next;
				}
				return;
			case EventType.PostCacheLoad:
				break;
			}
			while (graphModifier != null)
			{
				graphModifier.OnPostCacheLoad();
				graphModifier = graphModifier.next;
			}
		}

		protected virtual void OnEnable()
		{
			OnDisable();
			if (root == null)
			{
				root = this;
				return;
			}
			next = root;
			root.prev = this;
			root = this;
		}

		protected virtual void OnDisable()
		{
			if (root == this)
			{
				root = next;
				if (root != null)
				{
					root.prev = null;
				}
			}
			else
			{
				if (prev != null)
				{
					prev.next = next;
				}
				if (next != null)
				{
					next.prev = prev;
				}
			}
			prev = null;
			next = null;
		}

		public virtual void OnPostScan()
		{
		}

		public virtual void OnPreScan()
		{
		}

		public virtual void OnLatePostScan()
		{
		}

		public virtual void OnPostCacheLoad()
		{
		}

		public virtual void OnGraphsPreUpdate()
		{
		}

		public virtual void OnGraphsPostUpdate()
		{
		}
	}
}
