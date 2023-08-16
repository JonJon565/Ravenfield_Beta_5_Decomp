using System.Collections.Generic;
using EasyRoads3D;
using UnityEngine;

[ExecuteInEditMode]
public class MarkerScript : MonoBehaviour
{
	public float tension = 0.5f;

	public float ri;

	public float OOQOQQOO;

	public float li;

	public float ODODQQOO;

	public float rs;

	public float ODOQQOOO;

	public float ls;

	public float DODOQQOO;

	public float rt;

	public float qt;

	public float ODDQODOO;

	public float lt;

	public float ODDOQOQQ;

	public bool OQOQDOCQOC;

	public bool ODQDOQOO;

	public float OQCCQDCDOQ;

	public float ODOOQQOO;

	public Transform[] ODQDOQCCCOs;

	public float[] trperc;

	public Vector3 oldPos = Vector3.zero;

	public bool autoUpdate;

	public bool changed;

	public Transform surface;

	public bool ODDCQOCODC;

	private Vector3 position;

	private bool updated;

	private int frameCount;

	private float currentstamp;

	private float newstamp;

	private bool mousedown;

	private Vector3 lookAtPoint;

	public bool bridgeObject;

	public bool distHeights;

	public RoadObjectScript objectScript;

	public List<string> OQODQQDO = new List<string>();

	public List<bool> ODOQQQDO = new List<bool>();

	public List<bool> OQQODQQOO = new List<bool>();

	public List<float> ODDOQQOO = new List<float>();

	public string[] ODDOOQDO;

	public bool[] ODDGDOOO;

	public bool[] ODDQOOO;

	public float[] ODDQOODO;

	public float[] ODOQODOO;

	public float[] ODDOQDO;

	public int markerNum;

	public string distance = "0";

	public string OQCOCOCDCQ = "0";

	public string OQCCOCCDOD = "0";

	public bool newSegment;

	public float floorDepth = 2f;

	public float oldFloorDepth = 2f;

	public float waterLevel = 0.5f;

	public bool lockWaterLevel = true;

	public bool tunnelFlag;

	public bool sharpCorner;

	public bool snapMarker;

	public int markerInt;

	private void Start()
	{
		foreach (Transform item in base.transform)
		{
			surface = item;
		}
	}

	private void OnDrawGizmos()
	{
		if (!(objectScript != null))
		{
			return;
		}
		if (!objectScript.OCDCCDOOOD)
		{
			if (snapMarker && ODCCQOCQOD.terrain != null)
			{
				Vector3 worldPosition = base.transform.position;
				worldPosition.y = ODCCQOCQOD.terrain.SampleHeight(worldPosition) + ODCCQOCQOD.terrain.transform.position.y;
				base.transform.position = worldPosition;
			}
			Vector3 vector = base.transform.position - oldPos;
			if (OQOQDOCQOC && oldPos != Vector3.zero && vector != Vector3.zero)
			{
				int num = 0;
				Transform[] oDQDOQCCCOs = ODQDOQCCCOs;
				foreach (Transform transform in oDQDOQCCCOs)
				{
					transform.position += vector * trperc[num];
					if (snapMarker && ODCCQOCQOD.terrain != null)
					{
						Vector3 worldPosition = transform.position;
						worldPosition.y = ODCCQOCQOD.terrain.SampleHeight(worldPosition);
						transform.position = worldPosition;
					}
					num++;
				}
			}
			if (oldPos != Vector3.zero && vector != Vector3.zero)
			{
				changed = true;
				if (objectScript.OCDCCDOOOD)
				{
					objectScript.OQQCDCOCQO.specialRoadMaterial = true;
				}
			}
			oldPos = base.transform.position;
		}
		else if (objectScript.ODODDDOO)
		{
			base.transform.position = oldPos;
		}
	}

	private void SetObjectScript()
	{
		objectScript = base.transform.parent.parent.GetComponent<RoadObjectScript>();
		if (objectScript.OQQCDCOCQO == null)
		{
			List<ODODDQQO> arr = OCOQDQCQDO.OOOCODQOOQ(false);
			objectScript.OQDCDCOODC(arr, OCOQDQCQDO.ODOCQDDCCD(arr), OCOQDQCQDO.ODCOCCOQCQ(arr));
		}
	}

	private void GetMarkerCount()
	{
		int num = 0;
		foreach (Transform item in base.transform.parent)
		{
			if (item == base.transform)
			{
				markerInt = num;
				break;
			}
			num++;
		}
	}

	public void LeftIndent(float change, float perc)
	{
		ri += change * perc;
		if (ri < objectScript.indent)
		{
			ri = objectScript.indent;
		}
		OOQOQQOO = ri;
	}

	public void RightIndent(float change, float perc)
	{
		li += change * perc;
		if (li < objectScript.indent)
		{
			li = objectScript.indent;
		}
		ODODQQOO = li;
	}

	public void LeftSurrounding(float change, float perc)
	{
		rs += change * perc;
		if (rs < objectScript.indent)
		{
			rs = objectScript.indent;
		}
		ODOQQOOO = rs;
	}

	public void RightSurrounding(float change, float perc)
	{
		ls += change * perc;
		if (ls < objectScript.indent)
		{
			ls = objectScript.indent;
		}
		DODOQQOO = ls;
	}

	public void LeftTilting(float change, float perc)
	{
		rt += change * perc;
		if (rt < 0f)
		{
			rt = 0f;
		}
		ODDQODOO = rt;
	}

	public void RightTilting(float change, float perc)
	{
		lt += change * perc;
		if (lt < 0f)
		{
			lt = 0f;
		}
		ODDOQOQQ = lt;
	}

	public void FloorDepth(float change, float perc)
	{
		floorDepth += change * perc;
		if (floorDepth > 0f)
		{
			floorDepth = 0f;
		}
		oldFloorDepth = floorDepth;
	}

	public bool InSelected()
	{
		for (int i = 0; i < objectScript.ODQDOQCCCOs.Length; i++)
		{
			if (objectScript.ODQDOQCCCOs[i] == base.gameObject)
			{
				return true;
			}
		}
		return false;
	}
}
