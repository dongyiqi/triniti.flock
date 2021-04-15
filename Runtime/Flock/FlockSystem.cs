using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Triniti.Flock
{
    public class FlockGroup : ComponentSystemGroup
    {
    }


    [UpdateInGroup(typeof(FlockGroup))]
    public class FlockSystem : SystemBase
    {
        private FlockSetting _flockSetting = new FlockSetting();
        private EntityQuery _flockEntityQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _flockEntityQuery = GetEntityQuery(
                ComponentType.ReadWrite<LocalToWorld>(), ComponentType.ReadOnly<FlockEntityData>(),
                ComponentType.ReadOnly<FlockNavigationData>());
        }


        protected override void OnUpdate()
        {
            var flockSetting = _flockSetting;
            var flockEntitiesCount = _flockEntityQuery.CalculateEntityCount();
            var copyPositions = new NativeArray<float2>(flockEntitiesCount, Allocator.TempJob);
            
            var initialPositionsJobHandle = Entities.WithName("InitialPositionsJob").WithAll<FlockEntityData>().ForEach(
                    (int entityInQueryIndex, in LocalToWorld localToWorld) =>
                    {
                        copyPositions[entityInQueryIndex] = localToWorld.Position.xz;
                    })
                .ScheduleParallel(Dependency);
            var initialJobBarrier = initialPositionsJobHandle;


            //steer
            var steerJobHandle = Entities.WithName("SteerJob").ForEach((int entityInQueryIndex, ref LocalToWorld localToWorld) => { })
                .ScheduleParallel(Dependency);

            Dependency = JobHandle.CombineDependencies(initialPositionsJobHandle, steerJobHandle);
            var disposeJobHandle = copyPositions.Dispose(Dependency);
            Dependency = disposeJobHandle;
        }
    }
}