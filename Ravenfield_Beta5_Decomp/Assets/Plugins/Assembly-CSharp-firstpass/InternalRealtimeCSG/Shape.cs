using System;
using UnityEngine;

namespace InternalRealtimeCSG
{
	[Serializable]
	public sealed class Shape
	{
		[SerializeField]
		public CutNode[] CutNodes;

		[SerializeField]
		public Surface[] Surfaces = new Surface[0];

		[SerializeField]
		public TexGen[] TexGens = new TexGen[0];

		[SerializeField]
		public TexGenFlags[] TexGenFlags = new TexGenFlags[0];

		[SerializeField]
		public Material[] Materials = new Material[0];
	}
}
