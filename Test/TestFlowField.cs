using System;
using Triniti.Flock.FlowField;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace Triniti.Flock.Test
{
   

    public class TestFlowField : MonoBehaviour
    {
        public int GridSize = 20;
        public float CellSize = 1;
        private GridMap _gridMap;
        public Toggle EditMode;

        private NativeArray<FlowFieldCell> _flowFieldCells;

        [ContextMenu("TestBake")]
        public void TestBake()
        {
            // var blobBuilder = new BlobBuilder(Allocator.Temp);
            // ref var blobAsset = ref blobBuilder.ConstructRoot<TesstBlobAsset>();
            // var arrayBuilder = blobBuilder.Allocate(ref blobAsset.ByteArray, (int) math.pow(50, 3));
            // var writer = new Unity.Entities.Serialization.StreamBinaryWriter("D:1.bytes");
            // var testBlobAssetReference = blobBuilder.CreateBlobAssetReference<TesstBlobAsset>(Allocator.Persistent);
            //
            // writer.Write<TesstBlobAsset>(testBlobAssetReference);
            // writer.Dispose();
            // blobBuilder.Dispose();
        }

        private void Awake()
        {
            _gridMap = new GridMap().Initialize(GridSize, CellSize);
            _flowFieldCells = new NativeArray<FlowFieldCell>(_gridMap.GridMapData.CellCount, Allocator.Persistent);
            GridMap.Instance = _gridMap;
        }

        private void Update()
        {
            if (EditMode.isOn)
            {
                if (Input.GetMouseButton(0))
                {
                    if (!TestHelper.GetPoint(out var point)) return;
                    _SetCost(point, CostCell.IMPASSABLE);
                }
                else if (Input.GetMouseButton(1))
                {
                    if (!TestHelper.GetPoint(out var point)) return;
                    _SetCost(point, 1);
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (!TestHelper.GetPoint(out var point)) return;
                    new MemsetNativeArray<FlowFieldCell> {Source = _flowFieldCells, Value = new FlowFieldCell {BestCost = float.MaxValue}}
                        .Run(_flowFieldCells.Length);
                    var tempQueue = new NativeQueue<int>(Allocator.TempJob);
                    //navigation mode
                    var buildFlowField = new BuildFlowField
                    {
                        Map = _gridMap,
                        DestinationCellIndex = FlowFieldHelper.GetFlatIndexFromWorldPos(point, in _gridMap.GridMapData),
                        FlowFieldCells = _flowFieldCells,
                        CostWeightArray = FlowFieldHelper.CostWeightArray,
                        IndicesToCheck = tempQueue,
                    };
                    new BuildFlowFieldJob
                    {
                        BuildFlowFieldTask = buildFlowField
                    }.Run();
                    tempQueue.Dispose();
                }
            }
        }


        private void _SetCost(float2 point, byte value)
        {
            var index = _gridMap.GridMapData.PointToCellIndex(point);

            if (math.any(index < 0) || math.any(index > _gridMap.GridMapData.GridSize)) return;
            _gridMap.CostCellArray[index] = new CostCell {Value = value};
        }

        private void OnDestroy()
        {
            _gridMap.Dispose();
            _flowFieldCells.Dispose();
            FlowFieldHelper.CostWeightArray.Dispose();
        }

        private void OnDrawGizmos()
        {
            //draw grid
            if (!gameObject.activeSelf) return;
            var anchor = _gridMap.GridMapData.Anchor;
            var gridSize = _gridMap.GridMapData.GridSize;
            var cellSize = _gridMap.GridMapData.CellSize;
            //vertical
            Gizmos.color = Color.white;

            for (int x = 0; x <= gridSize.x; x++)
            {
                var start = anchor + new float2(x, 0) * cellSize;
                var end = start + new float2(0, (gridSize.x) * cellSize);
                Gizmos.DrawLine(new Vector3(start.x, 0, start.y), new Vector3(end.x, 0, end.y));
            }

            for (int y = 0; y <= gridSize.y; y++)
            {
                var start = anchor + new float2(0, y) * cellSize;
                var end = start + new float2((gridSize.y) * cellSize, 0);
                Gizmos.DrawLine(new Vector3(start.x, 0, start.y), new Vector3(end.x, 0, end.y));
            }

            Gizmos.color = Color.gray * 0.5f;
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    var cost = _gridMap.CostCellArray[x, y];
                    if (cost.Value == CostCell.IMPASSABLE)
                    {
                        var pivot = _gridMap.GridMapData.Anchor + (new float2(x, y) + 0.5f) * cellSize;
                        Gizmos.DrawCube(new Vector3(pivot.x, 0, pivot.y), new Vector3(cellSize, 0.1f, cellSize));
                    }
                }
            }

            if (!EditMode.isOn)
            {
                Gizmos.color = Color.green;
                //draw flow field direction in cell
                for (int i = 0; i < _gridMap.GridMapData.CellCount; i++)
                {
                    var position = FlowFieldHelper.GetCellPositionFromFlatIndex(i, in _gridMap.GridMapData);
                    var direction = _flowFieldCells[i].BestDirection;
                    Gizmos.DrawLine(new Vector3(position.x, 0, position.y),
                        new Vector3(position.x, 0, position.y) +
                        0.8f * _gridMap.GridMapData.CellSize * new Vector3(direction.x, 0, direction.y));
                }
            }
        }
    }
}