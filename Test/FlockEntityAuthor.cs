using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Triniti.Flock;
using Unity.Mathematics;
using Unity.Transforms;

namespace Triniti.Flock.Test
{
    public class FlockEntityAuthor : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] public float SeparationRadius = 1;
        [SerializeField] public float NeighborRadius = 5;
        [SerializeField] public float MaxPower = 1;
        [SerializeField] public float MaxSpeed = 5;
        [SerializeField] public byte _filter = 0;
        public static List<Entity> FlockEntityList = new List<Entity>();

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            FlockEntityList.Add(entity);
            float3 position = transform.position;
            dstManager.AddComponentData(entity, new FlockEntityData
            {
                SeparationRadius = SeparationRadius,
                Filter = _filter,
                NeighborRadius = NeighborRadius,
            });
            dstManager.AddComponentData(entity, new TransformData
            {
                Position = new float2(position.x, position.y),
            });
            dstManager.AddComponentData(entity, new SteerData
            {
                //Position = new float2(position.x, position.y),
                Velocity = math.forward(transform.rotation).xz * Math.Constants.EPSILON,
                MaxSpeed = MaxSpeed,
                MaxForce = MaxPower,
            });
            dstManager.AddComponent<FlockNeighborsData>(entity);

            dstManager.AddComponentData(entity, new SteerArriveData
            {
                Goal = new float2(position.x, position.z),
                ArriveRadius = 1,
            });
            dstManager.SetComponentData(entity, new LocalToWorld
            {
                Value = float4x4.TRS(position, transform.rotation, new float3(1, 1, 1))
            });
        }
    }
}