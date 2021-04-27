using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Triniti.Flock
{
    //TODO:use TransformData instead of SteerData (position)
    public struct TransformData : IComponentData
    {
        public float2 Position;
        public float2 Forward;
    }

    public struct SteerData : IComponentData
    {
        //public float2 Position;
        //steer read only
        public float2 Velocity;

        ////Here steer means delta of velocity (= force/mass*deltaTime)
        public float2 Steer;

        //static  trust to weight ratio here means
        public float MaxForce;

        public float MaxSpeed;
        public float MaxSpeedRate;
        public float DebugSpeed;
    }

    //direct move
    public struct SteerArriveData : IComponentData
    {
        public float2 Goal;
        public float ArriveRadius;
    }

    [UpdateInGroup(typeof(FlockGroup)), UpdateBefore(typeof(FlockSystem))]
    public class SteerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            //seek & arrive
            Entities.WithName("ClearSteeringJob").ForEach((ref SteerData steerData) => { steerData.Steer = float2.zero; })
                .ScheduleParallel();
            var deltaTime = Time.DeltaTime;

            Entities.WithName("DirectSteerArriveJob").ForEach(
                (ref SteerData steerData, in SteerArriveData steerArriveData, in TransformData transformData) =>
                {
                    //TODO:add flow field path navigation by http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter23_Crowd_Pathfinding_and_Steering_Using_Flow_Field_Tiles.pdf
                    //TODO:get desiredVelocity by goal or flow field navigation
                    var distanceSq = math.lengthsq(steerArriveData.Goal - transformData.Position);
                    if (distanceSq < Math.Constants.EPSILON)
                    {
                        return;
                    }

                    var steer = float2.zero;
                    var desiredVelocity = math.normalize(steerArriveData.Goal - transformData.Position) * steerData.MaxSpeed *
                                          steerData.MaxSpeedRate;
                    var inArriveRange = distanceSq < steerArriveData.ArriveRadius * steerArriveData.ArriveRadius;
                    if (inArriveRange)
                    {
                        desiredVelocity *= distanceSq / (steerArriveData.ArriveRadius * steerArriveData.ArriveRadius);
                    }

                    steer = desiredVelocity - steerData.Velocity;
                    //in arrive range do not multiply deltaTime so the agent could stop right on the destination
                    steer = math.normalizesafe(steer) *
                            math.min(math.length(steer), steerData.MaxForce * math.select(deltaTime, 1, inArriveRange));

                    //Debug.Log($"steer:{steer} len:{math.length(steer)} curSpeed:{math.length(flockSteerData.Velocity)}");
                    steerData.Steer = steer;
                }).ScheduleParallel();

            //steer with flow field
            Entities.WithName("SteerArriveWithFlowFieldJob").ForEach((ref SteerData steerData, in TransformData transformData) => { })
                .ScheduleParallel();
        }
    }
}