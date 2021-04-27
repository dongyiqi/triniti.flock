using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Triniti.Flock
{
    public struct NativeArray2D<T> : IDisposable, IEnumerable<T>, IEnumerable, IEquatable<NativeArray2D<T>> where T : struct
    {
        private NativeArray<T> _array;

        private int2 _dimension; // x is row count y is column count
        public bool IsCreated => _array.IsCreated;
        public int Length => _array.Length;

        public int2 Length2D => _dimension;

        //row order
        public NativeArray2D(int2 dimension, Allocator allocator)
        {
            _dimension = dimension;
            _array = new NativeArray<T>(dimension.x * dimension.y, allocator);
        }

        public T this[int i]
        {
            get => _array[i];
            [WriteAccessRequired] set => _array[i] = value;
        }

        public T this[int x, int y]
        {
            get => _array[x * _dimension.x + y];
            [WriteAccessRequired] set => _array[x * _dimension.x + y] = value;
        }

        public T this[int2 i]
        {
            get => _array[i.x * _dimension.x + i.y];
            [WriteAccessRequired] set => _array[i.x * _dimension.x + i.y] = value;
        }

        public void Sort<U>(U comp) where U : IComparer<T> => _array.Sort(comp);

        public void Dispose() => _array.Dispose();
        public void Dispose(JobHandle jobHandle) => _array.Dispose(jobHandle);
        public NativeArray<T>.Enumerator GetEnumerator() => _array.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => (IEnumerator<T>) _array.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) _array.GetEnumerator();

        public bool Equals(NativeArray2D<T> other) => this._array.Equals(other._array);

        public unsafe void* GetUnsafePtr() => _array.GetUnsafePtr();
    }

    [BurstCompile]
    public struct MemsetNativeArray2D<T> : IJobParallelFor where T : struct
    {
        public NativeArray2D<T> Source;
        public T Value;

        // #todo Need equivalent of IJobParallelFor that's per-chunk so we can do memset per chunk here.
        public void Execute(int index) => Source[index] = Value;
    }
}