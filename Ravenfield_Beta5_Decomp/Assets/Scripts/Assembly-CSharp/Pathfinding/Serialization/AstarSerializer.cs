using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Pathfinding.Ionic.Zip;
using Pathfinding.Serialization.JsonFx;
using Pathfinding.Util;
using UnityEngine;

namespace Pathfinding.Serialization
{
	public class AstarSerializer
	{
		private const string binaryExt = ".binary";

		private const string jsonExt = ".json";

		private AstarData data;

		public JsonWriterSettings writerSettings;

		public JsonReaderSettings readerSettings;

		private ZipFile zip;

		private MemoryStream zipStream;

		private GraphMeta meta;

		private SerializeSettings settings;

		private NavGraph[] graphs;

		private Dictionary<NavGraph, int> graphIndexInZip;

		private int graphIndexOffset;

		private uint checksum = uint.MaxValue;

		private UTF8Encoding encoding = new UTF8Encoding();

		private static StringBuilder _stringBuilder = new StringBuilder();

		public AstarSerializer(AstarData data)
		{
			this.data = data;
			settings = SerializeSettings.Settings;
		}

		public AstarSerializer(AstarData data, SerializeSettings settings)
		{
			this.data = data;
			this.settings = settings;
		}

		private static StringBuilder GetStringBuilder()
		{
			_stringBuilder.Length = 0;
			return _stringBuilder;
		}

		public void SetGraphIndexOffset(int offset)
		{
			graphIndexOffset = offset;
		}

		private void AddChecksum(byte[] bytes)
		{
			checksum = Checksum.GetChecksum(bytes, checksum);
		}

		public uint GetChecksum()
		{
			return checksum;
		}

		public void OpenSerialize()
		{
			zip = new ZipFile();
			zip.AlternateEncoding = Encoding.UTF8;
			zip.AlternateEncodingUsage = ZipOption.Always;
			writerSettings = new JsonWriterSettings();
			writerSettings.AddTypeConverter(new VectorConverter());
			writerSettings.AddTypeConverter(new BoundsConverter());
			writerSettings.AddTypeConverter(new LayerMaskConverter());
			writerSettings.AddTypeConverter(new MatrixConverter());
			writerSettings.AddTypeConverter(new GuidConverter());
			writerSettings.AddTypeConverter(new UnityObjectConverter());
			writerSettings.PrettyPrint = settings.prettyPrint;
			meta = new GraphMeta();
		}

		public byte[] CloseSerialize()
		{
			byte[] array = SerializeMeta();
			AddChecksum(array);
			zip.AddEntry("meta.json", array);
			DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			foreach (ZipEntry entry in zip.Entries)
			{
				entry.AccessedTime = dateTime;
				entry.CreationTime = dateTime;
				entry.LastModified = dateTime;
				entry.ModifiedTime = dateTime;
			}
			MemoryStream memoryStream = new MemoryStream();
			zip.Save(memoryStream);
			array = memoryStream.ToArray();
			memoryStream.Dispose();
			zip.Dispose();
			zip = null;
			return array;
		}

		public void SerializeGraphs(NavGraph[] _graphs)
		{
			if (graphs != null)
			{
				throw new InvalidOperationException("Cannot serialize graphs multiple times.");
			}
			graphs = _graphs;
			if (zip == null)
			{
				throw new NullReferenceException("You must not call CloseSerialize before a call to this function");
			}
			if (graphs == null)
			{
				graphs = new NavGraph[0];
			}
			for (int i = 0; i < graphs.Length; i++)
			{
				if (graphs[i] != null)
				{
					byte[] array = Serialize(graphs[i]);
					AddChecksum(array);
					zip.AddEntry("graph" + i + ".json", array);
				}
			}
		}

		private byte[] SerializeMeta()
		{
			if (graphs == null)
			{
				throw new Exception("No call to SerializeGraphs has been done");
			}
			meta.version = AstarPath.Version;
			meta.graphs = graphs.Length;
			meta.guids = new string[graphs.Length];
			meta.typeNames = new string[graphs.Length];
			meta.nodeCounts = new int[graphs.Length];
			for (int i = 0; i < graphs.Length; i++)
			{
				if (graphs[i] != null)
				{
					meta.guids[i] = graphs[i].guid.ToString();
					meta.typeNames[i] = graphs[i].GetType().FullName;
				}
			}
			StringBuilder stringBuilder = GetStringBuilder();
			JsonWriter jsonWriter = new JsonWriter(stringBuilder, writerSettings);
			jsonWriter.Write(meta);
			return encoding.GetBytes(stringBuilder.ToString());
		}

		public byte[] Serialize(NavGraph graph)
		{
			StringBuilder stringBuilder = GetStringBuilder();
			JsonWriter jsonWriter = new JsonWriter(stringBuilder, writerSettings);
			jsonWriter.Write(graph);
			return encoding.GetBytes(stringBuilder.ToString());
		}

		[Obsolete("Not used anymore. You can safely remove the call to this function.")]
		public void SerializeNodes()
		{
		}

		private static int GetMaxNodeIndexInAllGraphs(NavGraph[] graphs)
		{
			int maxIndex = 0;
			for (int i = 0; i < graphs.Length; i++)
			{
				if (graphs[i] == null)
				{
					continue;
				}
				graphs[i].GetNodes(delegate(GraphNode node)
				{
					maxIndex = Math.Max(node.NodeIndex, maxIndex);
					if (node.NodeIndex == -1)
					{
						Debug.LogError("Graph contains destroyed nodes. This is a bug.");
					}
					return true;
				});
			}
			return maxIndex;
		}

		private static byte[] SerializeNodeIndices(NavGraph[] graphs)
		{
			MemoryStream memoryStream = new MemoryStream();
			BinaryWriter wr = new BinaryWriter(memoryStream);
			int maxNodeIndexInAllGraphs = GetMaxNodeIndexInAllGraphs(graphs);
			wr.Write(maxNodeIndexInAllGraphs);
			int maxNodeIndex2 = 0;
			for (int i = 0; i < graphs.Length; i++)
			{
				if (graphs[i] != null)
				{
					graphs[i].GetNodes(delegate(GraphNode node)
					{
						maxNodeIndex2 = Math.Max(node.NodeIndex, maxNodeIndex2);
						wr.Write(node.NodeIndex);
						return true;
					});
				}
			}
			if (maxNodeIndex2 != maxNodeIndexInAllGraphs)
			{
				throw new Exception("Some graphs are not consistent in their GetNodes calls, sequential calls give different results.");
			}
			byte[] result = memoryStream.ToArray();
			wr.Close();
			return result;
		}

		private static byte[] SerializeGraphExtraInfo(NavGraph graph)
		{
			MemoryStream memoryStream = new MemoryStream();
			BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			GraphSerializationContext ctx = new GraphSerializationContext(binaryWriter);
			graph.SerializeExtraInfo(ctx);
			byte[] result = memoryStream.ToArray();
			binaryWriter.Close();
			return result;
		}

		private static byte[] SerializeGraphNodeReferences(NavGraph graph)
		{
			MemoryStream memoryStream = new MemoryStream();
			BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			GraphSerializationContext ctx = new GraphSerializationContext(binaryWriter);
			graph.GetNodes(delegate(GraphNode node)
			{
				node.SerializeReferences(ctx);
				return true;
			});
			binaryWriter.Close();
			return memoryStream.ToArray();
		}

		public void SerializeExtraInfo()
		{
			if (!settings.nodes)
			{
				return;
			}
			if (graphs == null)
			{
				throw new InvalidOperationException("Cannot serialize extra info with no serialized graphs (call SerializeGraphs first)");
			}
			byte[] array = SerializeNodeIndices(graphs);
			AddChecksum(array);
			zip.AddEntry("graph_references.binary", array);
			for (int i = 0; i < graphs.Length; i++)
			{
				if (graphs[i] != null)
				{
					array = SerializeGraphExtraInfo(graphs[i]);
					AddChecksum(array);
					zip.AddEntry("graph" + i + "_extra.binary", array);
					array = SerializeGraphNodeReferences(graphs[i]);
					AddChecksum(array);
					zip.AddEntry("graph" + i + "_references.binary", array);
				}
			}
		}

		public void SerializeEditorSettings(GraphEditorBase[] editors)
		{
			if (editors == null || !settings.editorSettings)
			{
				return;
			}
			for (int i = 0; i < editors.Length && editors[i] != null; i++)
			{
				StringBuilder stringBuilder = GetStringBuilder();
				JsonWriter jsonWriter = new JsonWriter(stringBuilder, writerSettings);
				jsonWriter.Write(editors[i]);
				byte[] bytes = encoding.GetBytes(stringBuilder.ToString());
				if (bytes.Length > 2)
				{
					AddChecksum(bytes);
					zip.AddEntry("graph" + i + "_editor.json", bytes);
				}
			}
		}

		public bool OpenDeserialize(byte[] bytes)
		{
			//Discarded unreachable code: IL_00cc
			readerSettings = new JsonReaderSettings();
			readerSettings.AddTypeConverter(new VectorConverter());
			readerSettings.AddTypeConverter(new BoundsConverter());
			readerSettings.AddTypeConverter(new LayerMaskConverter());
			readerSettings.AddTypeConverter(new MatrixConverter());
			readerSettings.AddTypeConverter(new GuidConverter());
			readerSettings.AddTypeConverter(new UnityObjectConverter());
			zipStream = new MemoryStream();
			zipStream.Write(bytes, 0, bytes.Length);
			zipStream.Position = 0L;
			try
			{
				zip = ZipFile.Read(zipStream);
			}
			catch (Exception ex)
			{
				Debug.LogError("Caught exception when loading from zip\n" + ex);
				zipStream.Dispose();
				return false;
			}
			meta = DeserializeMeta(zip["meta.json"]);
			if (FullyDefinedVersion(meta.version) > FullyDefinedVersion(AstarPath.Version))
			{
				Debug.LogWarning(string.Concat("Trying to load data from a newer version of the A* Pathfinding Project\nCurrent version: ", AstarPath.Version, " Data version: ", meta.version, "\nThis is usually fine as the stored data is usually backwards and forwards compatible.\nHowever node data (not settings) can get corrupted between versions (even though I try my best to keep compatibility), so it is recommended to recalculate any caches (those for faster startup) and resave any files. Even if it seems to load fine, it might cause subtle bugs.\n"));
			}
			else if (FullyDefinedVersion(meta.version) < FullyDefinedVersion(AstarPath.Version))
			{
				Debug.LogWarning(string.Concat("Trying to load data from an older version of the A* Pathfinding Project\nCurrent version: ", AstarPath.Version, " Data version: ", meta.version, "\nThis is usually fine, it just means you have upgraded to a new version.\nHowever node data (not settings) can get corrupted between versions (even though I try my best to keep compatibility), so it is recommended to recalculate any caches (those for faster startup) and resave any files. Even if it seems to load fine, it might cause subtle bugs.\n"));
			}
			return true;
		}

		private static Version FullyDefinedVersion(Version v)
		{
			return new Version(Mathf.Max(v.Major, 0), Mathf.Max(v.Minor, 0), Mathf.Max(v.Build, 0), Mathf.Max(v.Revision, 0));
		}

		public void CloseDeserialize()
		{
			zipStream.Dispose();
			zip.Dispose();
			zip = null;
			zipStream = null;
		}

		private NavGraph DeserializeGraph(int zipIndex, int graphIndex)
		{
			Type graphType = meta.GetGraphType(zipIndex);
			if (object.Equals(graphType, null))
			{
				return null;
			}
			ZipEntry zipEntry = zip["graph" + zipIndex + ".json"];
			if (zipEntry == null)
			{
				throw new FileNotFoundException("Could not find data for graph " + zipIndex + " in zip. Entry 'graph" + zipIndex + ".json' does not exist");
			}
			NavGraph obj = data.CreateGraph(graphType);
			obj.graphIndex = (uint)graphIndex;
			string @string = GetString(zipEntry);
			JsonReader jsonReader = new JsonReader(@string, readerSettings);
			jsonReader.PopulateObject(ref obj);
			if (obj.guid.ToString() != meta.guids[zipIndex])
			{
				throw new Exception(string.Concat("Guid in graph file not equal to guid defined in meta file. Have you edited the data manually?\n", obj.guid, " != ", meta.guids[zipIndex]));
			}
			return obj;
		}

		public NavGraph[] DeserializeGraphs()
		{
			List<NavGraph> list = new List<NavGraph>();
			graphIndexInZip = new Dictionary<NavGraph, int>();
			for (int i = 0; i < meta.graphs; i++)
			{
				int graphIndex = list.Count + graphIndexOffset;
				NavGraph navGraph = DeserializeGraph(i, graphIndex);
				if (navGraph != null)
				{
					list.Add(navGraph);
					graphIndexInZip[navGraph] = i;
				}
			}
			graphs = list.ToArray();
			return graphs;
		}

		private bool DeserializeExtraInfo(NavGraph graph)
		{
			int num = graphIndexInZip[graph];
			ZipEntry zipEntry = zip["graph" + num + "_extra.binary"];
			if (zipEntry == null)
			{
				return false;
			}
			BinaryReader binaryReader = GetBinaryReader(zipEntry);
			GraphSerializationContext ctx = new GraphSerializationContext(binaryReader, null, graph.graphIndex);
			graph.DeserializeExtraInfo(ctx);
			return true;
		}

		private bool AnyDestroyedNodesInGraphs()
		{
			bool result = false;
			for (int i = 0; i < graphs.Length; i++)
			{
				graphs[i].GetNodes(delegate(GraphNode node)
				{
					if (node.Destroyed)
					{
						result = true;
					}
					return true;
				});
			}
			return result;
		}

		private GraphNode[] DeserializeNodeReferenceMap()
		{
			//Discarded unreachable code: IL_0097
			ZipEntry zipEntry = zip["graph_references.binary"];
			if (zipEntry == null)
			{
				throw new Exception("Node references not found in the data. Was this loaded from an older version of the A* Pathfinding Project?");
			}
			BinaryReader reader = GetBinaryReader(zipEntry);
			int num = reader.ReadInt32();
			GraphNode[] int2Node = new GraphNode[num + 1];
			try
			{
				for (int i = 0; i < graphs.Length; i++)
				{
					graphs[i].GetNodes(delegate(GraphNode node)
					{
						int num2 = reader.ReadInt32();
						int2Node[num2] = node;
						return true;
					});
				}
			}
			catch (Exception innerException)
			{
				throw new Exception("Some graph(s) has thrown an exception during GetNodes, or some graph(s) have deserialized more or fewer nodes than were serialized", innerException);
			}
			if (reader.BaseStream.Position != reader.BaseStream.Length)
			{
				throw new Exception(reader.BaseStream.Length / 4 + " nodes were serialized, but only data for " + reader.BaseStream.Position / 4 + " nodes was found. The data looks corrupt.");
			}
			reader.Close();
			return int2Node;
		}

		private void DeserializeNodeReferences(NavGraph graph, GraphNode[] int2Node)
		{
			int num = graphIndexInZip[graph];
			ZipEntry zipEntry = zip["graph" + num + "_references.binary"];
			if (zipEntry == null)
			{
				throw new Exception("Node references for graph " + num + " not found in the data. Was this loaded from an older version of the A* Pathfinding Project?");
			}
			BinaryReader binaryReader = GetBinaryReader(zipEntry);
			GraphSerializationContext ctx = new GraphSerializationContext(binaryReader, int2Node, graph.graphIndex);
			graph.GetNodes(delegate(GraphNode node)
			{
				node.DeserializeReferences(ctx);
				return true;
			});
		}

		public void DeserializeExtraInfo()
		{
			bool flag = false;
			for (int i = 0; i < graphs.Length; i++)
			{
				flag |= DeserializeExtraInfo(graphs[i]);
			}
			if (flag)
			{
				if (AnyDestroyedNodesInGraphs())
				{
					Debug.LogError("Graph contains destroyed nodes. This is a bug.");
				}
				GraphNode[] int2Node = DeserializeNodeReferenceMap();
				for (int j = 0; j < graphs.Length; j++)
				{
					DeserializeNodeReferences(graphs[j], int2Node);
				}
			}
		}

		public void PostDeserialization()
		{
			for (int i = 0; i < graphs.Length; i++)
			{
				graphs[i].PostDeserialization();
			}
		}

		public void DeserializeEditorSettings(GraphEditorBase[] graphEditors)
		{
			if (graphEditors == null)
			{
				return;
			}
			for (int i = 0; i < graphEditors.Length; i++)
			{
				if (graphEditors[i] == null)
				{
					continue;
				}
				for (int j = 0; j < graphs.Length; j++)
				{
					if (graphEditors[i].target == graphs[j])
					{
						int num = graphIndexInZip[graphs[j]];
						ZipEntry zipEntry = zip["graph" + num + "_editor.json"];
						if (zipEntry != null)
						{
							string @string = GetString(zipEntry);
							JsonReader jsonReader = new JsonReader(@string, readerSettings);
							GraphEditorBase obj = graphEditors[i];
							jsonReader.PopulateObject(ref obj);
							graphEditors[i] = obj;
							break;
						}
					}
				}
			}
		}

		private static BinaryReader GetBinaryReader(ZipEntry entry)
		{
			MemoryStream memoryStream = new MemoryStream();
			entry.Extract(memoryStream);
			memoryStream.Position = 0L;
			return new BinaryReader(memoryStream);
		}

		private static string GetString(ZipEntry entry)
		{
			MemoryStream memoryStream = new MemoryStream();
			entry.Extract(memoryStream);
			memoryStream.Position = 0L;
			StreamReader streamReader = new StreamReader(memoryStream);
			string result = streamReader.ReadToEnd();
			memoryStream.Position = 0L;
			streamReader.Dispose();
			return result;
		}

		private GraphMeta DeserializeMeta(ZipEntry entry)
		{
			if (entry == null)
			{
				throw new Exception("No metadata found in serialized data.");
			}
			string @string = GetString(entry);
			JsonReader jsonReader = new JsonReader(@string, readerSettings);
			return (GraphMeta)jsonReader.Deserialize(typeof(GraphMeta));
		}

		public static void SaveToFile(string path, byte[] data)
		{
			using (FileStream fileStream = new FileStream(path, FileMode.Create))
			{
				fileStream.Write(data, 0, data.Length);
			}
		}

		public static byte[] LoadFromFile(string path)
		{
			//Discarded unreachable code: IL_002c
			using (FileStream fileStream = new FileStream(path, FileMode.Open))
			{
				byte[] array = new byte[(int)fileStream.Length];
				fileStream.Read(array, 0, (int)fileStream.Length);
				return array;
			}
		}
	}
}
