using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;

namespace Triniti.Flock
{
    public static class GroupFormation
    {
        //从第一排的中间开始排序 横排从里到外，纵排从前到后
        public struct FormationPositionSort : IComparer<float2>
        {
            public int Compare(float2 a, float2 b)
            {
                var valueA = (-math.abs(a.x) + a.y * 64);
                var valueB = (-math.abs(b.x) + b.y * 64);
                return (int) math.round(valueB - valueA);
            }
        }

        public struct FormationMemberSort : IComparer<MemberSortData>
        {
            public int Compare(MemberSortData a, MemberSortData b)
            {
                return (int) ((b.LocalPosition.y - a.LocalPosition.y) * 100);
                var valueA = (-(a.LocalPosition.x) + a.LocalPosition.y) * 100;
                var valueB = (-(b.LocalPosition.x) + b.LocalPosition.y) * 100;
                return (int) math.round(valueB - valueA);
            }
        }

        //TODO:Managing coherent groups. Computer Animation and Virtual Worlds
        //refer to http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter20_Hierarchical_Architecture_for_Group_Navigation_Behaviors.pdf
        //1.formation slots sort and sort member list the same way as sort slots and assign (i)th entity to the (i)th slot
        private static Dictionary<int, NativeArray<float2>> _formationCache;
        public static FormationPositionSort FormationPositionSortInstance = new FormationPositionSort();
        public static FormationMemberSort FormationMemberSortInstance = new FormationMemberSort();

        public static void Initialize()
        {
            _formationCache = new Dictionary<int, NativeArray<float2>>();
            _BuildDefaultFormation();
        }

        public static void Dispose()
        {
            foreach (var formation in _formationCache)
            {
                formation.Value.Dispose();
            }

            _formationCache.Clear();
        }

        public static NativeArray<float2> GetDefaultFormationSlots()
        {
            if (_formationCache != null && _formationCache.ContainsKey(0))
                return _formationCache[0];
            return new NativeArray<float2>();
        }

        private static void _BuildDefaultFormation()
        {
            int2 grid = new int2(7, 4);
            float2 padding = new float2(2, 2);
            var offset = 0.5f * padding * (grid - 1);
            int index = 0;
            var positionSlots = new NativeArray<float2>(grid.x * grid.y, Allocator.Persistent);
            for (int row = 0; row < grid.y; row++)
            {
                for (int column = 0; column < grid.x; column++)
                {
                    var curIndex = new int2(column, row);
                    var position = curIndex * padding - offset;
                    positionSlots[row * grid.x + column] = position;
                }
            }

            positionSlots.Sort(FormationPositionSortInstance);
            _formationCache.Add(0, positionSlots);
        }
    }
}