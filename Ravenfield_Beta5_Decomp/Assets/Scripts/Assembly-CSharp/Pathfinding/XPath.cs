using System;
using UnityEngine;

namespace Pathfinding
{
	public class XPath : ABPath
	{
		public PathEndingCondition endingCondition;

		public new static XPath Construct(Vector3 start, Vector3 end, OnPathDelegate callback = null)
		{
			XPath xPath = PathPool.GetPath<XPath>();
			xPath.Setup(start, end, callback);
			xPath.endingCondition = new ABPathEndingCondition(xPath);
			return xPath;
		}

		public override void Reset()
		{
			base.Reset();
			endingCondition = null;
		}

		protected override void CompletePathIfStartIsValidTarget()
		{
			PathNode pathNode = base.pathHandler.GetPathNode(startNode);
			if (endingCondition.TargetFound(pathNode))
			{
				endNode = pathNode.node;
				endPoint = (Vector3)endNode.position;
				Trace(pathNode);
				base.CompleteState = PathCompleteState.Complete;
			}
		}

		public override void CalculateStep(long targetTick)
		{
			int num = 0;
			while (base.CompleteState == PathCompleteState.NotCalculated)
			{
				searchedNodes++;
				if (endingCondition.TargetFound(currentR))
				{
					base.CompleteState = PathCompleteState.Complete;
					break;
				}
				currentR.node.Open(this, currentR, base.pathHandler);
				if (base.pathHandler.HeapEmpty())
				{
					Error();
					LogError("Searched whole area but could not find target");
					return;
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
				endNode = currentR.node;
				endPoint = (Vector3)endNode.position;
				Trace(currentR);
			}
		}
	}
}
