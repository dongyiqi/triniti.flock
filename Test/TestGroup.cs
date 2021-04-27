using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Triniti.Flock.Test
{
    public class TestGroup : MonoBehaviour
    {
        public GameObject FlockEntityPrefab;
        public int MemberCount = 25;
        public bool EnableMove;

        private Entity _groupEntity;

        //use constant speed
        private void Awake()
        {
            GroupFormation.Initialize();
        }

        private void Start()
        {
            var flockData = FlockEntityPrefab.GetComponent<FlockEntityAuthor>();

            //create group
            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            _groupEntity = entityManager.CreateEntity();
            entityManager.SetName(_groupEntity, "GroupEntity");
            entityManager.AddComponent<GroupFlag>(_groupEntity);
            entityManager.AddComponent<TransformData>(_groupEntity);
            entityManager.AddComponentData(_groupEntity, new SteerData
            {
                MaxSpeed = flockData.MaxSpeed,
                MaxForce = flockData.MaxPower * 100,
                MaxSpeedRate = 1,
            });
            var memberList = ecb.AddBuffer<GroupMemberElement>(_groupEntity);
            var convertSetting = new GameObjectConversionSettings
            {
                DestinationWorld = world,
            };
            var formationSlots = GroupFormation.GetDefaultFormationSlots();

            for (int i = 0; i < MemberCount; i++)
            {
                //spawn entity
                var flockEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(FlockEntityPrefab, convertSetting);
                entityManager.SetName(flockEntity, $"FlockEntity:{i}");
                var spawnPosition = formationSlots[i];
                //spawnPosition = new float2(Random.Range(-10, 10), Random.Range(-10, 10));
                entityManager.SetComponentData(flockEntity, new TransformData {Position = spawnPosition});
                //entityManager.SetComponentData(flockEntity, new SteerArriveData {Goal = spawnPosition, ArriveRadius = 1});
                entityManager.RemoveComponent<Prefab>(flockEntity);
                entityManager.RemoveComponent<LinkedEntityGroup>(flockEntity);
                entityManager.AddComponentData(flockEntity, new GroupOwner {GroupEntity = _groupEntity});
                entityManager.AddComponentData(flockEntity, new FormationLocalPosition {Value = formationSlots[i]});
                memberList.Add(flockEntity);
            }

            ecb.Playback(entityManager);
            ecb.Dispose();
        }

        private void OnDrawGizmos()
        {
            var formationSlots = GroupFormation.GetDefaultFormationSlots();
            if (!formationSlots.IsCreated) return;

            Gizmos.color = Color.yellow;
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (_groupEntity != Entity.Null)
            {
                var transformData = entityManager.GetComponentData<TransformData>(_groupEntity);
                var position = new Vector3(transformData.Position.x, 0, transformData.Position.y);
                var forward = new Vector3(transformData.Forward.x, 0, transformData.Forward.y);
                Gizmos.DrawWireSphere(position, 1);

                Gizmos.DrawLine(position, position + forward);
            }


            var localToWorld = Math.TrsFloat3x3(_curDestination, _curForward);
            int index = 0;
            foreach (var slot in formationSlots)
            {
                var position = math.mul(localToWorld, new float3(slot, 1));
                Gizmos.color = new Color(1 - index / (float) formationSlots.Length, 0, 0);
                index++;
                Gizmos.DrawSphere(new Vector3(position.x, 0, position.y), 0.1f);
            }

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(new Vector3(_curDestination.x, 0, _curDestination.y), 0.5f);
            Gizmos.DrawLine(new Vector3(_curDestination.x, 0, _curDestination.y),
                new Vector3(_curDestination.x, 0, _curDestination.y) + new Vector3(_curForward.x, 0, _curForward.y));

            //draw move line
            var world = World.DefaultGameObjectInjectionWorld;
            var members = entityManager.GetBuffer<GroupMemberElement>(_groupEntity);
            foreach (var member in members)
            {
                if (!entityManager.HasComponent<SteerArriveData>(member)) continue;
                var position = entityManager.GetComponentData<TransformData>(member).Position;
                var destination = entityManager.GetComponentData<SteerArriveData>(member).Goal;
                Gizmos.DrawLine(new Vector3(position.x, 0, position.y), new Vector3(destination.x, 0, destination.y));
            }
        }

        private float2 _curDestination;
        private float2 _curForward;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && EnableMove)
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                //射线碰到了物体
                if (Physics.Raycast(ray, out var hit))
                {
                    float arriveRadius = 0.5f;
                    var position = new float2(hit.point.x, hit.point.z);
                    //position = new float2(1,2);
                    var world = World.DefaultGameObjectInjectionWorld;
                    var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                    var groupPositionNow = entityManager.GetComponentData<TransformData>(_groupEntity).Position;
                    _curForward = math.normalizesafe(position - groupPositionNow);
                    //_curForward = new float2(1, 0);
                    _curDestination = position;
                    entityManager.AddComponentData(_groupEntity, new GroupMoveData
                    {
                        Destination = _curDestination,
                        Forward = _curForward,
                        ArriveRadius = arriveRadius,
                    });
                    //make group to move
                    entityManager.AddComponentData(_groupEntity, new SteerArriveData
                    {
                        Goal = position,
                        ArriveRadius = arriveRadius,
                    });
                }
            }

            //move squad
        }

        private float2 _position;
        private float2 _forward;

        private void OnDestroy()
        {
            GroupFormation.Dispose();
        }
    }
}