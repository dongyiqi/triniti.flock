using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Triniti.Flock
{
    [UpdateInGroup(typeof(FlockGroup)), UpdateBefore(typeof(FlockSystem))]
    public class SteerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            //seek & arrive
            Entities.WithName("ClearSteeringJob").ForEach((ref FlockSteerData flockSteerData) => { flockSteerData.Steer = float2.zero; })
                .ScheduleParallel();
            var deltaTime = Time.DeltaTime;
            Entities.WithName("SteerArriveJob").ForEach((ref FlockSteerData flockSteerData, in FlockArriveData flockArriveData) =>
            {
                var distanceSq = math.lengthsq(flockArriveData.Goal - flockSteerData.Position);
                if (distanceSq < mathex.EPSILON)
                    return;
                var steer = float2.zero;
                var desiredVelocity = math.normalize(flockArriveData.Goal - flockSteerData.Position) * flockSteerData.MaxSpeed;
                var inArriveRange = distanceSq < flockArriveData.ArriveRadius * flockArriveData.ArriveRadius;
                if (inArriveRange)
                {
                    desiredVelocity *= distanceSq / (flockArriveData.ArriveRadius * flockArriveData.ArriveRadius);
                }

                steer = desiredVelocity - flockSteerData.Velocity;
                steer = math.normalizesafe(steer) *
                        math.min(math.length(steer), flockSteerData.MaxForce * math.@select(deltaTime, 1, inArriveRange));


                //Debug.Log($"steer:{steer} len:{math.length(steer)} curSpeed:{math.length(flockSteerData.Velocity)}");
                flockSteerData.Steer = steer;
            }).ScheduleParallel();
        }
    }
}