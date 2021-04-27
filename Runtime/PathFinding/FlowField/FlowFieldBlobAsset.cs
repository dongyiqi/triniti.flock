using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;

namespace Triniti.Flock.FlowField
{
    public struct FlowFieldBlobAssetReference : IComponentData
    {
        private BlobAssetReference<FlowFieldBlobAsset> Reference;
    }

    public struct FlowFieldBlobAsset
    {
        public GridMap.Data GridMapData;
        public BlobArray<byte> CostCell; //gridSize.x*gridSize.y o(n2)
        public BlobArray<byte> FlowFieldCell; // searchIndex*gridSize.x*gridSize.y o(n3)
    }

    public static class FlowFieldBlobAssetSerializer
    {
        public static void Serialize(string path, in GridMap gridMap, NativeArray<FlowFieldCell>[] flowFieldCells)
        {
            var cellCount = gridMap.GridMapData.CellCount;
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var flowFieldBlobAsset = ref blobBuilder.ConstructRoot<FlowFieldBlobAsset>();
            flowFieldBlobAsset.GridMapData = gridMap.GridMapData;
            var blobBuilderCostCell = blobBuilder.Allocate(ref flowFieldBlobAsset.CostCell, cellCount);
            var blobBuilderFlowFieldCell = blobBuilder.Allocate(ref flowFieldBlobAsset.FlowFieldCell, cellCount * cellCount);
            unsafe
            {
                UnsafeUtility.MemCpy(blobBuilderCostCell.GetUnsafePtr(), gridMap.CostCellArray.GetUnsafePtr(),
                    gridMap.GridMapData.CellCount);
                for (int i = 0; i < flowFieldCells.Length; i++)
                {
                    var startIndex = i * cellCount;
                    // var target = blobBuilderFlowFieldCell.GetUnsafePtr() + startIndex;
                    // UnsafeUtility.MemCpy(, gridMap.CostCellArray.GetUnsafePtr(),
                    //     gridMap.GridMapData.CellCount);
                    for (int j = 0; j < flowFieldCells.Length; j++)
                    {
                        blobBuilderFlowFieldCell[startIndex + j] = flowFieldCells[i][j].Direction;
                    }
                }
            }


            var serializeBlobAssetReference = blobBuilder.CreateBlobAssetReference<FlowFieldBlobAsset>(Allocator.Persistent);
            var writer = new StreamBinaryWriter($"{Application.dataPath}/1.bytes");
            writer.Write(serializeBlobAssetReference);
            writer.Dispose();
            blobBuilder.Dispose();
        }

        public static GridMap ConvertToGridMap(in BlobAssetReference<FlowFieldBlobAsset> blobAssetReference)
        {
            ref var flowFieldBlobAsset = ref blobAssetReference.Value;
            var gridMap = new GridMap().Initialize(flowFieldBlobAsset.GridMapData.GridSize.x, flowFieldBlobAsset.GridMapData.CellSize);
            unsafe
            {
                UnsafeUtility.MemCpy(gridMap.CostCellArray.GetUnsafePtr(), flowFieldBlobAsset.CostCell.GetUnsafePtr(),
                    gridMap.GridMapData.CellCount);
            }

            return gridMap;
        }

        public static BlobAssetReference<FlowFieldBlobAsset> Deserialize(byte[] data)
        {
            BlobAssetReference<FlowFieldBlobAsset> reference;
            unsafe
            {
                fixed (byte* p = data)
                {
                    var reader = new MemoryBinaryReader(p);
                    reference = reader.Read<FlowFieldBlobAsset>();
                }
            }

            return reference;
        }
    }
}