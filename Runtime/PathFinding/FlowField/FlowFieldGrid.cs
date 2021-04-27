using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Triniti.Flock.FlowField
{
    //GridMap data is the singleton data in one battle
    public struct GridMap
    {
        public static GridMap Instance;

        public struct Data
        {
            public int2 GridSize;
            public float CellSize;
            public float2 Center;

            public int CellCount;
            public float2 Anchor;
            public float2 InverseGridSize;
            public float InverseCellSize;

            public Data(int gridSize, float cellSize, float2 center)
            {
                GridSize = new int2(gridSize, gridSize);
                CellSize = cellSize;
                Center = center;

                CellCount = GridSize.x * GridSize.y;
                Anchor = Center - 0.5f * (float2) GridSize * CellSize;
                InverseGridSize = 1 / (float2) GridSize;
                InverseCellSize = 1 / CellSize;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int2 PointToCellIndex(float2 position) => (int2) ((position - Anchor) * InverseCellSize);
        }

        public Data GridMapData;
        public NativeArray2D<CostCell> CostCellArray;

        public GridMap Initialize(int gridSize, float cellSize)
        {
            GridMapData = new Data(gridSize, cellSize, float2.zero);
            CostCellArray = new NativeArray2D<CostCell>(GridMapData.GridSize, Allocator.Persistent);
            new MemsetNativeArray2D<CostCell> {Source = CostCellArray, Value = new CostCell {Value = 1}}.Run(CostCellArray.Length);
            for (int i = 0; i < GridMapData.CellCount; i++)
            {
                CostCellArray[i] = new CostCell {Value = 1};
            }

            return this;
        }

        public void Dispose()
        {
            CostCellArray.Dispose();
        }
    }
}