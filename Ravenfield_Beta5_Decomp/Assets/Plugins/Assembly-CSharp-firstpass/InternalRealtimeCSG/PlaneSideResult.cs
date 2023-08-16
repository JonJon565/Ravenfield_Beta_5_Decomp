using System;

namespace InternalRealtimeCSG
{
	[Serializable]
	internal enum PlaneSideResult : byte
	{
		Intersects = 0,
		Inside = 1,
		Outside = 2
	}
}
