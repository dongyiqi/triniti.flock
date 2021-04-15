using Unity.Entities;
using UnityEngine;
using Triniti.Flock;

public class FlockEntityAuthor : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField] public float Radius = 1;
    [SerializeField] public float NeighborRadius = 5;
    [SerializeField] public float MaxSpeed = 1;
    private byte _filter = 0;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new FlockEntityData
        {
            BodyRadius = Radius,
            NeighborRadius = NeighborRadius,
            MaxSpeed = MaxSpeed,
            Filter = _filter
        });
    }
}