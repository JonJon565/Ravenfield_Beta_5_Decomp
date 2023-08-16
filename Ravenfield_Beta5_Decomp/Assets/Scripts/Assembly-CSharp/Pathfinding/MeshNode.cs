using System.Collections.Generic;
using Pathfinding.Serialization;
using UnityEngine;

namespace Pathfinding
{
	public abstract class MeshNode : GraphNode
	{
		public GraphNode[] connections;

		public uint[] connectionCosts;

		protected MeshNode(AstarPath astar)
			: base(astar)
		{
		}

		public abstract Int3 GetVertex(int i);

		public abstract int GetVertexCount();

		public abstract Vector3 ClosestPointOnNode(Vector3 p);

		public abstract Vector3 ClosestPointOnNodeXZ(Vector3 p);

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

		public override void FloodFill(Stack<GraphNode> stack, uint region)
		{
			if (connections == null)
			{
				return;
			}
			for (int i = 0; i < connections.Length; i++)
			{
				GraphNode graphNode = connections[i];
				if (graphNode.Area != region)
				{
					graphNode.Area = region;
					stack.Push(graphNode);
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

		public virtual bool ContainsPoint(Int3 p)
		{
			bool flag = false;
			int vertexCount = GetVertexCount();
			int num = 0;
			int i = vertexCount - 1;
			while (num < vertexCount)
			{
				if (((GetVertex(num).z <= p.z && p.z < GetVertex(i).z) || (GetVertex(i).z <= p.z && p.z < GetVertex(num).z)) && p.x < (GetVertex(i).x - GetVertex(num).x) * (p.z - GetVertex(num).z) / (GetVertex(i).z - GetVertex(num).z) + GetVertex(num).x)
				{
					flag = !flag;
				}
				i = num++;
			}
			return flag;
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
