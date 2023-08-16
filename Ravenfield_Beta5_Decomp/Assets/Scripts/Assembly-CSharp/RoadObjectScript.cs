using System.Collections.Generic;
using EasyRoads3D;
using UnityEngine;

public class RoadObjectScript : MonoBehaviour
{
	public static string version = string.Empty;

	public int objectType;

	public bool displayRoad = true;

	public float roadWidth = 5f;

	public float indent = 3f;

	public float surrounding = 5f;

	public float raise = 1f;

	public float raiseMarkers = 0.5f;

	public bool OOQDOOQQ;

	public bool renderRoad = true;

	public bool beveledRoad;

	public bool applySplatmap;

	public int splatmapLayer = 4;

	public bool autoUpdate = true;

	public float geoResolution = 5f;

	public int roadResolution = 1;

	public float tuw = 15f;

	public int splatmapSmoothLevel;

	public float opacity = 1f;

	public int expand;

	public int offsetX;

	public int offsetY;

	private Material surfaceMaterial;

	public float surfaceOpacity = 1f;

	public float smoothDistance = 1f;

	public float smoothSurDistance = 3f;

	private bool handleInsertFlag;

	public bool handleVegetation = true;

	public float ODCQCQQQCO = 2f;

	public float ODCCQCDCDQ = 1f;

	public int materialType;

	private string[] materialStrings;

	public string uname;

	public string email;

	private MarkerScript[] mSc;

	private bool ODQDOQCDQC;

	private bool[] OCCDQODQCD;

	private bool[] OQQCQQDOCQ;

	public string[] OQDCCDQDOD;

	public string[] ODODQOQO;

	public int[] ODODQOQOInt;

	public int ODCDCCOODQ = -1;

	public int OCQDCCQCOD = -1;

	public static GUISkin ODCOOCDQQC;

	public static GUISkin OCOCCCDQCO;

	public bool OODODOCCCC;

	private Vector3 cPos;

	private Vector3 ePos;

	public bool ODDCQOCODC;

	public static Texture2D ODCCDDCCDO;

	public int markers = 1;

	public OOCOOCQQDQ OQQCDCOCQO;

	private GameObject ODOQDQOO;

	public bool OCDCCDOOOD;

	public bool doTerrain;

	private Transform ODQDOQCCCO;

	public GameObject[] ODQDOQCCCOs;

	private static string ODOCCCQQDD;

	public Transform obj;

	private string ODDODDOQDO;

	public static string erInit = string.Empty;

	public static Transform OQQDCQCDCQ;

	private RoadObjectScript OOOOOCQOCO;

	public bool flyby;

	private Vector3 pos;

	private float fl;

	private float oldfl;

	private bool OQDOOQCOCO;

	private bool ODCCOOCOQD;

	private bool OCCQDDOQDQ;

	public Transform ODOQCQDDCQ;

	public int OdQODQOD = 1;

	public float OOQQQDOD;

	public float OOQQQDODOffset;

	public float OOQQQDODLength;

	public bool ODODDDOO;

	public static string[] ODOQDOQO;

	public static string[] ODODOQQO;

	public static string[] ODODQOOQ;

	public int ODQDOOQO;

	public string[] ODQQQQQO;

	public string[] ODODDQOO;

	public bool[] ODODQQOD;

	public int[] OOQQQOQO;

	public int ODOQOOQO;

	public bool forceY;

	public float yChange;

	public float floorDepth = 2f;

	public float waterLevel = 1.5f;

	public bool lockWaterLevel = true;

	public float lastY;

	public string distance = "0";

	public string markerDisplayStr = "Hide Markers";

	public static string[] objectStrings;

	public string objectText = "Road";

	public bool applyAnimation;

	public float waveSize = 1.5f;

	public float waveHeight = 0.15f;

	public bool snapY = true;

	private TextAnchor origAnchor;

	public bool autoODODDQQO;

	public Texture2D roadTexture;

	public Texture2D roadMaterial;

	public string[] ODCQOQQDCQ;

	public string[] ODCDODQODC;

	public int selectedWaterMaterial;

	public int selectedWaterScript;

	private bool doRestore;

	public bool doFlyOver;

	public static GameObject tracer;

	public Camera goCam;

	public float speed = 1f;

	public float offset;

	public bool camInit;

	public GameObject customMesh;

	public static bool disableFreeAlerts = true;

	public bool multipleTerrains;

	public bool editRestore = true;

	public Material roadMaterialEdit;

	public static int backupLocation;

	public string[] backupStrings = new string[2] { "Outside Assets folder path", "Inside Assets folder path" };

	public Vector3[] leftVecs = new Vector3[0];

	public Vector3[] rightVecs = new Vector3[0];

	public bool applyTangents;

	public bool sosBuild;

	public float splinePos;

	public float camHeight = 3f;

	public Vector3 splinePosV3 = Vector3.zero;

	public bool blendFlag;

	public float startBlendDistance = 5f;

	public float endBlendDistance = 5f;

	public bool iOS;

	public static string extensionPath = string.Empty;

	public void OQDCDCOODC(List<ODODDQQO> arr, string[] DOODQOQO, string[] OODDQOQO)
	{
		OCODQDOCOC(base.transform, arr, DOODQOQO, OODDQOQO);
	}

	public void OCOODDOOQQ(MarkerScript markerScript)
	{
		ODQDOQCCCO = markerScript.transform;
		List<GameObject> list = new List<GameObject>();
		for (int i = 0; i < ODQDOQCCCOs.Length; i++)
		{
			if (ODQDOQCCCOs[i] != markerScript.gameObject)
			{
				list.Add(ODQDOQCCCOs[i]);
			}
		}
		list.Add(markerScript.gameObject);
		ODQDOQCCCOs = list.ToArray();
		ODQDOQCCCO = markerScript.transform;
		OQQCDCOCQO.ODQCDOOQQQ(ODQDOQCCCO, ODQDOQCCCOs, markerScript.OQOQDOCQOC, markerScript.OQCCQDCDOQ, ODOQCQDDCQ, out markerScript.ODQDOQCCCOs, out markerScript.trperc, ODQDOQCCCOs);
		OCQDCCQCOD = -1;
	}

	public void OCOQCOQQDO(MarkerScript markerScript)
	{
		if (markerScript.OQCCQDCDOQ != markerScript.ODOOQQOO || markerScript.OQCCQDCDOQ != markerScript.ODOOQQOO)
		{
			OQQCDCOCQO.ODQCDOOQQQ(ODQDOQCCCO, ODQDOQCCCOs, markerScript.OQOQDOCQOC, markerScript.OQCCQDCDOQ, ODOQCQDDCQ, out markerScript.ODQDOQCCCOs, out markerScript.trperc, ODQDOQCCCOs);
			markerScript.ODQDOQOO = markerScript.OQOQDOCQOC;
			markerScript.ODOOQQOO = markerScript.OQCCQDCDOQ;
		}
		if (OOOOOCQOCO.autoUpdate)
		{
			ODCQOCDDDC(OOOOOCQOCO.geoResolution, false, false);
		}
	}

	public void ResetMaterials(MarkerScript markerScript)
	{
		if (OQQCDCOCQO != null)
		{
			OQQCDCOCQO.ODQCDOOQQQ(ODQDOQCCCO, ODQDOQCCCOs, markerScript.OQOQDOCQOC, markerScript.OQCCQDCDOQ, ODOQCQDDCQ, out markerScript.ODQDOQCCCOs, out markerScript.trperc, ODQDOQCCCOs);
		}
	}

	public void OQCQDQCDDQ(MarkerScript markerScript)
	{
		if (markerScript.OQCCQDCDOQ != markerScript.ODOOQQOO)
		{
			OQQCDCOCQO.ODQCDOOQQQ(ODQDOQCCCO, ODQDOQCCCOs, markerScript.OQOQDOCQOC, markerScript.OQCCQDCDOQ, ODOQCQDDCQ, out markerScript.ODQDOQCCCOs, out markerScript.trperc, ODQDOQCCCOs);
			markerScript.ODOOQQOO = markerScript.OQCCQDCDOQ;
		}
		ODCQOCDDDC(OOOOOCQOCO.geoResolution, false, false);
	}

	private void OOOCDDDQQO(string ctrl, MarkerScript markerScript)
	{
		int num = 0;
		Transform[] oDQDOQCCCOs = markerScript.ODQDOQCCCOs;
		foreach (Transform transform in oDQDOQCCCOs)
		{
			MarkerScript component = transform.GetComponent<MarkerScript>();
			switch (ctrl)
			{
			case "rs":
				component.LeftSurrounding(markerScript.rs - markerScript.ODOQQOOO, markerScript.trperc[num]);
				break;
			case "ls":
				component.RightSurrounding(markerScript.ls - markerScript.DODOQQOO, markerScript.trperc[num]);
				break;
			case "ri":
				component.LeftIndent(markerScript.ri - markerScript.OOQOQQOO, markerScript.trperc[num]);
				break;
			case "li":
				component.RightIndent(markerScript.li - markerScript.ODODQQOO, markerScript.trperc[num]);
				break;
			case "rt":
				component.LeftTilting(markerScript.rt - markerScript.ODDQODOO, markerScript.trperc[num]);
				break;
			case "lt":
				component.RightTilting(markerScript.lt - markerScript.ODDOQOQQ, markerScript.trperc[num]);
				break;
			case "floorDepth":
				component.FloorDepth(markerScript.floorDepth - markerScript.oldFloorDepth, markerScript.trperc[num]);
				break;
			}
			num++;
		}
	}

	public void OQCCDQDCDQ()
	{
		if (markers > 1)
		{
			ODCQOCDDDC(OOOOOCQOCO.geoResolution, false, false);
		}
	}

	public void OCODQDOCOC(Transform tr, List<ODODDQQO> arr, string[] DOODQOQO, string[] OODDQOQO)
	{
		version = "2.5.7";
		ODCOOCDQQC = (GUISkin)Resources.Load("ER3DSkin", typeof(GUISkin));
		ODCCDDCCDO = (Texture2D)Resources.Load("ER3DLogo", typeof(Texture2D));
		if (objectStrings == null)
		{
			objectStrings = new string[3];
			objectStrings[0] = "Road Object";
			objectStrings[1] = "River Object";
			objectStrings[2] = "Procedural Mesh Object";
		}
		obj = tr;
		OQQCDCOCQO = new OOCOOCQQDQ();
		OOOOOCQOCO = obj.GetComponent<RoadObjectScript>();
		foreach (Transform item in obj)
		{
			if (item.name == "Markers")
			{
				ODOQCQDDCQ = item;
			}
		}
		RoadObjectScript[] array = (RoadObjectScript[])Object.FindObjectsOfType(typeof(RoadObjectScript));
		OOCOOCQQDQ.terrainList.Clear();
		Terrain[] array2 = (Terrain[])Object.FindObjectsOfType(typeof(Terrain));
		Terrain[] array3 = array2;
		foreach (Terrain terrain in array3)
		{
			Terrains terrains = new Terrains();
			terrains.terrain = terrain;
			if (!terrain.gameObject.GetComponent<EasyRoads3DTerrainID>())
			{
				EasyRoads3DTerrainID easyRoads3DTerrainID = terrain.gameObject.AddComponent<EasyRoads3DTerrainID>();
				terrains.id = (easyRoads3DTerrainID.terrainid = Random.Range(100000000, 999999999).ToString());
			}
			else
			{
				terrains.id = terrain.gameObject.GetComponent<EasyRoads3DTerrainID>().terrainid;
			}
			OOCOOCQQDQ.OODOQDQODQ(terrains);
		}
		ODCCQOCQOD.OODOQDQODQ();
		if (roadMaterialEdit == null)
		{
			roadMaterialEdit = (Material)Resources.Load("materials/roadMaterialEdit", typeof(Material));
		}
		if (objectType == 0 && GameObject.Find(base.gameObject.name + "/road") == null)
		{
			GameObject gameObject = new GameObject("road");
			gameObject.transform.parent = base.transform;
		}
		OQQCDCOCQO.OOCOCQOOQC(obj, ODOCCCQQDD, OOOOOCQOCO.roadWidth, surfaceOpacity, out ODDCQOCODC, out indent, applyAnimation, waveSize, waveHeight);
		OQQCDCOCQO.ODCCQCDCDQ = ODCCQCDCDQ;
		OQQCDCOCQO.ODCQCQQQCO = ODCQCQQQCO;
		OQQCDCOCQO.OdQODQOD = OdQODQOD + 1;
		OQQCDCOCQO.OOQQQDOD = OOQQQDOD;
		OQQCDCOCQO.OOQQQDODOffset = OOQQQDODOffset;
		OQQCDCOCQO.OOQQQDODLength = OOQQQDODLength;
		OQQCDCOCQO.objectType = objectType;
		OQQCDCOCQO.snapY = snapY;
		OQQCDCOCQO.terrainRendered = OCDCCDOOOD;
		OQQCDCOCQO.handleVegetation = handleVegetation;
		OQQCDCOCQO.raise = raise;
		OQQCDCOCQO.roadResolution = roadResolution;
		OQQCDCOCQO.multipleTerrains = multipleTerrains;
		OQQCDCOCQO.editRestore = editRestore;
		OQQCDCOCQO.roadMaterialEdit = roadMaterialEdit;
		OQQCDCOCQO.renderRoad = renderRoad;
		OQQCDCOCQO.rscrpts = array.Length;
		OQQCDCOCQO.blendFlag = blendFlag;
		OQQCDCOCQO.startBlendDistance = startBlendDistance;
		OQQCDCOCQO.endBlendDistance = endBlendDistance;
		OQQCDCOCQO.iOS = iOS;
		if (backupLocation == 0)
		{
			OCQQOCDCCD.backupFolder = "/EasyRoads3D";
		}
		else
		{
			OCQQOCDCCD.backupFolder = OCQQOCDCCD.extensionPath + "/Backups";
		}
		ODODQOQO = OQQCDCOCQO.OOOCCQCQOO();
		ODODQOQOInt = OQQCDCOCQO.OOOODCDDOQ();
		if (OCDCCDOOOD)
		{
			doRestore = true;
		}
		OOCOOQCQQD();
		if (arr != null || ODODQOOQ == null)
		{
			OQQODDQCQD(arr, DOODQOQO, OODDQOQO);
		}
		if (!doRestore)
		{
		}
	}

	public void UpdateBackupFolder()
	{
	}

	public void ODCCDOOOQO()
	{
		if ((!ODODDDOO || objectType == 2) && OCCDQODQCD != null)
		{
			for (int i = 0; i < OCCDQODQCD.Length; i++)
			{
				OCCDQODQCD[i] = false;
				OQQCQQDOCQ[i] = false;
			}
		}
	}

	public void OCOCDDCDDO(Vector3 pos)
	{
		if (!displayRoad)
		{
			displayRoad = true;
			OQQCDCOCQO.OQOOCOOCQC(displayRoad, ODOQCQDDCQ);
		}
		pos.y += OOOOOCQOCO.raiseMarkers;
		if (forceY && ODOQDQOO != null)
		{
			float num = Vector3.Distance(pos, ODOQDQOO.transform.position);
			pos.y = ODOQDQOO.transform.position.y + yChange * (num / 100f);
		}
		else if (forceY && markers == 0)
		{
			lastY = pos.y;
		}
		GameObject gameObject = null;
		gameObject = ((!(ODOQDQOO != null)) ? ((GameObject)Object.Instantiate(Resources.Load("marker", typeof(GameObject)))) : Object.Instantiate(ODOQDQOO));
		Transform transform = gameObject.transform;
		transform.position = pos;
		transform.parent = ODOQCQDDCQ;
		markers++;
		string text = ((markers < 10) ? ("Marker000" + markers) : ((markers >= 100) ? ("Marker0" + markers) : ("Marker00" + markers)));
		transform.gameObject.name = text;
		MarkerScript component = transform.GetComponent<MarkerScript>();
		component.ODDCQOCODC = false;
		component.objectScript = obj.GetComponent<RoadObjectScript>();
		if (ODOQDQOO == null)
		{
			component.waterLevel = OOOOOCQOCO.waterLevel;
			component.floorDepth = OOOOOCQOCO.floorDepth;
			component.ri = OOOOOCQOCO.indent;
			component.li = OOOOOCQOCO.indent;
			component.rs = OOOOOCQOCO.surrounding;
			component.ls = OOOOOCQOCO.surrounding;
			component.tension = 0.5f;
			if (objectType == 1)
			{
				pos.y -= waterLevel;
				transform.position = pos;
			}
		}
		if (objectType == 2 && component.surface != null)
		{
			component.surface.gameObject.SetActive(false);
		}
		ODOQDQOO = transform.gameObject;
		if (markers > 1)
		{
			ODCQOCDDDC(OOOOOCQOCO.geoResolution, false, false);
			if (materialType == 0)
			{
				OQQCDCOCQO.OCDDQQCCCO(materialType);
			}
		}
	}

	public void ODCQOCDDDC(float geo, bool renderMode, bool camMode)
	{
		OQQCDCOCQO.ODDODQOCQO.Clear();
		int num = 0;
		foreach (Transform item in obj)
		{
			if (!(item.name == "Markers"))
			{
				continue;
			}
			foreach (Transform item2 in item)
			{
				MarkerScript component = item2.GetComponent<MarkerScript>();
				component.objectScript = obj.GetComponent<RoadObjectScript>();
				if (!component.ODDCQOCODC)
				{
					component.ODDCQOCODC = OQQCDCOCQO.OOCDQOCOQQ(item2);
				}
				OCQODDOCQD oCQODDOCQD = new OCQODDOCQD();
				oCQODDOCQD.position = item2.position;
				oCQODDOCQD.num = OQQCDCOCQO.ODDODQOCQO.Count;
				oCQODDOCQD.object1 = item2;
				oCQODDOCQD.object2 = component.surface;
				oCQODDOCQD.tension = component.tension;
				oCQODDOCQD.ri = component.ri;
				if (oCQODDOCQD.ri < 1f)
				{
					oCQODDOCQD.ri = 1f;
				}
				oCQODDOCQD.li = component.li;
				if (oCQODDOCQD.li < 1f)
				{
					oCQODDOCQD.li = 1f;
				}
				oCQODDOCQD.rt = component.rt;
				oCQODDOCQD.lt = component.lt;
				oCQODDOCQD.rs = component.rs;
				if (oCQODDOCQD.rs < 1f)
				{
					oCQODDOCQD.rs = 1f;
				}
				oCQODDOCQD.ODQQQQQOQC = component.rs;
				oCQODDOCQD.ls = component.ls;
				if (oCQODDOCQD.ls < 1f)
				{
					oCQODDOCQD.ls = 1f;
				}
				oCQODDOCQD.OOQDCCOOOO = component.ls;
				oCQODDOCQD.renderFlag = component.bridgeObject;
				oCQODDOCQD.OOCOODCCQC = component.distHeights;
				oCQODDOCQD.newSegment = component.newSegment;
				oCQODDOCQD.tunnelFlag = component.tunnelFlag;
				oCQODDOCQD.floorDepth = component.floorDepth;
				oCQODDOCQD.waterLevel = waterLevel;
				oCQODDOCQD.lockWaterLevel = component.lockWaterLevel;
				oCQODDOCQD.sharpCorner = component.sharpCorner;
				oCQODDOCQD.OCDOCOQOCD = OQQCDCOCQO;
				component.markerNum = num;
				component.distance = "-1";
				component.OQCCOCCDOD = "-1";
				OQQCDCOCQO.ODDODQOCQO.Add(oCQODDOCQD);
				num++;
			}
		}
		distance = "-1";
		OQQCDCOCQO.ODDQDCOCQC = OOOOOCQOCO.roadWidth;
		OQQCDCOCQO.OCOOQCCQCQ(geo, obj, OOOOOCQOCO.OOQDOOQQ, renderMode, camMode, objectType);
		if (OQQCDCOCQO.leftVecs.Count > 0)
		{
			leftVecs = OQQCDCOCQO.leftVecs.ToArray();
			rightVecs = OQQCDCOCQO.rightVecs.ToArray();
		}
	}

	public void StartCam()
	{
		ODCQOCDDDC(0.5f, false, true);
	}

	public void OOCOOQCQQD()
	{
		int num = 0;
		foreach (Transform item in obj)
		{
			if (!(item.name == "Markers"))
			{
				continue;
			}
			num = 1;
			foreach (Transform item2 in item)
			{
				string text = ((num >= 10) ? ((num >= 100) ? ("Marker0" + num) : ("Marker00" + num)) : ("Marker000" + num));
				item2.name = text;
				ODOQDQOO = item2.gameObject;
				num++;
			}
		}
		markers = num - 1;
		ODCQOCDDDC(OOOOOCQOCO.geoResolution, false, false);
	}

	public List<Transform> RebuildObjs()
	{
		RoadObjectScript[] array = (RoadObjectScript[])Object.FindObjectsOfType(typeof(RoadObjectScript));
		List<Transform> list = new List<Transform>();
		RoadObjectScript[] array2 = array;
		foreach (RoadObjectScript roadObjectScript in array2)
		{
			if (roadObjectScript.transform != base.transform)
			{
				list.Add(roadObjectScript.transform);
			}
		}
		return list;
	}

	public void RestoreTerrain1()
	{
		ODCQOCDDDC(OOOOOCQOCO.geoResolution, false, false);
		if (OQQCDCOCQO != null)
		{
			OQQCDCOCQO.OCODCCCOCD();
		}
		ODODDDOO = false;
	}

	public void ODCOQQDDDQ()
	{
		OQQCDCOCQO.ODCOQQDDDQ(OOOOOCQOCO.applySplatmap, OOOOOCQOCO.splatmapSmoothLevel, OOOOOCQOCO.renderRoad, OOOOOCQOCO.tuw, OOOOOCQOCO.roadResolution, OOOOOCQOCO.raise, OOOOOCQOCO.opacity, OOOOOCQOCO.expand, OOOOOCQOCO.offsetX, OOOOOCQOCO.offsetY, OOOOOCQOCO.beveledRoad, OOOOOCQOCO.splatmapLayer, OOOOOCQOCO.OdQODQOD, OOQQQDOD, OOQQQDODOffset, OOQQQDODLength);
	}

	public void OQDDODOCDO()
	{
		OQQCDCOCQO.OQDDODOCDO(OOOOOCQOCO.renderRoad, OOOOOCQOCO.tuw, OOOOOCQOCO.roadResolution, OOOOOCQOCO.raise, OOOOOCQOCO.beveledRoad, OOOOOCQOCO.OdQODQOD, OOQQQDOD, OOQQQDODOffset, OOQQQDODLength);
	}

	public void OCQQDCOCCQ(Vector3 pos, bool doInsert)
	{
		if (!displayRoad)
		{
			displayRoad = true;
			OQQCDCOCQO.OQOOCOOCQC(displayRoad, ODOQCQDDCQ);
		}
		int first = -1;
		int second = -1;
		float dist = 10000f;
		float dist2 = 10000f;
		Vector3 tmpPos = pos;
		OCQODDOCQD k = OQQCDCOCQO.ODDODQOCQO[0];
		OCQODDOCQD k2 = OQQCDCOCQO.ODDODQOCQO[1];
		if (doInsert)
		{
			Debug.Log("Start Insert" + doInsert);
		}
		OQQCDCOCQO.ODOOQDCDQQ(pos, out first, out second, out dist, out dist2, out k, out k2, out tmpPos, doInsert);
		if (doInsert)
		{
			Debug.Log("marker 1: " + first);
			Debug.Log("marker 2: " + second);
		}
		pos = tmpPos;
		if (doInsert && first >= 0 && second >= 0)
		{
			if (OOOOOCQOCO.OOQDOOQQ && second == OQQCDCOCQO.ODDODQOCQO.Count - 1)
			{
				OCOCDDCDDO(pos);
			}
			else
			{
				OCQODDOCQD oCQODDOCQD = OQQCDCOCQO.ODDODQOCQO[second];
				string text = oCQODDOCQD.object1.name;
				int num = second + 2;
				for (int i = second; i < OQQCDCOCQO.ODDODQOCQO.Count - 1; i++)
				{
					oCQODDOCQD = OQQCDCOCQO.ODDODQOCQO[i];
					string text2 = ((num >= 10) ? ((num >= 100) ? ("Marker0" + num) : ("Marker00" + num)) : ("Marker000" + num));
					oCQODDOCQD.object1.name = text2;
					num++;
				}
				oCQODDOCQD = OQQCDCOCQO.ODDODQOCQO[first];
				Transform transform = (Transform)Object.Instantiate(oCQODDOCQD.object1.transform, pos, oCQODDOCQD.object1.rotation);
				transform.gameObject.name = text;
				transform.parent = ODOQCQDDCQ;
				MarkerScript component = transform.GetComponent<MarkerScript>();
				component.ODDCQOCODC = false;
				float num2 = dist + dist2;
				float num3 = dist / num2;
				float num4 = k.ri - k2.ri;
				component.ri = k.ri - num4 * num3;
				num4 = k.li - k2.li;
				component.li = k.li - num4 * num3;
				num4 = k.rt - k2.rt;
				component.rt = k.rt - num4 * num3;
				num4 = k.lt - k2.lt;
				component.lt = k.lt - num4 * num3;
				num4 = k.rs - k2.rs;
				component.rs = k.rs - num4 * num3;
				num4 = k.ls - k2.ls;
				component.ls = k.ls - num4 * num3;
				ODCQOCDDDC(OOOOOCQOCO.geoResolution, false, false);
				if (materialType == 0)
				{
					OQQCDCOCQO.OCDDQQCCCO(materialType);
				}
				if (objectType == 2)
				{
					component.surface.gameObject.SetActive(false);
				}
			}
		}
		OOCOOQCQQD();
	}

	public void OCQQCODCCC()
	{
		Object.DestroyImmediate(OOOOOCQOCO.ODQDOQCCCO.gameObject);
		ODQDOQCCCO = null;
		OOCOOQCQQD();
	}

	public void OQOQCQODQQ()
	{
		OQQCDCOCQO.OCCDCODDCC(12);
	}

	public List<SideObjectParams> OQOOQQDCQC()
	{
		List<SideObjectParams> list = new List<SideObjectParams>();
		foreach (Transform item in obj)
		{
			if (!(item.name == "Markers"))
			{
				continue;
			}
			foreach (Transform item2 in item)
			{
				MarkerScript component = item2.GetComponent<MarkerScript>();
				SideObjectParams sideObjectParams = new SideObjectParams();
				sideObjectParams.ODDGDOOO = component.ODDGDOOO;
				sideObjectParams.ODDQOODO = component.ODDQOODO;
				sideObjectParams.ODDQOOO = component.ODDQOOO;
				list.Add(sideObjectParams);
			}
		}
		return list;
	}

	public void OOQQCOCCCQ()
	{
		List<string> list = new List<string>();
		List<int> list2 = new List<int>();
		List<string> list3 = new List<string>();
		for (int i = 0; i < ODODOQQO.Length; i++)
		{
			if (ODODQQOD[i])
			{
				list.Add(ODODQOOQ[i]);
				list3.Add(ODODOQQO[i]);
				list2.Add(i);
			}
		}
		ODODDQOO = list.ToArray();
		OOQQQOQO = list2.ToArray();
	}

	public void OQQODDQCQD(List<ODODDQQO> arr, string[] DOODQOQO, string[] OODDQOQO)
	{
		bool flag = false;
		ODODOQQO = DOODQOQO;
		ODODQOOQ = OODDQOQO;
		List<MarkerScript> list = new List<MarkerScript>();
		if (obj == null)
		{
			OCODQDOCOC(base.transform, null, null, null);
		}
		foreach (Transform item in obj)
		{
			if (!(item.name == "Markers"))
			{
				continue;
			}
			foreach (Transform item2 in item)
			{
				MarkerScript component = item2.GetComponent<MarkerScript>();
				component.OQODQQDO.Clear();
				component.ODOQQQDO.Clear();
				component.OQQODQQOO.Clear();
				component.ODDOQQOO.Clear();
				list.Add(component);
			}
		}
		mSc = list.ToArray();
		List<bool> list2 = new List<bool>();
		int num = 0;
		int num2 = 0;
		if (ODQQQQQO != null)
		{
			if (arr.Count == 0)
			{
				return;
			}
			for (int i = 0; i < ODODOQQO.Length; i++)
			{
				ODODDQQO oDODDQQO = arr[i];
				for (int j = 0; j < ODQQQQQO.Length; j++)
				{
					if (!(ODODOQQO[i] == ODQQQQQO[j]))
					{
						continue;
					}
					num++;
					if (ODODQQOD.Length > j)
					{
						list2.Add(ODODQQOD[j]);
					}
					else
					{
						list2.Add(false);
					}
					MarkerScript[] array = mSc;
					foreach (MarkerScript markerScript in array)
					{
						int num3 = -1;
						for (int l = 0; l < markerScript.ODDOOQDO.Length; l++)
						{
							if (oDODDQQO.id == markerScript.ODDOOQDO[l])
							{
								num3 = l;
								break;
							}
						}
						if (num3 >= 0)
						{
							markerScript.OQODQQDO.Add(markerScript.ODDOOQDO[num3]);
							markerScript.ODOQQQDO.Add(markerScript.ODDGDOOO[num3]);
							markerScript.OQQODQQOO.Add(markerScript.ODDQOOO[num3]);
							if (oDODDQQO.sidewaysDistanceUpdate == 0 || (oDODDQQO.sidewaysDistanceUpdate == 2 && markerScript.ODDQOODO[num3] != oDODDQQO.oldSidwaysDistance))
							{
								markerScript.ODDOQQOO.Add(markerScript.ODDQOODO[num3]);
							}
							else
							{
								markerScript.ODDOQQOO.Add(oDODDQQO.splinePosition);
							}
						}
						else
						{
							markerScript.OQODQQDO.Add(oDODDQQO.id);
							markerScript.ODOQQQDO.Add(oDODDQQO.markerActive);
							markerScript.OQQODQQOO.Add(true);
							markerScript.ODDOQQOO.Add(oDODDQQO.splinePosition);
						}
					}
				}
				if (oDODDQQO.sidewaysDistanceUpdate != 0)
				{
				}
				flag = false;
			}
		}
		for (int m = 0; m < ODODOQQO.Length; m++)
		{
			ODODDQQO oDODDQQO2 = arr[m];
			bool flag2 = false;
			for (int n = 0; n < ODQQQQQO.Length; n++)
			{
				if (ODODOQQO[m] == ODQQQQQO[n])
				{
					flag2 = true;
				}
			}
			if (!flag2)
			{
				num2++;
				list2.Add(false);
				MarkerScript[] array2 = mSc;
				foreach (MarkerScript markerScript2 in array2)
				{
					markerScript2.OQODQQDO.Add(oDODDQQO2.id);
					markerScript2.ODOQQQDO.Add(oDODDQQO2.markerActive);
					markerScript2.OQQODQQOO.Add(true);
					markerScript2.ODDOQQOO.Add(oDODDQQO2.splinePosition);
				}
			}
		}
		ODODQQOD = list2.ToArray();
		ODQQQQQO = new string[ODODOQQO.Length];
		ODODOQQO.CopyTo(ODQQQQQO, 0);
		List<int> list3 = new List<int>();
		for (int num5 = 0; num5 < ODODQQOD.Length; num5++)
		{
			if (ODODQQOD[num5])
			{
				list3.Add(num5);
			}
		}
		OOQQQOQO = list3.ToArray();
		MarkerScript[] array3 = mSc;
		foreach (MarkerScript markerScript3 in array3)
		{
			markerScript3.ODDOOQDO = markerScript3.OQODQQDO.ToArray();
			markerScript3.ODDGDOOO = markerScript3.ODOQQQDO.ToArray();
			markerScript3.ODDQOOO = markerScript3.OQQODQQOO.ToArray();
			markerScript3.ODDQOODO = markerScript3.ODDOQQOO.ToArray();
		}
		if (!flag)
		{
		}
	}

	public void SetMultipleTerrains(bool flag)
	{
		RoadObjectScript[] array = (RoadObjectScript[])Object.FindObjectsOfType(typeof(RoadObjectScript));
		RoadObjectScript[] array2 = array;
		foreach (RoadObjectScript roadObjectScript in array2)
		{
			roadObjectScript.multipleTerrains = flag;
			if (roadObjectScript.OQQCDCOCQO != null)
			{
				roadObjectScript.OQQCDCOCQO.multipleTerrains = flag;
			}
		}
	}

	public bool CheckWaterHeights()
	{
		if (ODCCQOCQOD.terrain == null)
		{
			return false;
		}
		bool result = true;
		float y = ODCCQOCQOD.terrain.transform.position.y;
		foreach (Transform item in obj)
		{
			if (!(item.name == "Markers"))
			{
				continue;
			}
			foreach (Transform item2 in item)
			{
				if (item2.position.y - y <= 0.1f)
				{
					result = false;
				}
			}
		}
		return result;
	}
}
