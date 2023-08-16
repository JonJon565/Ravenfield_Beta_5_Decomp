using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Pathfinding;
using Pathfinding.Util;
using UnityEngine;

[ExecuteInEditMode]
[HelpURL("http://arongranberg.com/astar/docs/class_astar_path.php")]
[AddComponentMenu("Pathfinding/Pathfinder")]
public class AstarPath : MonoBehaviour
{
	public enum AstarDistribution
	{
		WebsiteDownload = 0,
		AssetStore = 1
	}

	private enum GraphUpdateOrder
	{
		GraphUpdate = 0,
		FloodFill = 1
	}

	private struct GUOSingle
	{
		public GraphUpdateOrder order;

		public IUpdatableGraph graph;

		public GraphUpdateObject obj;
	}

	public struct AstarWorkItem
	{
		public System.Action init;

		public Func<bool, bool> update;

		public AstarWorkItem(Func<bool, bool> update)
		{
			init = null;
			this.update = update;
		}

		public AstarWorkItem(System.Action init, Func<bool, bool> update)
		{
			this.init = init;
			this.update = update;
		}
	}

	public static readonly AstarDistribution Distribution = AstarDistribution.AssetStore;

	public static readonly string Branch = "master_Pro";

	public static readonly bool HasPro = true;

	public AstarData astarData;

	public static AstarPath active;

	public bool showNavGraphs = true;

	public bool showUnwalkableNodes = true;

	public GraphDebugMode debugMode;

	public float debugFloor;

	public float debugRoof = 20000f;

	public bool manualDebugFloorRoof;

	public bool showSearchTree;

	public float unwalkableNodeDebugSize = 0.3f;

	public PathLog logPathResults = PathLog.Normal;

	public float maxNearestNodeDistance = 100f;

	public bool scanOnStartup = true;

	public bool fullGetNearestSearch;

	public bool prioritizeGraphs;

	public float prioritizeGraphsLimit = 1f;

	public AstarColor colorSettings;

	[SerializeField]
	protected string[] tagNames;

	public Heuristic heuristic = Heuristic.Euclidean;

	public float heuristicScale = 1f;

	public ThreadCount threadCount;

	public float maxFrameTime = 1f;

	public int minAreaSize;

	public bool limitGraphUpdates = true;

	public float maxGraphUpdateFreq = 0.2f;

	[NonSerialized]
	public float lastScanTime;

	[NonSerialized]
	public Path debugPath;

	[NonSerialized]
	public string inGameDebugPath;

	private bool graphUpdateRoutineRunning;

	private bool isRegisteredForUpdate;

	private bool workItemsQueued;

	private bool queuedWorkItemFloodFill;

	public static System.Action OnAwakeSettings;

	public static OnGraphDelegate OnGraphPreScan;

	public static OnGraphDelegate OnGraphPostScan;

	public static OnPathDelegate OnPathPreSearch;

	public static OnPathDelegate OnPathPostSearch;

	public static OnScanDelegate OnPreScan;

	public static OnScanDelegate OnPostScan;

	public static OnScanDelegate OnLatePostScan;

	public static OnScanDelegate OnGraphsUpdated;

	public static System.Action On65KOverflow;

	private static System.Action OnThreadSafeCallback;

	public System.Action OnDrawGizmosCallback;

	public System.Action OnUnloadGizmoMeshes;

	[Obsolete]
	public System.Action OnGraphsWillBeUpdated;

	[Obsolete]
	public System.Action OnGraphsWillBeUpdated2;

	private Queue<GraphUpdateObject> graphUpdateQueue;

	private Stack<GraphNode> floodStack;

	private ThreadControlQueue pathQueue = new ThreadControlQueue(0);

	private static Thread[] threads;

	private Thread graphUpdateThread;

	private static PathThreadInfo[] threadInfos = new PathThreadInfo[0];

	private static IEnumerator threadEnumerator;

	private static LockFreeStack pathReturnStack = new LockFreeStack();

	public EuclideanEmbedding euclideanEmbedding = new EuclideanEmbedding();

	private int nextNodeIndex = 1;

	private Stack<int> nodeIndexPool = new Stack<int>();

	private Path pathReturnPop;

	private Queue<GUOSingle> graphUpdateQueueAsync = new Queue<GUOSingle>();

	private Queue<GUOSingle> graphUpdateQueueRegular = new Queue<GUOSingle>();

	public bool showGraphs;

	public static bool isEditor = true;

	public uint lastUniqueAreaIndex;

	private static readonly object safeUpdateLock = new object();

	private AutoResetEvent graphUpdateAsyncEvent = new AutoResetEvent(false);

	private ManualResetEvent processingGraphUpdatesAsync = new ManualResetEvent(true);

	private float lastGraphUpdate = -9999f;

	private ushort nextFreePathID = 1;

	private Queue<AstarWorkItem> workItems = new Queue<AstarWorkItem>();

	private bool processingWorkItems;

	private static int waitForPathDepth = 0;

	public static Version Version
	{
		get
		{
			return new Version(3, 8, 1);
		}
	}

	[Obsolete]
	public Type[] graphTypes
	{
		get
		{
			return astarData.graphTypes;
		}
	}

	public NavGraph[] graphs
	{
		get
		{
			if (astarData == null)
			{
				astarData = new AstarData();
			}
			return astarData.graphs;
		}
		set
		{
			if (astarData == null)
			{
				astarData = new AstarData();
			}
			astarData.graphs = value;
		}
	}

	public float maxNearestNodeDistanceSqr
	{
		get
		{
			return maxNearestNodeDistance * maxNearestNodeDistance;
		}
	}

	public PathHandler debugPathData
	{
		get
		{
			if (debugPath == null)
			{
				return null;
			}
			return debugPath.pathHandler;
		}
	}

	public bool isScanning { get; private set; }

	public static int NumParallelThreads
	{
		get
		{
			return (threadInfos != null) ? threadInfos.Length : 0;
		}
	}

	public static bool IsUsingMultithreading
	{
		get
		{
			if (threads != null && threads.Length > 0)
			{
				return true;
			}
			if (threads != null && threads.Length == 0 && threadEnumerator != null)
			{
				return false;
			}
			if (Application.isPlaying)
			{
				throw new Exception("Not 'using threading' and not 'not using threading'... Are you sure pathfinding is set up correctly?\nIf scripts are reloaded in unity editor during play this could happen.\n" + ((threads == null) ? "NULL" : (string.Empty + threads.Length)) + " " + (threadEnumerator != null));
			}
			return false;
		}
	}

	public bool IsAnyGraphUpdatesQueued
	{
		get
		{
			return graphUpdateQueue != null && graphUpdateQueue.Count > 0;
		}
	}

	public string[] GetTagNames()
	{
		if (tagNames == null || tagNames.Length != 32)
		{
			tagNames = new string[32];
			for (int i = 0; i < tagNames.Length; i++)
			{
				tagNames[i] = string.Empty + i;
			}
			tagNames[0] = "Basic Ground";
		}
		return tagNames;
	}

	public static string[] FindTagNames()
	{
		if (active != null)
		{
			return active.GetTagNames();
		}
		AstarPath astarPath = UnityEngine.Object.FindObjectOfType(typeof(AstarPath)) as AstarPath;
		if (astarPath != null)
		{
			active = astarPath;
			return astarPath.GetTagNames();
		}
		return new string[1] { "There is no AstarPath component in the scene" };
	}

	public ushort GetNextPathID()
	{
		if (nextFreePathID == 0)
		{
			nextFreePathID++;
			Debug.Log("65K cleanup (this message is harmless, it just means you have searched a lot of paths)");
			if (On65KOverflow != null)
			{
				System.Action on65KOverflow = On65KOverflow;
				On65KOverflow = null;
				on65KOverflow();
			}
		}
		return nextFreePathID++;
	}

	private void OnDrawGizmos()
	{
		if (isScanning)
		{
			return;
		}
		if (active == null)
		{
			active = this;
		}
		else if (active != this)
		{
			return;
		}
		if (graphs == null || (pathQueue != null && pathQueue.AllReceiversBlocked && workItems.Count > 0))
		{
			return;
		}
		if (showNavGraphs && !manualDebugFloorRoof)
		{
			debugFloor = float.PositiveInfinity;
			debugRoof = float.NegativeInfinity;
			for (int i = 0; i < graphs.Length; i++)
			{
				if (graphs[i] == null || !graphs[i].drawGizmos)
				{
					continue;
				}
				graphs[i].GetNodes(delegate(GraphNode node)
				{
					if (!active.showSearchTree || debugPathData == null || NavGraph.InSearchTree(node, debugPath))
					{
						PathNode pathNode = ((debugPathData == null) ? null : debugPathData.GetPathNode(node));
						if (pathNode != null || debugMode == GraphDebugMode.Penalty)
						{
							switch (debugMode)
							{
							case GraphDebugMode.F:
								debugFloor = Mathf.Min(debugFloor, pathNode.F);
								debugRoof = Mathf.Max(debugRoof, pathNode.F);
								break;
							case GraphDebugMode.G:
								debugFloor = Mathf.Min(debugFloor, pathNode.G);
								debugRoof = Mathf.Max(debugRoof, pathNode.G);
								break;
							case GraphDebugMode.H:
								debugFloor = Mathf.Min(debugFloor, pathNode.H);
								debugRoof = Mathf.Max(debugRoof, pathNode.H);
								break;
							case GraphDebugMode.Penalty:
								debugFloor = Mathf.Min(debugFloor, node.Penalty);
								debugRoof = Mathf.Max(debugRoof, node.Penalty);
								break;
							}
						}
					}
					return true;
				});
			}
			if (float.IsInfinity(debugFloor))
			{
				debugFloor = 0f;
				debugRoof = 1f;
			}
			if (debugRoof - debugFloor < 1f)
			{
				debugRoof += 1f;
			}
		}
		for (int j = 0; j < graphs.Length; j++)
		{
			if (graphs[j] != null && graphs[j].drawGizmos)
			{
				graphs[j].OnDrawGizmos(showNavGraphs);
			}
		}
		if (showNavGraphs)
		{
			euclideanEmbedding.OnDrawGizmos();
		}
		if (showUnwalkableNodes && showNavGraphs)
		{
			Gizmos.color = AstarColor.UnwalkableNode;
			GraphNodeDelegateCancelable del = DrawUnwalkableNode;
			for (int k = 0; k < graphs.Length; k++)
			{
				if (graphs[k] != null && graphs[k].drawGizmos)
				{
					graphs[k].GetNodes(del);
				}
			}
		}
		if (OnDrawGizmosCallback != null)
		{
			OnDrawGizmosCallback();
		}
	}

	private bool DrawUnwalkableNode(GraphNode node)
	{
		if (!node.Walkable)
		{
			Gizmos.DrawCube((Vector3)node.position, Vector3.one * unwalkableNodeDebugSize);
		}
		return true;
	}

	private void OnGUI()
	{
		if (logPathResults == PathLog.InGame && inGameDebugPath != string.Empty)
		{
			GUI.Label(new Rect(5f, 5f, 400f, 600f), inGameDebugPath);
		}
	}

	private static void AstarLog(string s)
	{
		if (object.ReferenceEquals(active, null))
		{
			Debug.Log("No AstarPath object was found : " + s);
		}
		else if (active.logPathResults != 0 && active.logPathResults != PathLog.OnlyErrors)
		{
			Debug.Log(s);
		}
	}

	private static void AstarLogError(string s)
	{
		if (active == null)
		{
			Debug.Log("No AstarPath object was found : " + s);
		}
		else if (active.logPathResults != 0)
		{
			Debug.LogError(s);
		}
	}

	private void LogPathResults(Path p)
	{
		if (logPathResults != 0 && (logPathResults != PathLog.OnlyErrors || p.error))
		{
			string message = p.DebugString(logPathResults);
			if (logPathResults == PathLog.InGame)
			{
				inGameDebugPath = message;
			}
			else
			{
				Debug.Log(message);
			}
		}
	}

	private void Update()
	{
		if (!Application.isPlaying)
		{
			return;
		}
		PerformBlockingActions();
		if (threadEnumerator != null)
		{
			try
			{
				threadEnumerator.MoveNext();
			}
			catch (Exception ex)
			{
				threadEnumerator = null;
				if (!(ex is ThreadControlQueue.QueueTerminationException))
				{
					Debug.LogException(ex);
					Debug.LogError("Unhandled exception during pathfinding. Terminating.");
					pathQueue.TerminateReceivers();
					try
					{
						pathQueue.PopNoBlock(false);
					}
					catch
					{
					}
				}
			}
		}
		ReturnPaths(true);
	}

	private void PerformBlockingActions(bool force = false, bool unblockOnComplete = true)
	{
		if (!pathQueue.AllReceiversBlocked)
		{
			return;
		}
		ReturnPaths(false);
		if (OnThreadSafeCallback != null)
		{
			System.Action onThreadSafeCallback = OnThreadSafeCallback;
			OnThreadSafeCallback = null;
			onThreadSafeCallback();
		}
		if (ProcessWorkItems(force) != 2)
		{
			return;
		}
		workItemsQueued = false;
		if (unblockOnComplete)
		{
			if (euclideanEmbedding.dirty)
			{
				euclideanEmbedding.RecalculateCosts();
			}
			pathQueue.Unblock();
		}
	}

	public void QueueWorkItemFloodFill()
	{
		if (!pathQueue.AllReceiversBlocked)
		{
			throw new Exception("You are calling QueueWorkItemFloodFill from outside a WorkItem. This might cause unexpected behaviour.");
		}
		queuedWorkItemFloodFill = true;
	}

	public void EnsureValidFloodFill()
	{
		if (queuedWorkItemFloodFill)
		{
			FloodFill();
		}
	}

	public void AddWorkItem(AstarWorkItem itm)
	{
		workItems.Enqueue(itm);
		if (!workItemsQueued)
		{
			workItemsQueued = true;
			if (!isScanning)
			{
				InterruptPathfinding();
			}
		}
	}

	private int ProcessWorkItems(bool force)
	{
		//Discarded unreachable code: IL_0099
		if (pathQueue.AllReceiversBlocked)
		{
			if (processingWorkItems)
			{
				throw new Exception("Processing work items recursively. Please do not wait for other work items to be completed inside work items. If you think this is not caused by any of your scripts, this might be a bug.");
			}
			processingWorkItems = true;
			while (workItems.Count > 0)
			{
				AstarWorkItem astarWorkItem = workItems.Peek();
				if (astarWorkItem.init != null)
				{
					astarWorkItem.init();
					astarWorkItem.init = null;
				}
				bool flag;
				try
				{
					flag = astarWorkItem.update == null || astarWorkItem.update(force);
				}
				catch
				{
					workItems.Dequeue();
					processingWorkItems = false;
					throw;
				}
				if (!flag)
				{
					if (force)
					{
						Debug.LogError("Misbehaving WorkItem. 'force'=true but the work item did not complete.\nIf force=true is passed to a WorkItem it should always return true.");
					}
					processingWorkItems = false;
					return 1;
				}
				workItems.Dequeue();
			}
			EnsureValidFloodFill();
			processingWorkItems = false;
			return 2;
		}
		return 0;
	}

	public void QueueGraphUpdates()
	{
		if (!isRegisteredForUpdate)
		{
			isRegisteredForUpdate = true;
			AstarWorkItem itm = default(AstarWorkItem);
			itm.init = QueueGraphUpdatesInternal;
			itm.update = ProcessGraphUpdates;
			AddWorkItem(itm);
		}
	}

	private IEnumerator DelayedGraphUpdate()
	{
		graphUpdateRoutineRunning = true;
		yield return new WaitForSeconds(maxGraphUpdateFreq - (Time.time - lastGraphUpdate));
		QueueGraphUpdates();
		graphUpdateRoutineRunning = false;
	}

	public void UpdateGraphs(Bounds bounds, float t)
	{
		UpdateGraphs(new GraphUpdateObject(bounds), t);
	}

	public void UpdateGraphs(GraphUpdateObject ob, float t)
	{
		StartCoroutine(UpdateGraphsInteral(ob, t));
	}

	private IEnumerator UpdateGraphsInteral(GraphUpdateObject ob, float t)
	{
		yield return new WaitForSeconds(t);
		UpdateGraphs(ob);
	}

	public void UpdateGraphs(Bounds bounds)
	{
		UpdateGraphs(new GraphUpdateObject(bounds));
	}

	public void UpdateGraphs(GraphUpdateObject ob)
	{
		if (graphUpdateQueue == null)
		{
			graphUpdateQueue = new Queue<GraphUpdateObject>();
		}
		graphUpdateQueue.Enqueue(ob);
		if (limitGraphUpdates && Time.time - lastGraphUpdate < maxGraphUpdateFreq)
		{
			if (!graphUpdateRoutineRunning)
			{
				StartCoroutine(DelayedGraphUpdate());
			}
		}
		else
		{
			QueueGraphUpdates();
		}
	}

	public void FlushGraphUpdates()
	{
		if (IsAnyGraphUpdatesQueued)
		{
			QueueGraphUpdates();
			FlushWorkItems(true, true);
		}
	}

	public void FlushWorkItems(bool unblockOnComplete = true, bool block = false)
	{
		BlockUntilPathQueueBlocked();
		PerformBlockingActions(block, unblockOnComplete);
	}

	private void QueueGraphUpdatesInternal()
	{
		isRegisteredForUpdate = false;
		bool flag = false;
		while (graphUpdateQueue.Count > 0)
		{
			GraphUpdateObject graphUpdateObject = graphUpdateQueue.Dequeue();
			if (graphUpdateObject.requiresFloodFill)
			{
				flag = true;
			}
			foreach (IUpdatableGraph updateableGraph in astarData.GetUpdateableGraphs())
			{
				NavGraph graph = updateableGraph as NavGraph;
				if (graphUpdateObject.nnConstraint == null || graphUpdateObject.nnConstraint.SuitableGraph(active.astarData.GetGraphIndex(graph), graph))
				{
					GUOSingle item = default(GUOSingle);
					item.order = GraphUpdateOrder.GraphUpdate;
					item.obj = graphUpdateObject;
					item.graph = updateableGraph;
					graphUpdateQueueRegular.Enqueue(item);
				}
			}
		}
		if (flag)
		{
			GUOSingle item2 = default(GUOSingle);
			item2.order = GraphUpdateOrder.FloodFill;
			graphUpdateQueueRegular.Enqueue(item2);
		}
		debugPath = null;
		GraphModifier.TriggerEvent(GraphModifier.EventType.PreUpdate);
	}

	private bool ProcessGraphUpdates(bool force)
	{
		if (force)
		{
			processingGraphUpdatesAsync.WaitOne();
		}
		else if (!processingGraphUpdatesAsync.WaitOne(0))
		{
			return false;
		}
		if (graphUpdateQueueAsync.Count != 0)
		{
			throw new Exception("Queue should be empty at this stage");
		}
		while (graphUpdateQueueRegular.Count > 0)
		{
			GUOSingle item = graphUpdateQueueRegular.Peek();
			GraphUpdateThreading graphUpdateThreading = ((item.order == GraphUpdateOrder.FloodFill) ? GraphUpdateThreading.SeparateThread : item.graph.CanUpdateAsync(item.obj));
			bool flag = force;
			if (!Application.isPlaying || graphUpdateThread == null || !graphUpdateThread.IsAlive)
			{
				flag = true;
			}
			if (!flag && graphUpdateThreading == GraphUpdateThreading.SeparateAndUnityInit)
			{
				if (graphUpdateQueueAsync.Count > 0)
				{
					processingGraphUpdatesAsync.Reset();
					graphUpdateAsyncEvent.Set();
					return false;
				}
				item.graph.UpdateAreaInit(item.obj);
				graphUpdateQueueRegular.Dequeue();
				graphUpdateQueueAsync.Enqueue(item);
				processingGraphUpdatesAsync.Reset();
				graphUpdateAsyncEvent.Set();
				return false;
			}
			if (!flag && graphUpdateThreading == GraphUpdateThreading.SeparateThread)
			{
				graphUpdateQueueRegular.Dequeue();
				graphUpdateQueueAsync.Enqueue(item);
				continue;
			}
			if (graphUpdateQueueAsync.Count > 0)
			{
				if (force)
				{
					throw new Exception("This should not happen");
				}
				processingGraphUpdatesAsync.Reset();
				graphUpdateAsyncEvent.Set();
				return false;
			}
			graphUpdateQueueRegular.Dequeue();
			if (item.order == GraphUpdateOrder.FloodFill)
			{
				FloodFill();
				continue;
			}
			if (graphUpdateThreading == GraphUpdateThreading.SeparateAndUnityInit)
			{
				try
				{
					item.graph.UpdateAreaInit(item.obj);
				}
				catch (Exception ex)
				{
					Debug.LogError("Error while initializing GraphUpdates\n" + ex);
				}
			}
			try
			{
				item.graph.UpdateArea(item.obj);
			}
			catch (Exception ex2)
			{
				Debug.LogError("Error while updating graphs\n" + ex2);
			}
		}
		if (graphUpdateQueueAsync.Count > 0)
		{
			processingGraphUpdatesAsync.Reset();
			graphUpdateAsyncEvent.Set();
			return false;
		}
		GraphModifier.TriggerEvent(GraphModifier.EventType.PostUpdate);
		if (OnGraphsUpdated != null)
		{
			OnGraphsUpdated(this);
		}
		return true;
	}

	private void ProcessGraphUpdatesAsync(object _astar)
	{
		AstarPath astarPath = _astar as AstarPath;
		if (object.ReferenceEquals(astarPath, null))
		{
			Debug.LogError("ProcessGraphUpdatesAsync started with invalid parameter _astar (was no AstarPath object)");
			return;
		}
		while (!astarPath.pathQueue.IsTerminating)
		{
			graphUpdateAsyncEvent.WaitOne();
			if (astarPath.pathQueue.IsTerminating)
			{
				graphUpdateQueueAsync.Clear();
				processingGraphUpdatesAsync.Set();
				break;
			}
			while (graphUpdateQueueAsync.Count > 0)
			{
				GUOSingle gUOSingle = graphUpdateQueueAsync.Dequeue();
				try
				{
					if (gUOSingle.order == GraphUpdateOrder.GraphUpdate)
					{
						gUOSingle.graph.UpdateArea(gUOSingle.obj);
						continue;
					}
					if (gUOSingle.order == GraphUpdateOrder.FloodFill)
					{
						astarPath.FloodFill();
						continue;
					}
					throw new NotSupportedException(string.Empty + gUOSingle.order);
				}
				catch (Exception ex)
				{
					Debug.LogError("Exception while updating graphs:\n" + ex);
				}
			}
			processingGraphUpdatesAsync.Set();
		}
	}

	public void FlushThreadSafeCallbacks()
	{
		if (OnThreadSafeCallback != null)
		{
			BlockUntilPathQueueBlocked();
			PerformBlockingActions();
		}
	}

	public static int CalculateThreadCount(ThreadCount count)
	{
		if (count == ThreadCount.AutomaticLowLoad || count == ThreadCount.AutomaticHighLoad)
		{
			int num = Mathf.Max(1, SystemInfo.processorCount);
			int num2 = SystemInfo.systemMemorySize;
			if (num2 <= 0)
			{
				Debug.LogError("Machine reporting that is has <= 0 bytes of RAM. This is definitely not true, assuming 1 GiB");
				num2 = 1024;
			}
			if (num <= 1)
			{
				return 0;
			}
			if (num2 <= 512)
			{
				return 0;
			}
			if (count == ThreadCount.AutomaticHighLoad)
			{
				if (num2 <= 1024)
				{
					num = Math.Min(num, 2);
				}
			}
			else
			{
				num /= 2;
				num = Mathf.Max(1, num);
				if (num2 <= 1024)
				{
					num = Math.Min(num, 2);
				}
				num = Math.Min(num, 6);
			}
			return num;
		}
		return (int)count;
	}

	private void Awake()
	{
		active = this;
		if (UnityEngine.Object.FindObjectsOfType(typeof(AstarPath)).Length > 1)
		{
			Debug.LogError("You should NOT have more than one AstarPath component in the scene at any time.\nThis can cause serious errors since the AstarPath component builds around a singleton pattern.");
		}
		base.useGUILayout = false;
		isEditor = Application.isEditor;
		if (!Application.isPlaying)
		{
			return;
		}
		if (OnAwakeSettings != null)
		{
			OnAwakeSettings();
		}
		GraphModifier.FindAllModifiers();
		RelevantGraphSurface.FindAllGraphSurfaces();
		int num = CalculateThreadCount(threadCount);
		threads = new Thread[num];
		threadInfos = new PathThreadInfo[Math.Max(num, 1)];
		pathQueue = new ThreadControlQueue(threadInfos.Length);
		for (int i = 0; i < threadInfos.Length; i++)
		{
			threadInfos[i] = new PathThreadInfo(i, this, new PathHandler(i, threadInfos.Length));
		}
		if (num == 0)
		{
			threadEnumerator = CalculatePaths(threadInfos[0]);
		}
		else
		{
			threadEnumerator = null;
		}
		for (int j = 0; j < threads.Length; j++)
		{
			threads[j] = new Thread(CalculatePathsThreaded);
			threads[j].Name = "Pathfinding Thread " + j;
			threads[j].IsBackground = true;
		}
		for (int k = 0; k < threads.Length; k++)
		{
			if (logPathResults == PathLog.Heavy)
			{
				Debug.Log("Starting pathfinding thread " + k);
			}
			threads[k].Start(threadInfos[k]);
		}
		if (num != 0)
		{
			graphUpdateThread = new Thread(ProcessGraphUpdatesAsync);
			graphUpdateThread.IsBackground = true;
			graphUpdateThread.Priority = System.Threading.ThreadPriority.Lowest;
			graphUpdateThread.Start(this);
		}
		Initialize();
		FlushWorkItems();
		euclideanEmbedding.dirty = true;
		if (scanOnStartup && (!astarData.cacheStartup || astarData.file_cachedStartup == null))
		{
			Scan();
		}
	}

	internal void VerifyIntegrity()
	{
		if (active != this)
		{
			throw new Exception("Singleton pattern broken. Make sure you only have one AstarPath object in the scene");
		}
		if (astarData == null)
		{
			throw new NullReferenceException("AstarData is null... Astar not set up correctly?");
		}
		if (astarData.graphs == null)
		{
			astarData.graphs = new NavGraph[0];
		}
		if (pathQueue == null && !Application.isPlaying)
		{
			pathQueue = new ThreadControlQueue(0);
		}
		if (threadInfos == null && !Application.isPlaying)
		{
			threadInfos = new PathThreadInfo[0];
		}
		if (!IsUsingMultithreading)
		{
		}
	}

	public void SetUpReferences()
	{
		active = this;
		if (astarData == null)
		{
			astarData = new AstarData();
		}
		if (colorSettings == null)
		{
			colorSettings = new AstarColor();
		}
		colorSettings.OnEnable();
	}

	private void Initialize()
	{
		SetUpReferences();
		astarData.FindGraphTypes();
		astarData.Awake();
		astarData.UpdateShortcuts();
		for (int i = 0; i < astarData.graphs.Length; i++)
		{
			if (astarData.graphs[i] != null)
			{
				astarData.graphs[i].Awake();
			}
		}
	}

	private void OnDisable()
	{
		if (OnUnloadGizmoMeshes != null)
		{
			OnUnloadGizmoMeshes();
		}
	}

	private void OnDestroy()
	{
		if (!Application.isPlaying)
		{
			return;
		}
		if (logPathResults == PathLog.Heavy)
		{
			Debug.Log("+++ AstarPath Component Destroyed - Cleaning Up Pathfinding Data +++");
		}
		if (active != this)
		{
			return;
		}
		BlockUntilPathQueueBlocked();
		euclideanEmbedding.dirty = false;
		FlushWorkItems(false, true);
		pathQueue.TerminateReceivers();
		if (logPathResults == PathLog.Heavy)
		{
			Debug.Log("Processing Eventual Work Items");
		}
		graphUpdateAsyncEvent.Set();
		if (threads != null)
		{
			for (int i = 0; i < threads.Length; i++)
			{
				if (!threads[i].Join(50))
				{
					Debug.LogError("Could not terminate pathfinding thread[" + i + "] in 50ms, trying Thread.Abort");
					threads[i].Abort();
				}
			}
		}
		if (logPathResults == PathLog.Heavy)
		{
			Debug.Log("Returning Paths");
		}
		ReturnPaths(false);
		pathReturnStack.PopAll();
		if (logPathResults == PathLog.Heavy)
		{
			Debug.Log("Destroying Graphs");
		}
		astarData.OnDestroy();
		if (logPathResults == PathLog.Heavy)
		{
			Debug.Log("Cleaning up variables");
		}
		floodStack = null;
		graphUpdateQueue = null;
		OnDrawGizmosCallback = null;
		OnAwakeSettings = null;
		OnGraphPreScan = null;
		OnGraphPostScan = null;
		OnPathPreSearch = null;
		OnPathPostSearch = null;
		OnPreScan = null;
		OnPostScan = null;
		OnLatePostScan = null;
		On65KOverflow = null;
		OnGraphsUpdated = null;
		OnThreadSafeCallback = null;
		threads = null;
		threadInfos = null;
		active = null;
	}

	public void FloodFill(GraphNode seed)
	{
		FloodFill(seed, lastUniqueAreaIndex + 1);
		lastUniqueAreaIndex++;
	}

	public void FloodFill(GraphNode seed, uint area)
	{
		if (area > 131071)
		{
			Debug.LogError("Too high area index - The maximum area index is " + 131071u);
			return;
		}
		if (area < 0)
		{
			Debug.LogError("Too low area index - The minimum area index is 0");
			return;
		}
		if (floodStack == null)
		{
			floodStack = new Stack<GraphNode>(1024);
		}
		Stack<GraphNode> stack = floodStack;
		stack.Clear();
		stack.Push(seed);
		seed.Area = area;
		while (stack.Count > 0)
		{
			stack.Pop().FloodFill(stack, area);
		}
	}

	[ContextMenu("Flood Fill Graphs")]
	public void FloodFill()
	{
		queuedWorkItemFloodFill = false;
		if (astarData.graphs == null)
		{
			return;
		}
		uint area = 0u;
		lastUniqueAreaIndex = 0u;
		if (floodStack == null)
		{
			floodStack = new Stack<GraphNode>(1024);
		}
		Stack<GraphNode> stack = floodStack;
		for (int i = 0; i < graphs.Length; i++)
		{
			NavGraph navGraph = graphs[i];
			if (navGraph != null)
			{
				navGraph.GetNodes(delegate(GraphNode node)
				{
					node.Area = 0u;
					return true;
				});
			}
		}
		int smallAreasDetected = 0;
		bool warnAboutAreas = false;
		List<GraphNode> smallAreaList = ListPool<GraphNode>.Claim();
		for (int j = 0; j < graphs.Length; j++)
		{
			NavGraph navGraph2 = graphs[j];
			if (navGraph2 == null)
			{
				continue;
			}
			GraphNodeDelegateCancelable del = delegate(GraphNode node)
			{
				if (node.Walkable && node.Area == 0)
				{
					area++;
					uint num = area;
					if (area > 131071)
					{
						if (smallAreaList.Count > 0)
						{
							GraphNode graphNode = smallAreaList[smallAreaList.Count - 1];
							num = graphNode.Area;
							smallAreaList.RemoveAt(smallAreaList.Count - 1);
							stack.Clear();
							stack.Push(graphNode);
							graphNode.Area = 131071u;
							while (stack.Count > 0)
							{
								stack.Pop().FloodFill(stack, 131071u);
							}
							smallAreasDetected++;
						}
						else
						{
							area--;
							num = area;
							warnAboutAreas = true;
						}
					}
					stack.Clear();
					stack.Push(node);
					int num2 = 1;
					node.Area = num;
					while (stack.Count > 0)
					{
						num2++;
						stack.Pop().FloodFill(stack, num);
					}
					if (num2 < minAreaSize)
					{
						smallAreaList.Add(node);
					}
				}
				return true;
			};
			navGraph2.GetNodes(del);
		}
		lastUniqueAreaIndex = area;
		if (warnAboutAreas)
		{
			Debug.LogError("Too many areas - The maximum number of areas is " + 131071u + ". Try raising the A* Inspector -> Settings -> Min Area Size value. Enable the optimization ASTAR_MORE_AREAS under the Optimizations tab.");
		}
		if (smallAreasDetected > 0)
		{
			AstarLog(smallAreasDetected + " small areas were detected (fewer than " + minAreaSize + " nodes),these might have the same IDs as other areas, but it shouldn't affect pathfinding in any significant way (you might get All Nodes Searched as a reason for path failure).\nWhich areas are defined as 'small' is controlled by the 'Min Area Size' variable, it can be changed in the A* inspector-->Settings-->Min Area Size\nThe small areas will use the area id " + 131071u);
		}
		ListPool<GraphNode>.Release(smallAreaList);
	}

	public int GetNewNodeIndex()
	{
		if (nodeIndexPool.Count > 0)
		{
			return nodeIndexPool.Pop();
		}
		return nextNodeIndex++;
	}

	public void InitializeNode(GraphNode node)
	{
		if (!pathQueue.AllReceiversBlocked)
		{
			throw new Exception("Trying to initialize a node when it is not safe to initialize any nodes. Must be done during a graph update");
		}
		if (threadInfos == null)
		{
			threadInfos = new PathThreadInfo[0];
		}
		for (int i = 0; i < threadInfos.Length; i++)
		{
			threadInfos[i].runData.InitializeNode(node);
		}
	}

	public void DestroyNode(GraphNode node)
	{
		if (node.NodeIndex != -1)
		{
			nodeIndexPool.Push(node.NodeIndex);
			if (threadInfos == null)
			{
				threadInfos = new PathThreadInfo[0];
			}
			for (int i = 0; i < threadInfos.Length; i++)
			{
				threadInfos[i].runData.DestroyNode(node);
			}
		}
	}

	public void BlockUntilPathQueueBlocked()
	{
		if (pathQueue == null)
		{
			return;
		}
		pathQueue.Block();
		while (!pathQueue.AllReceiversBlocked)
		{
			if (IsUsingMultithreading)
			{
				Thread.Sleep(1);
			}
			else
			{
				threadEnumerator.MoveNext();
			}
		}
	}

	public void Scan()
	{
		ScanLoop(null);
	}

	public void ScanLoop(OnScanStatus statusCallback)
	{
		if (graphs == null)
		{
			return;
		}
		isScanning = true;
		euclideanEmbedding.dirty = false;
		VerifyIntegrity();
		BlockUntilPathQueueBlocked();
		ReturnPaths(false);
		BlockUntilPathQueueBlocked();
		if (!Application.isPlaying)
		{
			GraphModifier.FindAllModifiers();
			RelevantGraphSurface.FindAllGraphSurfaces();
		}
		RelevantGraphSurface.UpdateAllPositions();
		astarData.UpdateShortcuts();
		if (statusCallback != null)
		{
			statusCallback(new Progress(0.05f, "Pre processing graphs"));
		}
		if (OnPreScan != null)
		{
			OnPreScan(this);
		}
		GraphModifier.TriggerEvent(GraphModifier.EventType.PreScan);
		DateTime utcNow = DateTime.UtcNow;
		for (int j = 0; j < graphs.Length; j++)
		{
			if (graphs[j] != null)
			{
				graphs[j].GetNodes(delegate(GraphNode node)
				{
					node.Destroy();
					return true;
				});
			}
		}
		for (int i = 0; i < graphs.Length; i++)
		{
			NavGraph navGraph = graphs[i];
			if (navGraph == null)
			{
				if (statusCallback != null)
				{
					statusCallback(new Progress(Mathf.Lerp(0.05f, 0.7f, ((float)i + 0.5f) / (float)(graphs.Length + 1)), "Skipping graph " + (i + 1) + " of " + graphs.Length + " because it is null"));
				}
				continue;
			}
			if (OnGraphPreScan != null)
			{
				if (statusCallback != null)
				{
					statusCallback(new Progress(Mathf.Lerp(0.1f, 0.7f, (float)i / (float)graphs.Length), "Scanning graph " + (i + 1) + " of " + graphs.Length + " - Pre processing"));
				}
				OnGraphPreScan(navGraph);
			}
			float minp = Mathf.Lerp(0.1f, 0.7f, (float)i / (float)graphs.Length);
			float maxp = Mathf.Lerp(0.1f, 0.7f, ((float)i + 0.95f) / (float)graphs.Length);
			if (statusCallback != null)
			{
				statusCallback(new Progress(minp, "Scanning graph " + (i + 1) + " of " + graphs.Length));
			}
			OnScanStatus statusCallback2 = null;
			if (statusCallback != null)
			{
				statusCallback2 = delegate(Progress p)
				{
					p.progress = Mathf.Lerp(minp, maxp, p.progress);
					statusCallback(p);
				};
			}
			navGraph.ScanInternal(statusCallback2);
			navGraph.GetNodes(delegate(GraphNode node)
			{
				node.GraphIndex = (uint)i;
				return true;
			});
			if (OnGraphPostScan != null)
			{
				if (statusCallback != null)
				{
					statusCallback(new Progress(Mathf.Lerp(0.1f, 0.7f, ((float)i + 0.95f) / (float)graphs.Length), "Scanning graph " + (i + 1) + " of " + graphs.Length + " - Post processing"));
				}
				OnGraphPostScan(navGraph);
			}
		}
		if (statusCallback != null)
		{
			statusCallback(new Progress(0.8f, "Post processing graphs"));
		}
		if (OnPostScan != null)
		{
			OnPostScan(this);
		}
		GraphModifier.TriggerEvent(GraphModifier.EventType.PostScan);
		try
		{
			FlushWorkItems(false, true);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
		isScanning = false;
		if (statusCallback != null)
		{
			statusCallback(new Progress(0.9f, "Computing areas"));
		}
		FloodFill();
		VerifyIntegrity();
		if (statusCallback != null)
		{
			statusCallback(new Progress(0.95f, "Late post processing"));
		}
		if (OnLatePostScan != null)
		{
			OnLatePostScan(this);
		}
		GraphModifier.TriggerEvent(GraphModifier.EventType.LatePostScan);
		euclideanEmbedding.dirty = true;
		euclideanEmbedding.RecalculatePivots();
		PerformBlockingActions(true);
		lastScanTime = (float)(DateTime.UtcNow - utcNow).TotalSeconds;
		GC.Collect();
		AstarLog("Scanning - Process took " + (lastScanTime * 1000f).ToString("0") + " ms to complete");
	}

	public static void WaitForPath(Path p)
	{
		if (active == null)
		{
			throw new Exception("Pathfinding is not correctly initialized in this scene (yet?). AstarPath.active is null.\nDo not call this function in Awake");
		}
		if (p == null)
		{
			throw new ArgumentNullException("Path must not be null");
		}
		if (active.pathQueue.IsTerminating)
		{
			return;
		}
		if (p.GetState() == PathState.Created)
		{
			throw new Exception("The specified path has not been started yet.");
		}
		waitForPathDepth++;
		if (waitForPathDepth == 5)
		{
			Debug.LogError("You are calling the WaitForPath function recursively (maybe from a path callback). Please don't do this.");
		}
		if (p.GetState() < PathState.ReturnQueue)
		{
			if (IsUsingMultithreading)
			{
				while (p.GetState() < PathState.ReturnQueue)
				{
					if (active.pathQueue.IsTerminating)
					{
						waitForPathDepth--;
						throw new Exception("Pathfinding Threads seems to have crashed.");
					}
					Thread.Sleep(1);
					active.PerformBlockingActions();
				}
			}
			else
			{
				while (p.GetState() < PathState.ReturnQueue)
				{
					if (active.pathQueue.IsEmpty && p.GetState() != PathState.Processing)
					{
						waitForPathDepth--;
						throw new Exception(string.Concat("Critical error. Path Queue is empty but the path state is '", p.GetState(), "'"));
					}
					threadEnumerator.MoveNext();
					active.PerformBlockingActions();
				}
			}
		}
		active.ReturnPaths(false);
		waitForPathDepth--;
	}

	[Obsolete("The threadSafe parameter has been deprecated")]
	public static void RegisterSafeUpdate(System.Action callback, bool threadSafe)
	{
		RegisterSafeUpdate(callback);
	}

	public static void RegisterSafeUpdate(System.Action callback)
	{
		if (callback == null || !Application.isPlaying)
		{
			return;
		}
		if (active.pathQueue.AllReceiversBlocked)
		{
			active.pathQueue.Lock();
			try
			{
				if (active.pathQueue.AllReceiversBlocked)
				{
					callback();
					return;
				}
			}
			finally
			{
				active.pathQueue.Unlock();
			}
		}
		lock (safeUpdateLock)
		{
			OnThreadSafeCallback = (System.Action)Delegate.Combine(OnThreadSafeCallback, callback);
		}
		active.pathQueue.Block();
	}

	private void InterruptPathfinding()
	{
		pathQueue.Block();
	}

	public static void StartPath(Path p, bool pushToFront = false)
	{
		if (object.ReferenceEquals(active, null))
		{
			Debug.LogError("There is no AstarPath object in the scene");
			return;
		}
		if (p.GetState() != 0)
		{
			throw new Exception(string.Concat("The path has an invalid state. Expected ", PathState.Created, " found ", p.GetState(), "\nMake sure you are not requesting the same path twice"));
		}
		if (active.pathQueue.IsTerminating)
		{
			p.Error();
			p.LogError("No new paths are accepted");
			return;
		}
		if (active.graphs == null || active.graphs.Length == 0)
		{
			Debug.LogError("There are no graphs in the scene");
			p.Error();
			p.LogError("There are no graphs in the scene");
			Debug.LogError(p.errorLog);
			return;
		}
		p.Claim(active);
		p.AdvanceState(PathState.PathQueue);
		if (pushToFront)
		{
			active.pathQueue.PushFront(p);
		}
		else
		{
			active.pathQueue.Push(p);
		}
	}

	public void OnApplicationQuit()
	{
		if (logPathResults == PathLog.Heavy)
		{
			Debug.Log("+++ Application Quitting - Cleaning Up +++");
		}
		OnDestroy();
		if (threads == null)
		{
			return;
		}
		for (int i = 0; i < threads.Length; i++)
		{
			if (threads[i] != null && threads[i].IsAlive)
			{
				threads[i].Abort();
			}
		}
	}

	public void ReturnPaths(bool timeSlice)
	{
		Path next = pathReturnStack.PopAll();
		if (pathReturnPop == null)
		{
			pathReturnPop = next;
		}
		else
		{
			Path next2 = pathReturnPop;
			while (next2.next != null)
			{
				next2 = next2.next;
			}
			next2.next = next;
		}
		long num = ((!timeSlice) ? 0 : (DateTime.UtcNow.Ticks + 10000));
		int num2 = 0;
		while (pathReturnPop != null)
		{
			Path path = pathReturnPop;
			pathReturnPop = pathReturnPop.next;
			path.next = null;
			path.ReturnPath();
			path.AdvanceState(PathState.Returned);
			path.Release(this, true);
			num2++;
			if (num2 > 5 && timeSlice)
			{
				num2 = 0;
				if (DateTime.UtcNow.Ticks >= num)
				{
					break;
				}
			}
		}
	}

	private static void CalculatePathsThreaded(object _threadInfo)
	{
		//Discarded unreachable code: IL_0029, IL_021e
		PathThreadInfo pathThreadInfo;
		try
		{
			pathThreadInfo = (PathThreadInfo)_threadInfo;
		}
		catch (Exception ex)
		{
			Debug.LogError("Arguments to pathfinding threads must be of type ThreadStartInfo\n" + ex);
			throw new ArgumentException("Argument must be of type ThreadStartInfo", ex);
		}
		AstarPath astar = pathThreadInfo.astar;
		try
		{
			PathHandler runData = pathThreadInfo.runData;
			if (runData.nodes == null)
			{
				throw new NullReferenceException("NodeRuns must be assigned to the threadInfo.runData.nodes field before threads are started\nthreadInfo is an argument to the thread functions");
			}
			long num = (long)(astar.maxFrameTime * 10000f);
			long num2 = DateTime.UtcNow.Ticks + num;
			while (true)
			{
				Path path = astar.pathQueue.Pop();
				num = (long)(astar.maxFrameTime * 10000f);
				path.PrepareBase(runData);
				path.AdvanceState(PathState.Processing);
				if (OnPathPreSearch != null)
				{
					OnPathPreSearch(path);
				}
				long ticks = DateTime.UtcNow.Ticks;
				long num3 = 0L;
				path.Prepare();
				if (!path.IsDone())
				{
					astar.debugPath = path;
					path.Initialize();
					while (!path.IsDone())
					{
						path.CalculateStep(num2);
						path.searchIterations++;
						if (path.IsDone())
						{
							break;
						}
						num3 += DateTime.UtcNow.Ticks - ticks;
						Thread.Sleep(0);
						ticks = DateTime.UtcNow.Ticks;
						num2 = ticks + num;
						if (astar.pathQueue.IsTerminating)
						{
							path.Error();
						}
					}
					num3 += DateTime.UtcNow.Ticks - ticks;
					path.duration = (float)num3 * 0.0001f;
				}
				path.Cleanup();
				astar.LogPathResults(path);
				if (path.immediateCallback != null)
				{
					path.immediateCallback(path);
				}
				if (OnPathPostSearch != null)
				{
					OnPathPostSearch(path);
				}
				pathReturnStack.Push(path);
				path.AdvanceState(PathState.ReturnQueue);
				if (DateTime.UtcNow.Ticks > num2)
				{
					Thread.Sleep(1);
					num2 = DateTime.UtcNow.Ticks + num;
				}
			}
		}
		catch (Exception ex2)
		{
			if (ex2 is ThreadAbortException || ex2 is ThreadControlQueue.QueueTerminationException)
			{
				if (astar.logPathResults == PathLog.Heavy)
				{
					Debug.LogWarning("Shutting down pathfinding thread #" + pathThreadInfo.threadIndex);
				}
				return;
			}
			Debug.LogException(ex2);
			Debug.LogError("Unhandled exception during pathfinding. Terminating.");
			astar.pathQueue.TerminateReceivers();
		}
		Debug.LogError("Error : This part should never be reached.");
		astar.pathQueue.ReceiverTerminated();
	}

	private static IEnumerator CalculatePaths(object _threadInfo)
	{
		PathThreadInfo threadInfo;
		try
		{
			threadInfo = (PathThreadInfo)_threadInfo;
		}
		catch (Exception ex)
		{
			Exception e = ex;
			Debug.LogError("Arguments to pathfinding threads must be of type ThreadStartInfo\n" + e);
			throw new ArgumentException("Argument must be of type ThreadStartInfo", e);
		}
		int numPaths = 0;
		PathHandler runData = threadInfo.runData;
		AstarPath astar = threadInfo.astar;
		if (runData.nodes == null)
		{
			throw new NullReferenceException("NodeRuns must be assigned to the threadInfo.runData.nodes field before threads are started\nthreadInfo is an argument to the thread functions");
		}
		long maxTicks = (long)(active.maxFrameTime * 10000f);
		long targetTick = DateTime.UtcNow.Ticks + maxTicks;
		while (true)
		{
			Path p = null;
			bool blockedBefore = false;
			while (p == null)
			{
				try
				{
					p = astar.pathQueue.PopNoBlock(blockedBefore);
					if (p == null)
					{
						blockedBefore = true;
					}
				}
				catch (ThreadControlQueue.QueueTerminationException)
				{
					yield break;
				}
				if (p == null)
				{
					yield return null;
				}
			}
			maxTicks = (long)(active.maxFrameTime * 10000f);
			p.PrepareBase(runData);
			p.AdvanceState(PathState.Processing);
			OnPathDelegate tmpOnPathPreSearch = OnPathPreSearch;
			if (tmpOnPathPreSearch != null)
			{
				tmpOnPathPreSearch(p);
			}
			numPaths++;
			long startTicks = DateTime.UtcNow.Ticks;
			long totalTicks2 = 0L;
			p.Prepare();
			if (!p.IsDone())
			{
				active.debugPath = p;
				p.Initialize();
				while (!p.IsDone())
				{
					p.CalculateStep(targetTick);
					p.searchIterations++;
					if (p.IsDone())
					{
						break;
					}
					totalTicks2 += DateTime.UtcNow.Ticks - startTicks;
					yield return null;
					startTicks = DateTime.UtcNow.Ticks;
					if (astar.pathQueue.IsTerminating)
					{
						p.Error();
					}
					targetTick = DateTime.UtcNow.Ticks + maxTicks;
				}
				totalTicks2 += DateTime.UtcNow.Ticks - startTicks;
				p.duration = (float)totalTicks2 * 0.0001f;
			}
			p.Cleanup();
			active.LogPathResults(p);
			OnPathDelegate tmpImmediateCallback = p.immediateCallback;
			if (tmpImmediateCallback != null)
			{
				tmpImmediateCallback(p);
			}
			OnPathDelegate tmpOnPathPostSearch = OnPathPostSearch;
			if (tmpOnPathPostSearch != null)
			{
				tmpOnPathPostSearch(p);
			}
			pathReturnStack.Push(p);
			p.AdvanceState(PathState.ReturnQueue);
			if (DateTime.UtcNow.Ticks > targetTick)
			{
				yield return null;
				targetTick = DateTime.UtcNow.Ticks + maxTicks;
				numPaths = 0;
			}
		}
	}

	public NNInfo GetNearest(Vector3 position)
	{
		return GetNearest(position, NNConstraint.None);
	}

	public NNInfo GetNearest(Vector3 position, NNConstraint constraint)
	{
		return GetNearest(position, constraint, null);
	}

	public NNInfo GetNearest(Vector3 position, NNConstraint constraint, GraphNode hint)
	{
		if (graphs == null)
		{
			return default(NNInfo);
		}
		float num = float.PositiveInfinity;
		NNInfo result = default(NNInfo);
		int num2 = -1;
		for (int i = 0; i < graphs.Length; i++)
		{
			NavGraph navGraph = graphs[i];
			if (navGraph == null || !constraint.SuitableGraph(i, navGraph))
			{
				continue;
			}
			NNInfo nNInfo = ((!fullGetNearestSearch) ? navGraph.GetNearest(position, constraint) : navGraph.GetNearestForce(position, constraint));
			GraphNode node = nNInfo.node;
			if (node != null)
			{
				float magnitude = (nNInfo.clampedPosition - position).magnitude;
				if (prioritizeGraphs && magnitude < prioritizeGraphsLimit)
				{
					num = magnitude;
					result = nNInfo;
					num2 = i;
					break;
				}
				if (magnitude < num)
				{
					num = magnitude;
					result = nNInfo;
					num2 = i;
				}
			}
		}
		if (num2 == -1)
		{
			return result;
		}
		if (result.constrainedNode != null)
		{
			result.node = result.constrainedNode;
			result.clampedPosition = result.constClampedPosition;
		}
		if (!fullGetNearestSearch && result.node != null && !constraint.Suitable(result.node))
		{
			NNInfo nearestForce = graphs[num2].GetNearestForce(position, constraint);
			if (nearestForce.node != null)
			{
				result = nearestForce;
			}
		}
		if (!constraint.Suitable(result.node) || (constraint.constrainDistance && (result.clampedPosition - position).sqrMagnitude > maxNearestNodeDistanceSqr))
		{
			return default(NNInfo);
		}
		return result;
	}

	public GraphNode GetNearest(Ray ray)
	{
		if (graphs == null)
		{
			return null;
		}
		float minDist = float.PositiveInfinity;
		GraphNode nearestNode = null;
		Vector3 lineDirection = ray.direction;
		Vector3 lineOrigin = ray.origin;
		for (int i = 0; i < graphs.Length; i++)
		{
			NavGraph navGraph = graphs[i];
			navGraph.GetNodes(delegate(GraphNode node)
			{
				Vector3 vector = (Vector3)node.position;
				Vector3 vector2 = lineOrigin + Vector3.Dot(vector - lineOrigin, lineDirection) * lineDirection;
				float num = Mathf.Abs(vector2.x - vector.x);
				num *= num;
				if (num > minDist)
				{
					return true;
				}
				num = Mathf.Abs(vector2.z - vector.z);
				num *= num;
				if (num > minDist)
				{
					return true;
				}
				float sqrMagnitude = (vector2 - vector).sqrMagnitude;
				if (sqrMagnitude < minDist)
				{
					minDist = sqrMagnitude;
					nearestNode = node;
				}
				return true;
			});
		}
		return nearestNode;
	}
}
