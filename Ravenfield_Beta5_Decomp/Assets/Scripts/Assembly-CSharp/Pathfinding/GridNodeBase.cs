namespace Pathfinding
{
	public abstract class GridNodeBase : GraphNode
	{
		protected int nodeInGridIndex;

		public int NodeInGridIndex
		{
			get
			{
				return nodeInGridIndex;
			}
			set
			{
				nodeInGridIndex = value;
			}
		}

		protected GridNodeBase(AstarPath astar)
			: base(astar)
		{
		}
	}
}
