using System;
using System.Collections;
using Pathfinding;
using UnityEngine;

[HelpURL("http://arongranberg.com/astar/docs/class_procedural_grid_mover.php")]
public class ProceduralGridMover : MonoBehaviour
{
	public float updateDistance = 10f;

	public Transform target;

	public bool floodFill;

	private GridGraph graph;

	private GridNode[] tmp;

	public bool updatingGraph { get; private set; }

	public void Start()
	{
		if (AstarPath.active == null)
		{
			throw new Exception("There is no AstarPath object in the scene");
		}
		graph = AstarPath.active.astarData.gridGraph;
		if (graph == null)
		{
			throw new Exception("The AstarPath object has no GridGraph");
		}
		UpdateGraph();
	}

	private void Update()
	{
		Vector3 a = PointToGraphSpace(graph.center);
		Vector3 b = PointToGraphSpace(target.position);
		if (VectorMath.SqrDistanceXZ(a, b) > updateDistance * updateDistance)
		{
			UpdateGraph();
		}
	}

	private Vector3 PointToGraphSpace(Vector3 p)
	{
		return graph.inverseMatrix.MultiplyPoint(p);
	}

	public void UpdateGraph()
	{
		if (updatingGraph)
		{
			return;
		}
		updatingGraph = true;
		IEnumerator ie = UpdateGraphCoroutine();
		AstarPath.active.AddWorkItem(new AstarPath.AstarWorkItem(delegate(bool force)
		{
			if (force)
			{
				while (ie.MoveNext())
				{
				}
			}
			bool flag = !ie.MoveNext();
			if (flag)
			{
				updatingGraph = false;
			}
			return flag;
		}));
	}

	private IEnumerator UpdateGraphCoroutine()
	{
		Vector3 dir = PointToGraphSpace(target.position) - PointToGraphSpace(graph.center);
		dir.x = Mathf.Round(dir.x);
		dir.z = Mathf.Round(dir.z);
		dir.y = 0f;
		if (dir == Vector3.zero)
		{
			yield break;
		}
		Int2 offset = new Int2(-Mathf.RoundToInt(dir.x), -Mathf.RoundToInt(dir.z));
		graph.center += graph.matrix.MultiplyVector(dir);
		graph.GenerateMatrix();
		if (tmp == null || tmp.Length != graph.nodes.Length)
		{
			tmp = new GridNode[graph.nodes.Length];
		}
		int width = graph.width;
		int depth = graph.depth;
		GridNode[] nodes = graph.nodes;
		if (Mathf.Abs(offset.x) <= width && Mathf.Abs(offset.y) <= depth)
		{
			for (int z9 = 0; z9 < depth; z9++)
			{
				int pz = z9 * width;
				int tz = (z9 + offset.y + depth) % depth * width;
				for (int x3 = 0; x3 < width; x3++)
				{
					tmp[tz + (x3 + offset.x + width) % width] = nodes[pz + x3];
				}
			}
			yield return null;
			for (int z8 = 0; z8 < depth; z8++)
			{
				int pz2 = z8 * width;
				for (int x4 = 0; x4 < width; x4++)
				{
					GridNode node = tmp[pz2 + x4];
					node.NodeInGridIndex = pz2 + x4;
					nodes[pz2 + x4] = node;
				}
			}
			IntRect r3 = new IntRect(0, 0, offset.x, offset.y);
			int minz = r3.ymax;
			int maxz = depth;
			if (r3.xmin > r3.xmax)
			{
				int tmp3 = r3.xmax;
				r3.xmax = width + r3.xmin;
				r3.xmin = width + tmp3;
			}
			if (r3.ymin > r3.ymax)
			{
				int tmp2 = r3.ymax;
				r3.ymax = depth + r3.ymin;
				r3.ymin = depth + tmp2;
				minz = 0;
				maxz = r3.ymin;
			}
			r3 = r3.Expand(graph.erodeIterations + 1);
			r3 = IntRect.Intersection(r3, new IntRect(0, 0, width, depth));
			yield return null;
			for (int z7 = r3.ymin; z7 < r3.ymax; z7++)
			{
				for (int x5 = 0; x5 < width; x5++)
				{
					graph.UpdateNodePositionCollision(nodes[z7 * width + x5], x5, z7, false);
				}
			}
			yield return null;
			for (int z6 = minz; z6 < maxz; z6++)
			{
				for (int x6 = r3.xmin; x6 < r3.xmax; x6++)
				{
					graph.UpdateNodePositionCollision(nodes[z6 * width + x6], x6, z6, false);
				}
			}
			yield return null;
			for (int z5 = r3.ymin; z5 < r3.ymax; z5++)
			{
				for (int x7 = 0; x7 < width; x7++)
				{
					graph.CalculateConnections(x7, z5, nodes[z5 * width + x7]);
				}
			}
			yield return null;
			for (int z4 = minz; z4 < maxz; z4++)
			{
				for (int x8 = r3.xmin; x8 < r3.xmax; x8++)
				{
					graph.CalculateConnections(x8, z4, nodes[z4 * width + x8]);
				}
			}
			yield return null;
			for (int z3 = 0; z3 < depth; z3++)
			{
				for (int x9 = 0; x9 < width; x9++)
				{
					if (x9 == 0 || z3 == 0 || x9 >= width - 1 || z3 >= depth - 1)
					{
						graph.CalculateConnections(x9, z3, nodes[z3 * width + x9]);
					}
				}
			}
		}
		else
		{
			for (int z2 = 0; z2 < depth; z2++)
			{
				for (int x = 0; x < width; x++)
				{
					graph.UpdateNodePositionCollision(nodes[z2 * width + x], x, z2, false);
				}
			}
			for (int z = 0; z < depth; z++)
			{
				for (int x2 = 0; x2 < width; x2++)
				{
					graph.CalculateConnections(x2, z, nodes[z * width + x2]);
				}
			}
		}
		if (floodFill)
		{
			yield return null;
			AstarPath.active.QueueWorkItemFloodFill();
		}
	}
}
