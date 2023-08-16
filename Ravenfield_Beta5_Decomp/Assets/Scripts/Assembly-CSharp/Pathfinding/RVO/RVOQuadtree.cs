using System;
using Pathfinding.RVO.Sampled;
using UnityEngine;

namespace Pathfinding.RVO
{
	public class RVOQuadtree
	{
		private struct Node
		{
			public int child00;

			public int child01;

			public int child10;

			public int child11;

			public byte count;

			public Agent linkedList;

			public void Add(Agent agent)
			{
				agent.next = linkedList;
				linkedList = agent;
			}

			public void Distribute(Node[] nodes, Rect r)
			{
				Vector2 center = r.center;
				while (linkedList != null)
				{
					Agent next = linkedList.next;
					if (linkedList.position.x > center.x)
					{
						if (linkedList.position.z > center.y)
						{
							nodes[child11].Add(linkedList);
						}
						else
						{
							nodes[child10].Add(linkedList);
						}
					}
					else if (linkedList.position.z > center.y)
					{
						nodes[child01].Add(linkedList);
					}
					else
					{
						nodes[child00].Add(linkedList);
					}
					linkedList = next;
				}
				count = 0;
			}
		}

		private const int LeafSize = 15;

		private float maxRadius;

		private Node[] nodes = new Node[42];

		private int filledNodes = 1;

		private Rect bounds;

		public void Clear()
		{
			nodes[0] = default(Node);
			filledNodes = 1;
			maxRadius = 0f;
		}

		public void SetBounds(Rect r)
		{
			bounds = r;
		}

		public int GetNodeIndex()
		{
			if (filledNodes == nodes.Length)
			{
				Node[] array = new Node[nodes.Length * 2];
				for (int i = 0; i < nodes.Length; i++)
				{
					array[i] = nodes[i];
				}
				nodes = array;
			}
			nodes[filledNodes] = default(Node);
			nodes[filledNodes].child00 = filledNodes;
			filledNodes++;
			return filledNodes - 1;
		}

		public void Insert(Agent agent)
		{
			int num = 0;
			Rect r = bounds;
			Vector2 vector = new Vector2(agent.position.x, agent.position.z);
			agent.next = null;
			maxRadius = Math.Max(agent.radius, maxRadius);
			int num2 = 0;
			while (true)
			{
				num2++;
				if (nodes[num].child00 == num)
				{
					if (nodes[num].count < 15 || num2 > 10)
					{
						break;
					}
					Node node = nodes[num];
					node.child00 = GetNodeIndex();
					node.child01 = GetNodeIndex();
					node.child10 = GetNodeIndex();
					node.child11 = GetNodeIndex();
					nodes[num] = node;
					nodes[num].Distribute(nodes, r);
				}
				if (nodes[num].child00 == num)
				{
					continue;
				}
				Vector2 center = r.center;
				if (vector.x > center.x)
				{
					if (vector.y > center.y)
					{
						num = nodes[num].child11;
						r = Rect.MinMaxRect(center.x, center.y, r.xMax, r.yMax);
					}
					else
					{
						num = nodes[num].child10;
						r = Rect.MinMaxRect(center.x, r.yMin, r.xMax, center.y);
					}
				}
				else if (vector.y > center.y)
				{
					num = nodes[num].child01;
					r = Rect.MinMaxRect(r.xMin, center.y, center.x, r.yMax);
				}
				else
				{
					num = nodes[num].child00;
					r = Rect.MinMaxRect(r.xMin, r.yMin, center.x, center.y);
				}
			}
			nodes[num].Add(agent);
			nodes[num].count++;
		}

		public void Query(Vector2 p, float radius, Agent agent)
		{
			QueryRec(0, p, radius, agent, bounds);
		}

		private float QueryRec(int i, Vector2 p, float radius, Agent agent, Rect r)
		{
			if (nodes[i].child00 == i)
			{
				for (Agent agent2 = nodes[i].linkedList; agent2 != null; agent2 = agent2.next)
				{
					float num = agent.InsertAgentNeighbour(agent2, radius * radius);
					if (num < radius * radius)
					{
						radius = Mathf.Sqrt(num);
					}
				}
			}
			else
			{
				Vector2 center = r.center;
				if (p.x - radius < center.x)
				{
					if (p.y - radius < center.y)
					{
						radius = QueryRec(nodes[i].child00, p, radius, agent, Rect.MinMaxRect(r.xMin, r.yMin, center.x, center.y));
					}
					if (p.y + radius > center.y)
					{
						radius = QueryRec(nodes[i].child01, p, radius, agent, Rect.MinMaxRect(r.xMin, center.y, center.x, r.yMax));
					}
				}
				if (p.x + radius > center.x)
				{
					if (p.y - radius < center.y)
					{
						radius = QueryRec(nodes[i].child10, p, radius, agent, Rect.MinMaxRect(center.x, r.yMin, r.xMax, center.y));
					}
					if (p.y + radius > center.y)
					{
						radius = QueryRec(nodes[i].child11, p, radius, agent, Rect.MinMaxRect(center.x, center.y, r.xMax, r.yMax));
					}
				}
			}
			return radius;
		}

		public void DebugDraw()
		{
			DebugDrawRec(0, bounds);
		}

		private void DebugDrawRec(int i, Rect r)
		{
			Debug.DrawLine(new Vector3(r.xMin, 0f, r.yMin), new Vector3(r.xMax, 0f, r.yMin), Color.white);
			Debug.DrawLine(new Vector3(r.xMax, 0f, r.yMin), new Vector3(r.xMax, 0f, r.yMax), Color.white);
			Debug.DrawLine(new Vector3(r.xMax, 0f, r.yMax), new Vector3(r.xMin, 0f, r.yMax), Color.white);
			Debug.DrawLine(new Vector3(r.xMin, 0f, r.yMax), new Vector3(r.xMin, 0f, r.yMin), Color.white);
			if (nodes[i].child00 != i)
			{
				Vector2 center = r.center;
				DebugDrawRec(nodes[i].child11, Rect.MinMaxRect(center.x, center.y, r.xMax, r.yMax));
				DebugDrawRec(nodes[i].child10, Rect.MinMaxRect(center.x, r.yMin, r.xMax, center.y));
				DebugDrawRec(nodes[i].child01, Rect.MinMaxRect(r.xMin, center.y, center.x, r.yMax));
				DebugDrawRec(nodes[i].child00, Rect.MinMaxRect(r.xMin, r.yMin, center.x, center.y));
			}
			for (Agent agent = nodes[i].linkedList; agent != null; agent = agent.next)
			{
				Debug.DrawLine(nodes[i].linkedList.position + Vector3.up, agent.position + Vector3.up, new Color(1f, 1f, 0f, 0.5f));
			}
		}
	}
}
