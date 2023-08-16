using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PaverRoad
{
	[Serializable]
	public class Segment
	{
		public Vector3 position;

		public Vector3 forwardPosition;
	}

	public List<Segment> segments;
}
