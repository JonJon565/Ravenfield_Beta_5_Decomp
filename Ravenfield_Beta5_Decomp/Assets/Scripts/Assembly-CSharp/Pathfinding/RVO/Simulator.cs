using System;
using System.Collections.Generic;
using System.Threading;
using Pathfinding.RVO.Sampled;
using UnityEngine;

namespace Pathfinding.RVO
{
	public class Simulator
	{
		public enum SamplingAlgorithm
		{
			AdaptiveSampling = 0,
			GradientDecent = 1
		}

		internal class WorkerContext
		{
			public const int KeepCount = 3;

			public Agent.VO[] vos = new Agent.VO[20];

			public Vector2[] bestPos = new Vector2[3];

			public float[] bestSizes = new float[3];

			public float[] bestScores = new float[4];

			public Vector2[] samplePos = new Vector2[50];

			public float[] sampleSize = new float[50];
		}

		private class Worker
		{
			[NonSerialized]
			public Thread thread;

			public int start;

			public int end;

			public int task;

			public AutoResetEvent runFlag = new AutoResetEvent(false);

			public ManualResetEvent waitFlag = new ManualResetEvent(true);

			public Simulator simulator;

			private bool terminate;

			private WorkerContext context = new WorkerContext();

			public Worker(Simulator sim)
			{
				simulator = sim;
				thread = new Thread(Run);
				thread.IsBackground = true;
				thread.Name = "RVO Simulator Thread";
				thread.Start();
			}

			public void Execute(int task)
			{
				this.task = task;
				waitFlag.Reset();
				runFlag.Set();
			}

			public void WaitOne()
			{
				waitFlag.WaitOne();
			}

			public void Terminate()
			{
				terminate = true;
			}

			public void Run()
			{
				runFlag.WaitOne();
				while (!terminate)
				{
					try
					{
						List<Agent> agents = simulator.GetAgents();
						if (task == 0)
						{
							for (int i = start; i < end; i++)
							{
								agents[i].CalculateNeighbours();
								agents[i].CalculateVelocity(context);
							}
						}
						else if (task == 1)
						{
							for (int j = start; j < end; j++)
							{
								agents[j].Update();
								agents[j].BufferSwitch();
							}
						}
						else
						{
							if (task != 2)
							{
								Debug.LogError("Invalid Task Number: " + task);
								throw new Exception("Invalid Task Number: " + task);
							}
							simulator.BuildQuadtree();
						}
					}
					catch (Exception message)
					{
						Debug.LogError(message);
					}
					waitFlag.Set();
					runFlag.WaitOne();
				}
			}
		}

		private bool doubleBuffering = true;

		private float desiredDeltaTime = 0.05f;

		private bool interpolation = true;

		private Worker[] workers;

		private List<Agent> agents;

		public List<ObstacleVertex> obstacles;

		public SamplingAlgorithm algorithm;

		private RVOQuadtree quadtree = new RVOQuadtree();

		public float qualityCutoff = 0.05f;

		public float stepScale = 1.5f;

		private float deltaTime;

		private float prevDeltaTime;

		private float lastStep = -99999f;

		private float lastStepInterpolationReference = -9999f;

		private bool doUpdateObstacles;

		private bool doCleanObstacles;

		private bool oversampling;

		private float wallThickness = 1f;

		private WorkerContext coroutineWorkerContext = new WorkerContext();

		public RVOQuadtree Quadtree
		{
			get
			{
				return quadtree;
			}
		}

		public float DeltaTime
		{
			get
			{
				return deltaTime;
			}
		}

		public float PrevDeltaTime
		{
			get
			{
				return prevDeltaTime;
			}
		}

		public bool Multithreading
		{
			get
			{
				return workers != null && workers.Length > 0;
			}
		}

		public float DesiredDeltaTime
		{
			get
			{
				return desiredDeltaTime;
			}
			set
			{
				desiredDeltaTime = Math.Max(value, 0f);
			}
		}

		public float WallThickness
		{
			get
			{
				return wallThickness;
			}
			set
			{
				wallThickness = Math.Max(value, 0f);
			}
		}

		public bool Interpolation
		{
			get
			{
				return interpolation;
			}
			set
			{
				interpolation = value;
			}
		}

		public bool Oversampling
		{
			get
			{
				return oversampling;
			}
			set
			{
				oversampling = value;
			}
		}

		public Simulator(int workers, bool doubleBuffering)
		{
			this.workers = new Worker[workers];
			this.doubleBuffering = doubleBuffering;
			for (int i = 0; i < workers; i++)
			{
				this.workers[i] = new Worker(this);
			}
			agents = new List<Agent>();
			obstacles = new List<ObstacleVertex>();
		}

		public List<Agent> GetAgents()
		{
			return agents;
		}

		public List<ObstacleVertex> GetObstacles()
		{
			return obstacles;
		}

		public void ClearAgents()
		{
			if (Multithreading && doubleBuffering)
			{
				for (int i = 0; i < workers.Length; i++)
				{
					workers[i].WaitOne();
				}
			}
			for (int j = 0; j < agents.Count; j++)
			{
				agents[j].simulator = null;
			}
			agents.Clear();
		}

		public void OnDestroy()
		{
			if (workers != null)
			{
				for (int i = 0; i < workers.Length; i++)
				{
					workers[i].Terminate();
				}
			}
		}

		~Simulator()
		{
			OnDestroy();
		}

		public IAgent AddAgent(IAgent agent)
		{
			if (agent == null)
			{
				throw new ArgumentNullException("Agent must not be null");
			}
			Agent agent2 = agent as Agent;
			if (agent2 == null)
			{
				throw new ArgumentException("The agent must be of type Agent. Agent was of type " + agent.GetType());
			}
			if (agent2.simulator != null && agent2.simulator == this)
			{
				throw new ArgumentException("The agent is already in the simulation");
			}
			if (agent2.simulator != null)
			{
				throw new ArgumentException("The agent is already added to another simulation");
			}
			agent2.simulator = this;
			if (Multithreading && doubleBuffering)
			{
				for (int i = 0; i < workers.Length; i++)
				{
					workers[i].WaitOne();
				}
			}
			agents.Add(agent2);
			return agent;
		}

		public IAgent AddAgent(Vector3 position)
		{
			Agent agent = new Agent(position);
			if (Multithreading && doubleBuffering)
			{
				for (int i = 0; i < workers.Length; i++)
				{
					workers[i].WaitOne();
				}
			}
			agents.Add(agent);
			agent.simulator = this;
			return agent;
		}

		public void RemoveAgent(IAgent agent)
		{
			if (agent == null)
			{
				throw new ArgumentNullException("Agent must not be null");
			}
			Agent agent2 = agent as Agent;
			if (agent2 == null)
			{
				throw new ArgumentException("The agent must be of type Agent. Agent was of type " + agent.GetType());
			}
			if (agent2.simulator != this)
			{
				throw new ArgumentException("The agent is not added to this simulation");
			}
			if (Multithreading && doubleBuffering)
			{
				for (int i = 0; i < workers.Length; i++)
				{
					workers[i].WaitOne();
				}
			}
			agent2.simulator = null;
			if (!agents.Remove(agent2))
			{
				throw new ArgumentException("Critical Bug! This should not happen. Please report this.");
			}
		}

		public ObstacleVertex AddObstacle(ObstacleVertex v)
		{
			if (v == null)
			{
				throw new ArgumentNullException("Obstacle must not be null");
			}
			if (Multithreading && doubleBuffering)
			{
				for (int i = 0; i < workers.Length; i++)
				{
					workers[i].WaitOne();
				}
			}
			obstacles.Add(v);
			UpdateObstacles();
			return v;
		}

		public ObstacleVertex AddObstacle(Vector3[] vertices, float height)
		{
			return AddObstacle(vertices, height, Matrix4x4.identity);
		}

		public ObstacleVertex AddObstacle(Vector3[] vertices, float height, Matrix4x4 matrix, RVOLayer layer = RVOLayer.DefaultObstacle)
		{
			if (vertices == null)
			{
				throw new ArgumentNullException("Vertices must not be null");
			}
			if (vertices.Length < 2)
			{
				throw new ArgumentException("Less than 2 vertices in an obstacle");
			}
			ObstacleVertex obstacleVertex = null;
			ObstacleVertex obstacleVertex2 = null;
			bool flag = matrix == Matrix4x4.identity;
			if (Multithreading && doubleBuffering)
			{
				for (int i = 0; i < workers.Length; i++)
				{
					workers[i].WaitOne();
				}
			}
			for (int j = 0; j < vertices.Length; j++)
			{
				ObstacleVertex obstacleVertex3 = new ObstacleVertex();
				if (obstacleVertex == null)
				{
					obstacleVertex = obstacleVertex3;
				}
				else
				{
					obstacleVertex2.next = obstacleVertex3;
				}
				obstacleVertex3.prev = obstacleVertex2;
				obstacleVertex3.layer = layer;
				obstacleVertex3.position = ((!flag) ? matrix.MultiplyPoint3x4(vertices[j]) : vertices[j]);
				obstacleVertex3.height = height;
				obstacleVertex2 = obstacleVertex3;
			}
			obstacleVertex2.next = obstacleVertex;
			obstacleVertex.prev = obstacleVertex2;
			ObstacleVertex obstacleVertex4 = obstacleVertex;
			do
			{
				Vector3 vector = obstacleVertex4.next.position - obstacleVertex4.position;
				obstacleVertex4.dir = new Vector2(vector.x, vector.z).normalized;
				obstacleVertex4 = obstacleVertex4.next;
			}
			while (obstacleVertex4 != obstacleVertex);
			obstacles.Add(obstacleVertex);
			UpdateObstacles();
			return obstacleVertex;
		}

		public ObstacleVertex AddObstacle(Vector3 a, Vector3 b, float height)
		{
			ObstacleVertex obstacleVertex = new ObstacleVertex();
			ObstacleVertex obstacleVertex2 = new ObstacleVertex();
			obstacleVertex.layer = RVOLayer.DefaultObstacle;
			obstacleVertex2.layer = RVOLayer.DefaultObstacle;
			obstacleVertex.prev = obstacleVertex2;
			obstacleVertex2.prev = obstacleVertex;
			obstacleVertex.next = obstacleVertex2;
			obstacleVertex2.next = obstacleVertex;
			obstacleVertex.position = a;
			obstacleVertex2.position = b;
			obstacleVertex.height = height;
			obstacleVertex2.height = height;
			obstacleVertex2.ignore = true;
			obstacleVertex.dir = new Vector2(b.x - a.x, b.z - a.z).normalized;
			obstacleVertex2.dir = -obstacleVertex.dir;
			if (Multithreading && doubleBuffering)
			{
				for (int i = 0; i < workers.Length; i++)
				{
					workers[i].WaitOne();
				}
			}
			obstacles.Add(obstacleVertex);
			UpdateObstacles();
			return obstacleVertex;
		}

		public void UpdateObstacle(ObstacleVertex obstacle, Vector3[] vertices, Matrix4x4 matrix)
		{
			if (vertices == null)
			{
				throw new ArgumentNullException("Vertices must not be null");
			}
			if (obstacle == null)
			{
				throw new ArgumentNullException("Obstacle must not be null");
			}
			if (vertices.Length < 2)
			{
				throw new ArgumentException("Less than 2 vertices in an obstacle");
			}
			if (obstacle.split)
			{
				throw new ArgumentException("Obstacle is not a start vertex. You should only pass those ObstacleVertices got from AddObstacle method calls");
			}
			if (Multithreading && doubleBuffering)
			{
				for (int i = 0; i < workers.Length; i++)
				{
					workers[i].WaitOne();
				}
			}
			int num = 0;
			ObstacleVertex obstacleVertex = obstacle;
			while (true)
			{
				if (obstacleVertex.next.split)
				{
					obstacleVertex.next = obstacleVertex.next.next;
					obstacleVertex.next.prev = obstacleVertex;
					continue;
				}
				if (num >= vertices.Length)
				{
					Debug.DrawLine(obstacleVertex.prev.position, obstacleVertex.position, Color.red);
					throw new ArgumentException("Obstacle has more vertices than supplied for updating (" + vertices.Length + " supplied)");
				}
				obstacleVertex.position = matrix.MultiplyPoint3x4(vertices[num]);
				num++;
				obstacleVertex = obstacleVertex.next;
				if (obstacleVertex == obstacle)
				{
					break;
				}
			}
			obstacleVertex = obstacle;
			do
			{
				Vector3 vector = obstacleVertex.next.position - obstacleVertex.position;
				obstacleVertex.dir = new Vector2(vector.x, vector.z).normalized;
				obstacleVertex = obstacleVertex.next;
			}
			while (obstacleVertex != obstacle);
			ScheduleCleanObstacles();
			UpdateObstacles();
		}

		private void ScheduleCleanObstacles()
		{
			doCleanObstacles = true;
		}

		private void CleanObstacles()
		{
			for (int i = 0; i < obstacles.Count; i++)
			{
				ObstacleVertex obstacleVertex = obstacles[i];
				ObstacleVertex obstacleVertex2 = obstacleVertex;
				while (true)
				{
					if (obstacleVertex2.next.split)
					{
						obstacleVertex2.next = obstacleVertex2.next.next;
						obstacleVertex2.next.prev = obstacleVertex2;
						continue;
					}
					obstacleVertex2 = obstacleVertex2.next;
					if (obstacleVertex2 == obstacleVertex)
					{
						break;
					}
				}
			}
		}

		public void RemoveObstacle(ObstacleVertex v)
		{
			if (v == null)
			{
				throw new ArgumentNullException("Vertex must not be null");
			}
			if (Multithreading && doubleBuffering)
			{
				for (int i = 0; i < workers.Length; i++)
				{
					workers[i].WaitOne();
				}
			}
			obstacles.Remove(v);
			UpdateObstacles();
		}

		public void UpdateObstacles()
		{
			doUpdateObstacles = true;
		}

		private void BuildQuadtree()
		{
			quadtree.Clear();
			if (agents.Count > 0)
			{
				Rect bounds = Rect.MinMaxRect(agents[0].position.x, agents[0].position.y, agents[0].position.x, agents[0].position.y);
				for (int i = 1; i < agents.Count; i++)
				{
					Vector3 position = agents[i].position;
					bounds = Rect.MinMaxRect(Mathf.Min(bounds.xMin, position.x), Mathf.Min(bounds.yMin, position.z), Mathf.Max(bounds.xMax, position.x), Mathf.Max(bounds.yMax, position.z));
				}
				quadtree.SetBounds(bounds);
				for (int j = 0; j < agents.Count; j++)
				{
					quadtree.Insert(agents[j]);
				}
			}
		}

		public void Update()
		{
			if (lastStep < 0f)
			{
				lastStep = Time.time;
				deltaTime = DesiredDeltaTime;
				prevDeltaTime = deltaTime;
				lastStepInterpolationReference = lastStep;
			}
			if (Time.time - lastStep >= DesiredDeltaTime)
			{
				for (int i = 0; i < agents.Count; i++)
				{
					agents[i].Interpolate((Time.time - lastStepInterpolationReference) / DeltaTime);
				}
				lastStepInterpolationReference = Time.time;
				prevDeltaTime = DeltaTime;
				deltaTime = Time.time - lastStep;
				lastStep = Time.time;
				deltaTime = Math.Max(deltaTime, 0.0005f);
				if (Multithreading)
				{
					if (doubleBuffering)
					{
						for (int j = 0; j < workers.Length; j++)
						{
							workers[j].WaitOne();
						}
						if (!Interpolation)
						{
							for (int k = 0; k < agents.Count; k++)
							{
								agents[k].Interpolate(1f);
							}
						}
					}
					if (doCleanObstacles)
					{
						CleanObstacles();
						doCleanObstacles = false;
						doUpdateObstacles = true;
					}
					if (doUpdateObstacles)
					{
						doUpdateObstacles = false;
					}
					BuildQuadtree();
					for (int l = 0; l < workers.Length; l++)
					{
						workers[l].start = l * agents.Count / workers.Length;
						workers[l].end = (l + 1) * agents.Count / workers.Length;
					}
					for (int m = 0; m < workers.Length; m++)
					{
						workers[m].Execute(1);
					}
					for (int n = 0; n < workers.Length; n++)
					{
						workers[n].WaitOne();
					}
					for (int num = 0; num < workers.Length; num++)
					{
						workers[num].Execute(0);
					}
					if (!doubleBuffering)
					{
						for (int num2 = 0; num2 < workers.Length; num2++)
						{
							workers[num2].WaitOne();
						}
						if (!Interpolation)
						{
							for (int num3 = 0; num3 < agents.Count; num3++)
							{
								agents[num3].Interpolate(1f);
							}
						}
					}
				}
				else
				{
					if (doCleanObstacles)
					{
						CleanObstacles();
						doCleanObstacles = false;
						doUpdateObstacles = true;
					}
					if (doUpdateObstacles)
					{
						doUpdateObstacles = false;
					}
					BuildQuadtree();
					for (int num4 = 0; num4 < agents.Count; num4++)
					{
						agents[num4].Update();
						agents[num4].BufferSwitch();
					}
					for (int num5 = 0; num5 < agents.Count; num5++)
					{
						agents[num5].CalculateNeighbours();
						agents[num5].CalculateVelocity(coroutineWorkerContext);
					}
					if (oversampling)
					{
						for (int num6 = 0; num6 < agents.Count; num6++)
						{
							agents[num6].Velocity = agents[num6].newVelocity;
						}
						for (int num7 = 0; num7 < agents.Count; num7++)
						{
							Vector3 newVelocity = agents[num7].newVelocity;
							agents[num7].CalculateVelocity(coroutineWorkerContext);
							agents[num7].newVelocity = (newVelocity + agents[num7].newVelocity) * 0.5f;
						}
					}
					if (!Interpolation)
					{
						for (int num8 = 0; num8 < agents.Count; num8++)
						{
							agents[num8].Interpolate(1f);
						}
					}
				}
			}
			if (Interpolation)
			{
				for (int num9 = 0; num9 < agents.Count; num9++)
				{
					agents[num9].Interpolate((Time.time - lastStepInterpolationReference) / DeltaTime);
				}
			}
		}
	}
}
