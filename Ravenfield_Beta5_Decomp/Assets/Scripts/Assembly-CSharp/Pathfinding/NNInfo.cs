using UnityEngine;

namespace Pathfinding
{
	public struct NNInfo
	{
		public GraphNode node;

		public GraphNode constrainedNode;

		public Vector3 clampedPosition;

		public Vector3 constClampedPosition;

		public NNInfo(GraphNode node)
		{
			this.node = node;
			constrainedNode = null;
			clampedPosition = Vector3.zero;
			constClampedPosition = Vector3.zero;
			UpdateInfo();
		}

		public void SetConstrained(GraphNode constrainedNode, Vector3 clampedPosition)
		{
			this.constrainedNode = constrainedNode;
			constClampedPosition = clampedPosition;
		}

		public void UpdateInfo()
		{
			clampedPosition = ((node == null) ? Vector3.zero : ((Vector3)node.position));
			constClampedPosition = ((constrainedNode == null) ? Vector3.zero : ((Vector3)constrainedNode.position));
		}

		public static explicit operator Vector3(NNInfo ob)
		{
			return ob.clampedPosition;
		}

		public static explicit operator GraphNode(NNInfo ob)
		{
			return ob.node;
		}

		public static explicit operator NNInfo(GraphNode ob)
		{
			return new NNInfo(ob);
		}
	}
}
