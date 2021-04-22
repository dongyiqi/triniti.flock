using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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
        }
#if false
        private void _CreateMesh()
        {
            var mesh = new Mesh();
            float radius = 1;
            mesh.vertices = new[]
            {
                new Vector3(-radius, 0, -radius), new Vector3(radius, 0, -radius), new Vector3(radius, 0, radius),
                new Vector3(-radius, 0, radius)
            };
            mesh.triangles = new[] {0, 1, 3, 1, 2, 3};
            mesh.uv = new[] {new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)};
            UnityEditor.AssetDatabase.CreateAsset(mesh, "Assets/mesh.asset");
        }
#endif
        private Vector3 _destination;

        private float3 _HackGetLocalFormationPosition(int index)
        {
            var row = index / 5;
            var column = index - row * 5;
            return new float3(column * 2, 0, -row * 2);
        }

        private float3 _HackGetWorldFormationPosition(float3 position, float3 forward, int index)
        {
            float4x4 matrix = float4x4.TRS(position, Quaternion.LookRotation(forward, math.up()), new float3(1, 1, 1));
            var localPosition = _HackGetLocalFormationPosition(index);
            return math.mul(matrix, new float4(localPosition, 1)).xyz;
        }

        // Update is called once per frame
        void Update()
        {
            return;
            if (Input.GetMouseButtonDown(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                //射线碰到了物体
                if (Physics.Raycast(ray, out var hit))
                {
                    var forward = math.normalize(hit.point - _destination);

                    _destination = hit.point;
                    //_destination = new Vector3(10,0,0);
                    var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                    for (int i = 0; i < FlockEntityAuthor.FlockEntityList.Count; i++)
                    {
                        var position = new float2(_destination.x + Random.Range(-DesinationRange, DesinationRange),
                            _destination.z + Random.Range(-DesinationRange, DesinationRange));
                        position = _HackGetWorldFormationPosition(_destination, forward, i).xz;
                        entityManager.SetComponentData(FlockEntityAuthor.FlockEntityList[i], new SteerArriveData
                        {
                            Goal = position,
                            ArriveRadius = 1,
                        });
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            //Gizmos.DrawWireSphere(_destination, 1f);
        }
    }
}