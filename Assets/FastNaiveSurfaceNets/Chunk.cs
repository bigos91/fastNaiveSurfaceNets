using System;
using Unity.Collections;

namespace NaiveSurfaceNets
{
	public class Chunk : IDisposable
	{
		// this should be equal to 32
		public const int ChunkSize = 32;
		public const int ChunkSizeMinusOne = ChunkSize - 1;
		public const int xShift = 10;
		public const int yShift = 5;
		public const int zShift = 0;

		public NativeArray<sbyte> data;

		public Chunk()
		{
			data = new NativeArray<sbyte>(ChunkSize * ChunkSize * ChunkSize, Allocator.Persistent, NativeArrayOptions.ClearMemory);
		}

		public void Dispose()
		{
			data.Dispose();
		}
	}
}