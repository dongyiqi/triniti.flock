using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Triniti.Flock
{
    //component data on member
    public struct KeepFormationSteerWeight : IComponentData
    {
        public float Value;
    }

    public struct FormationLocalPosition : IComponentData
    {
        public float2 Value;
    }

    public struct GroupOwner : IComponentData
    {
        public Entity GroupEntity;
    }
    
    public struct GroupMoveEventData : IComponentData
    {
        public float2 Destination;
        public float2 Forward;
        public float ArriveRadius;
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