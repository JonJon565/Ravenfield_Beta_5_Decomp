using System;
using UnityEngine;

namespace Pathfinding
{
	[AddComponentMenu("Pathfinding/Link")]
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_node_link.php")]
	public class NodeLink : GraphModifier
	{
		public Transform end;

		public float costFactor = 1f;

		public bool oneWay;

		public bool deleteConnection;

		public Transform Start
		{
			get
			{
				return base.transform;
			}
		}

		public Transform End
		{
			get
			{
				return end;
			}
		}

		public override void OnPostScan()
		{
			if (AstarPath.active.isScanning)
			{
				InternalOnPostScan();
				return;
			}
			AstarPath.active.AddWorkItem(new AstarPath.AstarWorkItem(delegate
			{
				InternalOnPostScan();
				return true;
			}));
		}

		public void InternalOnPostScan()
		{
			Apply();
		}

		public override void OnGraphsPostUpdate()
		{
			if (!AstarPath.active.isScanning)
			{
				AstarPath.active.AddWorkItem(new AstarPath.AstarWorkItem(delegate
				{
					InternalOnPostScan();
					return true;
				}));
			}
		}

		public virtual void Apply()
		{
			if (Start == null || End == null || AstarPath.active == null)
			{
				return;
			}
			GraphNode node = AstarPath.active.GetNearest(Start.position).node;
			GraphNode node2 = AstarPath.active.GetNearest(End.position).node;
			if (node == null || node2 == null)
			{
				return;
			}
			if (deleteConnection)
			{
				node.RemoveConnection(node2);
				if (!oneWay)
				{
					node2.RemoveConnection(node);
				}
				return;
			}
			uint cost = (uint)Math.Round((float)(node.position - node2.position).costMagnitude * costFactor);
			node.AddConnection(node2, cost);
			if (!oneWay)
			{
				node2.AddConnection(node, cost);
			}
		}

		public void OnDrawGizmos()
		{
			if (!(Start == null) && !(End == null))
			{
				Vector3 position = Start.position;
				Vector3 position2 = End.position;
				Gizmos.color = ((!deleteConnection) ? Color.green : Color.red);
				DrawGizmoBezier(position, position2);
			}
		}

		private void DrawGizmoBezier(Vector3 p1, Vector3 p2)
		{
			Vector3 vector = p2 - p1;
			if (!(vector == Vector3.zero))
			{
				Vector3 rhs = Vector3.Cross(Vector3.up, vector);
				Vector3 normalized = Vector3.Cross(vector, rhs).normalized;
				normalized *= vector.magnitude * 0.1f;
				Vector3 p3 = p1 + normalized;
				Vector3 p4 = p2 + normalized;
				Vector3 from = p1;
				for (int i = 1; i <= 20; i++)
				{
					float t = (float)i / 20f;
					Vector3 vector2 = AstarSplines.CubicBezier(p1, p3, p4, p2, t);
					Gizmos.DrawLine(from, vector2);
					from = vector2;
				}
			}
		}
	}
}
