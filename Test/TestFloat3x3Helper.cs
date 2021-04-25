using System;
using Unity.Collections;
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

        [ContextMenu("HungarianAlgorithm")]
        public void TestHungarianAlgorithm()
        {
            var testNativeArray2D = new NativeArray2D<int>(new int2(3, 3), Allocator.Temp)
            {
                [0, 0] = 0, [0, 1] = 2, [0, 2] = 3,
                [1, 0] = 2, [1, 1] = 3, [1, 2] = 0,
                [2, 0] = 2, [2, 1] = 0, [2, 2] = 3
            };


            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    Debug.Log(testNativeArray2D[i, j]);
                }

                Debug.Log("-------");
            }

            testNativeArray2D.Dispose();
            return;

            testNativeArray2D.Dispose();
            var input = new int[3, 3];
            input[0, 0] = 0;
            input[0, 1] = 2;
            input[0, 2] = 3;

            input[1, 0] = 2;
            input[1, 1] = 3;
            input[1, 2] = 0;

            input[2, 0] = 2;
            input[2, 1] = 0;
            input[2, 2] = 3;
            var a = new HungarianAlgorithm(input);
            var result = a.Run();
            Debug.Log(result);
        }
    }
}