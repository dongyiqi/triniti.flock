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
            var positionMap = GetComponentDataFromEntity<TransformData>(true);

            #region simple group move formation algorithm reference from https: //www.gdcvault.com/play/1020832/The-Simplest-AI-Trick-in

            var seed = UnityEngine.Time.realtimeSinceStartup;
            var formation = GroupFormation.GetDefaultFormationSlots();
            Entities.WithName("GroupStartMoveJob").WithoutBurst().WithReadOnly(positionMap).WithReadOnly(formation).WithAll<GroupFlag>().ForEach(
                    (Entity entity, int entityInQueryIndex, ref DynamicBuffer<GroupMemberElement> groupMembers,
                        in GroupMoveData groupMoveData) =>
                    {
                        var localToWorldMatrix = Math.TrsFloat3x3(groupMoveData.Destination, groupMoveData.Forward);
                        var worldToLocalMatrix = math.inverse(localToWorldMatrix);
                        var destinationArray = new NativeArray<float2>(groupMembers.Length, Allocator.Temp);
                        var destinationList = new NativeList<float2>(groupMembers.Length, Allocator.Temp);
                        var validFormation = groupMembers.Length < formation.Length;
                        for (int i = 0; i < groupMembers.Length; i++)
                        {
                            var position = math.mul(localToWorldMatrix, new float3(formation[i], 1)).xy;
                            destinationArray[i] = position;
                            destinationList.Add(formation[i]);
                        }

                        var memberSortData = new NativeArray<MemberSortData>(groupMembers.Length, Allocator.Temp);
                        for (int i = 0; i < groupMembers.Length; i++)
                        {
                            var memberPosition = positionMap[groupMembers[i]].Position;
                            memberSortData[i] = new MemberSortData
                            {
                                Entity = groupMembers[i],
                                LocalPosition = math.mul(worldToLocalMatrix, new float3(memberPosition, 1)).xy,
                                WorldPosition = memberPosition,
                                DestinationPosition = destinationArray[i]
                            };
                        }

                        var destinationMatrix = new int[groupMembers.Length, groupMembers.Length];
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

                        var bestMatchResult = new HungarianAlgorithm(destinationMatrix).Run();
                        
                        //MW2 regroup algorithm sort by forward first then sort by width
                        //sort by mid to border front to end
                        //memberSortData.Sort(memberSorter);

                        //assign slot from end to front by min distance
                        // for (int i = memberSortData.Length - 1; i >= 0; i--)
                        // {
                        //     var minDistanceIndex = -1;
                        //     var minDistanceSq = float.MaxValue;
                        //     for (int j = 0; j < destinationList.Length; j++)
                        //     {
                        //         var distanceSq = math.distancesq(memberSortData[i].LocalPosition, destinationList[j]);
                        //         if (distanceSq < minDistanceSq)
                        //         {
                        //             minDistanceSq = distanceSq;
                        //             minDistanceIndex = j;
                        //         }
                        //     }
                        //
                        //     var memberSortDataTemp = memberSortData[i];
                        //     memberSortDataTemp.MaxMatchDestinationIndex = minDistanceIndex;
                        //     memberSortDataTemp.DestinationPosition = destinationList[minDistanceIndex];
                        //     memberSortData[i] = memberSortDataTemp;
                        //     destinationList.RemoveAt(minDistanceIndex);
                        // }
                        //
                        // //~
                        // var random = new Random();
                        // random.InitState((uint) seed);
                        //
                        // for (int i = 0; i < groupMembers.Length * groupMembers.Length * groupMembers.Length; i++)
                        // {
                        //     var indexA = random.NextInt(0, groupMembers.Length);
                        //     var indexB = random.NextInt(0, groupMembers.Length);
                        //     var a = memberSortData[indexA];
                        //     var b = memberSortData[indexB];
                        //     var distanceAA = math.distancesq(a.WorldPosition, a.DestinationPosition);
                        //     var distanceAB = math.distancesq(a.WorldPosition, b.DestinationPosition);
                        //     var distanceBB = math.distancesq(b.WorldPosition, b.DestinationPosition);
                        //     var distanceBA = math.distancesq(b.WorldPosition, b.DestinationPosition);
                        //     if (distanceAA + distanceBB > distanceAB + distanceBA)
                        //     {
                        //         var tempDestination = a.DestinationPosition;
                        //         a.DestinationPosition = b.DestinationPosition;
                        //         b.DestinationPosition = tempDestination;
                        //         memberSortData[indexA] = a;
                        //         memberSortData[indexB] = b;
                        //     }
                        // }

                        for (int i = 0; i < memberSortData.Length; i++)
                        {
                            ecb.SetComponent(entityInQueryIndex, memberSortData[i].Entity, new SteerArriveData
                            {
                                //Goal = destinationArray[i],
                                //Goal = memberSortData[i].DestinationPosition,
                                Goal = destinationArray[bestMatchResult[i]],
                                ArriveRadius = 1,
                            });
                        }

                        destinationArray.Dispose();
                        memberSortData.Dispose();
                        ecb.RemoveComponent<GroupMoveData>(entityInQueryIndex, entity);
                    })
                .ScheduleParallel();

            #endregion


            _endSimulationEcbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}