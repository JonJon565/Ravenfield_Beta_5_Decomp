using System;
using System.Collections;
using Pathfinding;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[HelpURL("http://arongranberg.com/astar/docs/class_dynamic_grid_obstacle.php")]
public class DynamicGridObstacle : MonoBehaviour
{
	private Collider col;

	public float updateError = 1f;

	public float checkTime = 0.2f;

	private Bounds prevBounds;

	private bool isWaitingForUpdate;

	private void Start()
	{
		col = GetComponent<Collider>();
		if (col == null)
		{
			throw new Exception("A collider must be attached to the GameObject for DynamicGridObstacle to work");
		}
		StartCoroutine(UpdateGraphs());
	}

	private IEnumerator UpdateGraphs()
	{
		if (col == null || AstarPath.active == null)
		{
			Debug.LogWarning("No collider is attached to the GameObject. Canceling check");
			yield break;
		}
		while ((bool)col)
		{
			while (isWaitingForUpdate)
			{
				yield return new WaitForSeconds(checkTime);
			}
			Bounds newBounds = col.bounds;
			Bounds merged = newBounds;
			merged.Encapsulate(prevBounds);
			Vector3 minDiff = merged.min - newBounds.min;
			Vector3 maxDiff = merged.max - newBounds.max;
			if (Mathf.Abs(minDiff.x) > updateError || Mathf.Abs(minDiff.y) > updateError || Mathf.Abs(minDiff.z) > updateError || Mathf.Abs(maxDiff.x) > updateError || Mathf.Abs(maxDiff.y) > updateError || Mathf.Abs(maxDiff.z) > updateError)
			{
				isWaitingForUpdate = true;
				DoUpdateGraphs();
			}
			yield return new WaitForSeconds(checkTime);
		}
		OnDestroy();
	}

	public void OnDestroy()
	{
		if (AstarPath.active != null)
		{
			GraphUpdateObject ob = new GraphUpdateObject(prevBounds);
			AstarPath.active.UpdateGraphs(ob);
		}
	}

	public void DoUpdateGraphs()
	{
		if (!(col == null))
		{
			isWaitingForUpdate = false;
			Bounds bounds = col.bounds;
			Bounds bounds2 = bounds;
			bounds2.Encapsulate(prevBounds);
			if (BoundsVolume(bounds2) < BoundsVolume(bounds) + BoundsVolume(prevBounds))
			{
				AstarPath.active.UpdateGraphs(bounds2);
			}
			else
			{
				AstarPath.active.UpdateGraphs(prevBounds);
				AstarPath.active.UpdateGraphs(bounds);
			}
			prevBounds = bounds;
		}
	}

	private static float BoundsVolume(Bounds b)
	{
		return Math.Abs(b.size.x * b.size.y * b.size.z);
	}
}
