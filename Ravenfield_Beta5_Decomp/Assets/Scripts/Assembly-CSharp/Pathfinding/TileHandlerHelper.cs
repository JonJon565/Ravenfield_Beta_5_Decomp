using System;
using System.Collections.Generic;
using Pathfinding.Util;
using UnityEngine;

namespace Pathfinding
{
	[HelpURL("http://arongranberg.com/astar/docs/class_pathfinding_1_1_tile_handler_helper.php")]
	public class TileHandlerHelper : MonoBehaviour
	{
		private TileHandler handler;

		public float updateInterval;

		private float lastUpdateTime = -999f;

		private readonly List<Bounds> forcedReloadBounds = new List<Bounds>();

		public void UseSpecifiedHandler(TileHandler handler)
		{
			this.handler = handler;
		}

		private void OnEnable()
		{
			NavmeshCut.OnDestroyCallback += HandleOnDestroyCallback;
		}

		private void OnDisable()
		{
			NavmeshCut.OnDestroyCallback -= HandleOnDestroyCallback;
		}

		public void DiscardPending()
		{
			List<NavmeshCut> all = NavmeshCut.GetAll();
			for (int i = 0; i < all.Count; i++)
			{
				if (all[i].RequiresUpdate())
				{
					all[i].NotifyUpdated();
				}
			}
		}

		private void Start()
		{
			if (UnityEngine.Object.FindObjectsOfType(typeof(TileHandlerHelper)).Length > 1)
			{
				Debug.LogError("There should only be one TileHandlerHelper per scene. Destroying.");
				UnityEngine.Object.Destroy(this);
			}
			else if (handler == null)
			{
				if (AstarPath.active == null || AstarPath.active.astarData.recastGraph == null)
				{
					Debug.LogWarning("No AstarPath object in the scene or no RecastGraph on that AstarPath object");
				}
				handler = new TileHandler(AstarPath.active.astarData.recastGraph);
				handler.CreateTileTypesFromGraph();
			}
		}

		private void HandleOnDestroyCallback(NavmeshCut obj)
		{
			forcedReloadBounds.Add(obj.LastBounds);
			lastUpdateTime = -999f;
		}

		private void Update()
		{
			if (updateInterval != -1f && !(Time.realtimeSinceStartup - lastUpdateTime < updateInterval) && handler != null)
			{
				ForceUpdate();
			}
		}

		public void ForceUpdate()
		{
			if (handler == null)
			{
				throw new Exception("Cannot update graphs. No TileHandler. Do not call this method in Awake.");
			}
			lastUpdateTime = Time.realtimeSinceStartup;
			List<NavmeshCut> all = NavmeshCut.GetAll();
			if (forcedReloadBounds.Count == 0)
			{
				int num = 0;
				for (int i = 0; i < all.Count; i++)
				{
					if (all[i].RequiresUpdate())
					{
						num++;
						break;
					}
				}
				if (num == 0)
				{
					return;
				}
			}
			bool flag = handler.StartBatchLoad();
			for (int j = 0; j < forcedReloadBounds.Count; j++)
			{
				handler.ReloadInBounds(forcedReloadBounds[j]);
			}
			forcedReloadBounds.Clear();
			for (int k = 0; k < all.Count; k++)
			{
				if (all[k].enabled)
				{
					if (all[k].RequiresUpdate())
					{
						handler.ReloadInBounds(all[k].LastBounds);
						handler.ReloadInBounds(all[k].GetBounds());
					}
				}
				else if (all[k].RequiresUpdate())
				{
					handler.ReloadInBounds(all[k].LastBounds);
				}
			}
			for (int l = 0; l < all.Count; l++)
			{
				if (all[l].RequiresUpdate())
				{
					all[l].NotifyUpdated();
				}
			}
			if (flag)
			{
				handler.EndBatchLoad();
			}
		}
	}
}
