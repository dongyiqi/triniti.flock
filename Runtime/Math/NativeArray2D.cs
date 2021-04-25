using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Triniti.Flock
{
    public struct NativeArray2D<T> : IDisposable, IEnumerable<T>, IEnumerable, IEquatable<NativeArray<T>>
        where T : struct
    {
        private NativeArray<T> _array;

        private int2 _dimension; // x is row count y is column count
        public bool IsCreated => _array.IsCreated;
        public int Length => _array.Length;

        //row order
        public NativeArray2D(int2 dimension, Allocator allocator)
        {
            _dimension = dimension;
            _array = new NativeArray<T>(dimension.x * dimension.y, allocator);
        }

        public T this[int x] => _array[x];

        public T this[int x, int y]
        {
            get => _array[x * _dimension.x + y];
            [WriteAccessRequired] set => _array[x * _dimension.x + y] = value;
        }

        public void Sort<U>(U comp) where U : IComparer<T> => _array.Sort(comp);

        public void Dispose() => _array.Dispose();
        public void Dispose(JobHandle jobHandle) => _array.Dispose(jobHandle);
        public NativeArray<T>.Enumerator GetEnumerator() => _array.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => (IEnumerator<T>) _array.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => (IEnumerator) _array.GetEnumerator();

        public bool Equals(NativeArray<T> other)
        {
            throw new NotImplementedException();
        }
    }
}