using System;

namespace Pathfinding.Util
{
	public static class Memory
	{
		public static void MemSet(byte[] array, byte value)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			int num = 32;
			int num2 = 0;
			int num3 = Math.Min(num, array.Length);
			while (num2 < num3)
			{
				array[num2++] = value;
			}
			num3 = array.Length;
			while (num2 < num3)
			{
				Buffer.BlockCopy(array, 0, array, num2, Math.Min(num, num3 - num2));
				num2 += num;
				num *= 2;
			}
		}

		public static void MemSet<T>(T[] array, T value, int byteSize) where T : struct
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			int num = 32;
			int i = 0;
			int num2;
			for (num2 = Math.Min(num, array.Length); i < num2; i++)
			{
				array[i] = value;
			}
			num2 = array.Length;
			while (i < num2)
			{
				Buffer.BlockCopy(array, 0, array, i * byteSize, Math.Min(num, num2 - i) * byteSize);
				i += num;
				num *= 2;
			}
		}

		public static void MemSet<T>(T[] array, T value, int totalSize, int byteSize) where T : struct
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			int num = 32;
			int i = 0;
			int num2;
			for (num2 = Math.Min(num, totalSize); i < num2; i++)
			{
				array[i] = value;
			}
			num2 = totalSize;
			while (i < num2)
			{
				Buffer.BlockCopy(array, 0, array, i * byteSize, Math.Min(num, totalSize - i) * byteSize);
				i += num;
				num *= 2;
			}
		}
	}
}
