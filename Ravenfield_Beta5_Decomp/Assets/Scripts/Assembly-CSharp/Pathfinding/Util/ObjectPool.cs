using System;
using System.Collections.Generic;

namespace Pathfinding.Util
{
	public static class ObjectPool<T> where T : class, IAstarPooledObject, new()
	{
		private static List<T> pool;

		static ObjectPool()
		{
			pool = new List<T>();
		}

		public static T Claim()
		{
			if (pool.Count > 0)
			{
				T result = pool[pool.Count - 1];
				pool.RemoveAt(pool.Count - 1);
				return result;
			}
			return new T();
		}

		public static void Warmup(int count)
		{
			T[] array = new T[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = Claim();
			}
			for (int j = 0; j < count; j++)
			{
				Release(array[j]);
			}
		}

		public static void Release(T obj)
		{
			for (int i = 0; i < pool.Count; i++)
			{
				if (pool[i] == obj)
				{
					throw new InvalidOperationException("The object is released even though it is in the pool. Are you releasing it twice?");
				}
			}
			obj.OnEnterPool();
			pool.Add(obj);
		}

		public static void Clear()
		{
			pool.Clear();
		}

		public static int GetSize()
		{
			return pool.Count;
		}
	}
}
