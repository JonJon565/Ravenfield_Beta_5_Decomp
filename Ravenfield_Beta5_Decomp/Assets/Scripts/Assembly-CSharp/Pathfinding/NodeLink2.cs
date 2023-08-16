using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_node_link2.php")]
	[AddComponentMenu("Pathfinding/Link2")]
	public class NodeLink2 : GraphModifier
	{
		protected static Dictionary<GraphNode, NodeLink2> reference = new Dictionary<GraphNode, NodeLink2>();

		public Transform end;

		public float costFactor = 1f;

		public bool oneWay;

		private PointNode startNode;

		private PointNode endNode;

		private MeshNode connectedNode1;

		private MeshNode connectedNode2;

		private Vector3 clamped1;

		private Vector3 clamped2;

		private bool postScanCalled;

		private static readonly Color GizmosColor = new Color(0.80784315f, 8f / 15f, 16f / 85f, 0.5f);

		private static readonly Color GizmosColorSelected = new Color(47f / 51f, 41f / 85f, 0.1254902f, 1f);

		public Transform StartTransform
		{
			get
			{
				return base.transform;
			}
		}

		public Transform EndTransform
		{
			get
			{
				return end;
			}
		}

		public GraphNode StartNode
		{
			get
			{
				return startNode;
			}
		}

		public GraphNode EndNode
		{
			get
			{
				return endNode;
			}
		}

		public static NodeLink2 GetNodeLink(GraphNode node)
		{
			NodeLink2 value;
			reference.TryGetValue(node, out value);
			return value;
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
			if (!(EndTransform == null) && !(StartTransform == null))
			{
				if (AstarPath.active.astarData.pointGraph == null)
				{
					AstarPath.active.astarData.AddGraph(new PointGraph());
				}
				NodeLink2 value;
				if (startNode != null && reference.TryGetValue(startNode, out value) && value == this)
				{
					reference.Remove(startNode);
				}
				NodeLink2 value2;
				if (endNode != null && reference.TryGetValue(endNode, out value2) && value2 == this)
				{
					reference.Remove(endNode);
				}
				startNode = AstarPath.active.astarData.pointGraph.AddNode((Int3)StartTransform.position);
				endNode = AstarPath.active.astarData.pointGraph.AddNode((Int3)EndTransform.position);
				connectedNode1 = null;
				connectedNode2 = null;
				if (startNode == null || endNode == null)
				{
					startNode = null;
					endNode = null;
					return;
				}
				postScanCalled = true;
				reference[startNode] = this;
				reference[endNode] = this;
				Apply(true);
			}
		}

		public override void OnGraphsPostUpdate()
		{
			if (!AstarPath.active.isScanning)
			{
				if (connectedNode1 != null && connectedNode1.Destroyed)
				{
					connectedNode1 = null;
				}
				if (connectedNode2 != null && connectedNode2.Destroyed)
				{
					connectedNode2 = null;
				}
				if (!postScanCalled)
				{
					OnPostScan();
				}
				else
				{
					Apply(false);
				}
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			if (AstarPath.active != null && AstarPath.active.astarData != null && AstarPath.active.astarData.pointGraph != null)
			{
				OnGraphsPostUpdate();
			}
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			postScanCalled = false;
			NodeLink2 value;
			if (startNode != null && reference.TryGetValue(startNode, out value) && value == this)
			{
				reference.Remove(startNode);
			}
			NodeLink2 value2;
			if (endNode != null && reference.TryGetValue(endNode, out value2) && value2 == this)
			{
				reference.Remove(endNode);
			}
			if (startNode != null && endNode != null)
			{
				startNode.RemoveConnection(endNode);
				endNode.RemoveConnection(startNode);
				if (connectedNode1 != null && connectedNode2 != null)
				{
					startNode.RemoveConnection(connectedNode1);
					connectedNode1.RemoveConnection(startNode);
					endNode.RemoveConnection(connectedNode2);
					connectedNode2.RemoveConnection(endNode);
				}
			}
		}

		private void RemoveConnections(GraphNode node)
		{
			node.ClearConnections(true);
		}

		[ContextMenu("Recalculate neighbours")]
		private void ContextApplyForce()
		{
			if (Application.isPlaying)
			{
				Apply(true);
				if (AstarPath.active != null)
				{
					AstarPath.active.FloodFill();
				}
			}
		}

		public void Apply(bool forceNewCheck)
		{
			NNConstraint none = NNConstraint.None;
			int graphIndex = (int)startNode.GraphIndex;
			none.graphMask = ~(1 << graphIndex);
			startNode.SetPosition((Int3)StartTransform.position);
			endNode.SetPosition((Int3)EndTransform.position);
			RemoveConnections(startNode);
			RemoveConnections(endNode);
			uint cost = (uint)Mathf.RoundToInt((float)((Int3)(StartTransform.position - EndTransform.position)).costMagnitude * costFactor);
			startNode.AddConnection(endNode, cost);
			endNode.AddConnection(startNode, cost);
			if (connectedNode1 == null || forceNewCheck)
			{
				NNInfo nearest = AstarPath.active.GetNearest(StartTransform.position, none);
				connectedNode1 = nearest.node as MeshNode;
				clamped1 = nearest.clampedPosition;
			}
			if (connectedNode2 == null || forceNewCheck)
			{
				NNInfo nearest2 = AstarPath.active.GetNearest(EndTransform.position, none);
				connectedNode2 = nearest2.node as MeshNode;
				clamped2 = nearest2.clampedPosition;
			}
			if (connectedNode2 != null && connectedNode1 != null)
			{
				connectedNode1.AddConnection(startNode, (uint)Mathf.RoundToInt((float)((Int3)(clamped1 - StartTransform.position)).costMagnitude * costFactor));
				if (!oneWay)
				{
					connectedNode2.AddConnection(endNode, (uint)Mathf.RoundToInt((float)((Int3)(clamped2 - EndTransform.position)).costMagnitude * costFactor));
				}
				if (!oneWay)
				{
					startNode.AddConnection(connectedNode1, (uint)Mathf.RoundToInt((float)((Int3)(clamped1 - StartTransform.position)).costMagnitude * costFactor));
				}
				endNode.AddConnection(connectedNode2, (uint)Mathf.RoundToInt((float)((Int3)(clamped2 - EndTransform.position)).costMagnitude * costFactor));
			}
		}

		private void DrawCircle(Vector3 o, float r, int detail, Color col)
		{
			Vector3 from = new Vector3(Mathf.Cos(0f) * r, 0f, Mathf.Sin(0f) * r) + o;
			Gizmos.color = col;
			for (int i = 0; i <= detail; i++)
			{
				float f = (float)i * (float)Math.PI * 2f / (float)detail;
				Vector3 vector = new Vector3(Mathf.Cos(f) * r, 0f, Mathf.Sin(f) * r) + o;
				Gizmos.DrawLine(from, vector);
				from = vector;
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

		public virtual void OnDrawGizmosSelected()
		{
			OnDrawGizmos(true);
		}

		public void OnDrawGizmos()
		{
			OnDrawGizmos(false);
		}

		public void OnDrawGizmos(bool selected)
		{
			Color color = ((!selected) ? GizmosColor : GizmosColorSelected);
			if (StartTransform != null)
			{
				DrawCircle(StartTransform.position, 0.4f, 10, color);
			}
			if (EndTransform != null)
			{
				DrawCircle(EndTransform.position, 0.4f, 10, color);
			}
			if (StartTransform != null && EndTransform != null)
			{
				Gizmos.color = color;
				DrawGizmoBezier(StartTransform.position, EndTransform.position);
				if (selected)
				{
					Vector3 normalized = Vector3.Cross(Vector3.up, EndTransform.position - StartTransform.position).normalized;
					DrawGizmoBezier(StartTransform.position + normalized * 0.1f, EndTransform.position + normalized * 0.1f);
					DrawGizmoBezier(StartTransform.position - normalized * 0.1f, EndTransform.position - normalized * 0.1f);
				}
			}
		}
	}
}
