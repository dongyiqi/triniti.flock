using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;

/*
 C# hungarian algorithm implementation
 Copyright (C) 2015  Ivan Jurin
 This program is free software; you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation; either version 2 of the License, or
 (at your option) any later version.
 This program is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 GNU General Public License for more details.
 You should have received a copy of the GNU General Public License along
 with this program; if not, write to the Free Software Foundation, Inc.,
 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/
namespace Triniti.Flock
{
    //TODO:use unity jobs and native collection to optimize
    public struct HungarianAlgorithm 
    {
        //input
        public  NativeArray2D<int> CostMatrix;

        //output
        public NativeArray<int> MatchX;

        private int _n; //number of elements
        private int _maxMatch;
        private NativeArray<int> _lx; //labels for workers //minimal value in row
        private NativeArray<int> _ly; //labels for jobs     //minimal value in column
        private NativeArray<bool> _s;

        private NativeArray<bool> _t;

        //private NativeArray<int> _matchX; //vertex matched with x
        private NativeArray<int> _matchY; //vertex matched with y
        private NativeArray<int> _slack;
        private NativeArray<int> _slackx;
        private NativeArray<int> _prev; //memorizing paths

        /// <summary>
        /// 
        /// </summary>
        /// <param name="costMatrix"></param>
        public void Execute()
        {
            Run();
        }

        private void Allocation()
        {
            _n = CostMatrix.Length2D.x;

            _lx = new NativeArray<int>(_n, Allocator.Temp);
            _ly = new NativeArray<int>(_n, Allocator.Temp);

            _s = new NativeArray<bool>(_n, Allocator.Temp);
            _t = new NativeArray<bool>(_n, Allocator.Temp);
            //_matchX = new NativeArray<int>(_n, Allocator.Temp);
            _matchY = new NativeArray<int>(_n, Allocator.Temp);
            _slack = new NativeArray<int>(_n, Allocator.Temp);
            _slackx = new NativeArray<int>(_n, Allocator.Temp);
            _prev = new NativeArray<int>(_n, Allocator.Temp);
        }

        private void Dispose()
        {
            _lx.Dispose();
            _ly.Dispose();
            _s.Dispose();
            _t.Dispose();
            //return value dispose outside or pass in
            //_matchX.Dispose();
            _matchY.Dispose();
            _slack.Dispose();
            _slackx.Dispose();
            _prev.Dispose();
        }

        public void Run()
        {
            Allocation();
            InitMatches();
            if (_n != CostMatrix.Length2D.y)
                return;

            InitLbls();

            _maxMatch = 0;

            InitialMatching();

            var q = new Queue<int>();

            #region augment

            while (_maxMatch != _n)
            {
                q.Clear();

                InitSt();
                //Array.Clear(S,0,n);
                //Array.Clear(T, 0, n);


                //parameters for keeping the position of root node and two other nodes
                var root = 0;
                int x;
                var y = 0;

                //find root of the tree
                for (x = 0; x < _n; x++)
                {
                    if (MatchX[x] != -1) continue;
                    q.Enqueue(x);
                    root = x;
                    _prev[x] = -2;

                    _s[x] = true;
                    break;
                }

                //init slack
                for (var i = 0; i < _n; i++)
                {
                    _slack[i] = CostMatrix[root, i] - _lx[root] - _ly[i];
                    _slackx[i] = root;
                }

                //finding augmenting path
                while (true)
                {
                    while (q.Count != 0)
                    {
                        x = q.Dequeue();
                        var lxx = _lx[x];
                        for (y = 0; y < _n; y++)
                        {
                            if (CostMatrix[x, y] != lxx + _ly[y] || _t[y]) continue;
                            if (_matchY[y] == -1) break; //augmenting path found!
                            _t[y] = true;
                            q.Enqueue(_matchY[y]);

                            AddToTree(_matchY[y], x);
                        }

                        if (y < _n) break; //augmenting path found!
                    }

                    if (y < _n) break; //augmenting path found!
                    UpdateLabels(); //augmenting path not found, update labels

                    for (y = 0; y < _n; y++)
                    {
                        //in this cycle we add edges that were added to the equality graph as a
                        //result of improving the labeling, we add edge (slackx[y], y) to the tree if
                        //and only if !T[y] &&  slack[y] == 0, also with this edge we add another one
                        //(y, yx[y]) or augment the matching, if y was exposed

                        if (_t[y] || _slack[y] != 0) continue;
                        if (_matchY[y] == -1) //found exposed vertex-augmenting path exists
                        {
                            x = _slackx[y];
                            break;
                        }

                        _t[y] = true;
                        if (_s[_matchY[y]]) continue;
                        q.Enqueue(_matchY[y]);
                        AddToTree(_matchY[y], _slackx[y]);
                    }

                    if (y < _n) break;
                }

                _maxMatch++;

                //inverse edges along the augmenting path
                int ty;
                for (int cx = x, cy = y; cx != -2; cx = _prev[cx], cy = ty)
                {
                    ty = MatchX[cx];
                    _matchY[cy] = cx;
                    MatchX[cx] = cy;
                }
            }

            #endregion

            Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitMatches()
        {
            for (var i = 0; i < _n; i++)
            {
                MatchX[i] = -1;
                _matchY[i] = -1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitSt()
        {
            for (var i = 0; i < _n; i++)
            {
                _s[i] = false;
                _t[i] = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //get the minimal value in each row (_lx) and column(_ly)
        private void InitLbls()
        {
            for (var i = 0; i < _n; i++)
            {
                var minRow = CostMatrix[i, 0];
                for (var j = 0; j < _n; j++)
                {
                    if (CostMatrix[i, j] < minRow)
                        minRow = CostMatrix[i, j];
                    if (minRow == 0) break;
                }

                _lx[i] = minRow;
            }

            for (var j = 0; j < _n; j++)
            {
                var minColumn = CostMatrix[0, j] - _lx[0];
                for (var i = 0; i < _n; i++)
                {
                    if (CostMatrix[i, j] - _lx[i] < minColumn)
                        minColumn = CostMatrix[i, j] - _lx[i];
                    if (minColumn == 0) break;
                }

                _ly[j] = minColumn;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateLabels()
        {
            var delta = int.MaxValue;
            ;
            for (var i = 0; i < _n; i++)
                if (!_t[i])
                    if (delta > _slack[i])
                        delta = _slack[i];
            for (var i = 0; i < _n; i++)
            {
                if (_s[i])
                    _lx[i] = _lx[i] + delta;
                if (_t[i])
                    _ly[i] = _ly[i] - delta;
                else _slack[i] = _slack[i] - delta;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToTree(int x, int prevx)
        {
            //x-current vertex, prevx-vertex from x before x in the alternating path,
            //so we are adding edges (prevx, matchX[x]), (matchX[x],x)

            _s[x] = true; //adding x to S
            _prev[x] = prevx;

            var lxx = _lx[x];
            //updateing slack
            for (var y = 0; y < _n; y++)
            {
                if (CostMatrix[x, y] - lxx - _ly[y] >= _slack[y]) continue;
                _slack[y] = CostMatrix[x, y] - lxx - _ly[y];
                _slackx[y] = x;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void InitialMatching()
        {
            for (var x = 0; x < _n; x++)
            {
                for (var y = 0; y < _n; y++)
                {
                    if (CostMatrix[x, y] != _lx[x] + _ly[y] || _matchY[y] != -1)
                        continue;
                    MatchX[x] = y;
                    _matchY[y] = x;
                    _maxMatch++;
                    break;
                }
            }
        }
    }
}