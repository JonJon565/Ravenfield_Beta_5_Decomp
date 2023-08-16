using Pathfinding.Serialization;
using UnityEngine;

namespace Pathfinding
{
	public class PointNode : GraphNode
	{
		public GraphNode[] connections;

		public uint[] connectionCosts;

		public GameObject gameObject;

		public PointNode next;

		public PointNode(AstarPath astar)
			: base(astar)
		{
		}

		public void SetPosition(Int3 value)
		{
			position = value;
		}

		public override void GetConnections(GraphNodeDelegate del)
		{
			if (connections != null)
			{
				for (int i = 0; i < connections.Length; i++)
				{
					del(connections[i]);
				}
			}
		}

		public override void ClearConnections(bool alsoReverse)
		{
			if (alsoReverse && connections != null)
			{
				for (int i = 0; i < connections.Length; i++)
				{
					connections[i].RemoveConnection(this);
				}
			}
			connections = null;
			connectionCosts = null;
		}

		public override void UpdateRecursiveG(Path path, PathNode pathNode, PathHandler handler)
		{
			UpdateG(path, pathNode);
			handler.PushNode(pathNode);
			for (int i = 0; i < connections.Length; i++)
			{
				GraphNode graphNode = connections[i];
				PathNode pathNode2 = handler.GetPathNode(graphNode);
				if (pathNode2.parent == pathNode && pathNode2.pathID == handler.PathID)
				{
					graphNode.UpdateRecursiveG(path, pathNode2, handler);
				}
			}
		}

		public override bool ContainsConnection(GraphNode node)
		{
			for (int i = 0; i < connections.Length; i++)
			{
				if (connections[i] == node)
				{
					return true;
				}
			}
			return false;
		}

		public override void AddConnection(GraphNode node, uint cost)
		{
			if (connections != null)
			{
				for (int i = 0; i < connections.Length; i++)
				{
					if (connections[i] == node)
					{
						connectionCosts[i] = cost;
						return;
					}
				}
			}
			int num = ((connections != null) ? connections.Length : 0);
			GraphNode[] array = new GraphNode[num + 1];
			uint[] array2 = new uint[num + 1];
			for (int j = 0; j < num; j++)
			{
				array[j] = connections[j];
				array2[j] = connectionCosts[j];
			}
			array[num] = node;
			array2[num] = cost;
			connections = array;
			connectionCosts = array2;
		}

		public override void RemoveConnection(GraphNode node)
		{
			if (connections == null)
			{
				return;
			}
			for (int i = 0; i < connections.Length; i++)
			{
				if (connections[i] == node)
				{
					int num = connections.Length;
					GraphNode[] array = new GraphNode[num - 1];
					uint[] array2 = new uint[num - 1];
					for (int j = 0; j < i; j++)
					{
						array[j] = connections[j];
						array2[j] = connectionCosts[j];
					}
					for (int k = i + 1; k < num; k++)
					{
						array[k - 1] = connections[k];
						array2[k - 1] = connectionCosts[k];
					}
					connections = array;
					connectionCosts = array2;
					break;
				}
			}
		}

		public override void Open(Path path, PathNode pathNode, PathHandler handler)
		{
			if (connections == null)
			{
				return;
			}
			for (int i = 0; i < connections.Length; i++)
			{
				GraphNode graphNode = connections[i];
				if (!path.CanTraverse(graphNode))
				{
					continue;
				}
				PathNode pathNode2 = handler.GetPathNode(graphNode);
				if (pathNode2.pathID != handler.PathID)
				{
					pathNode2.parent = pathNode;
					pathNode2.pathID = handler.PathID;
					pathNode2.cost = connectionCosts[i];
					pathNode2.H = path.CalculateHScore(graphNode);
					graphNode.UpdateG(path, pathNode2);
					handler.PushNode(pathNode2);
					continue;
				}
				uint num = connectionCosts[i];
				if (pathNode.G + num + path.GetTraversalCost(graphNode) < pathNode2.G)
				{
					pathNode2.cost = num;
					pathNode2.parent = pathNode;
					graphNode.UpdateRecursiveG(path, pathNode2, handler);
				}
				else if (pathNode2.G + num + path.GetTraversalCost(this) < pathNode.G && graphNode.ContainsConnection(this))
				{
					pathNode.parent = pathNode2;
					pathNode.cost = num;
					UpdateRecursiveG(path, pathNode, handler);
				}
			}
		}

		public override void SerializeNode(GraphSerializationContext ctx)
		{
			base.SerializeNode(ctx);
			ctx.writer.Write(position.x);
			ctx.writer.Write(position.y);
			ctx.writer.Write(position.z);
		}

		public override void DeserializeNode(GraphSerializationContext ctx)
		{
			base.DeserializeNode(ctx);
			position = new Int3(ctx.reader.ReadInt32(), ctx.reader.ReadInt32(), ctx.reader.ReadInt32());
		}

		public override void SerializeReferences(GraphSerializationContext ctx)
		{
			if (connections == null)
			{
				ctx.writer.Write(-1);
				return;
			}
			ctx.writer.Write(connections.Length);
			for (int i = 0; i < connections.Length; i++)
			{
				ctx.writer.Write(ctx.GetNodeIdentifier(connections[i]));
				ctx.writer.Write(connectionCosts[i]);
			}
		}

		public override void DeserializeReferences(GraphSerializationContext ctx)
		{
			int num = ctx.reader.ReadInt32();
			if (num == -1)
			{
				connections = null;
				connectionCosts = null;
				return;
			}
			connections = new GraphNode[num];
			connectionCosts = new uint[num];
			for (int i = 0; i < num; i++)
			{
				connections[i] = ctx.GetNodeFromIdentifier(ctx.reader.ReadInt32());
				connectionCosts[i] = ctx.reader.ReadUInt32();
			}
		}
	}
}
