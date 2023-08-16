using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding.RVO
{
	[AddComponentMenu("Pathfinding/Local Avoidance/RVO Navmesh")]
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_r_v_o_1_1_r_v_o_navmesh.php")]
	public class RVONavmesh : GraphModifier
	{
		public float wallHeight = 5f;

		private readonly List<ObstacleVertex> obstacles = new List<ObstacleVertex>();

		private Simulator lastSim;

		public override void OnPostCacheLoad()
		{
			OnLatePostScan();
		}

		public override void OnLatePostScan()
		{
			if (Application.isPlaying)
			{
				RemoveObstacles();
				NavGraph[] graphs = AstarPath.active.graphs;
				RVOSimulator rVOSimulator = UnityEngine.Object.FindObjectOfType(typeof(RVOSimulator)) as RVOSimulator;
				if (rVOSimulator == null)
				{
					throw new NullReferenceException("No RVOSimulator could be found in the scene. Please add one to any GameObject");
				}
				Simulator simulator = rVOSimulator.GetSimulator();
				for (int i = 0; i < graphs.Length; i++)
				{
					AddGraphObstacles(simulator, graphs[i]);
				}
				simulator.UpdateObstacles();
			}
		}

		public void RemoveObstacles()
		{
			if (lastSim != null)
			{
				Simulator simulator = lastSim;
				lastSim = null;
				for (int i = 0; i < obstacles.Count; i++)
				{
					simulator.RemoveObstacle(obstacles[i]);
				}
				obstacles.Clear();
			}
		}

		public void AddGraphObstacles(Simulator sim, NavGraph graph)
		{
			if (obstacles.Count > 0 && lastSim != null && lastSim != sim)
			{
				Debug.LogError("Simulator has changed but some old obstacles are still added for the previous simulator. Deleting previous obstacles.");
				RemoveObstacles();
			}
			lastSim = sim;
			INavmesh navmesh = graph as INavmesh;
			if (navmesh == null)
			{
				return;
			}
			int[] uses = new int[20];
			navmesh.GetNodes(delegate(GraphNode _node)
			{
				TriangleMeshNode triangleMeshNode = _node as TriangleMeshNode;
				uses[0] = (uses[1] = (uses[2] = 0));
				if (triangleMeshNode != null)
				{
					for (int i = 0; i < triangleMeshNode.connections.Length; i++)
					{
						TriangleMeshNode triangleMeshNode2 = triangleMeshNode.connections[i] as TriangleMeshNode;
						if (triangleMeshNode2 != null)
						{
							int num = triangleMeshNode.SharedEdge(triangleMeshNode2);
							if (num != -1)
							{
								uses[num] = 1;
							}
						}
					}
					for (int j = 0; j < 3; j++)
					{
						if (uses[j] == 0)
						{
							Vector3 a = (Vector3)triangleMeshNode.GetVertex(j);
							Vector3 b = (Vector3)triangleMeshNode.GetVertex((j + 1) % triangleMeshNode.GetVertexCount());
							float val = Math.Abs(a.y - b.y);
							val = Math.Max(val, 5f);
							obstacles.Add(sim.AddObstacle(a, b, wallHeight));
						}
					}
				}
				return true;
			});
		}
	}
}
