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
                ComponentType.ReadWrite<FlockSteerData>(), ComponentType.ReadOnly<FlockNeighborsData>());
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
                    (int entityInQueryIndex, in FlockNeighborsData flockNeighborsData, in FlockSteerData flockSteerData,
                        in FlockSteerData flockNavigationData) =>
                    {
                        if (flockNeighborsData.NeighborCount > 0)
                        {
                            cohesionArray[entityInQueryIndex] =
                                math.normalizesafe(flockNeighborsData.MeanPosition - flockSteerData.Position);
                            alignmentArray[entityInQueryIndex] = math.normalizesafe(flockNeighborsData.MeanVelocity);
                        }

                        separationArray[entityInQueryIndex] = flockNeighborsData.SeparationVector;

                        //steerArray[entityInQueryIndex] = math.normalizesafe(flockNavigationData.Goal - localToWorld.Position.xz);
                    })
                .ScheduleParallel(Dependency);

            var flockJobBarrier = flockJobHandle;

            //drive
            var deltaTime = Time.DeltaTime;
            var driveJobHandle = Entities.WithName("DriveJob").ForEach(
                (int entityInQueryIndex, ref FlockSteerData flockSteerData) =>
                {
                    var combinedSteering = flockSetting.CohesionWeight * cohesionArray[entityInQueryIndex]
                                           + flockSetting.AlignmentWeight * alignmentArray[entityInQueryIndex]
                                           + flockSetting.SeparationWeight * separationArray[entityInQueryIndex]
                                           + flockSetting.SteerWeight * flockSteerData.Steer;
                    //+ flockSetting.GuideWeight * steerArray[entityInQueryIndex];
                    //temp hack
                    //flockSteerData.Forward = math.normalizesafe(flockSteerData.Velocity);

                    //move along forward and lerp forward
                    //var curForward = math.normalizesafe(flockSteerData.Velocity);
                    //var nextForward = math.normalizesafe(curForward+ flockSteerData.RotateSpeed * deltaTime * (combinedSteering - curForward));
                    combinedSteering = math.normalizesafe(combinedSteering) *
                                       math.min(math.length(combinedSteering), flockSteerData.MaxForce);

                    var velocity = flockSteerData.Velocity + combinedSteering;
                    velocity = math.normalizesafe(velocity) * math.min(math.length(velocity), flockSteerData.MaxSpeed);
                    flockSteerData.Position += velocity * deltaTime;
                    flockSteerData.Velocity = velocity;
                    flockSteerData.DebugSpeed = math.length(flockSteerData.Velocity);
                }).ScheduleParallel(flockJobBarrier);

            var syncLocalToWorldJob = Entities.WithName("SyncLocalToWorldJob").ForEach(
                (ref LocalToWorld localToWorld, in FlockSteerData flockSteerData) =>
                {
                    var position = new float3(flockSteerData.Position.x, 0, flockSteerData.Position.y);
                    var rotation = quaternion.LookRotationSafe(new float3(flockSteerData.Velocity.x, 0, flockSteerData.Velocity.y),
                        math.up());
                    localToWorld.Value = float4x4.TRS(position, rotation, new float3(1, 1, 1));
                }).ScheduleParallel(driveJobHandle);

            Dependency = JobHandle.CombineDependencies(flockJobBarrier, syncLocalToWorldJob);
            var disposeJobHandle = cohesionArray.Dispose(Dependency);
            disposeJobHandle = alignmentArray.Dispose(disposeJobHandle);
            disposeJobHandle = separationArray.Dispose(disposeJobHandle);

            //disposeJobHandle = steerArray.Dispose(Dependency);
            Dependency = disposeJobHandle;
        }
    }
}