using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Triniti.Flock
{
    [UpdateInGroup(typeof(FlockGroup)), UpdateAfter(typeof(SteerSystem))]
    public class EndSteerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var deltaTime = Time.DeltaTime;
            Entities.WithName("SteerJob").ForEach((ref TransformData transformData, ref SteerData steerData) =>
            {
                steerData.Steer = math.normalizesafe(steerData.Steer) * math.min(math.length(steerData.Steer), steerData.MaxForce);
                var velocity = steerData.Velocity + steerData.Steer;
                velocity = math.normalizesafe(velocity) * math.min(math.length(velocity), steerData.MaxSpeed * steerData.MaxSpeedRate);
                transformData.Position += velocity * deltaTime;
                transformData.Forward = math.normalizesafe(velocity);
                steerData.Velocity = velocity;
                
                steerData.DebugSpeed = math.length(steerData.Velocity);
            }).ScheduleParallel();
            Entities.WithName("KeepDestinationForward").WithAll<KeepDestinationForward>()
                .ForEach((ref TransformData transformData, in SteerArriveData steerArriveData) =>
                {
                    transformData.Forward = math.normalizesafe(steerArriveData.Goal - transformData.Position);
                }).ScheduleParallel();
            Entities.WithName("SyncLocalToWorldJob").ForEach((ref LocalToWorld localToWorld, in TransformData transformData) =>
            {
                var position = new float3(transformData.Position.x, 0, transformData.Position.y);
                var rotation = quaternion.LookRotationSafe(new float3(transformData.Forward.x, 0, transformData.Forward.y), math.up());
                localToWorld.Value = float4x4.TRS(position, rotation, new float3(1, 1, 1));
            }).ScheduleParallel();
        }
    }
}