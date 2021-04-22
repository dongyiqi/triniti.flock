﻿using Unity.Entities;
using Unity.Mathematics;

namespace Triniti.Flock
{
    public struct FlockEntityData : IComponentData
    {
        //TODO:consider filter in FlockNeighborsData
        public byte Filter; //only check the same filter

        //static cohesion alignment
        public float NeighborRadius;
        public float SeparationRadius;

        //TODO：check if the separation radius is needed or not
    }

    public struct FlockNeighborsData : IComponentData
    {
        //dynamic
        public float2 MeanPosition;
        public float2 MeanVelocity;

        public float2 SeparationVector;
        public int NeighborCount;
    }

    //neighbor's average position & forward
}