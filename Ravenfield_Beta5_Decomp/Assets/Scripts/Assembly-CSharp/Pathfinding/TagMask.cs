using System;

namespace Pathfinding
{
	[Serializable]
	public class TagMask
	{
		public int tagsChange;

		public int tagsSet;

		public TagMask()
		{
		}

		public TagMask(int change, int set)
		{
			tagsChange = change;
			tagsSet = set;
		}

		public override string ToString()
		{
			return string.Empty + Convert.ToString(tagsChange, 2) + "\n" + Convert.ToString(tagsSet, 2);
		}
	}
}
