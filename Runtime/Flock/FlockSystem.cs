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
                ComponentType.ReadOnly<FlockNeighborsData>());
        }

        protected override void OnUpdate()
        {
            var flockSetting = _flockSetting;
            var flockEntitiesCount = _flockEntityQuery.CalculateEntityCount();
            var cohesionArray = new NativeArray<float2>(flockEntitiesCount, Allocator.TempJob);
            var alignmentArray = new NativeArray<float2>(flockEntitiesCount, Allocator.TempJob);
            var separationArray = new NativeArray<float2>(flockEntitiesCount, Allocator.TempJob);
            var guideArray = new NativeArray<float2>(flockEntitiesCount, Allocator.TempJob);
            var flockJobHandle = Entities.WithName("CohesionJob").WithAll<FlockEntityData>().ForEach(
                    (int entityInQueryIndex, in FlockNeighborsData flockNeighborsData, in LocalToWorld localToWorld,
                        in FlockNavigationData flockNavigationData) =>
                    {
                        cohesionArray[entityInQueryIndex] =
                            math.normalizesafe(flockNeighborsData.AveragePosition - localToWorld.Position.xz);
                        alignmentArray[entityInQueryIndex] = math.normalizesafe(flockNeighborsData.AverageForward);
                        separationArray[entityInQueryIndex] = flockNeighborsData.SeparationVector;
                        guideArray[entityInQueryIndex] = math.normalizesafe(flockNavigationData.Destination - localToWorld.Position.xz);
                    })
                .ScheduleParallel(Dependency);

            var flockJobBarrier = flockJobHandle;

            //steer
            var deltaTime = Time.DeltaTime;
            var steerJobHandle = Entities.WithName("SteerJob").ForEach(
                (int entityInQueryIndex, ref LocalToWorld localToWorld, in FlockEntityData flockEntityData) =>
                {
                    var forward = flockSetting.CohesionWeight * cohesionArray[entityInQueryIndex]
                                  + flockSetting.AlignmentWeight * alignmentArray[entityInQueryIndex]
                                  + flockSetting.SeparationWeight * separationArray[entityInQueryIndex];
                                    //+ flockSetting.GuideWeight * guideArray[entityInQueryIndex];
                    var forward3D = new float3(forward.x, 0, forward.y);
                    var position = localToWorld.Position + forward3D * flockEntityData.MaxSpeed * deltaTime;

                    var rotation = quaternion.LookRotation(forward3D, math.up());
                    localToWorld.Value = float4x4.TRS(position, rotation, new float3(1, 1, 1));
                }
            ).ScheduleParallel(flockJobBarrier);

            Dependency = JobHandle.CombineDependencies(flockJobBarrier, steerJobHandle);
            var disposeJobHandle = cohesionArray.Dispose(Dependency);
            disposeJobHandle = alignmentArray.Dispose(Dependency);
            disposeJobHandle = separationArray.Dispose(Dependency);
            disposeJobHandle = guideArray.Dispose(Dependency);
            Dependency = disposeJobHandle;
        }
    }
}