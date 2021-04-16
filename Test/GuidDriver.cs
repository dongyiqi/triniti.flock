using System.Collections;
using System.Collections.Generic;
using Triniti.Flock;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class GuidDriver : MonoBehaviour
{
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

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //射线碰到了物体
            if (Physics.Raycast(ray, out var hit))
            {
                var position = hit.point;
                var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                for (int i = 0; i < FlockEntityAuthor.FlockEntityList.Count; i++)
                {
                    entityManager.SetComponentData(FlockEntityAuthor.FlockEntityList[i], new FlockNavigationData
                    {
                        Destination = new float2(position.x, position.z)
                    });
                }
            }
        }
    }
}