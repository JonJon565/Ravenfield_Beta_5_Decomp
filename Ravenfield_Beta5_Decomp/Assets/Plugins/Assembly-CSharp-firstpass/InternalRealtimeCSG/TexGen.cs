using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace InternalRealtimeCSG
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct TexGen
	{
		public Color Color;

		public Vector2 Translation;

		public Vector2 Scale;

		public float RotationAngle;

		public int MaterialIndex;

		public uint SmoothingGroup;

		public TexGen(int materialIndex)
		{
			Color = Color.white;
			Translation = Vector3.zero;
			Scale = Vector3.one;
			RotationAngle = 0f;
			MaterialIndex = materialIndex;
			SmoothingGroup = 0u;
		}
	}
}
