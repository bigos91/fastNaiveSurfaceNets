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
	/// Generate spherical SDF
	/// </summary>
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
	public struct SphereJob : IJobParallelFor
	{
		[NoAlias][WriteOnly][NativeDisableParallelForRestriction] public NativeArray<sbyte> volume;
		public float time;
		public float3 sphereCenter;

		public void Execute(int jobIndex)
		{
			var flatIndex = jobIndex * Chunk.ChunkSize * Chunk.ChunkSize;
			var sphereRadius = 14.4f + math.sin(time);
			var x = jobIndex;
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
	}


	/// <summary>
	/// Generate something Like SDF from 4d noise.
	/// Noise function output does not match proper SDF, but it is good enough.
	/// </summary>
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
	public struct NoiseJob : IJobParallelFor
	{
		[NoAlias][WriteOnly][NativeDisableParallelForRestriction] public NativeArray<sbyte> volume;
		public float time;
		public float noiseFreq;

		/// <summary>
		/// Parallel job for x coordinate
		/// </summary>
		public void Execute(int jobIndex)
		{
			var flatIndex = jobIndex * Chunk.ChunkSize * Chunk.ChunkSize;

			float4 position = new float4(jobIndex * noiseFreq, 0, 0, time * noiseFreq);

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
	}


	/// <summary>
	/// Generate more proper SDF than noise based
	/// </summary>
	[BurstCompile(CompileSynchronously = true, DisableSafetyChecks = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
	public struct SphereblobJob : IJobParallelFor, IJob
	{
		[NoAlias][NativeDisableParallelForRestriction][WriteOnly] public NativeArray<sbyte> volume;
		[NoAlias][NativeDisableParallelForRestriction][ReadOnly] public NativeArray<float3> positions;
		[NoAlias][NativeDisableParallelForRestriction] public NativeArray<float4> deltas;
		public float time;
		public float noiseFreq;

		/// <summary>
		/// Single threaded job to modify delta positions of spheres
		/// </summary>
		public void Execute()
		{
			for (int i = 0; i < positions.Length; i++)
			{
				var pos = positions[i] * noiseFreq + time;
				deltas[i] = new float4
				{
					x = noise.snoise(pos.yz) * 5.0f,
					y = noise.snoise(pos.xz) * 5.0f,
					z = noise.snoise(pos.xy) * 5.0f,
					w = noise.snoise(pos.xy + time * 3.0f) * 1.5f + 4.0f
				};

			}
		}

		/// <summary>
		/// Parallel job for x coordinate
		/// </summary>
		public void Execute(int jobIndex)
		{
			var flatIndex = jobIndex * Chunk.ChunkSize * Chunk.ChunkSize;
			var x = jobIndex;

			for (int y = 0; y < Chunk.ChunkSize; y++)
			{
				for (int z = 0; z < Chunk.ChunkSize; z++)
				{
					var value = 1.0f;
					var voxelPos = new float3(x, y, z);

					for (int i = 0; i < positions.Length; i++)
					{
						var vector = voxelPos - (positions[i] + deltas[i].xyz);
						var val = math.length(vector) - deltas[i].w;

						value = math.min(value, math.max(val, -1.0f));
					}

					volume[flatIndex] = (sbyte)(value * -127.0f);
					flatIndex++;
				}
			}
		}
	}

}