using System;
using System.Collections.Generic;
using Pathfinding.RVO;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[HelpURL("http://arongranberg.com/astar/docs/class_lightweight_r_v_o.php")]
public class LightweightRVO : MonoBehaviour
{
	public enum RVOExampleType
	{
		Circle = 0,
		Line = 1,
		Point = 2,
		RandomStreams = 3
	}

	public int agentCount = 100;

	public float exampleScale = 100f;

	public RVOExampleType type;

	public float radius = 3f;

	public float maxSpeed = 2f;

	public float agentTimeHorizon = 10f;

	[HideInInspector]
	public float obstacleTimeHorizon = 10f;

	public int maxNeighbours = 10;

	public float neighbourDist = 15f;

	public Vector3 renderingOffset = Vector3.up * 0.1f;

	public bool debug;

	private Mesh mesh;

	private Simulator sim;

	private List<IAgent> agents;

	private List<Vector3> goals;

	private List<Color> colors;

	private Vector3[] verts;

	private Vector2[] uv;

	private int[] tris;

	private Color[] meshColors;

	private Vector3[] interpolatedVelocities;

	public void Start()
	{
		mesh = new Mesh();
		RVOSimulator rVOSimulator = UnityEngine.Object.FindObjectOfType(typeof(RVOSimulator)) as RVOSimulator;
		if (rVOSimulator == null)
		{
			Debug.LogError("No RVOSimulator could be found in the scene. Please add a RVOSimulator component to any GameObject");
			return;
		}
		sim = rVOSimulator.GetSimulator();
		GetComponent<MeshFilter>().mesh = mesh;
		CreateAgents(agentCount);
	}

	public void OnGUI()
	{
		if (GUILayout.Button("2"))
		{
			CreateAgents(2);
		}
		if (GUILayout.Button("10"))
		{
			CreateAgents(10);
		}
		if (GUILayout.Button("100"))
		{
			CreateAgents(100);
		}
		if (GUILayout.Button("500"))
		{
			CreateAgents(500);
		}
		if (GUILayout.Button("1000"))
		{
			CreateAgents(1000);
		}
		if (GUILayout.Button("5000"))
		{
			CreateAgents(5000);
		}
		GUILayout.Space(5f);
		if (GUILayout.Button("Random Streams"))
		{
			type = RVOExampleType.RandomStreams;
			CreateAgents((agents == null) ? 100 : agents.Count);
		}
		if (GUILayout.Button("Line"))
		{
			type = RVOExampleType.Line;
			CreateAgents((agents == null) ? 10 : Mathf.Min(agents.Count, 100));
		}
		if (GUILayout.Button("Circle"))
		{
			type = RVOExampleType.Circle;
			CreateAgents((agents == null) ? 100 : agents.Count);
		}
		if (GUILayout.Button("Point"))
		{
			type = RVOExampleType.Point;
			CreateAgents((agents == null) ? 100 : agents.Count);
		}
	}

	private float uniformDistance(float radius)
	{
		float num = UnityEngine.Random.value + UnityEngine.Random.value;
		if (num > 1f)
		{
			return radius * (2f - num);
		}
		return radius * num;
	}

	public void CreateAgents(int num)
	{
		agentCount = num;
		agents = new List<IAgent>(agentCount);
		goals = new List<Vector3>(agentCount);
		colors = new List<Color>(agentCount);
		sim.ClearAgents();
		if (type == RVOExampleType.Circle)
		{
			float num2 = Mathf.Sqrt((float)agentCount * radius * radius * 4f / (float)Math.PI) * exampleScale * 0.05f;
			for (int i = 0; i < agentCount; i++)
			{
				Vector3 vector = new Vector3(Mathf.Cos((float)i * (float)Math.PI * 2f / (float)agentCount), 0f, Mathf.Sin((float)i * (float)Math.PI * 2f / (float)agentCount)) * num2;
				IAgent item = sim.AddAgent(vector);
				agents.Add(item);
				goals.Add(-vector);
				colors.Add(HSVToRGB((float)i * 360f / (float)agentCount, 0.8f, 0.6f));
			}
		}
		else if (type == RVOExampleType.Line)
		{
			for (int j = 0; j < agentCount; j++)
			{
				Vector3 position = new Vector3((float)((j % 2 == 0) ? 1 : (-1)) * exampleScale, 0f, (float)(j / 2) * radius * 2.5f);
				IAgent item2 = sim.AddAgent(position);
				agents.Add(item2);
				goals.Add(new Vector3(0f - position.x, position.y, position.z));
				colors.Add((j % 2 != 0) ? Color.blue : Color.red);
			}
		}
		else if (type == RVOExampleType.Point)
		{
			for (int k = 0; k < agentCount; k++)
			{
				Vector3 position2 = new Vector3(Mathf.Cos((float)k * (float)Math.PI * 2f / (float)agentCount), 0f, Mathf.Sin((float)k * (float)Math.PI * 2f / (float)agentCount)) * exampleScale;
				IAgent item3 = sim.AddAgent(position2);
				agents.Add(item3);
				goals.Add(new Vector3(0f, position2.y, 0f));
				colors.Add(HSVToRGB((float)k * 360f / (float)agentCount, 0.8f, 0.6f));
			}
		}
		else if (type == RVOExampleType.RandomStreams)
		{
			float num3 = Mathf.Sqrt((float)agentCount * radius * radius * 4f / (float)Math.PI) * exampleScale * 0.05f;
			for (int l = 0; l < agentCount; l++)
			{
				float f = UnityEngine.Random.value * (float)Math.PI * 2f;
				float num4 = UnityEngine.Random.value * (float)Math.PI * 2f;
				Vector3 position3 = new Vector3(Mathf.Cos(f), 0f, Mathf.Sin(f)) * uniformDistance(num3);
				IAgent item4 = sim.AddAgent(position3);
				agents.Add(item4);
				goals.Add(new Vector3(Mathf.Cos(num4), 0f, Mathf.Sin(num4)) * uniformDistance(num3));
				colors.Add(HSVToRGB(num4 * 57.29578f, 0.8f, 0.6f));
			}
		}
		for (int m = 0; m < agents.Count; m++)
		{
			IAgent agent = agents[m];
			agent.Radius = radius;
			agent.MaxSpeed = maxSpeed;
			agent.AgentTimeHorizon = agentTimeHorizon;
			agent.ObstacleTimeHorizon = obstacleTimeHorizon;
			agent.MaxNeighbours = maxNeighbours;
			agent.NeighbourDist = neighbourDist;
			agent.DebugDraw = m == 0 && debug;
		}
		verts = new Vector3[4 * agents.Count];
		uv = new Vector2[verts.Length];
		tris = new int[agents.Count * 2 * 3];
		meshColors = new Color[verts.Length];
	}

	public void Update()
	{
		if (agents == null || mesh == null)
		{
			return;
		}
		if (agents.Count != goals.Count)
		{
			Debug.LogError("Agent count does not match goal count");
			return;
		}
		for (int i = 0; i < agents.Count; i++)
		{
			Vector3 interpolatedPosition = agents[i].InterpolatedPosition;
			Vector3 vector = goals[i] - interpolatedPosition;
			vector = Vector3.ClampMagnitude(vector, 1f);
			agents[i].DesiredVelocity = vector * agents[i].MaxSpeed;
		}
		if (interpolatedVelocities == null || interpolatedVelocities.Length < agents.Count)
		{
			Vector3[] array = new Vector3[agents.Count];
			if (interpolatedVelocities != null)
			{
				for (int j = 0; j < interpolatedVelocities.Length; j++)
				{
					array[j] = interpolatedVelocities[j];
				}
			}
			interpolatedVelocities = array;
		}
		for (int k = 0; k < agents.Count; k++)
		{
			IAgent agent = agents[k];
			interpolatedVelocities[k] = Vector3.Lerp(interpolatedVelocities[k], agent.Velocity, agent.Velocity.magnitude * Time.deltaTime * 4f);
			Vector3 vector2 = interpolatedVelocities[k].normalized * agent.Radius;
			if (vector2 == Vector3.zero)
			{
				vector2 = new Vector3(0f, 0f, agent.Radius);
			}
			Vector3 vector3 = Vector3.Cross(Vector3.up, vector2);
			Vector3 vector4 = agent.InterpolatedPosition + renderingOffset;
			int num = 4 * k;
			int num2 = 6 * k;
			verts[num] = vector4 + vector2 - vector3;
			verts[num + 1] = vector4 + vector2 + vector3;
			verts[num + 2] = vector4 - vector2 + vector3;
			verts[num + 3] = vector4 - vector2 - vector3;
			uv[num] = new Vector2(0f, 1f);
			uv[num + 1] = new Vector2(1f, 1f);
			uv[num + 2] = new Vector2(1f, 0f);
			uv[num + 3] = new Vector2(0f, 0f);
			meshColors[num] = colors[k];
			meshColors[num + 1] = colors[k];
			meshColors[num + 2] = colors[k];
			meshColors[num + 3] = colors[k];
			tris[num2] = num;
			tris[num2 + 1] = num + 1;
			tris[num2 + 2] = num + 2;
			tris[num2 + 3] = num;
			tris[num2 + 4] = num + 2;
			tris[num2 + 5] = num + 3;
		}
		mesh.Clear();
		mesh.vertices = verts;
		mesh.uv = uv;
		mesh.colors = meshColors;
		mesh.triangles = tris;
		mesh.RecalculateNormals();
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
