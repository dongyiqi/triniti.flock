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

    public class GroupMoveSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _endSimulationEcbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _endSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }

        //TODO:generate flow field data
        //TODO:update formation index by current members position and destination position


        protected override void OnUpdate()
        {
            var memberSorter = GroupFormation.FormationMemberSortInstance;
            var ecb = _endSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            var positionMap = GetComponentDataFromEntity<TransformData>();

            #region group average position & forward

            // Entities.WithNativeDisableParallelForRestriction(positionMap).ForEach(
            //     (Entity entity, ref TransformData transformData, in DynamicBuffer<GroupMemberElement> groupMembers) =>
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
            //         transformData.Forward = sumForward / groupMembers.Length;
            //     }).ScheduleParallel();

            #endregion

            #region simple group move formation algorithm reference from https: //www.gdcvault.com/play/1020832/The-Simplest-AI-Trick-in

            var seed = UnityEngine.Time.realtimeSinceStartup;
            var formation = GroupFormation.GetDefaultFormationSlots();
            Entities.WithName("GroupStartMoveJob").WithoutBurst().WithReadOnly(positionMap).WithReadOnly(formation).WithAll<GroupFlag>()
                .ForEach((Entity entity, int entityInQueryIndex, ref DynamicBuffer<GroupMemberElement> groupMembers,
                    in GroupMoveData groupMoveData) =>
                {
                    var localToWorldMatrix = Math.TrsFloat3x3(groupMoveData.Destination, groupMoveData.Forward);
                    var destinationArray = new NativeArray<float2>(groupMembers.Length, Allocator.Temp);
                    var destinationList = new NativeList<float2>(groupMembers.Length, Allocator.Temp);
                    var validFormation = groupMembers.Length < formation.Length;
                    for (int i = 0; i < groupMembers.Length; i++)
                    {
                        var position = math.mul(localToWorldMatrix, new float3(formation[i], 1)).xy;
                        destinationArray[i] = position;
                        destinationList.Add(formation[i]);
                    }

                    var destinationMatrix = new NativeArray2D<int>(new int2(groupMembers.Length, groupMembers.Length), Allocator.Temp);
                    for (int memberIndex = 0; memberIndex < groupMembers.Length; memberIndex++)
                    {
                        for (int destinationIndex = 0; destinationIndex < groupMembers.Length; destinationIndex++)
                        {
                            var memberPosition = positionMap[groupMembers[memberIndex]].Position;
                            var destinationPosition = destinationArray[destinationIndex];
                            destinationMatrix[memberIndex, destinationIndex] =
                                (int) (math.distancesq(memberPosition, destinationPosition) * 10);
                        }
                    }

                    var bestMatchResult = new NativeArray<int>(groupMembers.Length, Allocator.Temp);
                    new HungarianAlgorithm
                    {
                        CostMatrix = destinationMatrix,
                        MatchX = bestMatchResult,
                    }.Run();
                    destinationMatrix.Dispose();
                    for (int i = 0; i < groupMembers.Length; i++)
                    {
                        ecb.SetComponent(entityInQueryIndex, groupMembers[i], new SteerArriveData
                        {
                            Goal = destinationArray[bestMatchResult[i]],
                            ArriveRadius = 0.5f,
                        });
                    }

                    bestMatchResult.Dispose();
                    destinationArray.Dispose();
                    //memberSortData.Dispose();
                    ecb.RemoveComponent<GroupMoveData>(entityInQueryIndex, entity);
                })
                .ScheduleParallel();

            #endregion


            _endSimulationEcbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}