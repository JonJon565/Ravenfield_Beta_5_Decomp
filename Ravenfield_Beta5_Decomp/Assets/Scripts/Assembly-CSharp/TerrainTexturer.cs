using UnityEngine;

public class TerrainTexturer : MonoBehaviour
{
	public AnimationCurve cliffTextureFalloff;

	public bool oceanFloor;

	public float oceanFloorHeight = 5f;

	public float oceanFloorSmear = 3f;

	public bool patchy;

	public AnimationCurve patchiness;

	public float patchinessFrequency = 10f;

	[Range(0f, 128f)]
	public int detailAmount = 4;

	[Range(0f, 1f)]
	public float rubbleAmount = 0.1f;

	[Range(0f, 1f)]
	public float otherDetailChance = 0.1f;
}
