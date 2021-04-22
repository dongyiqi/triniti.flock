using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Triniti.Flock
{
    public struct MemberSortData
    {
        public Entity Entity;

        public float2 Position;
        public float Distance;
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

        public struct FormationMemberSortDistance : IComparer<MemberSortData>
        {
            public int Compare(MemberSortData a, MemberSortData b)
            {
                return (int) math.round((a.Distance - b.Distance) * 100);
            }
        }

        protected override void OnUpdate()
        {
            var memberSorter = GroupFormation.FormationMemberSortInstance;
            //var memberSorter = new FormationMemberSortDistance();
            var ecb = _endSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
            var positionMap = GetComponentDataFromEntity<TransformData>(true);

            //test only hack
            var formation = GroupFormation.GetDefaultFormationSlots();
            Entities.WithName("GroupStartMoveJob").WithReadOnly(positionMap).WithReadOnly(formation).WithAll<GroupFlag>().ForEach(
                    (Entity entity, int entityInQueryIndex, ref DynamicBuffer<GroupMemberElement> groupMembers,
                        in GroupMoveData groupMoveData) =>
                    {
                        var localToWorldMatrix = Math.TrsFloat3x3(groupMoveData.Destination, groupMoveData.Forward);
                        var worldToLocalMatrix = math.inverse(localToWorldMatrix);
                        var destinationArray = new NativeArray<float2>(groupMembers.Length, Allocator.Temp);
                        var validFormation = groupMembers.Length < formation.Length;
                        for (int i = 0; i < groupMembers.Length; i++)
                        {
                            destinationArray[i] = math.mul(localToWorldMatrix, new float3(formation[i], 1)).xy;
                        }

                        var memberSortData = new NativeArray<MemberSortData>(groupMembers.Length, Allocator.Temp);
                        for (int i = 0; i < groupMembers.Length; i++)
                        {
                            var memberPosition = positionMap[groupMembers[i]].Position;
                            memberSortData[i] = new MemberSortData
                            {
                                Entity = groupMembers[i],
                                Position = math.mul(worldToLocalMatrix, new float3(memberPosition, 1)).xy,
                                Distance = math.distancesq(memberPosition, groupMoveData.Destination)
                            };
                        }

                        memberSortData.Sort(memberSorter);

                        for (int i = 0; i < memberSortData.Length; i++)
                        {
                            ecb.SetComponent(entityInQueryIndex, memberSortData[i].Entity, new SteerArriveData
                            {
                                Goal = destinationArray[i],
                                ArriveRadius = 1,
                            });
                        }

                        destinationArray.Dispose();
                        memberSortData.Dispose();
                        ecb.RemoveComponent<GroupMoveData>(entityInQueryIndex, entity);
                    })
                .ScheduleParallel();
            _endSimulationEcbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}