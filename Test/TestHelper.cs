using Unity.Mathematics;
using UnityEngine;

namespace Triniti.Flock.Test
{
    public class TestHelper
    {
        public static bool GetPoint(out float2 point)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            //射线碰到了物体
            if (Physics.Raycast(ray, out var hit))
            {
                point = new float2(hit.point.x, hit.point.z);
                return true;
            }
            point = float2.zero;
            return false;
        }
    }
}