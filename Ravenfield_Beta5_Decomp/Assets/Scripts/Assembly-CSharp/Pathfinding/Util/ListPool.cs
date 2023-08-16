using System.Collections.Generic;

namespace Pathfinding.Util
{
	public static class ListPool<T>
	{
		private const int MaxCapacitySearchLength = 8;

		private static readonly List<List<T>> pool = new List<List<T>>();

		private static readonly HashSet<List<T>> inPool = new HashSet<List<T>>();

		public static List<T> Claim()
		{
			//Discarded unreachable code: IL_0067
			lock (pool)
			{
				if (pool.Count > 0)
				{
					List<T> list = pool[pool.Count - 1];
					pool.RemoveAt(pool.Count - 1);
					inPool.Remove(list);
					return list;
				}
				return new List<T>();
			}
		}

		public static List<T> Claim(int capacity)
		{
			//Discarded unreachable code: IL_0115
			lock (pool)
			{
				List<T> list = null;
				int index = -1;
				for (int i = 0; i < pool.Count && i < 8; i++)
				{
					List<T> list2 = pool[pool.Count - 1 - i];
					if (list2.Capacity >= capacity)
					{
						pool.RemoveAt(pool.Count - 1 - i);
						inPool.Remove(list2);
						return list2;
					}
					if (list == null || list2.Capacity > list.Capacity)
					{
						list = list2;
						index = pool.Count - 1 - i;
					}
				}
				if (list == null)
				{
					list = new List<T>(capacity);
				}
				else
				{
					list.Capacity = capacity;
					pool[index] = pool[pool.Count - 1];
					pool.RemoveAt(pool.Count - 1);
					inPool.Remove(list);
				}
				return list;
			}
		}

		public static void Warmup(int count, int size)
		{
			lock (pool)
			{
				List<T>[] array = new List<T>[count];
				for (int i = 0; i < count; i++)
				{
					array[i] = Claim(size);
				}
				for (int j = 0; j < count; j++)
				{
					Release(array[j]);
				}
			}
		}

		public static void Release(List<T> list)
		{
			list.Clear();
			lock (pool)
			{
				pool.Add(list);
			}
		}

		public static void Clear()
		{
			lock (pool)
			{
				pool.Clear();
			}
		}

		public static int GetSize()
		{
			return pool.Count;
		}
	}
}
