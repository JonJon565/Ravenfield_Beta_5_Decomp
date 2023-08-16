using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Pathfinding.Util;
using UnityEngine;

namespace Pathfinding
{
	public abstract class Path
	{
		public OnPathDelegate callback;

		public OnPathDelegate immediateCallback;

		private PathState state;

		private object stateLock = new object();

		private PathCompleteState pathCompleteState;

		private string _errorLog = string.Empty;

		public List<GraphNode> path;

		public List<Vector3> vectorPath;

		protected float maxFrameTime;

		protected PathNode currentR;

		public float duration;

		public int searchIterations;

		public int searchedNodes;

		internal bool pooled;

		protected bool hasBeenReset;

		public NNConstraint nnConstraint = PathNNConstraint.Default;

		internal Path next;

		public Heuristic heuristic;

		public float heuristicScale = 1f;

		protected GraphNode hTargetNode;

		protected Int3 hTarget;

		public int enabledTags = -1;

		private static readonly int[] ZeroTagPenalties = new int[32];

		protected int[] internalTagPenalties;

		protected int[] manualTagPenalties;

		private List<object> claimed = new List<object>();

		private bool releasedNotSilent;

		public PathHandler pathHandler { get; private set; }

		public PathCompleteState CompleteState
		{
			get
			{
				return pathCompleteState;
			}
			protected set
			{
				pathCompleteState = value;
			}
		}

		public bool error
		{
			get
			{
				return CompleteState == PathCompleteState.Error;
			}
		}

		public string errorLog
		{
			get
			{
				return _errorLog;
			}
		}

		public DateTime callTime { get; private set; }

		[Obsolete("Has been renamed to 'pooled' to use more widely underestood terminology")]
		internal bool recycled
		{
			get
			{
				return pooled;
			}
			set
			{
				pooled = value;
			}
		}

		public ushort pathID { get; private set; }

		public int[] tagPenalties
		{
			get
			{
				return manualTagPenalties;
			}
			set
			{
				if (value == null || value.Length != 32)
				{
					manualTagPenalties = null;
					internalTagPenalties = ZeroTagPenalties;
				}
				else
				{
					manualTagPenalties = value;
					internalTagPenalties = value;
				}
			}
		}

		public virtual bool FloodingPath
		{
			get
			{
				return false;
			}
		}

		public float GetTotalLength()
		{
			if (vectorPath == null)
			{
				return float.PositiveInfinity;
			}
			float num = 0f;
			for (int i = 0; i < vectorPath.Count - 1; i++)
			{
				num += Vector3.Distance(vectorPath[i], vectorPath[i + 1]);
			}
			return num;
		}

		public IEnumerator WaitForPath()
		{
			if (GetState() == PathState.Created)
			{
				throw new InvalidOperationException("This path has not been started yet");
			}
			while (GetState() != PathState.Returned)
			{
				yield return null;
			}
		}

		public uint CalculateHScore(GraphNode node)
		{
			switch (heuristic)
			{
			case Heuristic.Euclidean:
			{
				uint num3 = (uint)((float)(GetHTarget() - node.position).costMagnitude * heuristicScale);
				if (hTargetNode != null)
				{
					num3 = Math.Max(num3, AstarPath.active.euclideanEmbedding.GetHeuristic(node.NodeIndex, hTargetNode.NodeIndex));
				}
				return num3;
			}
			case Heuristic.Manhattan:
			{
				Int3 position = node.position;
				uint num3 = (uint)((float)(Math.Abs(hTarget.x - position.x) + Math.Abs(hTarget.y - position.y) + Math.Abs(hTarget.z - position.z)) * heuristicScale);
				if (hTargetNode != null)
				{
					num3 = Math.Max(num3, AstarPath.active.euclideanEmbedding.GetHeuristic(node.NodeIndex, hTargetNode.NodeIndex));
				}
				return num3;
			}
			case Heuristic.DiagonalManhattan:
			{
				Int3 @int = GetHTarget() - node.position;
				@int.x = Math.Abs(@int.x);
				@int.y = Math.Abs(@int.y);
				@int.z = Math.Abs(@int.z);
				int num = Math.Min(@int.x, @int.z);
				int num2 = Math.Max(@int.x, @int.z);
				uint num3 = (uint)((float)(14 * num / 10 + (num2 - num) + @int.y) * heuristicScale);
				if (hTargetNode != null)
				{
					num3 = Math.Max(num3, AstarPath.active.euclideanEmbedding.GetHeuristic(node.NodeIndex, hTargetNode.NodeIndex));
				}
				return num3;
			}
			default:
				return 0u;
			}
		}

		public uint GetTagPenalty(int tag)
		{
			return (uint)internalTagPenalties[tag];
		}

		public Int3 GetHTarget()
		{
			return hTarget;
		}

		public bool CanTraverse(GraphNode node)
		{
			return node.Walkable && ((enabledTags >> (int)node.Tag) & 1) != 0;
		}

		public uint GetTraversalCost(GraphNode node)
		{
			return GetTagPenalty((int)node.Tag) + node.Penalty;
		}

		public virtual uint GetConnectionSpecialCost(GraphNode a, GraphNode b, uint currentCost)
		{
			return currentCost;
		}

		public bool IsDone()
		{
			return CompleteState != PathCompleteState.NotCalculated;
		}

		public void AdvanceState(PathState s)
		{
			lock (stateLock)
			{
				state = (PathState)Math.Max((int)state, (int)s);
			}
		}

		public PathState GetState()
		{
			return state;
		}

		public void LogError(string msg)
		{
			if (AstarPath.isEditor || AstarPath.active.logPathResults != 0)
			{
				_errorLog += msg;
			}
			if (AstarPath.active.logPathResults != 0 && AstarPath.active.logPathResults != PathLog.InGame)
			{
				Debug.LogWarning(msg);
			}
		}

		public void ForceLogError(string msg)
		{
			Error();
			_errorLog += msg;
			Debug.LogError(msg);
		}

		public void Log(string msg)
		{
			if (AstarPath.isEditor || AstarPath.active.logPathResults != 0)
			{
				_errorLog += msg;
			}
		}

		public void Error()
		{
			CompleteState = PathCompleteState.Error;
		}

		private void ErrorCheck()
		{
			if (!hasBeenReset)
			{
				throw new Exception("The path has never been reset. Use pooling API or call Reset() after creating the path with the default constructor.");
			}
			if (pooled)
			{
				throw new Exception("The path is currently in a path pool. Are you sending the path for calculation twice?");
			}
			if (pathHandler == null)
			{
				throw new Exception("Field pathHandler is not set. Please report this bug.");
			}
			if (GetState() > PathState.Processing)
			{
				throw new Exception("This path has already been processed. Do not request a path with the same path object twice.");
			}
		}

		public virtual void OnEnterPool()
		{
			if (vectorPath != null)
			{
				ListPool<Vector3>.Release(vectorPath);
			}
			if (path != null)
			{
				ListPool<GraphNode>.Release(path);
			}
			vectorPath = null;
			path = null;
		}

		public virtual void Reset()
		{
			if (object.ReferenceEquals(AstarPath.active, null))
			{
				throw new NullReferenceException("No AstarPath object found in the scene. Make sure there is one or do not create paths in Awake");
			}
			hasBeenReset = true;
			state = PathState.Created;
			releasedNotSilent = false;
			pathHandler = null;
			callback = null;
			_errorLog = string.Empty;
			pathCompleteState = PathCompleteState.NotCalculated;
			path = ListPool<GraphNode>.Claim();
			vectorPath = ListPool<Vector3>.Claim();
			currentR = null;
			duration = 0f;
			searchIterations = 0;
			searchedNodes = 0;
			nnConstraint = PathNNConstraint.Default;
			next = null;
			heuristic = AstarPath.active.heuristic;
			heuristicScale = AstarPath.active.heuristicScale;
			enabledTags = -1;
			tagPenalties = null;
			callTime = DateTime.UtcNow;
			pathID = AstarPath.active.GetNextPathID();
			hTarget = Int3.zero;
			hTargetNode = null;
		}

		protected bool HasExceededTime(int searchedNodes, long targetTime)
		{
			return DateTime.UtcNow.Ticks >= targetTime;
		}

		public void Claim(object o)
		{
			if (object.ReferenceEquals(o, null))
			{
				throw new ArgumentNullException("o");
			}
			for (int i = 0; i < claimed.Count; i++)
			{
				if (object.ReferenceEquals(claimed[i], o))
				{
					throw new ArgumentException(string.Concat("You have already claimed the path with that object (", o, "). Are you claiming the path with the same object twice?"));
				}
			}
			claimed.Add(o);
		}

		[Obsolete("Use Release(o, true) instead")]
		public void ReleaseSilent(object o)
		{
			Release(o, true);
		}

		public void Release(object o, bool silent = false)
		{
			if (o == null)
			{
				throw new ArgumentNullException("o");
			}
			for (int i = 0; i < claimed.Count; i++)
			{
				if (object.ReferenceEquals(claimed[i], o))
				{
					claimed.RemoveAt(i);
					if (!silent)
					{
						releasedNotSilent = true;
					}
					if (claimed.Count == 0 && releasedNotSilent)
					{
						PathPool.Pool(this);
					}
					return;
				}
			}
			if (claimed.Count == 0)
			{
				throw new ArgumentException(string.Concat("You are releasing a path which is not claimed at all (most likely it has been pooled already). Are you releasing the path with the same object (", o, ") twice?\nCheck out the documentation on path pooling for help."));
			}
			throw new ArgumentException(string.Concat("You are releasing a path which has not been claimed with this object (", o, "). Are you releasing the path with the same object twice?\nCheck out the documentation on path pooling for help."));
		}

		protected virtual void Trace(PathNode from)
		{
			PathNode pathNode = from;
			int num = 0;
			while (pathNode != null)
			{
				pathNode = pathNode.parent;
				num++;
				if (num > 2048)
				{
					Debug.LogWarning("Infinite loop? >2048 node path. Remove this message if you really have that long paths (Path.cs, Trace method)");
					break;
				}
			}
			if (path.Capacity < num)
			{
				path.Capacity = num;
			}
			if (vectorPath.Capacity < num)
			{
				vectorPath.Capacity = num;
			}
			pathNode = from;
			for (int i = 0; i < num; i++)
			{
				path.Add(pathNode.node);
				pathNode = pathNode.parent;
			}
			int num2 = num / 2;
			for (int j = 0; j < num2; j++)
			{
				GraphNode value = path[j];
				path[j] = path[num - j - 1];
				path[num - j - 1] = value;
			}
			for (int k = 0; k < num; k++)
			{
				vectorPath.Add((Vector3)path[k].position);
			}
		}

		protected void DebugStringPrefix(PathLog logMode, StringBuilder text)
		{
			text.Append((!error) ? "Path Completed : " : "Path Failed : ");
			text.Append("Computation Time ");
			text.Append(duration.ToString((logMode != PathLog.Heavy) ? "0.00 ms " : "0.000 ms "));
			text.Append("Searched Nodes ").Append(searchedNodes);
			if (!error)
			{
				text.Append(" Path Length ");
				text.Append((path != null) ? path.Count.ToString() : "Null");
				if (logMode == PathLog.Heavy)
				{
					text.Append("\nSearch Iterations ").Append(searchIterations);
				}
			}
		}

		protected void DebugStringSuffix(PathLog logMode, StringBuilder text)
		{
			if (error)
			{
				text.Append("\nError: ").Append(errorLog);
			}
			if (logMode == PathLog.Heavy && !AstarPath.IsUsingMultithreading)
			{
				text.Append("\nCallback references ");
				if (callback != null)
				{
					text.Append(callback.Target.GetType().FullName).AppendLine();
				}
				else
				{
					text.AppendLine("NULL");
				}
			}
			text.Append("\nPath Number ").Append(pathID).Append(" (unique id)");
		}

		public virtual string DebugString(PathLog logMode)
		{
			if (logMode == PathLog.None || (!error && logMode == PathLog.OnlyErrors))
			{
				return string.Empty;
			}
			StringBuilder debugStringBuilder = pathHandler.DebugStringBuilder;
			debugStringBuilder.Length = 0;
			DebugStringPrefix(logMode, debugStringBuilder);
			DebugStringSuffix(logMode, debugStringBuilder);
			return debugStringBuilder.ToString();
		}

		public virtual void ReturnPath()
		{
			if (callback != null)
			{
				callback(this);
			}
		}

		internal void PrepareBase(PathHandler pathHandler)
		{
			if (pathHandler.PathID > pathID)
			{
				pathHandler.ClearPathIDs();
			}
			this.pathHandler = pathHandler;
			pathHandler.InitializeForPath(this);
			if (internalTagPenalties == null || internalTagPenalties.Length != 32)
			{
				internalTagPenalties = ZeroTagPenalties;
			}
			try
			{
				ErrorCheck();
			}
			catch (Exception ex)
			{
				ForceLogError("Exception in path " + pathID + "\n" + ex);
			}
		}

		public abstract void Prepare();

		public virtual void Cleanup()
		{
		}

		public abstract void Initialize();

		public abstract void CalculateStep(long targetTick);
	}
}
