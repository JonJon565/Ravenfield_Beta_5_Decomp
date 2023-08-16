using System;
using System.Collections.Generic;
using Pathfinding.Util;
using UnityEngine;

namespace Pathfinding
{
	public class RichPath
	{
		private int currentPart;

		private readonly List<RichPathPart> parts = new List<RichPathPart>();

		public Seeker seeker;

		public void Initialize(Seeker s, Path p, bool mergePartEndpoints, RichFunnel.FunnelSimplification simplificationMode)
		{
			if (p.error)
			{
				throw new ArgumentException("Path has an error");
			}
			List<GraphNode> path = p.path;
			if (path.Count == 0)
			{
				throw new ArgumentException("Path traverses no nodes");
			}
			seeker = s;
			for (int i = 0; i < parts.Count; i++)
			{
				if (parts[i] is RichFunnel)
				{
					ObjectPool<RichFunnel>.Release(parts[i] as RichFunnel);
				}
				else if (parts[i] is RichSpecial)
				{
					ObjectPool<RichSpecial>.Release(parts[i] as RichSpecial);
				}
			}
			parts.Clear();
			currentPart = 0;
			for (int j = 0; j < path.Count; j++)
			{
				if (path[j] is TriangleMeshNode)
				{
					NavGraph graph = AstarData.GetGraph(path[j]);
					RichFunnel richFunnel = ObjectPool<RichFunnel>.Claim().Initialize(this, graph);
					richFunnel.funnelSimplificationMode = simplificationMode;
					int num = j;
					for (uint graphIndex = path[num].GraphIndex; j < path.Count && (path[j].GraphIndex == graphIndex || path[j] is NodeLink3Node); j++)
					{
					}
					j--;
					if (num == 0)
					{
						richFunnel.exactStart = p.vectorPath[0];
					}
					else
					{
						richFunnel.exactStart = (Vector3)path[(!mergePartEndpoints) ? num : (num - 1)].position;
					}
					if (j == path.Count - 1)
					{
						richFunnel.exactEnd = p.vectorPath[p.vectorPath.Count - 1];
					}
					else
					{
						richFunnel.exactEnd = (Vector3)path[(!mergePartEndpoints) ? j : (j + 1)].position;
					}
					richFunnel.BuildFunnelCorridor(path, num, j);
					parts.Add(richFunnel);
				}
				else if (NodeLink2.GetNodeLink(path[j]) != null)
				{
					NodeLink2 nodeLink = NodeLink2.GetNodeLink(path[j]);
					int num2 = j;
					uint graphIndex2 = path[num2].GraphIndex;
					for (j++; j < path.Count && path[j].GraphIndex == graphIndex2; j++)
					{
					}
					j--;
					if (j - num2 > 1)
					{
						throw new Exception("NodeLink2 path length greater than two (2) nodes. " + (j - num2));
					}
					if (j - num2 != 0)
					{
						RichSpecial item = ObjectPool<RichSpecial>.Claim().Initialize(nodeLink, path[num2]);
						parts.Add(item);
					}
				}
			}
		}

		public bool PartsLeft()
		{
			return currentPart < parts.Count;
		}

		public void NextPart()
		{
			currentPart++;
			if (currentPart >= parts.Count)
			{
				currentPart = parts.Count;
			}
		}

		public RichPathPart GetCurrentPart()
		{
			return (currentPart >= parts.Count) ? null : parts[currentPart];
		}
	}
}
