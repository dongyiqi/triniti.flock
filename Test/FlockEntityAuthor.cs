using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Triniti.Flock;
using Unity.Mathematics;
using Unity.Transforms;

public class FlockEntityAuthor : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField] private float SeparationRadius = 1;
    [SerializeField] private float NeighborRadius = 5;
    [SerializeField] private float MaxSpeed = 1;
    [SerializeField] private byte _filter = 0;
    public static List<Entity> FlockEntityList = new List<Entity>();

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        FlockEntityList.Add(entity);

        dstManager.AddComponentData(entity, new FlockEntityData
        {
            SeparationRadius = SeparationRadius,
            MaxSpeed = MaxSpeed,
            Filter = _filter,
            NeighborRadius = NeighborRadius,
        });
        dstManager.AddComponent<FlockNeighborsData>(entity);
        var position = transform.position;
        dstManager.AddComponentData(entity, new FlockNavigationData
        {
            Destination = new float2(position.x, position.z)
        });
        dstManager.SetComponentData(entity, new LocalToWorld
        {
            Value = float4x4.TRS(transform.position, transform.rotation, new float3(1, 1, 1))
        });
    }
}