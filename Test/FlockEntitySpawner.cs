using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Triniti.Flock.Test
{
    public class FlockEntitySpawner : MonoBehaviour
    {
        [SerializeField] private float SpawnRange;
        [SerializeField] private int SpawnCount;
        [SerializeField] private GameObject FlockEnity;

        private void Awake()
        {
            for (int i = 0; i < SpawnCount; i++)
            {
                var position = new Vector3(Random.Range(-SpawnRange, SpawnRange), 0, Random.Range(-SpawnRange, SpawnRange));
                var rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
                Instantiate(FlockEnity, position, rotation);
            }
        }

        private void OnDrawGizmos()
        {
            
        }
    }
}