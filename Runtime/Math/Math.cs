using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.UIElements;

namespace Triniti.Flock
{
    public class Math
    {
        public static class Constants
        {
            public const float EPSILON = 0.0001f;
        }

        //f should be the normalized float(0,1) equal theta 0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3x3 TrsFloat3x3(float2 t, float2 f) //p:point f:forward
        {
            
            var theta = math.atan2(f.x, f.y);
            return TrsFloat3x3(t, theta);
            return new float3x3(
                new float3(f.x, f.y, 0),
                new float3(-f.y, f.x, 0),
                new float3(t.x, t.y, 1));
        }

        //逆时针旋转
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3x3 TrsFloat3x3(float2 t, float r)
        {
            math.sincos(r, out var sin, out var cos);

            return new float3x3(
                new float3(cos, -sin, 0),
                new float3(sin, cos, 0),
                new float3(t.x, t.y, 1));
        }
        
    }

    public struct LocalToWorld2D
    {
        public float3x3 Value;
        public float2 Forward => new float2(Value.c0.x, Value.c0.y);
        public float2 Position => new float2(Value.c2.x, Value.c2.y);
    }
}