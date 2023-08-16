using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace InternalRealtimeCSG
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct Surface
	{
		public CSGPlane Plane;

		public Vector3 Tangent;

		public Vector3 BiNormal;

		public int TexGenIndex;
	}
}
