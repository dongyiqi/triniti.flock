using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Triniti.Flock
{
    public struct NativeArray2D<T> where T : struct
    {
        private NativeArray<T> _array;

        private int2 _dimension; // x is row count y is column count

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
            set => _array[x * _dimension.x + y] = value;
        }

        public void Dispose() => _array.Dispose();
        public void Dispose(JobHandle jobHandle) => _array.Dispose(jobHandle);
    }
}