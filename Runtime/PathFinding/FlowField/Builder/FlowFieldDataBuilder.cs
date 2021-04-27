using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Triniti.Flock.FlowField
{
    public class FlowFieldDataBuilder : MonoBehaviour
    {
        public Camera Cam;
        public int GridSize = 20;
        public float CellSize = 1;
        public string Path = "FlowFieldBlobAsset.bytes";
        public bool EditMode = true;
        private GridMap _gridMap;
        private NativeArray<FlowFieldCell> _flowFieldCells;

        private void OnDestroy()
        {
            _gridMap.Dispose();
            if (_flowFieldCells.IsCreated)
                _flowFieldCells.Dispose();
            FlowFieldHelper.CostWeightArray.Dispose();
        }

        [ContextMenu("CreateNewGridMap")]
        private void _CreateNewMap()
        {
            _gridMap = new GridMap().Initialize(GridSize, CellSize);
            _flowFieldCells = new NativeArray<FlowFieldCell>(_gridMap.GridMapData.CellCount, Allocator.Persistent);
            new MemsetNativeArray<FlowFieldCell>
            {
                Source = _flowFieldCells, Value = new FlowFieldCell
                {
                    BestCost = float.MaxValue,
                    Direction = FlowFieldCell.NO_DIRECTION,
                }
            }.Run(_flowFieldCells.Length);
        }

        public TextAsset LoadMapGridData;
        private BlobAssetReference<FlowFieldBlobAsset> _blobAssetReference;

        [ContextMenu("Load")]
        private void _Load()
        {
            _blobAssetReference = FlowFieldBlobAssetSerializer.Deserialize(LoadMapGridData.bytes);
            _gridMap = FlowFieldBlobAssetSerializer.ConvertToGridMap(in _blobAssetReference);
        }

        [ContextMenu("Save")]
        private void _Save()
        {
            var startTime = Time.realtimeSinceStartup;
            var tempQueue = new NativeQueue<int>(Allocator.TempJob);
            NativeArray<FlowFieldCell>[] allFlowFieldCells = new NativeArray<FlowFieldCell>[_gridMap.GridMapData.CellCount];
            for (int i = 0; i < _gridMap.GridMapData.CellCount; i++)
            {
                var flowFieldCells = new NativeArray<FlowFieldCell>(_gridMap.GridMapData.CellCount, Allocator.TempJob);
                new MemsetNativeArray<FlowFieldCell>
                {
                    Source = flowFieldCells, Value = new FlowFieldCell
                    {
                        BestCost = float.MaxValue,
                        Direction = FlowFieldCell.NO_DIRECTION,
                    }
                }.Run(_flowFieldCells.Length);
                var buildFlowField = new BuildFlowField
                {
                    Map = _gridMap,
                    DestinationCellIndex = i,
                    FlowFieldCells = flowFieldCells,
                    CostWeightArray = FlowFieldHelper.CostWeightArray,
                    IndicesToCheck = tempQueue,
                };
                new BuildFlowFieldJob
                {
                    BuildFlowFieldTask = buildFlowField
                }.Run();
                allFlowFieldCells[i] = flowFieldCells;
            }

            tempQueue.Dispose();
            FlowFieldBlobAssetSerializer.Serialize(Path, in _gridMap, allFlowFieldCells);
            foreach (var flowFieldCell in allFlowFieldCells)
            {
                flowFieldCell.Dispose();
            }

            Debug.Log($"build time:{Time.realtimeSinceStartup - startTime}");
        }

        private void _SetCost(float2 point, byte value)
        {
            var index = _gridMap.GridMapData.PointToCellIndex(point);

            if (math.any(index < 0) || math.any(index > _gridMap.GridMapData.GridSize)) return;
            _gridMap.CostCellArray[index] = new CostCell {Value = value};
        }

        public bool GetPoint(out float2 point)
        {
            var ray = Cam.ScreenPointToRay(Input.mousePosition);
            //射线碰到了物体
            if (Physics.Raycast(ray, out var hit))
            {
                point = new float2(hit.point.x, hit.point.z);
                return true;
            }

            point = float2.zero;
            return false;
        }

        private void Update()
        {
            if (EditMode)
            {
                if (Input.GetMouseButton(0))
                {
                    if (!GetPoint(out var point)) return;
                    _SetCost(point, CostCell.IMPASSABLE);
                }
                else if (Input.GetMouseButton(1))
                {
                    if (!GetPoint(out var point)) return;
                    _SetCost(point, 1);
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    //test navigation result
                    if (!GetPoint(out var point)) return;
                    _navigationIndex = FlowFieldHelper.GetFlatIndexFromWorldPos(point, in _gridMap.GridMapData);
                    return;
                    new MemsetNativeArray<FlowFieldCell>
                        {
                            Source = _flowFieldCells, Value = new FlowFieldCell
                            {
                                BestCost = float.MaxValue,
                                Direction = FlowFieldCell.NO_DIRECTION,
                            }
                        }
                        .Run(_flowFieldCells.Length);
                    var tempQueue = new NativeQueue<int>(Allocator.TempJob);
                    //navigation mode
                    var buildFlowField = new BuildFlowField
                    {
                        Map = _gridMap,
                        DestinationCellIndex = _navigationIndex,
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

        private int _navigationIndex;

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

            if (!EditMode)
            {
                Gizmos.color = Color.green;
                //draw flow field direction in cell
                // if (_flowFieldCells.IsCreated)
                // {
                //     for (int i = 0; i < _gridMap.GridMapData.CellCount; i++)
                //     {
                //         if (_flowFieldCells[i].Direction == FlowFieldCell.NO_DIRECTION) continue;
                //         var position = FlowFieldHelper.GetCellPositionFromFlatIndex(i, in _gridMap.GridMapData);
                //         var direction = _flowFieldCells[i].BestDirection;
                //         direction = FlowFieldHelper.CardinalAndIntercardinalDirections[_flowFieldCells[i].Direction];
                //         Gizmos.DrawLine(new Vector3(position.x, 0, position.y),
                //             new Vector3(position.x, 0, position.y) +
                //             0.8f * _gridMap.GridMapData.CellSize * new Vector3(direction.x, 0, direction.y));
                //     }
                // }
                if (_blobAssetReference.IsCreated && _navigationIndex > 0)
                {
                    for (int i = 0; i < _gridMap.GridMapData.CellCount; i++)
                    {
                        var position = FlowFieldHelper.GetCellPositionFromFlatIndex(i, in _gridMap.GridMapData);
                        //_blobAssetReference.Value.FlowFieldCell.
                        var directionByte = _blobAssetReference.Value.FlowFieldCell[_navigationIndex * _gridMap.GridMapData.CellCount + i];
                        if (directionByte == FlowFieldCell.NO_DIRECTION) continue;

                        var direction = FlowFieldHelper.CardinalAndIntercardinalDirections[directionByte];
                        Gizmos.DrawLine(new Vector3(position.x, 0, position.y),
                            new Vector3(position.x, 0, position.y) +
                            0.8f * _gridMap.GridMapData.CellSize * new Vector3(direction.x, 0, direction.y));
                    }
                }
            }
        }
    }
}