using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

/// <summary>
/// Because NativeReference.value is not ref
/// </summary>
public unsafe struct UnsafePointer<T> : IDisposable where T : unmanaged
{
	[NoAlias][NativeDisableUnsafePtrRestriction] public T* pointer;

	public UnsafePointer(T defaultValue)
	{
		pointer = (T*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), Allocator.Persistent);
		*pointer = defaultValue;
	}

	public static UnsafePointer<T> Create()
	{
		return new UnsafePointer<T>(default);
	}

	public bool IsCreated => pointer != null;

	public ref T item => ref *pointer;

	public void Dispose()
	{
		if (IsCreated) UnsafeUtility.Free(pointer, Allocator.Persistent);
		pointer = null;
	}
}
