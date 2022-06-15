using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Profiling;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using WireframeImageEffect = SuperSystems.ImageEffects.WireframeImageEffect;

namespace NaiveSurfaceNets
{
	public class Example : MonoBehaviour
	{
		private bool regenerateOnce = true;
		private GenerateJob.Mode generateModePrev;
		public GenerateJob.Mode generateMode;
		public Mesher.NormalCalculationMode normalCalculationMode;
		
		public bool regenerateChunk = false;
		public bool drawNormals = true;
		public float noiseSpeed = 0.1f;
		public GameObject chunkGameObject;
		private MeshFilter chunkMeshFilter;


		Chunk chunk;
		Mesher mesher;
		GenerateJob generateJob;

		TimeCounter meshingCounter = new TimeCounter(samplesCount: 300);
		TimeCounter uploadingCounter = new TimeCounter();
		TimeCounter chunkRegenCounter = new TimeCounter();

		static Material lineMaterial;



		void OnGUI()
		{
			GUILayout.BeginHorizontal();

			{
				var wires = Camera.main.GetComponent<WireframeImageEffect>();

				GUILayout.BeginVertical(GUI.skin.box);
				GUILayout.Label("Chunk regenerate mean time: " + chunkRegenCounter.mean.ToString("F3") + " ms");
				GUILayout.Label("Meshing mean time: " + meshingCounter.mean.ToString("F3") + " ms");
				GUILayout.Label("Upload mean time: " + uploadingCounter.mean.ToString("F3") + " ms");
				GUILayout.BeginHorizontal();
				GUILayout.Label("Speed");
				noiseSpeed = GUILayout.HorizontalSlider(noiseSpeed, 0.0f, 2.0f);
				GUILayout.EndHorizontal();
				GUILayout.Space(10);
				drawNormals = GUILayout.Toggle(drawNormals, "Draw normals");
				wires.wireframeType = GUILayout.Toggle(wires.wireframeType == WireframeImageEffect.WireframeType.Solid, "Wireframe") ? WireframeImageEffect.WireframeType.Solid : WireframeImageEffect.WireframeType.None;

				GUILayout.Space(10);
				GUIToggles();

				GUILayout.Space(10);

				GUILayout.EndVertical();
			}
			{
				GUILayout.BeginVertical(GUI.skin.box);
				GUILayout.Label("Scrool to zoom, RMB to rotate (shift for faster)");
				GUILayout.Label("Vertices " + mesher.Vertices.Length);
				GUILayout.EndVertical();
			}

			GUILayout.EndHorizontal();
		}
		void GUIToggles()
		{
			generateMode = GUILayout.Toggle(generateMode == GenerateJob.Mode.SingleSphere, "Single sphere") ? GenerateJob.Mode.SingleSphere : generateMode;
			generateMode = GUILayout.Toggle(generateMode == GenerateJob.Mode.Noise, "Noise") ? GenerateJob.Mode.Noise : generateMode;
			generateMode = GUILayout.Toggle(generateMode == GenerateJob.Mode.Sphereblob, "Sphereblobs") ? GenerateJob.Mode.Sphereblob : generateMode;
			generateMode = GUILayout.Toggle(generateMode == GenerateJob.Mode.Terrain, "Terrain") ? GenerateJob.Mode.Terrain : generateMode;

			GUILayout.Space(10);
			normalCalculationMode = GUILayout.Toggle(normalCalculationMode == Mesher.NormalCalculationMode.FromSDF, "SDF normals") ? Mesher.NormalCalculationMode.FromSDF : normalCalculationMode;
			normalCalculationMode = GUILayout.Toggle(normalCalculationMode == Mesher.NormalCalculationMode.Recalculate, "Recalculate normals") ? Mesher.NormalCalculationMode.Recalculate : normalCalculationMode;
		}

		void Start()
		{
			if (chunkGameObject != null)
			{
				chunkMeshFilter = chunkGameObject.GetComponent<MeshFilter>();
				if (chunkMeshFilter.mesh == null)
					chunkMeshFilter.mesh = new Mesh();
			}

			chunk = new Chunk();
			mesher = new Mesher();

			PrepareGeneratorJobsData();
		}

		void PrepareGeneratorJobsData()
		{
			generateJob = new GenerateJob
			{
				volume = chunk.data,

				sphereCenter = new float3(15.5f, 15.5f, 15.5f),
				noiseFreq = 0.07f,
				spheresPositions = new NativeArray<float3>(50, Allocator.Persistent),
				spheresDeltas = new NativeArray<float4>(50, Allocator.Persistent)
			};

			for (int i = 0; i < generateJob.spheresPositions.Length; i++)
			{
				generateJob.spheresPositions[i] = new float3
				{
					x = UnityEngine.Random.value * (Chunk.ChunkSize - 10) + 5,
					y = UnityEngine.Random.value * (Chunk.ChunkSize - 10) + 5,
					z = UnityEngine.Random.value * (Chunk.ChunkSize - 10) + 5,
				};
			}
		}

		void Update()
		{
			if (generateMode != generateModePrev)
			{
				generateModePrev = generateMode;
				regenerateOnce = true;
			}

			if (regenerateChunk || regenerateOnce)
			{
				regenerateOnce = false;
				chunkRegenCounter.Start();
				generateJob.time += Time.deltaTime * noiseSpeed;
				generateJob.mode = generateMode;
				if (generateMode == GenerateJob.Mode.Sphereblob)
					generateJob.Run();
				generateJob.Schedule(32, 1).Complete();
				chunkRegenCounter.Stop();
			}

			meshingCounter.Start();
			mesher.StartMeshJob(chunk, normalCalculationMode);
			mesher.WaitForMeshJob();
			meshingCounter.Stop();

			uploadingCounter.Start();
			chunkMeshFilter.mesh.SetMesh(mesher);
			uploadingCounter.Stop();
		}

		void OnRenderObject()
		{
			if (mesher != null && drawNormals)
			{
				if (!lineMaterial)
				{
					// Unity has a built-in shader that is useful for drawing
					// simple colored things.
					Shader shader = Shader.Find("Hidden/Internal-Colored");
					lineMaterial = new Material(shader);
					lineMaterial.hideFlags = HideFlags.HideAndDontSave;
					// Turn on alpha blending
					lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					// Turn backface culling off
					lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
					// Turn off depth writes
					lineMaterial.SetInt("_ZWrite", 0);
				}

				var vertices = mesher.Vertices;

				lineMaterial.SetPass(0);
				GL.PushMatrix();
				GL.MultMatrix(transform.localToWorldMatrix);
				GL.Begin(GL.LINES);
				GL.Color(Color.cyan);
				for (int i = 0; i < vertices.Length; i++)
				{
					GL.Vertex(vertices[i].position);
					GL.Vertex(vertices[i].position + math.normalize(vertices[i].normal) * 0.2f);
				}
				GL.End();
				GL.PopMatrix();
			}
		}

		void OnDestroy()
		{
			chunk.Dispose();
			mesher.Dispose();
			generateJob.spheresPositions.Dispose();
			generateJob.spheresDeltas.Dispose();
		}
	}
}