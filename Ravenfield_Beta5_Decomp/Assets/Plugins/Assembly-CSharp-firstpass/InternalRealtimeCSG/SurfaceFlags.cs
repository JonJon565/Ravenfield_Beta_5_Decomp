using System;

namespace InternalRealtimeCSG
{
	[Serializable]
	[Flags]
	public enum SurfaceFlags
	{
		Default = 0,
		DisableCollidable = 1,
		DisableRenderable = 2,
		DisableShadowRender = 4,
		DisableNavMeshWalkable = 8
	}
}
