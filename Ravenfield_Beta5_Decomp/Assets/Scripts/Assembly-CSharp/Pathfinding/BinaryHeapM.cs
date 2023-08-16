using System;

namespace Pathfinding
{
	public class BinaryHeapM
	{
		private struct Tuple
		{
			public uint F;

			public PathNode node;

			public Tuple(uint f, PathNode node)
			{
				F = f;
				this.node = node;
			}
		}

		private const int D = 4;

		private const bool SortGScores = true;

		public int numberOfItems;

		public float growthFactor = 2f;

		private Tuple[] binaryHeap;

		public BinaryHeapM(int numberOfElements)
		{
			binaryHeap = new Tuple[numberOfElements];
			numberOfItems = 0;
		}

		public void Clear()
		{
			numberOfItems = 0;
		}

		internal PathNode GetNode(int i)
		{
			return binaryHeap[i].node;
		}

		internal void SetF(int i, uint f)
		{
			binaryHeap[i].F = f;
		}

		public void Add(PathNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException("node");
			}
			if (numberOfItems == binaryHeap.Length)
			{
				int num = Math.Max(binaryHeap.Length + 4, (int)Math.Round((float)binaryHeap.Length * growthFactor));
				if (num > 262144)
				{
					throw new Exception("Binary Heap Size really large (2^18). A heap size this large is probably the cause of pathfinding running in an infinite loop. \nRemove this check (in BinaryHeap.cs) if you are sure that it is not caused by a bug");
				}
				Tuple[] array = new Tuple[num];
				for (int i = 0; i < binaryHeap.Length; i++)
				{
					array[i] = binaryHeap[i];
				}
				binaryHeap = array;
			}
			Tuple tuple = new Tuple(node.F, node);
			binaryHeap[numberOfItems] = tuple;
			int num2 = numberOfItems;
			uint f = node.F;
			uint g = node.G;
			while (num2 != 0)
			{
				int num3 = (num2 - 1) / 4;
				if (f < binaryHeap[num3].F || (f == binaryHeap[num3].F && g > binaryHeap[num3].node.G))
				{
					binaryHeap[num2] = binaryHeap[num3];
					binaryHeap[num3] = tuple;
					num2 = num3;
					continue;
				}
				break;
			}
			numberOfItems++;
		}

		public PathNode Remove()
		{
			numberOfItems--;
			PathNode node = binaryHeap[0].node;
			binaryHeap[0] = binaryHeap[numberOfItems];
			int num = 0;
			while (true)
			{
				int num2 = num;
				uint f = binaryHeap[num].F;
				int num3 = num2 * 4 + 1;
				if (num3 <= numberOfItems && (binaryHeap[num3].F < f || (binaryHeap[num3].F == f && binaryHeap[num3].node.G < binaryHeap[num].node.G)))
				{
					f = binaryHeap[num3].F;
					num = num3;
				}
				if (num3 + 1 <= numberOfItems && (binaryHeap[num3 + 1].F < f || (binaryHeap[num3 + 1].F == f && binaryHeap[num3 + 1].node.G < binaryHeap[num].node.G)))
				{
					f = binaryHeap[num3 + 1].F;
					num = num3 + 1;
				}
				if (num3 + 2 <= numberOfItems && (binaryHeap[num3 + 2].F < f || (binaryHeap[num3 + 2].F == f && binaryHeap[num3 + 2].node.G < binaryHeap[num].node.G)))
				{
					f = binaryHeap[num3 + 2].F;
					num = num3 + 2;
				}
				if (num3 + 3 <= numberOfItems && (binaryHeap[num3 + 3].F < f || (binaryHeap[num3 + 3].F == f && binaryHeap[num3 + 3].node.G < binaryHeap[num].node.G)))
				{
					f = binaryHeap[num3 + 3].F;
					num = num3 + 3;
				}
				if (num2 != num)
				{
					Tuple tuple = binaryHeap[num2];
					binaryHeap[num2] = binaryHeap[num];
					binaryHeap[num] = tuple;
					continue;
				}
				break;
			}
			return node;
		}

		private void Validate()
		{
			for (int i = 1; i < numberOfItems; i++)
			{
				int num = (i - 1) / 4;
				if (binaryHeap[num].F > binaryHeap[i].F)
				{
					throw new Exception("Invalid state at " + i + ":" + num + " ( " + binaryHeap[num].F + " > " + binaryHeap[i].F + " ) ");
				}
			}
		}

		public void Rebuild()
		{
			for (int i = 2; i < numberOfItems; i++)
			{
				int num = i;
				Tuple tuple = binaryHeap[i];
				uint f = tuple.F;
				while (num != 1)
				{
					int num2 = num / 4;
					if (f < binaryHeap[num2].F)
					{
						binaryHeap[num] = binaryHeap[num2];
						binaryHeap[num2] = tuple;
						num = num2;
						continue;
					}
					break;
				}
			}
		}
	}
}
