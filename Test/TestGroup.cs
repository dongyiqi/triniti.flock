using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Triniti.Flock.Test
{
    public class TestGroup : MonoBehaviour
    {
        public GameObject FlockEntityPrefab;
        public int MemberCount = 25;

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
                entityManager.SetComponentData(flockEntity, new TransformData {Position = spawnPosition});
                entityManager.SetComponentData(flockEntity, new SteerArriveData {Goal = spawnPosition, ArriveRadius = 1});
                //entityManager.SetComponentData(flockEntity, new Translation {Value = new float3(spawnPosition.x, 0, spawnPosition.y)});
                //entityManager.SetComponentData(flockEntity, new Rotation {Value = transform.rotation});
                entityManager.RemoveComponent<Prefab>(flockEntity);
                entityManager.RemoveComponent<LinkedEntityGroup>(flockEntity);
                //entityManager.SetComponentData(flockEntity, new world);
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
                Gizmos.color = new Color(0, 0, 1 - index / (float) formationSlots.Length);
                index++;
                Gizmos.DrawSphere(new Vector3(position.x, 0, position.y), 0.1f);
            }

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(new Vector3(_curDestination.x, 0, _curDestination.y), 0.5f);
            Gizmos.DrawLine(new Vector3(_curDestination.x, 0, _curDestination.y),
                new Vector3(_curDestination.x, 0, _curDestination.y) + new Vector3(_curForward.x, 0, _curForward.y));
        }

        private float2 _curDestination;
        private float2 _curForward;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                //射线碰到了物体
                if (Physics.Raycast(ray, out var hit))
                {
                    var position = new float2(hit.point.x, hit.point.z);
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