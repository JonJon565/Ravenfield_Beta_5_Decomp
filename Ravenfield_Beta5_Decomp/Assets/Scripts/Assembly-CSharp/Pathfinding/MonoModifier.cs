using System;
using UnityEngine;

namespace Pathfinding
{
	[Serializable]
	public abstract class MonoModifier : MonoBehaviour, IPathModifier
	{
		[NonSerialized]
		public Seeker seeker;

		public abstract int Order { get; }

		public void OnEnable()
		{
		}

		public void OnDisable()
		{
		}

		public void Awake()
		{
			seeker = GetComponent<Seeker>();
			if (seeker != null)
			{
				seeker.RegisterModifier(this);
			}
		}

		public void OnDestroy()
		{
			if (seeker != null)
			{
				seeker.DeregisterModifier(this);
			}
		}

		public virtual void PreProcess(Path p)
		{
		}

		public abstract void Apply(Path p);
	}
}
