using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Triniti.Flock
{
    [UpdateInGroup(typeof(FlockGroup)), UpdateAfter(typeof(SteerSystem)), UpdateBefore(typeof(EndSteerSystem))]
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
            //var memberSorter = GroupFormation.FormationMemberSortInstance;
            var ecb = _endSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            var transformDataMap = GetComponentDataFromEntity<TransformData>(true);
            var steerArriveDataMap = GetComponentDataFromEntity<SteerArriveData>(true);
            //keep formation
            //method 1 use average as group centroid of the formation pivot
            //method 2 use a logic group entity with transform data & steer and find path on  this group logic

            #region simple group move formation algorithm reference from https: //www.gdcvault.com/play/1020832/The-Simplest-AI-Trick-in

            //hack use queue disable parallel restriction for now
            //TODO: process each move command on a single group in a job other than entities.foreach  parallel running
            var queue = new NativeQueue<int>(Allocator.TempJob);
            var formationSlots = GroupFormation.GetDefaultFormationSlots();
            Entities.WithName("GroupStartMoveJob").WithNativeDisableParallelForRestriction(queue)
                .WithReadOnly(transformDataMap).WithReadOnly(formationSlots).WithAll<GroupFlag>()
                .ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<GroupMemberElement> groupMembers,
                    in GroupMoveEventData groupMoveData) =>
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
                    ecb.RemoveComponent<GroupMoveEventData>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();

            #endregion

            queue.Dispose(Dependency);
            _endSimulationEcbSystem.AddJobHandleForProducer(Dependency);
            //return;

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
                        var localToWorld = groupMatrixMap[groupOwner.GroupEntity];
                        var formationSlotWorldPosition =
                            math.mul(localToWorld, new float3(formationLocalPosition.Value, 1)).xy;
                        var curPosition = transformData.Position;
                        var weight = 1; //0.5f;
                        var desireVelocity = (formationSlotWorldPosition - curPosition) * steerData.MaxSpeed * steerData.MaxSpeedRate;
                        desireVelocity = math.normalizesafe(desireVelocity) *
                                         math.min(math.length(desireVelocity), steerData.MaxSpeed * steerData.MaxSpeedRate);

                        var steer = (desireVelocity - steerData.Velocity) * deltaTime * weight;
                        steerData.Steer += steer;

                        var positionInLocalFormation = math.mul(math.inverse(localToWorld), new float3(transformData.Position, 1)).xy;
                        var offset = positionInLocalFormation - formationLocalPosition.Value;
                        steerData.MaxSpeedRate = math.clamp(1 + (-offset.y + math.abs(offset.x)) * 1, 0.7f, 1.3f);
                    })
                .ScheduleParallel();

            #endregion


            groupMatrixMap.Dispose(Dependency);
        }
    }
}