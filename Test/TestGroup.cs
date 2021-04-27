using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Triniti.Flock.Test
{
    public class TestGroup : MonoBehaviour
    {
        public GameObject FlockEntityPrefab;
        public int MemberCount = 25;
        public bool EnableMove;

        private Entity _groupEntity;

        private void Awake()
        {
            GroupFormation.Initialize();
        }

        private void Start()
        {
            //create group

            var world = World.DefaultGameObjectInjectionWorld;
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            _groupEntity = entityManager.CreateEntity();
            entityManager.SetName(_groupEntity, "GroupEntity");
            entityManager.AddComponent<GroupFlag>(_groupEntity);
            entityManager.AddComponent<TransformData>(_groupEntity);

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
                entityManager.AddComponentData(flockEntity, new SteerKeepFormation {MaxSpeedRate = 1});
                memberList.Add(flockEntity);
            }

            ecb.Playback(entityManager);
            ecb.Dispose();
        }

        private void OnDrawGizmos()
        {
            var formationSlots = GroupFormation.GetDefaultFormationSlots();
            var localToWorld = Math.TrsFloat3x3(_curDestination, _curForward);
            if (!formationSlots.IsCreated) return;
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
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
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
                    var position = new float2(hit.point.x, hit.point.z);
                    //position = new float2(1,2);
                    var world = World.DefaultGameObjectInjectionWorld;
                    var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                    _curForward = math.normalizesafe(position - _curDestination);
                    //_curForward = new float2(1, 0);
                    _curDestination = position;
                    entityManager.AddComponentData(_groupEntity, new GroupMoveData
                    {
                        Destination = _curDestination,
                        Forward = _curForward
                    });
                }
            }
        }

        private void OnDestroy()
        {
            GroupFormation.Dispose();
        }
    }
}