using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Pathfinding.Serialization;
using Pathfinding.Util;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pathfinding
{
	[Serializable]
	public class AstarData
	{
		[NonSerialized]
		public NavGraph[] graphs = new NavGraph[0];

		[SerializeField]
		private string dataString;

		[SerializeField]
		[FormerlySerializedAs("data")]
		private byte[] upgradeData;

		public byte[] data_backup;

		public TextAsset file_cachedStartup;

		public byte[] data_cachedStartup;

		[SerializeField]
		public bool cacheStartup;

		public static AstarPath active
		{
			get
			{
				return AstarPath.active;
			}
		}

		public NavMeshGraph navmesh { get; private set; }

		public GridGraph gridGraph { get; private set; }

		public LayerGridGraph layerGridGraph { get; private set; }

		public PointGraph pointGraph { get; private set; }

		public RecastGraph recastGraph { get; private set; }

		public Type[] graphTypes { get; private set; }

		private byte[] data
		{
			get
			{
				if (upgradeData != null && upgradeData.Length > 0)
				{
					data = upgradeData;
					upgradeData = null;
				}
				return (dataString == null) ? null : Convert.FromBase64String(dataString);
			}
			set
			{
				dataString = ((value == null) ? null : Convert.ToBase64String(value));
			}
		}

		public byte[] GetData()
		{
			return data;
		}

		public void SetData(byte[] data)
		{
			this.data = data;
		}

		public void Awake()
		{
			graphs = new NavGraph[0];
			if (cacheStartup && file_cachedStartup != null)
			{
				LoadFromCache();
			}
			else
			{
				DeserializeGraphs();
			}
		}

		public void UpdateShortcuts()
		{
			navmesh = (NavMeshGraph)FindGraphOfType(typeof(NavMeshGraph));
			gridGraph = (GridGraph)FindGraphOfType(typeof(GridGraph));
			layerGridGraph = (LayerGridGraph)FindGraphOfType(typeof(LayerGridGraph));
			pointGraph = (PointGraph)FindGraphOfType(typeof(PointGraph));
			recastGraph = (RecastGraph)FindGraphOfType(typeof(RecastGraph));
		}

		public void LoadFromCache()
		{
			AstarPath.active.BlockUntilPathQueueBlocked();
			if (file_cachedStartup != null)
			{
				byte[] bytes = file_cachedStartup.bytes;
				DeserializeGraphs(bytes);
				GraphModifier.TriggerEvent(GraphModifier.EventType.PostCacheLoad);
			}
			else
			{
				Debug.LogError("Can't load from cache since the cache is empty");
			}
		}

		public byte[] SerializeGraphs()
		{
			return SerializeGraphs(SerializeSettings.Settings);
		}

		public byte[] SerializeGraphs(SerializeSettings settings)
		{
			uint checksum;
			return SerializeGraphs(settings, out checksum);
		}

		public byte[] SerializeGraphs(SerializeSettings settings, out uint checksum)
		{
			AstarPath.active.BlockUntilPathQueueBlocked();
			AstarSerializer astarSerializer = new AstarSerializer(this, settings);
			astarSerializer.OpenSerialize();
			SerializeGraphsPart(astarSerializer);
			byte[] result = astarSerializer.CloseSerialize();
			checksum = astarSerializer.GetChecksum();
			return result;
		}

		public void SerializeGraphsPart(AstarSerializer sr)
		{
			sr.SerializeGraphs(graphs);
			sr.SerializeExtraInfo();
		}

		public void DeserializeGraphs()
		{
			if (data != null)
			{
				DeserializeGraphs(data);
			}
		}

		private void ClearGraphs()
		{
			if (graphs == null)
			{
				return;
			}
			for (int i = 0; i < graphs.Length; i++)
			{
				if (graphs[i] != null)
				{
					graphs[i].OnDestroy();
				}
			}
			graphs = null;
			UpdateShortcuts();
		}

		public void OnDestroy()
		{
			ClearGraphs();
		}

		public void DeserializeGraphs(byte[] bytes)
		{
			AstarPath.active.BlockUntilPathQueueBlocked();
			try
			{
				if (bytes != null)
				{
					AstarSerializer astarSerializer = new AstarSerializer(this);
					if (astarSerializer.OpenDeserialize(bytes))
					{
						DeserializeGraphsPart(astarSerializer);
						astarSerializer.CloseDeserialize();
						UpdateShortcuts();
					}
					else
					{
						Debug.Log("Invalid data file (cannot read zip).\nThe data is either corrupt or it was saved using a 3.0.x or earlier version of the system");
					}
					active.VerifyIntegrity();
					return;
				}
				throw new ArgumentNullException("bytes");
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Caught exception while deserializing data.\n" + ex);
				data_backup = bytes;
			}
		}

		public void DeserializeGraphsAdditive(byte[] bytes)
		{
			AstarPath.active.BlockUntilPathQueueBlocked();
			try
			{
				if (bytes != null)
				{
					AstarSerializer astarSerializer = new AstarSerializer(this);
					if (astarSerializer.OpenDeserialize(bytes))
					{
						DeserializeGraphsPartAdditive(astarSerializer);
						astarSerializer.CloseDeserialize();
					}
					else
					{
						Debug.Log("Invalid data file (cannot read zip).");
					}
					active.VerifyIntegrity();
					return;
				}
				throw new ArgumentNullException("bytes");
			}
			catch (Exception ex)
			{
				Debug.LogWarning("Caught exception while deserializing data.\n" + ex);
			}
		}

		public void DeserializeGraphsPart(AstarSerializer sr)
		{
			ClearGraphs();
			graphs = sr.DeserializeGraphs();
			sr.DeserializeExtraInfo();
			int i;
			for (i = 0; i < graphs.Length; i++)
			{
				if (graphs[i] != null)
				{
					graphs[i].GetNodes(delegate(GraphNode node)
					{
						node.GraphIndex = (uint)i;
						return true;
					});
				}
			}
			sr.PostDeserialization();
		}

		public void DeserializeGraphsPartAdditive(AstarSerializer sr)
		{
			if (graphs == null)
			{
				graphs = new NavGraph[0];
			}
			List<NavGraph> list = new List<NavGraph>(graphs);
			sr.SetGraphIndexOffset(list.Count);
			list.AddRange(sr.DeserializeGraphs());
			graphs = list.ToArray();
			int i;
			for (i = 0; i < graphs.Length; i++)
			{
				if (graphs[i] != null)
				{
					graphs[i].GetNodes(delegate(GraphNode node)
					{
						node.GraphIndex = (uint)i;
						return true;
					});
				}
			}
			sr.DeserializeExtraInfo();
			sr.PostDeserialization();
			for (int j = 0; j < graphs.Length; j++)
			{
				for (int k = j + 1; k < graphs.Length; k++)
				{
					if (graphs[j] != null && graphs[k] != null && graphs[j].guid == graphs[k].guid)
					{
						Debug.LogWarning("Guid Conflict when importing graphs additively. Imported graph will get a new Guid.\nThis message is (relatively) harmless.");
						graphs[j].guid = Pathfinding.Util.Guid.NewGuid();
						break;
					}
				}
			}
		}

		public void FindGraphTypes()
		{
			Assembly assembly = Assembly.GetAssembly(typeof(AstarPath));
			Type[] types = assembly.GetTypes();
			List<Type> list = new List<Type>();
			Type[] array = types;
			foreach (Type type in array)
			{
				for (Type baseType = type.BaseType; baseType != null; baseType = baseType.BaseType)
				{
					if (object.Equals(baseType, typeof(NavGraph)))
					{
						list.Add(type);
						break;
					}
				}
			}
			graphTypes = list.ToArray();
		}

		[Obsolete("If really necessary. Use System.Type.GetType instead.")]
		public Type GetGraphType(string type)
		{
			for (int i = 0; i < graphTypes.Length; i++)
			{
				if (graphTypes[i].Name == type)
				{
					return graphTypes[i];
				}
			}
			return null;
		}

		[Obsolete("Use CreateGraph(System.Type) instead")]
		public NavGraph CreateGraph(string type)
		{
			Debug.Log("Creating Graph of type '" + type + "'");
			for (int i = 0; i < graphTypes.Length; i++)
			{
				if (graphTypes[i].Name == type)
				{
					return CreateGraph(graphTypes[i]);
				}
			}
			Debug.LogError("Graph type (" + type + ") wasn't found");
			return null;
		}

		public NavGraph CreateGraph(Type type)
		{
			NavGraph navGraph = Activator.CreateInstance(type) as NavGraph;
			navGraph.active = active;
			return navGraph;
		}

		[Obsolete("Use AddGraph(System.Type) instead")]
		public NavGraph AddGraph(string type)
		{
			NavGraph navGraph = null;
			for (int i = 0; i < graphTypes.Length; i++)
			{
				if (graphTypes[i].Name == type)
				{
					navGraph = CreateGraph(graphTypes[i]);
				}
			}
			if (navGraph == null)
			{
				Debug.LogError("No NavGraph of type '" + type + "' could be found");
				return null;
			}
			AddGraph(navGraph);
			return navGraph;
		}

		public NavGraph AddGraph(Type type)
		{
			NavGraph navGraph = null;
			for (int i = 0; i < graphTypes.Length; i++)
			{
				if (object.Equals(graphTypes[i], type))
				{
					navGraph = CreateGraph(graphTypes[i]);
				}
			}
			if (navGraph == null)
			{
				Debug.LogError(string.Concat("No NavGraph of type '", type, "' could be found, ", graphTypes.Length, " graph types are avaliable"));
				return null;
			}
			AddGraph(navGraph);
			return navGraph;
		}

		public void AddGraph(NavGraph graph)
		{
			AstarPath.active.BlockUntilPathQueueBlocked();
			for (int i = 0; i < graphs.Length; i++)
			{
				if (graphs[i] == null)
				{
					graphs[i] = graph;
					graph.active = active;
					graph.Awake();
					graph.graphIndex = (uint)i;
					UpdateShortcuts();
					return;
				}
			}
			if (graphs != null && (long)graphs.Length >= 255L)
			{
				throw new Exception("Graph Count Limit Reached. You cannot have more than " + 255u + " graphs. Some compiler directives can change this limit, e.g ASTAR_MORE_AREAS, look under the 'Optimizations' tab in the A* Inspector");
			}
			List<NavGraph> list = new List<NavGraph>(graphs);
			list.Add(graph);
			graphs = list.ToArray();
			UpdateShortcuts();
			graph.active = active;
			graph.Awake();
			graph.graphIndex = (uint)(graphs.Length - 1);
		}

		public bool RemoveGraph(NavGraph graph)
		{
			active.FlushWorkItems(false, true);
			active.BlockUntilPathQueueBlocked();
			graph.OnDestroy();
			int num = Array.IndexOf(graphs, graph);
			if (num == -1)
			{
				return false;
			}
			graphs[num] = null;
			UpdateShortcuts();
			return true;
		}

		public static NavGraph GetGraph(GraphNode node)
		{
			if (node == null)
			{
				return null;
			}
			AstarPath astarPath = AstarPath.active;
			if (astarPath == null)
			{
				return null;
			}
			AstarData astarData = astarPath.astarData;
			if (astarData == null)
			{
				return null;
			}
			if (astarData.graphs == null)
			{
				return null;
			}
			uint graphIndex = node.GraphIndex;
			if (graphIndex >= astarData.graphs.Length)
			{
				return null;
			}
			return astarData.graphs[graphIndex];
		}

		public NavGraph FindGraphOfType(Type type)
		{
			if (graphs != null)
			{
				for (int i = 0; i < graphs.Length; i++)
				{
					if (graphs[i] != null && object.Equals(graphs[i].GetType(), type))
					{
						return graphs[i];
					}
				}
			}
			return null;
		}

		public IEnumerable FindGraphsOfType(Type type)
		{
			if (graphs == null)
			{
				yield break;
			}
			for (int i = 0; i < graphs.Length; i++)
			{
				if (graphs[i] != null && object.Equals(graphs[i].GetType(), type))
				{
					yield return graphs[i];
				}
			}
		}

		public IEnumerable GetUpdateableGraphs()
		{
			if (graphs == null)
			{
				yield break;
			}
			for (int i = 0; i < graphs.Length; i++)
			{
				if (graphs[i] is IUpdatableGraph)
				{
					yield return graphs[i];
				}
			}
		}

		public IEnumerable GetRaycastableGraphs()
		{
			if (graphs == null)
			{
				yield break;
			}
			for (int i = 0; i < graphs.Length; i++)
			{
				if (graphs[i] is IRaycastableGraph)
				{
					yield return graphs[i];
				}
			}
		}

		public int GetGraphIndex(NavGraph graph)
		{
			if (graph == null)
			{
				throw new ArgumentNullException("graph");
			}
			if (graphs != null)
			{
				for (int i = 0; i < graphs.Length; i++)
				{
					if (graph == graphs[i])
					{
						return i;
					}
				}
			}
			Debug.LogError("Graph doesn't exist");
			return -1;
		}
	}
}
