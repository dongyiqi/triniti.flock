using System;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Triniti.Flock.Test
{
    public class TestFloat3x3Helper : MonoBehaviour
    {
        public float2 Forward;
        public float Theta = 0;
        public float2 Point;
        public float2 Check;

        [ContextMenu("Transform")]
        public void Transform()
        {
            var localToWorld = Math.TrsFloat3x3(Point, math.radians(Theta));
            var localToWorld2 = Math.TrsFloat3x3(Point, math.normalizesafe(Forward));

            var worldToLocal = math.inverse(localToWorld);

            var p1 = math.mul(worldToLocal, new float3(Check, 1));
            var p2 = math.mul(math.inverse(localToWorld2), new float3(Check, 1));

            Debug.Log($"p1:{p1} p2:{p2} delta:{p1 - p2}");
        }
    }
}