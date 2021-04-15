using Unity.Entities;
using Unity.Mathematics;

namespace Triniti.Flock
{
    struct FlockNavigationData : IComponentData
    {
        public float2 Destination;
    }

    public struct FlockEntityData : IComponentData
    {
        public float BodyRadius;
        public float NeighborRadius;
        public float MaxSpeed;
        public byte Filter; //only check the same filter
    }

    struct FlockNeighborsData : IComponentData
    {
        public float2 AveragePosition;
        public float2 AverageForward;
    }
    //neighbor's average position & forward
}