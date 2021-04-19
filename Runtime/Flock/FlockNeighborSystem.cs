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
            var flockEntityVelocity = new NativeArray<float2>(flockCount, Allocator.TempJob);
            Entities.WithName("BruteForceInitializeJob").WithAll<FlockNeighborsData>()
                .ForEach((int entityInQueryIndex, in FlockEntityData flockEntityData, in FlockSteerData flockSteerData) =>
                {
                    flockEntityFilters[entityInQueryIndex] = flockEntityData.Filter;
                    flockEntityPositions[entityInQueryIndex] = flockSteerData.Position;
                    flockEntityVelocity[entityInQueryIndex] = flockSteerData.Velocity;
                }).ScheduleParallel();

            Entities.WithName("BruteForceCalcNeighborsJob").WithReadOnly(flockEntityFilters).WithReadOnly(flockEntityPositions)
                .WithReadOnly(flockEntityVelocity).ForEach((int entityInQueryIndex, ref FlockNeighborsData flockNeighborsData,
                    in FlockEntityData flockEntityData, in LocalToWorld localToWorld) =>
                {
                    var checkPosition = localToWorld.Position.xz;
                    var position = float2.zero;
                    var velocity = float2.zero;
                    var neighborRadiusSq = flockEntityData.NeighborRadius * flockEntityData.NeighborRadius;
                    var separationRadiusSq = flockEntityData.SeparationRadius * flockEntityData.SeparationRadius;
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
                        flockNeighborsData.SeparationVector = separation;
                    }
                })
                .ScheduleParallel();

            flockEntityFilters.Dispose(Dependency);
            flockEntityPositions.Dispose(Dependency);
            flockEntityVelocity.Dispose(Dependency);
        }
    }
}