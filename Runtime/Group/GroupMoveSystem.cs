using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Triniti.Flock
{
    public struct MemberSortData
    {
        public Entity Entity;
        public float2 LocalPosition;
        public int MaxMatchDestinationIndex;
        public float2 WorldPosition;
        public float2 DestinationPosition;
    }

    [UpdateInGroup(typeof(FlockGroup)), UpdateAfter(typeof(SteerSystem))]
    public class GroupMoveSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _endSimulationEcbSystem;
        private EntityQuery _groupQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _endSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
            _groupQuery = GetEntityQuery(ComponentType.ReadOnly<GroupFlag>());
        }

        //TODO:generate flow field data
        //TODO:update formation index by current members position and destination position
        protected override void OnUpdate()
        {
            var memberSorter = GroupFormation.FormationMemberSortInstance;
            var ecb = _endSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            var transformDataMap = GetComponentDataFromEntity<TransformData>(true);
            var steerArriveDataMap = GetComponentDataFromEntity<SteerArriveData>(true);
            //keep formation
            //method 1 use average as group centroid of the formation pivot
            //method 2 use a logic group entity with transform data & steer and find path on  this group logic 

            #region group average position & forward

            // Entities.WithNativeDisableParallelForRestriction(positionMap).WithNativeDisableContainerSafetyRestriction(positionMap)
            //     .WithReadOnly(positionMap)
            //     .ForEach((Entity entity, ref TransformData transformData, in DynamicBuffer<GroupMemberElement> groupMembers) =>
            //     {
            //         if (groupMembers.Length == 0) return;
            //
            //         var sumPosition = float2.zero;
            //         var sumForward = float2.zero;
            //         for (int i = 0; i < groupMembers.Length; i++)
            //         {
            //             var memberTransformData = positionMap[groupMembers[i]];
            //             sumPosition += memberTransformData.Position;
            //             sumForward += memberTransformData.Forward;
            //         }
            //
            //         transformData.Position = sumPosition / groupMembers.Length;
            //         transformData.Forward = math.normalize(sumForward / groupMembers.Length);
            //     }).ScheduleParallel();

            #endregion

            #region keep formation ASAP

            //temp solution without fully optimized
            // Entities.WithReadOnly(transformDataMap).WithReadOnly(steerArriveDataMap).WithAll<GroupFlag>().ForEach(
            //     (int entityInQueryIndex, in DynamicBuffer<GroupMemberElement> groupMembers) =>
            //     {
            //         var sumDistance = 0f;
            //         NativeArray<float> distanceToGoalArray = new NativeArray<float>(groupMembers.Length, Allocator.Temp);
            //
            //         for (var index = 0; index < groupMembers.Length; index++)
            //         {
            //             var member = groupMembers[index];
            //             if (!steerArriveDataMap.HasComponent(member.Value))
            //                 return;
            //             var distance = math.distance(steerArriveDataMap[member.Value].Goal, transformDataMap[member.Value].Position);
            //             sumDistance += distance;
            //             distanceToGoalArray[index] = distance;
            //         }
            //
            //         //5 1.5rate?
            //         var averageDistance = sumDistance / groupMembers.Length;
            //         for (var index = 0; index < groupMembers.Length; index++)
            //         {
            //             var distanceToAverage = distanceToGoalArray[index] - averageDistance;
            //             var rate = 1 + math.clamp(2f * distanceToAverage, -0.9f, 1.9f);
            //             // ecb.SetComponent(entityInQueryIndex, groupMembers[index].Value, new SteerKeepFormation
            //             // {
            //             //     MaxSpeedRate = rate
            //             // });
            //         }
            //
            //         distanceToGoalArray.Dispose();
            //     }).ScheduleParallel();

            #endregion

            #region simple group move formation algorithm reference from https: //www.gdcvault.com/play/1020832/The-Simplest-AI-Trick-in

            //hack use queue disable parallel restriction for now
            //TODO: process each move command on a single group in a job other than entities.foreach  parallel running
            var queue = new NativeQueue<int>(Allocator.TempJob);
            var formationSlots = GroupFormation.GetDefaultFormationSlots();
            Entities.WithName("GroupStartMoveJob").WithNativeDisableParallelForRestriction(queue)
                .WithReadOnly(transformDataMap).WithReadOnly(formationSlots).WithAll<GroupFlag>()
                .ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<GroupMemberElement> groupMembers,
                    in GroupMoveData groupMoveData) =>
                {
                    var localToWorldMatrix = Math.TrsFloat3x3(groupMoveData.Destination, groupMoveData.Forward);
                    var destinationArray = new NativeArray<float2>(groupMembers.Length, Allocator.Temp);
                    var destinationList = new NativeList<float2>(groupMembers.Length, Allocator.Temp);
                    var validFormation = groupMembers.Length < formationSlots.Length;
                    for (int i = 0; i < groupMembers.Length; i++)
                    {
                        var position = math.mul(localToWorldMatrix, new float3(formationSlots[i], 1)).xy;
                        destinationArray[i] = position;
                        destinationList.Add(formationSlots[i]);
                    }

                    var destinationMatrix = new NativeArray2D<int>(new int2(groupMembers.Length, groupMembers.Length), Allocator.Temp);
                    for (int memberIndex = 0; memberIndex < groupMembers.Length; memberIndex++)
                    {
                        for (int destinationIndex = 0; destinationIndex < groupMembers.Length; destinationIndex++)
                        {
                            var memberPosition = transformDataMap[groupMembers[memberIndex]].Position;
                            var destinationPosition = destinationArray[destinationIndex];
                            destinationMatrix[memberIndex, destinationIndex] =
                                (int) (math.distancesq(memberPosition, destinationPosition) * 10);
                        }
                    }

                    var bestMatchResult = new NativeArray<int>(groupMembers.Length, Allocator.Temp);
                    //may be performance bottleneck here
                    new HungarianAlgorithm
                    {
                        CostMatrix = destinationMatrix,
                        MatchX = bestMatchResult,
                        Queue = queue,
                    }.Run();

                    destinationMatrix.Dispose();
                    for (int i = 0; i < groupMembers.Length; i++)
                    {
                        ecb.AddComponent(entityInQueryIndex, groupMembers[i], new SteerArriveData
                        {
                            Goal = destinationArray[bestMatchResult[i]],
                            ArriveRadius = groupMoveData.ArriveRadius,
                        });
                        ecb.SetComponent(entityInQueryIndex, groupMembers[i], new FormationLocalPosition
                        {
                            Value = formationSlots[bestMatchResult[i]]
                        });
                    }

                    bestMatchResult.Dispose();
                    destinationArray.Dispose();
                    //memberSortData.Dispose();
                    ecb.RemoveComponent<GroupMoveData>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();

            #endregion

            queue.Dispose(Dependency);

            #region keep formation add force on member to go to position in group(slot)

            var deltaTime = Time.DeltaTime;
            var groupCount = _groupQuery.CalculateEntityCount();
            var groupMatrixMap = new NativeHashMap<Entity, float3x3>(groupCount, Allocator.TempJob);
            var groupMatrixMapWriter = groupMatrixMap.AsParallelWriter();
            Entities.WithName("BuildGroupLocalToWorldMatrixJob").WithAll<GroupFlag>().ForEach(
                (Entity groupEntity, in TransformData transformData) =>
                {
                    var matrix = Math.TrsFloat3x3(transformData.Position, transformData.Forward);
                    groupMatrixMapWriter.TryAdd(groupEntity, matrix);
                }).ScheduleParallel();
            Entities.WithReadOnly(groupMatrixMap).ForEach(
                    (ref SteerData steerData, in GroupOwner groupOwner, in TransformData transformData,
                        in FormationLocalPosition formationLocalPosition) =>
                    {
                        var formationSlotWorldPosition =
                            math.mul(groupMatrixMap[groupOwner.GroupEntity], new float3(formationLocalPosition.Value, 1)).xy;
                        var curPosition = transformData.Position;
                        var weight = 0.5f;
                        var desireVelocity = (formationSlotWorldPosition - curPosition) * steerData.MaxSpeed;
                        desireVelocity = math.normalizesafe(desireVelocity) * math.min(math.length(desireVelocity), steerData.MaxSpeed *
                            steerData.MaxSpeedRate);
                        var steer = (desireVelocity - steerData.Velocity) * deltaTime * weight;
                        steerData.Steer += steer;
                        steerData.MaxSpeedRate = math.clamp(1 * math.distance(formationSlotWorldPosition, curPosition), 1, 1.2f);
                    })
                .ScheduleParallel();

            #endregion

            _endSimulationEcbSystem.AddJobHandleForProducer(Dependency);

            groupMatrixMap.Dispose(Dependency);
        }
    }
}