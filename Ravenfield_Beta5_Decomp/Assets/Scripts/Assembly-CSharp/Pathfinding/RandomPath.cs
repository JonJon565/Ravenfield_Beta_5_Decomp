using System;
using UnityEngine;

namespace Pathfinding
{
	public class RandomPath : ABPath
	{
		public int searchLength;

		public int spread;

		public bool uniform;

		public float aimStrength;

		private PathNode chosenNodeR;

		private PathNode maxGScoreNodeR;

		private int maxGScore;

		public Vector3 aim;

		private int nodesEvaluatedRep;

		private readonly System.Random rnd = new System.Random();

		public override bool FloodingPath
		{
			get
			{
				return true;
			}
		}

		protected override bool hasEndPoint
		{
			get
			{
				return false;
			}
		}

		public RandomPath()
		{
		}

		[Obsolete("This constructor is obsolete. Please use the pooling API and the Construct methods")]
		public RandomPath(Vector3 start, int length, OnPathDelegate callback = null)
		{
			throw new Exception("This constructor is obsolete. Please use the pooling API and the Setup methods");
		}

		public override void Reset()
		{
			base.Reset();
			searchLength = 5000;
			spread = 5000;
			uniform = true;
			aimStrength = 0f;
			chosenNodeR = null;
			maxGScoreNodeR = null;
			maxGScore = 0;
			aim = Vector3.zero;
			nodesEvaluatedRep = 0;
		}

		public static RandomPath Construct(Vector3 start, int length, OnPathDelegate callback = null)
		{
			RandomPath randomPath = PathPool.GetPath<RandomPath>();
			randomPath.Setup(start, length, callback);
			return randomPath;
		}

		protected RandomPath Setup(Vector3 start, int length, OnPathDelegate callback)
		{
			base.callback = callback;
			searchLength = length;
			originalStartPoint = start;
			originalEndPoint = Vector3.zero;
			startPoint = start;
			endPoint = Vector3.zero;
			startIntPoint = (Int3)start;
			return this;
		}

		public override void ReturnPath()
		{
			if (path != null && path.Count > 0)
			{
				endNode = path[path.Count - 1];
				endPoint = (Vector3)endNode.position;
				originalEndPoint = endPoint;
				hTarget = endNode.position;
			}
			if (callback != null)
			{
				callback(this);
			}
		}

		public override void Prepare()
		{
			nnConstraint.tags = enabledTags;
			NNInfo nearest = AstarPath.active.GetNearest(startPoint, nnConstraint, startHint);
			startPoint = nearest.clampedPosition;
			endPoint = startPoint;
			startIntPoint = (Int3)startPoint;
			hTarget = (Int3)aim;
			startNode = nearest.node;
			endNode = startNode;
			if (startNode == null || endNode == null)
			{
				LogError("Couldn't find close nodes to the start point");
				Error();
			}
			else if (!startNode.Walkable)
			{
				LogError("The node closest to the start point is not walkable");
				Error();
			}
			else
			{
				heuristicScale = aimStrength;
			}
		}

		public override void Initialize()
		{
			PathNode pathNode = base.pathHandler.GetPathNode(startNode);
			pathNode.node = startNode;
			if (searchLength + spread <= 0)
			{
				base.CompleteState = PathCompleteState.Complete;
				Trace(pathNode);
				return;
			}
			pathNode.pathID = base.pathID;
			pathNode.parent = null;
			pathNode.cost = 0u;
			pathNode.G = GetTraversalCost(startNode);
			pathNode.H = CalculateHScore(startNode);
			startNode.Open(this, pathNode, base.pathHandler);
			searchedNodes++;
			if (base.pathHandler.HeapEmpty())
			{
				LogError("No open points, the start node didn't open any nodes");
				Error();
			}
			else
			{
				currentR = base.pathHandler.PopNode();
			}
		}

		public override void CalculateStep(long targetTick)
		{
			int num = 0;
			while (base.CompleteState == PathCompleteState.NotCalculated)
			{
				searchedNodes++;
				if (currentR.G >= searchLength)
				{
					nodesEvaluatedRep++;
					if (chosenNodeR == null || rnd.NextDouble() <= (double)(1f / (float)nodesEvaluatedRep))
					{
						chosenNodeR = currentR;
					}
					if (currentR.G >= searchLength + spread)
					{
						base.CompleteState = PathCompleteState.Complete;
						break;
					}
				}
				else if (currentR.G > maxGScore)
				{
					maxGScore = (int)currentR.G;
					maxGScoreNodeR = currentR;
				}
				currentR.node.Open(this, currentR, base.pathHandler);
				if (base.pathHandler.HeapEmpty())
				{
					if (chosenNodeR != null)
					{
						base.CompleteState = PathCompleteState.Complete;
					}
					else if (maxGScoreNodeR != null)
					{
						chosenNodeR = maxGScoreNodeR;
						base.CompleteState = PathCompleteState.Complete;
					}
					else
					{
						LogError("Not a single node found to search");
						Error();
					}
					break;
				}
				currentR = base.pathHandler.PopNode();
				if (num > 500)
				{
					if (DateTime.UtcNow.Ticks >= targetTick)
					{
						return;
					}
					num = 0;
					if (searchedNodes > 1000000)
					{
						throw new Exception("Probable infinite loop. Over 1,000,000 nodes searched");
					}
				}
				num++;
			}
			if (base.CompleteState == PathCompleteState.Complete)
			{
				Trace(chosenNodeR);
			}
		}
	}
}
