using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace InternalRealtimeCSG
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct CutNode
	{
		public static short Inside = -1;

		public static short Outside = -2;

		[SerializeField]
		public int planeIndex;

		[SerializeField]
		public short backNodeIndex;

		[SerializeField]
		public short frontNodeIndex;
	}
}
