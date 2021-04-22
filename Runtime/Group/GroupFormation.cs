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
                var valueA = (a.y) * 16 + a.x * 256;
                var valueB = (b.y) * 16 + b.x * 256;
                return (int) math.round(valueB - valueA);
            }
        }

        public struct FormationMemberSort : IComparer<MemberSortData>
        {
            public int Compare(MemberSortData a, MemberSortData b)
            {
                var valueA = (a.Position.y) * 16 + a.Position.x * 256;
                var valueB = (b.Position.y) * 16 + b.Position.x * 256;
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

        public static NativeArray<float2> GetDefaultFormationSlots() => _formationCache?[0] ?? new NativeArray<float2>();

        //axis-x is the forward direction
        private static void _BuildDefaultFormation()
        {
            //每排6个人
            int2 grid = new int2(2, 6);
            float2 padding = new float2(2, 2);
            var offset = 0.5f * padding * (grid - 1);
            int index = 0;
            var positionSlots = new NativeArray<float2>(grid.x * grid.y, Allocator.Persistent);
            for (int row = 0; row < grid.x; row++)
            {
                for (int column = 0; column < grid.y; column++)
                {
                    var curIndex = new int2(row, column);
                    var position = curIndex * padding - offset;
                    positionSlots[row * grid.y + column] = position;
                }
            }

            positionSlots.Sort(FormationPositionSortInstance);
            _formationCache.Add(0, positionSlots);
        }
    }
}