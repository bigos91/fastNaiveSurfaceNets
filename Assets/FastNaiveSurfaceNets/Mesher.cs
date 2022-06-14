using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

namespace NaiveSurfaceNets
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Vertex
	{
		public float3 position;
		public float3 normal;

		public static readonly VertexAttributeDescriptor[] VertexFormat =
		{new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32),
		 new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32)};
	}

	public class Mesher : IDisposable
	{
		private MesherJob mesherJob;
		private JobHandle meshJobHandle;

		public enum NormalCalculationMode { FromSDF, Recalculate }


		public Mesher()
		{
			Allocate();
			PrecalculateEdgeTable();
		}
		public void Dispose()
		{
			mesherJob.edgeTable.Dispose();
			mesherJob.indices.Dispose();
			mesherJob.vertices.Dispose();
			mesherJob.bounds.Dispose();
			mesherJob.buffer.Dispose();
		}
		private void Allocate()
		{
			var bufferSize = (Chunk.ChunkSize + 1) * (Chunk.ChunkSize + 1) * 2;

			mesherJob = new MesherJob()
			{
				// Edge table is readonly, and should be shared among different Mesher instances
				edgeTable = new NativeArray<ushort>(256, Allocator.Persistent),

				indices = new NativeList<int>(100, Allocator.Persistent),
				vertices = new NativeList<Vertex>(100, Allocator.Persistent),
				bounds = new UnsafePointer<Bounds>(default),

				buffer = new NativeArray<int>(bufferSize, Allocator.Persistent, NativeArrayOptions.ClearMemory)
			};
		}
		private void PrecalculateEdgeTable()
		{
			// Edge table is a lookup array for obtaining edgemasks.
			// Cornermask should be used as an index to search proper edgemask.
			// Cornermasks are 8bit binary flags where each bit tells if specific 'corner' (voxel) has negative or positive value.
			// What edgemask is ?
			// Its a bit mask of 12 edges of a cube.
			// Specific bit is enabled, if there is a 'crossing' of corresponding edge (sign change between 2 voxels).
			// If there is a sign change, such edge can produce vertex (or at least).
			// Final vertex position is calculated as a mean position of all vertices from all 'crossed' edges.
			// Magic behind calculating that edge table is unknown to me.

			var cubeEdges = new int[24];
			int k = 0;
			for (int i = 0; i < 8; ++i)
			{
				for (int j = 1; j <= 4; j <<= 1)
				{
					int p = i ^ j;
					if (i <= p)
					{
						cubeEdges[k++] = i;
						cubeEdges[k++] = p;
					}
				}
			}

			for (int i = 0; i < 256; ++i)
			{
				int em = 0;
				for (int j = 0; j < 24; j += 2)
				{
					var a = Convert.ToBoolean(i & (1 << cubeEdges[j]));
					var b = Convert.ToBoolean(i & (1 << cubeEdges[j + 1]));
					em |= a != b ? (1 << (j >> 1)) : 0;
				}
				mesherJob.edgeTable[i] = (ushort)em;
			}
		}



		public void StartMeshJob(Chunk chunk, NormalCalculationMode normalCalculationMode)
		{
			mesherJob.recalculateNormals = normalCalculationMode == NormalCalculationMode.Recalculate;
			mesherJob.volume = chunk.data;
			meshJobHandle = mesherJob.Schedule();
		}
		public bool IsFinished() => meshJobHandle.IsCompleted;
		public void WaitForMeshJob() => meshJobHandle.Complete();
		public NativeArray<int> Indices => mesherJob.indices.AsArray();
		public NativeArray<Vertex> Vertices => mesherJob.vertices.AsArray();
		public Bounds Bounds => mesherJob.bounds.item;
	}
}

