using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;


namespace NaiveSurfaceNets
{
	/// <summary>
	/// Generate SDF
	/// </summary>
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
	public struct GenerateJob : IJob, IJobParallelFor
	{
		public enum Mode { SingleSphere, Noise, Sphereblob, Terrain }
		public Mode mode;

		[NoAlias][WriteOnly][NativeDisableParallelForRestriction] public NativeArray<sbyte> volume;
		public float time;

		// used by different modes
		public float3 sphereCenter;
		public float noiseFreq;
		[NoAlias][NativeDisableParallelForRestriction][ReadOnly] public NativeArray<float3> spheresPositions;
		[NoAlias][NativeDisableParallelForRestriction] public NativeArray<float4> spheresDeltas;


		public void Execute()
		{
			SphereblobUpdate();
		}

		public void Execute(int jobIndex)
		{
			switch (mode)
			{
				case Mode.SingleSphere:
					SphereJob(jobIndex);
					break;
				case Mode.Noise:
					NoiseJob(jobIndex);
					break;
				case Mode.Sphereblob:
					SphereblobJob(jobIndex);
					break;
				case Mode.Terrain:
					TerrainJob(jobIndex);
					break;
				default:
					break;
			}
		}


		private void SphereJob(int x)
		{
			var flatIndex = x * Chunk.ChunkSize * Chunk.ChunkSize;
			var sphereRadius = 14.4f + math.sin(time);

			for (int y = 0; y < Chunk.ChunkSize; y++)
			{
				for (int z = 0; z < Chunk.ChunkSize; z++)
				{
					var voxelPosition = new float3(x, y, z);
					var val = math.distance(voxelPosition, sphereCenter) - sphereRadius;
					val = math.clamp(val, -1.0f, 1.0f) * -127;
					volume[flatIndex] = (sbyte)val;
					flatIndex++;
				}
			}
		}

		private void NoiseJob(int x)
		{
			var flatIndex = x * Chunk.ChunkSize * Chunk.ChunkSize;

			float4 position = new float4(x * noiseFreq, 0, 0, time * noiseFreq);

			for (int y = 0; y < Chunk.ChunkSize; y++)
			{
				position.y = y * noiseFreq;

				for (int z = 0; z < Chunk.ChunkSize; z++)
				{
					position.z = z * noiseFreq;

					var value = noise.snoise(position) * -127;
					volume[flatIndex] = (sbyte)value;
					flatIndex++;
				}
			}
		}

		private void SphereblobUpdate()
		{
			for (int i = 0; i < spheresPositions.Length; i++)
			{
				var pos = spheresPositions[i] * noiseFreq + time * 0.02f;
				spheresDeltas[i] = new float4
				{
					x = noise.snoise(pos.yz) * 5.0f,
					y = noise.snoise(pos.xz) * 5.0f,
					z = noise.snoise(pos.xy) * 5.0f,
					w = noise.snoise(pos.xy + time * 0.06f) * 1.5f + 4.0f
				};
			}
		}

		private void SphereblobJob(int x)
		{
			var flatIndex = x * Chunk.ChunkSize * Chunk.ChunkSize;

			for (int y = 0; y < Chunk.ChunkSize; y++)
			{
				for (int z = 0; z < Chunk.ChunkSize; z++)
				{
					var value = 1.0f;
					var voxelPos = new float3(x, y, z);

					for (int i = 0; i < spheresPositions.Length; i++)
					{
						var vector = voxelPos - (spheresPositions[i] + spheresDeltas[i].xyz);
						var val = math.length(vector) - spheresDeltas[i].w;

						value = math.min(value, math.max(val, -1.0f));
					}

					volume[flatIndex] = (sbyte)(value * -127.0f);
					flatIndex++;
				}
			}
		}

		private void TerrainJob(int x)
		{
			var flatIndex = x * Chunk.ChunkSize * Chunk.ChunkSize;
			
			for (int y = 0; y < Chunk.ChunkSize; y++)
			{
				for (int z = 0; z < Chunk.ChunkSize; z++)
				{
					float2 noisePos = new float2(x, z) + time;
					var val = y - (
						noise.snoise(noisePos * 0.01f) * 8.0f +
						noise.snoise(noisePos * 0.02f) * 4.0f +
						noise.snoise(noisePos * 0.04f) * 2.0f +
						noise.snoise(noisePos * 0.16f) * 0.5f +
						15.5f);
					val = math.clamp(val, -1.0f, 1.0f) * -127;
					volume[flatIndex] = (sbyte)val;
					flatIndex++;
				}
			}
		}
	}
}