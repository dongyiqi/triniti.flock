using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Triniti.Flock
{
    [UpdateInGroup(typeof(FlockGroup)), UpdateBefore(typeof(FlockSystem))]
    public class FlockNeighborBruteForceSystem : SystemBase
    {
        private EntityQuery _flockEntityQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _flockEntityQuery = GetEntityQuery(
                ComponentType.ReadWrite<FlockNeighborsData>(),
                ComponentType.ReadOnly<FlockEntityData>(),
                ComponentType.ReadOnly<LocalToWorld>());
        }

        protected override void OnUpdate()
        {
            var flockCount = _flockEntityQuery.CalculateEntityCount();
            var flockEntityFilters = new NativeArray<byte>(flockCount, Allocator.TempJob);
            var flockEntityPositions = new NativeArray<float2>(flockCount, Allocator.TempJob);
            var flockEntityForwards = new NativeArray<float2>(flockCount, Allocator.TempJob);
            Entities.WithName("BruteForceInitializeJob").WithAll<FlockNeighborsData>()
                .ForEach((int entityInQueryIndex, in FlockEntityData flockEntityData, in LocalToWorld localToWorld) =>
                {
                    flockEntityFilters[entityInQueryIndex] = flockEntityData.Filter;
                    flockEntityPositions[entityInQueryIndex] = localToWorld.Position.xz;
                    flockEntityForwards[entityInQueryIndex] = localToWorld.Forward.xz;
                }).ScheduleParallel();

            Entities.WithName("BruteForceCalcNeighborsJob").WithReadOnly(flockEntityFilters).WithReadOnly(flockEntityPositions)
                .WithReadOnly(flockEntityForwards)
                .ForEach((int entityInQueryIndex, ref FlockNeighborsData flockNeighborsData, in FlockEntityData flockEntityData,
                    in LocalToWorld localToWorld) =>
                {
                    var checkPosition = localToWorld.Position.xz;
                    var position = float2.zero;
                    var forward = float2.zero;
                    var neighborRadiusSq = flockEntityData.NeighborRadius * flockEntityData.NeighborRadius;
                    var separationRadiusSq = flockEntityData.SeparationRadius * flockEntityData.SeparationRadius;
                    var neighborsCount = 0;
                    var separation = float2.zero;
                    for (var i = 0; i < flockCount; i++)
                    {
                        if (entityInQueryIndex == i) continue;

                        var neighborPosition = flockEntityPositions[i];
                        if (flockEntityData.Filter != flockEntityFilters[i]) continue;
                        var distance = math.distancesq(neighborPosition, checkPosition);
                        if (distance < neighborRadiusSq)
                        {
                            neighborsCount++;
                            position += neighborPosition;
                            forward += flockEntityForwards[i];
                        }


                        if (distance < separationRadiusSq)
                        {
                            separation += (checkPosition - neighborPosition);
                        }
                    }

                    //remove self
                    flockNeighborsData.NeighborCount = neighborsCount;
                    if (neighborsCount > 0)
                    {
                        flockNeighborsData.AveragePosition = position / neighborsCount;
                        flockNeighborsData.AverageForward = forward / neighborsCount;
                        flockNeighborsData.SeparationVector = separation;
                    }
                })
                .ScheduleParallel();

            flockEntityFilters.Dispose(Dependency);
            flockEntityPositions.Dispose(Dependency);
            flockEntityForwards.Dispose(Dependency);
        }
    }
}