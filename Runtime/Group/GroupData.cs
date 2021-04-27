using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Triniti.Flock
{
    public struct GroupMoveData : IComponentData
    {
        public float2 Destination;
        public float2 Forward;
    }

    public struct GroupOwner : IComponentData
    {
        public Entity GroupEntity;
    }
    public struct GroupMemberElement : IBufferElementData
    {
        public static implicit operator Entity(GroupMemberElement e) => e.Value;
        public static implicit operator GroupMemberElement(Entity e) => new GroupMemberElement {Value = e};
        public Entity Value;
    }

    public struct GroupFlag : IComponentData
    {
    }

    //TODO:consider use blobassetreference or isharedcomponentdata
    struct GroupFormationData : ISharedComponentData, IEquatable<GroupFormationData>
    {
        public NativeArray<float2> PositionSlots; //sorted by y

        public bool Equals(GroupFormationData other) => PositionSlots.Equals(other.PositionSlots);

        public override bool Equals(object obj) => obj is GroupFormationData other && Equals(other);

        public override int GetHashCode() => PositionSlots.GetHashCode();
    }
}