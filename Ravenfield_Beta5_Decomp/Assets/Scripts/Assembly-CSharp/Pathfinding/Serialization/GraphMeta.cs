using System;

namespace Pathfinding.Serialization
{
	internal class GraphMeta
	{
		public Version version;

		public int graphs;

		public string[] guids;

		public string[] typeNames;

		public int[] nodeCounts;

		public Type GetGraphType(int i)
		{
			if (string.IsNullOrEmpty(typeNames[i]))
			{
				return null;
			}
			Type type = Type.GetType(typeNames[i]);
			if (!object.Equals(type, null))
			{
				return type;
			}
			throw new Exception("No graph of type '" + typeNames[i] + "' could be created, type does not exist");
		}
	}
}
