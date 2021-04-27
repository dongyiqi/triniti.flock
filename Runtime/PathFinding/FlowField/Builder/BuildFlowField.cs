using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Triniti.Flock.FlowField
{
    [BurstCompile]
    public struct BuildFlowFieldJob : IJob
    {
        public BuildFlowField BuildFlowFieldTask;

        public void Execute()
        {
            BuildFlowFieldTask.Execute();
        }
    }

    //175ms 20*20 grid on pc with cpu e3-1230 v3
    //Consider 1.to Build a offline calculation of flow field map for all the cell in grid and serialization in blob asset
    //2.cache prev data
    [BurstCompile]
    public struct BuildFlowField
    {
        //in
        [ReadOnly] public int DestinationCellIndex;
        [ReadOnly] public GridMap Map;
        [ReadOnly] public NativeArray<float> CostWeightArray;

        public NativeQueue<int> IndicesToCheck;

        //out
        public NativeArray<FlowFieldCell> FlowFieldCells;

        public void Execute()
        {
//            Profiler.BeginSample("BuildFlowField");
            ref var gridMapData = ref Map.GridMapData;
            ref var costCellArray = ref Map.CostCellArray;

            FlowFieldCells[DestinationCellIndex] = new FlowFieldCell {BestCost = 0};
            //var IndicesToCheck = new NativeQueue<int>(Allocator.Temp);
            var neighborIndices = new NativeArray<int>(8, Allocator.Temp);
            IndicesToCheck.Enqueue(DestinationCellIndex);
            while (IndicesToCheck.Count > 0)
            {
                var checkFlatIndex = IndicesToCheck.Dequeue();
                if (FlowFieldHelper.IsBorder(checkFlatIndex, in gridMapData)) continue;
                FlowFieldHelper.GetNeighborIndices(checkFlatIndex, gridMapData.GridSize, ref neighborIndices);
                var checkFlowFieldCell = FlowFieldCells[checkFlatIndex];
                int weightIndex = 0;
                foreach (var neighborIndex in neighborIndices)
                {
                    weightIndex++;
                    if (neighborIndex < 0 || neighborIndex >= costCellArray.Length) continue;
                    if (costCellArray[neighborIndex].Value == byte.MaxValue) continue;
                    var neighborFlowFieldCell = FlowFieldCells[neighborIndex];
                    var neighborCostCell = costCellArray[neighborIndex];
                    var neighborCost = neighborCostCell.Value * CostWeightArray[weightIndex - 1];
                    if (neighborCost + checkFlowFieldCell.BestCost < neighborFlowFieldCell.BestCost)
                    {
                        neighborFlowFieldCell.BestCost = neighborCost + checkFlowFieldCell.BestCost;
                        FlowFieldCells[neighborIndex] = neighborFlowFieldCell;
                        IndicesToCheck.Enqueue(neighborIndex);
                    }
                }

                for (int i = gridMapData.GridSize.y; i < gridMapData.CellCount - gridMapData.GridSize.y; i++)
                {
                    if (FlowFieldHelper.IsBorder(i, in gridMapData)) continue;

                    var curFlowFieldCell = FlowFieldCells[i];
                    var bestCost = curFlowFieldCell.BestCost;
                    if (bestCost == float.MaxValue)
                    {
                        curFlowFieldCell.BestDirection = int2.zero;
                        continue;
                    }

                    FlowFieldHelper.GetNeighborIndices(i, gridMapData.GridSize, ref neighborIndices);
                    byte directionIndex = 0;
                    foreach (var neighborIndex in neighborIndices)
                    {
                        var neighborData = FlowFieldCells[neighborIndex];
                        if (neighborData.BestCost < bestCost)
                        {
                            curFlowFieldCell.Direction = directionIndex;
                            bestCost = neighborData.BestCost;
                            //bestDirection = neighborIndex 2D - curCellIndex2D
                            curFlowFieldCell.BestDirection = FlowFieldHelper.GetCellIndexFromFlatIndex(neighborIndex, in gridMapData) -
                                                             FlowFieldHelper.GetCellIndexFromFlatIndex(i, in gridMapData);
                        }

                        directionIndex++;
                    }

                    FlowFieldCells[i] = curFlowFieldCell;
                }
            }

            //Profiler.EndSample();
        }
    }
}