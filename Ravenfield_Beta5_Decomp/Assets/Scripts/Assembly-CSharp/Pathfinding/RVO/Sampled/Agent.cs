using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Pathfinding.RVO.Sampled
{
	public class Agent : IAgent
	{
		public struct VO
		{
			public Vector2 origin;

			public Vector2 center;

			private Vector2 line1;

			private Vector2 line2;

			private Vector2 dir1;

			private Vector2 dir2;

			private Vector2 cutoffLine;

			private Vector2 cutoffDir;

			private float sqrCutoffDistance;

			private bool leftSide;

			private bool colliding;

			private float radius;

			private float weightFactor;

			public VO(Vector2 offset, Vector2 p0, Vector2 dir, float weightFactor)
			{
				colliding = true;
				line1 = p0;
				dir1 = -dir;
				origin = Vector2.zero;
				center = Vector2.zero;
				line2 = Vector2.zero;
				dir2 = Vector2.zero;
				cutoffLine = Vector2.zero;
				cutoffDir = Vector2.zero;
				sqrCutoffDistance = 0f;
				leftSide = false;
				radius = 0f;
				this.weightFactor = weightFactor * 0.5f;
			}

			public VO(Vector2 offset, Vector2 p1, Vector2 p2, Vector2 tang1, Vector2 tang2, float weightFactor)
			{
				this.weightFactor = weightFactor * 0.5f;
				colliding = false;
				cutoffLine = p1;
				cutoffDir = (p2 - p1).normalized;
				line1 = p1;
				dir1 = tang1;
				line2 = p2;
				dir2 = tang2;
				dir2 = -dir2;
				cutoffDir = -cutoffDir;
				origin = Vector2.zero;
				center = Vector2.zero;
				sqrCutoffDistance = 0f;
				leftSide = false;
				radius = 0f;
				weightFactor = 5f;
			}

			public VO(Vector2 center, Vector2 offset, float radius, Vector2 sideChooser, float inverseDt, float weightFactor)
			{
				this.weightFactor = weightFactor * 0.5f;
				origin = offset;
				weightFactor = 0.5f;
				if (center.magnitude < radius)
				{
					colliding = true;
					leftSide = false;
					line1 = center.normalized * (center.magnitude - radius);
					dir1 = new Vector2(line1.y, 0f - line1.x).normalized;
					line1 += offset;
					cutoffDir = Vector2.zero;
					cutoffLine = Vector2.zero;
					sqrCutoffDistance = 0f;
					dir2 = Vector2.zero;
					line2 = Vector2.zero;
					this.center = Vector2.zero;
					this.radius = 0f;
				}
				else
				{
					colliding = false;
					center *= inverseDt;
					radius *= inverseDt;
					Vector2 vector = center + offset;
					sqrCutoffDistance = center.magnitude - radius;
					this.center = center;
					cutoffLine = center.normalized * sqrCutoffDistance;
					cutoffDir = new Vector2(0f - cutoffLine.y, cutoffLine.x).normalized;
					cutoffLine += offset;
					sqrCutoffDistance *= sqrCutoffDistance;
					float num = Mathf.Atan2(0f - center.y, 0f - center.x);
					float num2 = Mathf.Abs(Mathf.Acos(radius / center.magnitude));
					this.radius = radius;
					leftSide = VectorMath.RightOrColinear(Vector2.zero, center, sideChooser);
					line1 = new Vector2(Mathf.Cos(num + num2), Mathf.Sin(num + num2)) * radius;
					dir1 = new Vector2(line1.y, 0f - line1.x).normalized;
					line2 = new Vector2(Mathf.Cos(num - num2), Mathf.Sin(num - num2)) * radius;
					dir2 = new Vector2(line2.y, 0f - line2.x).normalized;
					line1 += vector;
					line2 += vector;
				}
			}

			public static bool Left(Vector2 a, Vector2 dir, Vector2 p)
			{
				return dir.x * (p.y - a.y) - (p.x - a.x) * dir.y <= 0f;
			}

			public static float Det(Vector2 a, Vector2 dir, Vector2 p)
			{
				return (p.x - a.x) * dir.y - dir.x * (p.y - a.y);
			}

			public Vector2 Sample(Vector2 p, out float weight)
			{
				if (colliding)
				{
					float num = Det(line1, dir1, p);
					if (num >= 0f)
					{
						weight = num * weightFactor;
						return new Vector2(0f - dir1.y, dir1.x) * weight * GlobalIncompressibility;
					}
					weight = 0f;
					return new Vector2(0f, 0f);
				}
				float num2 = Det(cutoffLine, cutoffDir, p);
				if (num2 <= 0f)
				{
					weight = 0f;
					return Vector2.zero;
				}
				float num3 = Det(line1, dir1, p);
				float num4 = Det(line2, dir2, p);
				if (num3 >= 0f && num4 >= 0f)
				{
					if (leftSide)
					{
						if (num2 < radius)
						{
							weight = num2 * weightFactor;
							return new Vector2(0f - cutoffDir.y, cutoffDir.x) * weight;
						}
						weight = num3;
						return new Vector2(0f - dir1.y, dir1.x) * weight;
					}
					if (num2 < radius)
					{
						weight = num2 * weightFactor;
						return new Vector2(0f - cutoffDir.y, cutoffDir.x) * weight;
					}
					weight = num4 * weightFactor;
					return new Vector2(0f - dir2.y, dir2.x) * weight;
				}
				weight = 0f;
				return new Vector2(0f, 0f);
			}

			public float ScalarSample(Vector2 p)
			{
				if (colliding)
				{
					float num = Det(line1, dir1, p);
					if (num >= 0f)
					{
						return num * GlobalIncompressibility * weightFactor;
					}
					return 0f;
				}
				float num2 = Det(cutoffLine, cutoffDir, p);
				if (num2 <= 0f)
				{
					return 0f;
				}
				float num3 = Det(line1, dir1, p);
				float num4 = Det(line2, dir2, p);
				if (num3 >= 0f && num4 >= 0f)
				{
					if (leftSide)
					{
						if (num2 < radius)
						{
							return num2 * weightFactor;
						}
						return num3 * weightFactor;
					}
					if (num2 < radius)
					{
						return num2 * weightFactor;
					}
					return num4 * weightFactor;
				}
				return 0f;
			}
		}

		private const float WallWeight = 5f;

		private Vector3 smoothPos;

		public float radius;

		public float height;

		public float maxSpeed;

		public float neighbourDist;

		public float agentTimeHorizon;

		public float obstacleTimeHorizon;

		public float weight;

		public bool locked;

		private RVOLayer layer;

		private RVOLayer collidesWith;

		public int maxNeighbours;

		public Vector3 position;

		public Vector3 desiredVelocity;

		public Vector3 prevSmoothPos;

		internal Agent next;

		private Vector3 velocity;

		internal Vector3 newVelocity;

		public Simulator simulator;

		public List<Agent> neighbours = new List<Agent>();

		public List<float> neighbourDists = new List<float>();

		private List<ObstacleVertex> obstaclesBuffered = new List<ObstacleVertex>();

		private List<ObstacleVertex> obstacles = new List<ObstacleVertex>();

		private List<float> obstacleDists = new List<float>();

		public static Stopwatch watch1 = new Stopwatch();

		public static Stopwatch watch2 = new Stopwatch();

		public static float DesiredVelocityWeight = 0.02f;

		public static float DesiredVelocityScale = 0.1f;

		public static float GlobalIncompressibility = 30f;

		public Vector3 Position { get; private set; }

		public Vector3 InterpolatedPosition
		{
			get
			{
				return smoothPos;
			}
		}

		public Vector3 DesiredVelocity { get; set; }

		public RVOLayer Layer { get; set; }

		public RVOLayer CollidesWith { get; set; }

		public bool Locked { get; set; }

		public float Radius { get; set; }

		public float Height { get; set; }

		public float MaxSpeed { get; set; }

		public float NeighbourDist { get; set; }

		public float AgentTimeHorizon { get; set; }

		public float ObstacleTimeHorizon { get; set; }

		public Vector3 Velocity { get; set; }

		public bool DebugDraw { get; set; }

		public int MaxNeighbours { get; set; }

		public List<ObstacleVertex> NeighbourObstacles
		{
			get
			{
				return null;
			}
		}

		public Agent(Vector3 pos)
		{
			MaxSpeed = 2f;
			NeighbourDist = 15f;
			AgentTimeHorizon = 2f;
			ObstacleTimeHorizon = 2f;
			Height = 5f;
			Radius = 5f;
			MaxNeighbours = 10;
			Locked = false;
			position = pos;
			Position = position;
			prevSmoothPos = position;
			smoothPos = position;
			Layer = RVOLayer.DefaultAgent;
			CollidesWith = (RVOLayer)(-1);
		}

		public void Teleport(Vector3 pos)
		{
			Position = pos;
			smoothPos = pos;
			prevSmoothPos = pos;
		}

		public void SetYPosition(float yCoordinate)
		{
			Position = new Vector3(Position.x, yCoordinate, Position.z);
			smoothPos.y = yCoordinate;
			prevSmoothPos.y = yCoordinate;
		}

		public void BufferSwitch()
		{
			radius = Radius;
			height = Height;
			maxSpeed = MaxSpeed;
			neighbourDist = NeighbourDist;
			agentTimeHorizon = AgentTimeHorizon;
			obstacleTimeHorizon = ObstacleTimeHorizon;
			maxNeighbours = MaxNeighbours;
			desiredVelocity = DesiredVelocity;
			locked = Locked;
			collidesWith = CollidesWith;
			layer = Layer;
			Velocity = velocity;
			List<ObstacleVertex> list = obstaclesBuffered;
			obstaclesBuffered = obstacles;
			obstacles = list;
		}

		public void Update()
		{
			velocity = newVelocity;
			prevSmoothPos = smoothPos;
			position = prevSmoothPos;
			position += velocity * simulator.DeltaTime;
			Position = position;
		}

		public void Interpolate(float t)
		{
			smoothPos = prevSmoothPos + (Position - prevSmoothPos) * t;
		}

		public void CalculateNeighbours()
		{
			neighbours.Clear();
			neighbourDists.Clear();
			if (!locked)
			{
				float num;
				if (MaxNeighbours > 0)
				{
					num = neighbourDist * neighbourDist;
					simulator.Quadtree.Query(new Vector2(position.x, position.z), neighbourDist, this);
				}
				obstacles.Clear();
				obstacleDists.Clear();
				num = obstacleTimeHorizon * maxSpeed + radius;
				num *= num;
			}
		}

		private float Sqr(float x)
		{
			return x * x;
		}

		public float InsertAgentNeighbour(Agent agent, float rangeSq)
		{
			if (this == agent)
			{
				return rangeSq;
			}
			if ((agent.layer & collidesWith) == 0)
			{
				return rangeSq;
			}
			float num = Sqr(agent.position.x - position.x) + Sqr(agent.position.z - position.z);
			if (num < rangeSq)
			{
				if (neighbours.Count < maxNeighbours)
				{
					neighbours.Add(agent);
					neighbourDists.Add(num);
				}
				int num2 = neighbours.Count - 1;
				if (num < neighbourDists[num2])
				{
					while (num2 != 0 && num < neighbourDists[num2 - 1])
					{
						neighbours[num2] = neighbours[num2 - 1];
						neighbourDists[num2] = neighbourDists[num2 - 1];
						num2--;
					}
					neighbours[num2] = agent;
					neighbourDists[num2] = num;
				}
				if (neighbours.Count == maxNeighbours)
				{
					rangeSq = neighbourDists[neighbourDists.Count - 1];
				}
			}
			return rangeSq;
		}

		public void InsertObstacleNeighbour(ObstacleVertex ob1, float rangeSq)
		{
			ObstacleVertex obstacleVertex = ob1.next;
			float num = VectorMath.SqrDistancePointSegment(ob1.position, obstacleVertex.position, Position);
			if (num < rangeSq)
			{
				obstacles.Add(ob1);
				obstacleDists.Add(num);
				int num2 = obstacles.Count - 1;
				while (num2 != 0 && num < obstacleDists[num2 - 1])
				{
					obstacles[num2] = obstacles[num2 - 1];
					obstacleDists[num2] = obstacleDists[num2 - 1];
					num2--;
				}
				obstacles[num2] = ob1;
				obstacleDists[num2] = num;
			}
		}

		private static Vector3 To3D(Vector2 p)
		{
			return new Vector3(p.x, 0f, p.y);
		}

		private static void DrawCircle(Vector2 _p, float radius, Color col)
		{
			DrawCircle(_p, radius, 0f, (float)Math.PI * 2f, col);
		}

		private static void DrawCircle(Vector2 _p, float radius, float a0, float a1, Color col)
		{
			Vector3 vector = To3D(_p);
			while (a0 > a1)
			{
				a0 -= (float)Math.PI * 2f;
			}
			Vector3 vector2 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
			for (int i = 0; (float)i <= 40f; i++)
			{
				Vector3 vector3 = new Vector3(Mathf.Cos(Mathf.Lerp(a0, a1, (float)i / 40f)) * radius, 0f, Mathf.Sin(Mathf.Lerp(a0, a1, (float)i / 40f)) * radius);
				UnityEngine.Debug.DrawLine(vector + vector2, vector + vector3, col);
				vector2 = vector3;
			}
		}

		private static void DrawVO(Vector2 circleCenter, float radius, Vector2 origin)
		{
			float num = Mathf.Atan2((origin - circleCenter).y, (origin - circleCenter).x);
			float num2 = radius / (origin - circleCenter).magnitude;
			float num3 = ((!(num2 <= 1f)) ? 0f : Mathf.Abs(Mathf.Acos(num2)));
			DrawCircle(circleCenter, radius, num - num3, num + num3, Color.black);
			Vector2 p = new Vector2(Mathf.Cos(num - num3), Mathf.Sin(num - num3)) * radius;
			Vector2 p2 = new Vector2(Mathf.Cos(num + num3), Mathf.Sin(num + num3)) * radius;
			Vector2 p3 = -new Vector2(0f - p.y, p.x);
			Vector2 p4 = new Vector2(0f - p2.y, p2.x);
			p += circleCenter;
			p2 += circleCenter;
			UnityEngine.Debug.DrawRay(To3D(p), To3D(p3).normalized * 100f, Color.black);
			UnityEngine.Debug.DrawRay(To3D(p2), To3D(p4).normalized * 100f, Color.black);
		}

		private static void DrawCross(Vector2 p, float size = 1f)
		{
			DrawCross(p, Color.white, size);
		}

		private static void DrawCross(Vector2 p, Color col, float size = 1f)
		{
			size *= 0.5f;
			UnityEngine.Debug.DrawLine(new Vector3(p.x, 0f, p.y) - Vector3.right * size, new Vector3(p.x, 0f, p.y) + Vector3.right * size, col);
			UnityEngine.Debug.DrawLine(new Vector3(p.x, 0f, p.y) - Vector3.forward * size, new Vector3(p.x, 0f, p.y) + Vector3.forward * size, col);
		}

		internal void CalculateVelocity(Simulator.WorkerContext context)
		{
			if (locked)
			{
				newVelocity = Vector2.zero;
				return;
			}
			if (context.vos.Length < neighbours.Count + simulator.obstacles.Count)
			{
				context.vos = new VO[Mathf.Max(context.vos.Length * 2, neighbours.Count + simulator.obstacles.Count)];
			}
			Vector2 vector = new Vector2(position.x, position.z);
			VO[] vos = context.vos;
			int num = 0;
			Vector2 vector2 = new Vector2(velocity.x, velocity.z);
			float num2 = 1f / agentTimeHorizon;
			float wallThickness = simulator.WallThickness;
			float num3 = ((simulator.algorithm != Simulator.SamplingAlgorithm.GradientDecent) ? 5f : 1f);
			for (int i = 0; i < simulator.obstacles.Count; i++)
			{
				ObstacleVertex obstacleVertex = simulator.obstacles[i];
				ObstacleVertex obstacleVertex2 = obstacleVertex;
				do
				{
					if (obstacleVertex2.ignore || position.y > obstacleVertex2.position.y + obstacleVertex2.height || position.y + height < obstacleVertex2.position.y || (obstacleVertex2.layer & collidesWith) == 0)
					{
						obstacleVertex2 = obstacleVertex2.next;
						continue;
					}
					float num4 = VO.Det(new Vector2(obstacleVertex2.position.x, obstacleVertex2.position.z), obstacleVertex2.dir, vector);
					float num5 = num4;
					float num6 = Vector2.Dot(obstacleVertex2.dir, vector - new Vector2(obstacleVertex2.position.x, obstacleVertex2.position.z));
					bool flag = num6 <= wallThickness * 0.05f || num6 >= (new Vector2(obstacleVertex2.position.x, obstacleVertex2.position.z) - new Vector2(obstacleVertex2.next.position.x, obstacleVertex2.next.position.z)).magnitude - wallThickness * 0.05f;
					if (Mathf.Abs(num5) < neighbourDist)
					{
						if (num5 <= 0f && !flag && num5 > 0f - wallThickness)
						{
							vos[num] = new VO(vector, new Vector2(obstacleVertex2.position.x, obstacleVertex2.position.z) - vector, obstacleVertex2.dir, num3 * 2f);
							num++;
						}
						else if (num5 > 0f)
						{
							Vector2 p = new Vector2(obstacleVertex2.position.x, obstacleVertex2.position.z) - vector;
							Vector2 p2 = new Vector2(obstacleVertex2.next.position.x, obstacleVertex2.next.position.z) - vector;
							Vector2 normalized = p.normalized;
							Vector2 normalized2 = p2.normalized;
							vos[num] = new VO(vector, p, p2, normalized, normalized2, num3);
							num++;
						}
					}
					obstacleVertex2 = obstacleVertex2.next;
				}
				while (obstacleVertex2 != obstacleVertex);
			}
			for (int j = 0; j < neighbours.Count; j++)
			{
				Agent agent = neighbours[j];
				if (agent == this)
				{
					continue;
				}
				float num7 = Math.Min(position.y + height, agent.position.y + agent.height);
				float num8 = Math.Max(position.y, agent.position.y);
				if (!(num7 - num8 < 0f))
				{
					Vector2 vector3 = new Vector2(agent.Velocity.x, agent.velocity.z);
					float num9 = radius + agent.radius;
					Vector2 vector4 = new Vector2(agent.position.x, agent.position.z) - vector;
					Vector2 sideChooser = vector2 - vector3;
					Vector2 vector5 = ((!agent.locked) ? ((vector2 + vector3) * 0.5f) : vector3);
					vos[num] = new VO(vector4, vector5, num9, sideChooser, num2, 1f);
					num++;
					if (DebugDraw)
					{
						DrawVO(vector + vector4 * num2 + vector5, num9 * num2, vector + vector5);
					}
				}
			}
			Vector2 zero = Vector2.zero;
			if (simulator.algorithm == Simulator.SamplingAlgorithm.GradientDecent)
			{
				if (DebugDraw)
				{
					for (int k = 0; k < 40; k++)
					{
						for (int l = 0; l < 40; l++)
						{
							Vector2 vector6 = new Vector2((float)k * 15f / 40f, (float)l * 15f / 40f);
							Vector2 zero2 = Vector2.zero;
							float num10 = 0f;
							for (int m = 0; m < num; m++)
							{
								float num11;
								zero2 += vos[m].Sample(vector6 - vector, out num11);
								if (num11 > num10)
								{
									num10 = num11;
								}
							}
							Vector2 vector7 = new Vector2(desiredVelocity.x, desiredVelocity.z) - (vector6 - vector);
							zero2 += vector7 * DesiredVelocityScale;
							if (vector7.magnitude * DesiredVelocityWeight > num10)
							{
								num10 = vector7.magnitude * DesiredVelocityWeight;
							}
							if (num10 > 0f)
							{
								zero2 /= num10;
							}
							UnityEngine.Debug.DrawRay(To3D(vector6), To3D(vector7 * 0f), Color.blue);
							float score = 0f;
							Vector2 vector8 = vector6 - Vector2.one * 15f * 0.5f;
							Vector2 vector9 = Trace(vos, num, vector8, 0.01f, out score);
							if ((vector8 - vector9).sqrMagnitude < Sqr(0.375f) * 2.6f)
							{
								UnityEngine.Debug.DrawRay(To3D(vector9 + vector), Vector3.up * 1f, Color.red);
							}
						}
					}
				}
				float score2 = float.PositiveInfinity;
				float cutoff = new Vector2(velocity.x, velocity.z).magnitude * simulator.qualityCutoff;
				zero = Trace(vos, num, new Vector2(desiredVelocity.x, desiredVelocity.z), cutoff, out score2);
				if (DebugDraw)
				{
					DrawCross(zero + vector, Color.yellow, 0.5f);
				}
				Vector2 p3 = Velocity;
				float score3;
				Vector2 vector10 = Trace(vos, num, p3, cutoff, out score3);
				if (score3 < score2)
				{
					zero = vector10;
					score2 = score3;
				}
				if (DebugDraw)
				{
					DrawCross(vector10 + vector, Color.magenta, 0.5f);
				}
			}
			else
			{
				Vector2[] samplePos = context.samplePos;
				float[] sampleSize = context.sampleSize;
				int num12 = 0;
				Vector2 vector11 = new Vector2(desiredVelocity.x, desiredVelocity.z);
				float num13 = Mathf.Max(radius, Mathf.Max(vector11.magnitude, Velocity.magnitude));
				samplePos[num12] = vector11;
				sampleSize[num12] = num13 * 0.3f;
				num12++;
				samplePos[num12] = vector2;
				sampleSize[num12] = num13 * 0.3f;
				num12++;
				Vector2 vector12 = vector2 * 0.5f;
				Vector2 vector13 = new Vector2(vector12.y, 0f - vector12.x);
				for (int n = 0; n < 8; n++)
				{
					samplePos[num12] = vector13 * Mathf.Sin((float)n * (float)Math.PI * 2f / 8f) + vector12 * (1f + Mathf.Cos((float)n * (float)Math.PI * 2f / 8f));
					sampleSize[num12] = (1f - Mathf.Abs((float)n - 4f) / 8f) * num13 * 0.5f;
					num12++;
				}
				vector12 *= 0.6f;
				vector13 *= 0.6f;
				for (int num14 = 0; num14 < 6; num14++)
				{
					samplePos[num12] = vector13 * Mathf.Cos(((float)num14 + 0.5f) * (float)Math.PI * 2f / 6f) + vector12 * (1.6666666f + Mathf.Sin(((float)num14 + 0.5f) * (float)Math.PI * 2f / 6f));
					sampleSize[num12] = num13 * 0.3f;
					num12++;
				}
				for (int num15 = 0; num15 < 6; num15++)
				{
					samplePos[num12] = vector2 + new Vector2(num13 * 0.2f * Mathf.Cos(((float)num15 + 0.5f) * (float)Math.PI * 2f / 6f), num13 * 0.2f * Mathf.Sin(((float)num15 + 0.5f) * (float)Math.PI * 2f / 6f));
					sampleSize[num12] = num13 * 0.2f * 2f;
					num12++;
				}
				samplePos[num12] = vector2 * 0.5f;
				sampleSize[num12] = num13 * 0.4f;
				num12++;
				Vector2[] bestPos = context.bestPos;
				float[] bestSizes = context.bestSizes;
				float[] bestScores = context.bestScores;
				for (int num16 = 0; num16 < 3; num16++)
				{
					bestScores[num16] = float.PositiveInfinity;
				}
				bestScores[3] = float.NegativeInfinity;
				Vector2 vector14 = vector2;
				float num17 = float.PositiveInfinity;
				for (int num18 = 0; num18 < 3; num18++)
				{
					for (int num19 = 0; num19 < num12; num19++)
					{
						float num20 = 0f;
						for (int num21 = 0; num21 < num; num21++)
						{
							num20 = Math.Max(num20, vos[num21].ScalarSample(samplePos[num19]));
						}
						float magnitude = (samplePos[num19] - vector11).magnitude;
						float num22 = num20 + magnitude * DesiredVelocityWeight;
						num20 += magnitude * 0.001f;
						if (DebugDraw)
						{
							DrawCross(vector + samplePos[num19], Rainbow(Mathf.Log(num20 + 1f) * 5f), sampleSize[num19] * 0.5f);
						}
						if (num22 < bestScores[0])
						{
							for (int num23 = 0; num23 < 3; num23++)
							{
								if (num22 >= bestScores[num23 + 1])
								{
									bestScores[num23] = num22;
									bestSizes[num23] = sampleSize[num19];
									bestPos[num23] = samplePos[num19];
									break;
								}
							}
						}
						if (num20 < num17)
						{
							vector14 = samplePos[num19];
							num17 = num20;
							if (num20 == 0f)
							{
								num18 = 100;
								break;
							}
						}
					}
					num12 = 0;
					for (int num24 = 0; num24 < 3; num24++)
					{
						Vector2 vector15 = bestPos[num24];
						float num25 = bestSizes[num24];
						bestScores[num24] = float.PositiveInfinity;
						float num26 = num25 * 0.6f * 0.5f;
						samplePos[num12] = vector15 + new Vector2(num26, num26);
						samplePos[num12 + 1] = vector15 + new Vector2(0f - num26, num26);
						samplePos[num12 + 2] = vector15 + new Vector2(0f - num26, 0f - num26);
						samplePos[num12 + 3] = vector15 + new Vector2(num26, 0f - num26);
						sampleSize[num12 + 3] = (sampleSize[num12 + 2] = (sampleSize[num12 + 1] = (sampleSize[num12] = num25 * (num25 * 0.6f))));
						num12 += 4;
					}
				}
				zero = vector14;
			}
			if (DebugDraw)
			{
				DrawCross(zero + vector, 1f);
			}
			newVelocity = To3D(Vector2.ClampMagnitude(zero, maxSpeed));
		}

		private static Color Rainbow(float v)
		{
			Color result = new Color(v, 0f, 0f);
			if (result.r > 1f)
			{
				result.g = result.r - 1f;
				result.r = 1f;
			}
			if (result.g > 1f)
			{
				result.b = result.g - 1f;
				result.g = 1f;
			}
			return result;
		}

		private Vector2 Trace(VO[] vos, int voCount, Vector2 p, float cutoff, out float score)
		{
			score = 0f;
			float stepScale = simulator.stepScale;
			float num = float.PositiveInfinity;
			Vector2 result = p;
			for (int i = 0; i < 50; i++)
			{
				float num2 = 1f - (float)i / 50f;
				num2 *= stepScale;
				Vector2 zero = Vector2.zero;
				float num3 = 0f;
				for (int j = 0; j < voCount; j++)
				{
					float num4;
					Vector2 vector = vos[j].Sample(p, out num4);
					zero += vector;
					if (num4 > num3)
					{
						num3 = num4;
					}
				}
				Vector2 vector2 = new Vector2(desiredVelocity.x, desiredVelocity.z) - p;
				float val = vector2.magnitude * DesiredVelocityWeight;
				zero += vector2 * DesiredVelocityScale;
				num3 = (score = Math.Max(num3, val));
				if (score < num)
				{
					num = score;
				}
				result = p;
				if (score <= cutoff && i > 10)
				{
					break;
				}
				float sqrMagnitude = zero.sqrMagnitude;
				if (sqrMagnitude > 0f)
				{
					zero *= num3 / Mathf.Sqrt(sqrMagnitude);
				}
				zero *= num2;
				Vector2 p2 = p;
				p += zero;
				if (DebugDraw)
				{
					UnityEngine.Debug.DrawLine(To3D(p2) + position, To3D(p) + position, Rainbow(0.1f / score) * new Color(1f, 1f, 1f, 0.2f));
				}
			}
			score = num;
			return result;
		}

		public static bool IntersectionFactor(Vector2 start1, Vector2 dir1, Vector2 start2, Vector2 dir2, out float factor)
		{
			float num = dir2.y * dir1.x - dir2.x * dir1.y;
			if (num == 0f)
			{
				factor = 0f;
				return false;
			}
			float num2 = dir2.x * (start1.y - start2.y) - dir2.y * (start1.x - start2.x);
			factor = num2 / num;
			return true;
		}
	}
}
