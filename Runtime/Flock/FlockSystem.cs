using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Triniti.Flock
{
    public class FlockGroup : ComponentSystemGroup
    {
    }


    [UpdateInGroup(typeof(FlockGroup))]
    public class FlockSystem : SystemBase
    {
        private FlockSetting _flockSetting = new FlockSetting(10);
        private EntityQuery _flockEntityQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _flockEntityQuery = GetEntityQuery(
                ComponentType.ReadWrite<LocalToWorld>(), ComponentType.ReadOnly<FlockEntityData>(),
                ComponentType.ReadWrite<SteerData>(), ComponentType.ReadOnly<FlockNeighborsData>());
        }

        protected override void OnUpdate()
        {
            var flockSetting = _flockSetting;
            var flockEntitiesCount = _flockEntityQuery.CalculateEntityCount();
            var cohesionArray = new NativeArray<float2>(flockEntitiesCount, Allocator.TempJob);
            var alignmentArray = new NativeArray<float2>(flockEntitiesCount, Allocator.TempJob);
            var separationArray = new NativeArray<float2>(flockEntitiesCount, Allocator.TempJob);
            //var steerArray = new NativeArray<float2>(flockEntitiesCount, Allocator.TempJob);
            //flock
            var flockJobHandle = Entities.WithName("CohesionJob").WithAll<FlockEntityData>().ForEach(
                    (int entityInQueryIndex, in FlockNeighborsData flockNeighborsData, in TransformData transformData,
                        in SteerData flockNavigationData) =>
                    {
                        if (flockNeighborsData.NeighborCount > 0)
                        {
                            cohesionArray[entityInQueryIndex] =
                                math.normalizesafe(flockNeighborsData.MeanPosition - transformData.Position);
                            alignmentArray[entityInQueryIndex] = math.normalizesafe(flockNeighborsData.MeanVelocity);
                        }

                        separationArray[entityInQueryIndex] = flockNeighborsData.SeparationVector;

                        //steerArray[entityInQueryIndex] = math.normalizesafe(flockNavigationData.Goal - localToWorld.Position.xz);
                    })
                .ScheduleParallel(Dependency);

            var flockJobBarrier = flockJobHandle;

            //drive
            var deltaTime = Time.DeltaTime;
            var flockSteerJobHandle = Entities.WithName("FlockSteerJob").WithAll<FlockEntityData>().ForEach(
                (int entityInQueryIndex, ref TransformData transformData, ref SteerData steerData) =>
                {
                    //steer is the normalized value and should not time weight(for arrive behaviour)
                    var combinedSteering = steerData.Steer;
                    combinedSteering += flockSetting.CohesionWeight * cohesionArray[entityInQueryIndex]
                                        + flockSetting.AlignmentWeight * alignmentArray[entityInQueryIndex]
                                        + flockSetting.SeparationWeight * separationArray[entityInQueryIndex];
                    //combinedSteering = math.normalizesafe(combinedSteering) * math.min(math.length(combinedSteering), steerData.MaxForce);

                    steerData.Steer = combinedSteering;
                }).ScheduleParallel(flockJobBarrier);

            //TODO:add in final commit job or system


            Dependency = JobHandle.CombineDependencies(flockJobBarrier, flockSteerJobHandle);
            var disposeJobHandle = cohesionArray.Dispose(Dependency);
            disposeJobHandle = alignmentArray.Dispose(disposeJobHandle);
            disposeJobHandle = separationArray.Dispose(disposeJobHandle);

            //disposeJobHandle = steerArray.Dispose(Dependency);
            Dependency = disposeJobHandle;
        }
    }
}