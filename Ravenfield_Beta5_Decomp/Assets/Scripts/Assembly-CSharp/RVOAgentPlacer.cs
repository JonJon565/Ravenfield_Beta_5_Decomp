using System;
using System.Collections;
using UnityEngine;

[HelpURL("http://arongranberg.com/astar/docs/class_r_v_o_agent_placer.php")]
public class RVOAgentPlacer : MonoBehaviour
{
	private const float rad2Deg = 180f / (float)Math.PI;

	public int agents = 100;

	public float ringSize = 100f;

	public LayerMask mask;

	public GameObject prefab;

	public Vector3 goalOffset;

	public float repathRate = 1f;

	private IEnumerator Start()
	{
		yield return null;
		for (int i = 0; i < agents; i++)
		{
			float angle = (float)i / (float)agents * (float)Math.PI * 2f;
			Vector3 pos = new Vector3((float)Math.Cos(angle), 0f, (float)Math.Sin(angle)) * ringSize;
			Vector3 antipodal = -pos + goalOffset;
			GameObject go = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.Euler(0f, angle + 180f, 0f)) as GameObject;
			RVOExampleAgent ag = go.GetComponent<RVOExampleAgent>();
			if (ag == null)
			{
				Debug.LogError("Prefab does not have an RVOExampleAgent component attached");
				break;
			}
			go.transform.parent = base.transform;
			go.transform.position = pos;
			ag.repathRate = repathRate;
			ag.SetTarget(antipodal);
			ag.SetColor(GetColor(angle));
		}
	}

	public Color GetColor(float angle)
	{
		return HSVToRGB(angle * (180f / (float)Math.PI), 0.8f, 0.6f);
	}

	private static Color HSVToRGB(float h, float s, float v)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = s * v;
		float num5 = h / 60f;
		float num6 = num4 * (1f - Math.Abs(num5 % 2f - 1f));
		if (num5 < 1f)
		{
			num = num4;
			num2 = num6;
		}
		else if (num5 < 2f)
		{
			num = num6;
			num2 = num4;
		}
		else if (num5 < 3f)
		{
			num2 = num4;
			num3 = num6;
		}
		else if (num5 < 4f)
		{
			num2 = num6;
			num3 = num4;
		}
		else if (num5 < 5f)
		{
			num = num6;
			num3 = num4;
		}
		else if (num5 < 6f)
		{
			num = num4;
			num3 = num6;
		}
		float num7 = v - num4;
		num += num7;
		num2 += num7;
		num3 += num7;
		return new Color(num, num2, num3);
	}
}
