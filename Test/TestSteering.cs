using System;
using System.Collections;
using System.Collections.Generic;
using Triniti.Flock;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Triniti.Flock.Test
{
    public class TestSteering : MonoBehaviour
    {
        [SerializeField] public float DesinationRange = 5;

        // Start is called before the first frame update
        void Start()
        {
            //create mesh
            var mesh = new Mesh();
            float halfSize = -0.5f;
            mesh.vertices = new[]
            {
                new Vector3(-halfSize, 0, -halfSize), new Vector3(halfSize, 0, -halfSize), new Vector3(halfSize, 0, halfSize),
                new Vector3(-halfSize, 0, halfSize)
            };
            mesh.triangles = new[] {0, 1, 3, 1, 2, 3};
            mesh.uv = new[] {new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0)};
            AssetDatabase.CreateAsset(mesh, "Assets/mesh.asset");
        }

        private Vector3 _destination;

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                //射线碰到了物体
                if (Physics.Raycast(ray, out var hit))
                {
                    _destination = hit.point;
                    //_destination = new Vector3(10,0,0);
                    var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                    for (int i = 0; i < FlockEntityAuthor.FlockEntityList.Count; i++)
                    {
                        entityManager.SetComponentData(FlockEntityAuthor.FlockEntityList[i], new FlockArriveData
                        {
                            Goal = new float2(_destination.x + Random.Range(-DesinationRange, DesinationRange),
                                _destination.z + Random.Range(-DesinationRange, DesinationRange)),
                            ArriveRadius = 1,
                            ForceStopRange = 0.5f,
                        });
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(_destination, 1f);
        }
    }
}