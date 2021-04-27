using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Triniti.Flock.FlowField
{
    /// <summary>
    /// 5    11   17
    /// 4    10   16
    /// 3    9    15
    /// 2    8    14
    /// 1    7    13
    /// 0    6    12     
    /// </summary>
    public static class FlowFieldHelper
    {
        //TODO:包括所有格~，而不是忽略边缘
        public static bool ValidFlatIndex(int flatIndex, in GridMap.Data gridMapData)
        {
            return flatIndex >= 0 && flatIndex < gridMapData.CellCount;
        }

        public static bool IsBorder(int flatIndex, in GridMap.Data gridMapData)
        {
            var index2D = GetCellIndexFromFlatIndex(flatIndex, in gridMapData);
            return index2D.x == 0 || index2D.y == 0 || index2D.x == gridMapData.GridSize.x - 1 || index2D.y == gridMapData.GridSize.y - 1;
        }

        public static NativeArray<float> CostWeightArray =
            new NativeArray<float>(new[] {1, 1.4f, 1, 1.4f, 1, 1.4f, 1, 1.4f}, Allocator.Persistent);

        public static NativeArray<int2> CardinalAndIntercardinalDirections = new NativeArray<int2>(new[]
        {
            new int2(0, 1), //north
            new int2(1, 1), //north east
            new int2(1, 0), //east
            new int2(1, -1), //south east
            new int2(0, -1), //south
            new int2(-1, -1), //south west
            new int2(-1, 0), // west
            new int2(-1, 1), // north west
        }, Allocator.Persistent);
        //public static NativeArray<float> CostWeightArray = new NativeArray<float>(new[] {1, 1, 1, 1, 1, 1, 1, 1}, Allocator.Persistent);

        /// !优化边缘判定逻辑后，为了确保在边缘邻居计算的正确，忽略最边缘的一圈的寻路计算
        public static void GetNeighborIndices(int flatIndex, int2 gridSize, ref NativeArray<int> neighbourFlatIndex
            /*,in NativeArray<int2> directions*/)
        {
            //-1 -1 
            //TAssert.IsTrue(neighbourFlatIndex.Length == 8);
            //TODO:裁剪边缘的neighbour算到对面的错误 或者不处理边缘相邻的
            neighbourFlatIndex[0] = flatIndex + 1; // north 1
            neighbourFlatIndex[1] = flatIndex + gridSize.y + 1; // north east 1.4
            neighbourFlatIndex[2] = flatIndex + gridSize.y; // east 1
            neighbourFlatIndex[3] = flatIndex + gridSize.y - 1; // south east 1.4
            neighbourFlatIndex[4] = flatIndex - 1; // south 1
            neighbourFlatIndex[5] = flatIndex - gridSize.y - 1; // south west 1.4
            neighbourFlatIndex[6] = flatIndex - gridSize.y - 0; // west 1
            neighbourFlatIndex[7] = flatIndex - gridSize.y + 1; // north west 1.4

            // foreach (int2 curDirection in directions)
            // {
            //     int2 neighborIndex = GetIndexAtRelativePosition(originIndex, curDirection, gridSize);
            //
            //     if (neighborIndex.x >= 0)
            //     {
            //         results.Add(neighborIndex);
            //     }
            // }
        }

        private static int2 GetIndexAtRelativePosition(int2 originPos, int2 relativePos, int2 gridSize)
        {
            var finalPos = originPos + relativePos;
            if (finalPos.x < 0 || finalPos.x >= gridSize.x || finalPos.y < 0 || finalPos.y >= gridSize.y)
                return new int2(-1, -1);
            return finalPos;
        }

        //public static int ToFlatIndex(int2 index2D, int height) => height * index2D.x + index2D.y;

        //valid range is from 0~gridData.CellCount
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetFlatIndexFromWorldPos(float2 worldPosition, in GridMap.Data gridMapData)
        {
            var offset = worldPosition - gridMapData.Anchor;
            var index2D = (int2) (offset * gridMapData.InverseCellSize);
            index2D = (int2) math.clamp(index2D, float2.zero, gridMapData.GridSize - new int2(1, 1));
            //暂时不处理越界的判定 会形成tiling的效果 最多一层，默认不会出去 
            return gridMapData.GridSize.y * index2D.x + index2D.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 GetCellIndexFromFlatIndex(int flatIndex, in GridMap.Data gridMapData)
        {
            var indexX = (int) (flatIndex * gridMapData.InverseGridSize.x);
            var indexY = flatIndex - indexX * gridMapData.GridSize.x;
            return new int2(indexX, indexY);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 GetCellPositionFromFlatIndex(int flatIndex, in GridMap.Data gridMapData)
        {
            //var indexX = flatIndex / gridMapData.GridSize.x;
            var indexX = (int) (flatIndex * gridMapData.InverseGridSize.x);
            var indexY = flatIndex - indexX * gridMapData.GridSize.x;
            return new float2(indexX, indexY) * gridMapData.CellSize + 0.5f * gridMapData.CellSize + gridMapData.Anchor;
        }
    }
}