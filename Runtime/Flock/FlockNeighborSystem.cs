using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Triniti.Flock
{
    //to implement obstacle avoid instead of using separation alignment cohesion...
    [UpdateInGroup(typeof(FlockGroup)), UpdateBefore(typeof(FlockSystem))]
    public class FlockNeighborsSystem : SystemBase
    {
        private EntityQuery _flockEntityQuery;

        enum NeighborOptimizer
        {
            BruteForce,
            Grid,
            QuadTree
        }

        private NeighborOptimizer _neighborOptimizer = NeighborOptimizer.Grid;

        protected override void OnCreate()
        {
            base.OnCreate();
            _flockEntityQuery = GetEntityQuery(ComponentType.ReadWrite<NeighborsData>(), ComponentType.ReadOnly<FlockEntityData>(),
                ComponentType.ReadOnly<TransformData>());
        }

        protected override void OnUpdate()
        {
            if (_neighborOptimizer == NeighborOptimizer.BruteForce)
                _BruteForceCalcNeighbors();
            else if (_neighborOptimizer == NeighborOptimizer.Grid)
                _GridCalcNeighbors();
        }

        private void _BruteForceCalcNeighbors()
        {
            var flockCount = _flockEntityQuery.CalculateEntityCount();
            var flockEntityFilters = new NativeArray<byte>(flockCount, Allocator.TempJob);
            var flockEntityPositions = new NativeArray<float2>(flockCount, Allocator.TempJob);
            var flockEntityVelocity = new NativeArray<float2>(flockCount, Allocator.TempJob);
            Entities.WithName("BruteForceInitializeJob").WithAll<NeighborsData>()
                .ForEach((int entityInQueryIndex, in FlockEntityData flockEntityData, in SteerData steerData,
                    in TransformData transformData) =>
                {
                    flockEntityFilters[entityInQueryIndex] = flockEntityData.Filter;
                    flockEntityPositions[entityInQueryIndex] = transformData.Position;
                    flockEntityVelocity[entityInQueryIndex] = steerData.Velocity;
                }).ScheduleParallel();
            Entities.WithName("BruteForceCalcNeighborsJob").WithReadOnly(flockEntityFilters).WithReadOnly(flockEntityPositions)
                .WithReadOnly(flockEntityVelocity).ForEach((int entityInQueryIndex, ref NeighborsData flockNeighborsData,
                    in SteerData flockSteerData, in FlockEntityData flockEntityData, in TransformData transformData) =>
                {
                    var checkPosition = transformData.Position;
                    var position = float2.zero;
                    var velocity = float2.zero;
                    var neighborRadiusSq = flockNeighborsData.NeighborRadius * flockNeighborsData.NeighborRadius;
                    var separationRadiusSq = flockNeighborsData.SeparationRadius * flockNeighborsData.SeparationRadius;
                    var neighborsCount = 0;
                    var separation = float2.zero;
                    for (var i = 0; i < flockCount; i++)
                    {
                        if (entityInQueryIndex == i) continue;

                        var neighborPosition = flockEntityPositions[i];
                        if (flockEntityData.Filter != flockEntityFilters[i]) continue;
                        var distanceSq = math.distancesq(neighborPosition, checkPosition);
                        if (distanceSq < neighborRadiusSq)
                        {
                            neighborsCount++;
                            position += neighborPosition;
                            velocity += flockEntityVelocity[i];
                        }

                        //互斥和距离成反比 normalized dir = (checkPosition - neighborPosition) / distance
                        // normalized dir / distance = (checkPosition - neighborPosition) / distancesq
                        if (distanceSq < separationRadiusSq)
                        {
                            separation += (checkPosition - neighborPosition) / distanceSq;
                        }
                    }

                    //remove self
                    flockNeighborsData.NeighborCount = neighborsCount;
                    if (neighborsCount > 0)
                    {
                        flockNeighborsData.MeanPosition = position / neighborsCount;
                        flockNeighborsData.MeanVelocity = velocity / neighborsCount;
                        flockNeighborsData.SeparationVector = math.normalizesafe(separation) *
                                                              math.min(math.length(separation), flockSteerData.MaxForce);
                    }
                })
                .ScheduleParallel();
            var disposeJobHandle = flockEntityFilters.Dispose(Dependency);
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, flockEntityPositions.Dispose(disposeJobHandle));
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, flockEntityVelocity.Dispose(disposeJobHandle));
            Dependency = disposeJobHandle;
        }

        struct NeighborCache
        {
            public byte Filter;
            public float2 Position;
            public float2 Velocity;
        }

        private void _GridCalcNeighbors()
        {
            //make sure _flockEntityQuery.count == every Entities.foreach
            var flockCount = _flockEntityQuery.CalculateEntityCount();
            float cellRadius = 5;
            var neighborCacheArray = new NativeArray<NeighborCache>(flockCount, Allocator.TempJob);

            Entities.WithName("GridCalcNeighborsInitializeJob").WithAll<NeighborsData>()
                .ForEach((int entityInQueryIndex, in FlockEntityData flockEntityData, in SteerData steerData,
                    in TransformData transformData) =>
                {
                    neighborCacheArray[entityInQueryIndex] = new NeighborCache
                    {
                        Filter = flockEntityData.Filter,
                        Position = transformData.Position,
                        Velocity = steerData.Velocity,
                    };
                }).ScheduleParallel();

            var hashMap = new NativeMultiHashMap<int, int>(flockCount, Allocator.TempJob);
            var cellHashIndexArray = new NativeArray<int>(flockCount, Allocator.TempJob);
            //var cellCountArray = new NativeArray<int>(flockCount, Allocator.TempJob);
            //new MemsetNativeArray<int> {Source = cellCountArray, Value = 1,}.Schedule(flockCount, 64, Dependency);
            var parallelHashMap = hashMap.AsParallelWriter();
            Entities.WithName("HashPositionToGridIndexJob").WithAll<NeighborsData>().ForEach(
                (int entityInQueryIndex, in TransformData transformData) =>
                {
                    //ignore radius of flock entity
                    var hash = (int) math.hash(new int2(math.floor(transformData.Position / cellRadius)));
                    parallelHashMap.Add(hash, entityInQueryIndex);
                    cellHashIndexArray[entityInQueryIndex] = hash;
                }).ScheduleParallel();

            Entities.WithName("FindNeighborDataInSameCellJob").WithReadOnly(cellHashIndexArray).WithReadOnly(hashMap)
                .WithReadOnly(neighborCacheArray)
                .ForEach(
                    (int entityInQueryIndex, ref NeighborsData neighborsData, in TransformData transformData, in SteerData steerData) =>
                    {
                        var myCache = neighborCacheArray[entityInQueryIndex];
                        var cellHashIndex = cellHashIndexArray[entityInQueryIndex];
                        var aabbCheckRange = new float2(neighborsData.SeparationRadius, neighborsData.SeparationRadius);
                        if (!hashMap.TryGetFirstValue(cellHashIndex, out var queryIndex, out var iterator))
                            return;
                        var sumPosition = float2.zero;
                        var sumVelocity = float2.zero;
                        var neighborsCount = 0;

                        _ProcessNeighborData(queryIndex);

                        while (hashMap.TryGetNextValue(out queryIndex, ref iterator))
                        {
                            _ProcessNeighborData(queryIndex);
                        }

                        if (neighborsCount > 0)
                        {
                            neighborsData.MeanPosition = sumPosition / neighborsCount;

                            var separation = transformData.Position - sumPosition / neighborsCount;
                            separation
                                /= math.length(separation);
                            neighborsData.SeparationVector =
                                math.normalizesafe(separation) * math.min(math.length(separation), steerData.MaxForce);
                        }

                        void _ProcessNeighborData(int index)
                        {
                            var neighborCache = neighborCacheArray[index];
                            if (neighborCache.Filter != myCache.Filter || index == entityInQueryIndex)
                                return;
                            var deltaPosition = myCache.Position - neighborCache.Position;
                            if (math.any(math.abs(deltaPosition) > aabbCheckRange))
                                return;
                            if (math.lengthsq(deltaPosition) > aabbCheckRange.x * aabbCheckRange.x)
                                return;
                            sumPosition += neighborCache.Position;
                            sumVelocity += neighborCache.Velocity;
                            neighborsCount++;
                        }
                    }).ScheduleParallel();

            //var parallelHashMap = hashMap.AsParallelWriter();
            var disposeJobHandle = hashMap.Dispose(Dependency);
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, neighborCacheArray.Dispose(disposeJobHandle));
            disposeJobHandle = JobHandle.CombineDependencies(disposeJobHandle, cellHashIndexArray.Dispose(disposeJobHandle));

            Dependency = disposeJobHandle;
        }
    }
}