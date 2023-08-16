using System;
using System.Collections.Generic;
using Pathfinding.Serialization;
using UnityEngine;

namespace Pathfinding
{
	public abstract class GraphNode
	{
		private const int FlagsWalkableOffset = 0;

		private const uint FlagsWalkableMask = 1u;

		private const int FlagsAreaOffset = 1;

		private const uint FlagsAreaMask = 262142u;

		private const int FlagsGraphOffset = 24;

		private const uint FlagsGraphMask = 4278190080u;

		public const uint MaxAreaIndex = 131071u;

		public const uint MaxGraphIndex = 255u;

		private const int FlagsTagOffset = 19;

		private const uint FlagsTagMask = 16252928u;

		private int nodeIndex;

		protected uint flags;

		private uint penalty;

		public Int3 position;

		public bool Destroyed
		{
			get
			{
				return nodeIndex == -1;
			}
		}

		public int NodeIndex
		{
			get
			{
				return nodeIndex;
			}
		}

		public uint Flags
		{
			get
			{
				return flags;
			}
			set
			{
				flags = value;
			}
		}

		public uint Penalty
		{
			get
			{
				return penalty;
			}
			set
			{
				if (value > 16777215)
				{
					Debug.LogWarning("Very high penalty applied. Are you sure negative values haven't underflowed?\nPenalty values this high could with long paths cause overflows and in some cases infinity loops because of that.\nPenalty value applied: " + value);
				}
				penalty = value;
			}
		}

		public bool Walkable
		{
			get
			{
				return (flags & 1) != 0;
			}
			set
			{
				flags = (flags & 0xFFFFFFFEu) | (value ? 1u : 0u);
			}
		}

		public uint Area
		{
			get
			{
				return (flags & 0x3FFFE) >> 1;
			}
			set
			{
				flags = (flags & 0xFFFC0001u) | (value << 1);
			}
		}

		public uint GraphIndex
		{
			get
			{
				return (flags & 0xFF000000u) >> 24;
			}
			set
			{
				flags = (flags & 0xFFFFFFu) | (value << 24);
			}
		}

		public uint Tag
		{
			get
			{
				return (flags & 0xF80000) >> 19;
			}
			set
			{
				flags = (flags & 0xFF07FFFFu) | (value << 19);
			}
		}

		protected GraphNode(AstarPath astar)
		{
			if (!object.ReferenceEquals(astar, null))
			{
				nodeIndex = astar.GetNewNodeIndex();
				astar.InitializeNode(this);
				return;
			}
			throw new Exception("No active AstarPath object to bind to");
		}

		public void Destroy()
		{
			if (!Destroyed)
			{
				ClearConnections(true);
				if (AstarPath.active != null)
				{
					AstarPath.active.DestroyNode(this);
				}
				nodeIndex = -1;
			}
		}

		public void UpdateG(Path path, PathNode pathNode)
		{
			pathNode.G = pathNode.parent.G + pathNode.cost + path.GetTraversalCost(this);
		}

		public virtual void UpdateRecursiveG(Path path, PathNode pathNode, PathHandler handler)
		{
			UpdateG(path, pathNode);
			handler.PushNode(pathNode);
			GetConnections(delegate(GraphNode other)
			{
				PathNode pathNode2 = handler.GetPathNode(other);
				if (pathNode2.parent == pathNode && pathNode2.pathID == handler.PathID)
				{
					other.UpdateRecursiveG(path, pathNode2, handler);
				}
			});
		}

		public virtual void FloodFill(Stack<GraphNode> stack, uint region)
		{
			GetConnections(delegate(GraphNode other)
			{
				if (other.Area != region)
				{
					other.Area = region;
					stack.Push(other);
				}
			});
		}

		public abstract void GetConnections(GraphNodeDelegate del);

		public abstract void AddConnection(GraphNode node, uint cost);

		public abstract void RemoveConnection(GraphNode node);

		public abstract void ClearConnections(bool alsoReverse);

		public virtual bool ContainsConnection(GraphNode node)
		{
			bool contains = false;
			GetConnections(delegate(GraphNode neighbour)
			{
				contains |= neighbour == node;
			});
			return contains;
		}

		public virtual void RecalculateConnectionCosts()
		{
		}

		public virtual bool GetPortal(GraphNode other, List<Vector3> left, List<Vector3> right, bool backwards)
		{
			return false;
		}

		public abstract void Open(Path path, PathNode pathNode, PathHandler handler);

		public virtual void SerializeNode(GraphSerializationContext ctx)
		{
			ctx.writer.Write(Penalty);
			ctx.writer.Write(Flags);
		}

		public virtual void DeserializeNode(GraphSerializationContext ctx)
		{
			Penalty = ctx.reader.ReadUInt32();
			Flags = ctx.reader.ReadUInt32();
			GraphIndex = ctx.graphIndex;
		}

		public virtual void SerializeReferences(GraphSerializationContext ctx)
		{
		}

		public virtual void DeserializeReferences(GraphSerializationContext ctx)
		{
		}
	}
}
