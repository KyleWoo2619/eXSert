using System;

namespace EnemyBehavior.Pathfinding
{
    internal sealed class BinaryHeap<T>
    {
        private struct Node { public float Key; public T Item; }
        private Node[] _data;
        private int _count;
        public int Count => _count;

        public BinaryHeap(int capacity = 256)
        {
            _data = new Node[Math.Max(16, capacity)];
            _count = 0;
        }

        public void Clear() { _count = 0; }

        public void Push(float key, T item)
        {
            if (_count == _data.Length) Array.Resize(ref _data, _data.Length << 1);
            int i = _count++;
            _data[i].Key = key; _data[i].Item = item;
            SiftUp(i);
        }

        public T Pop(out float key)
        {
            var top = _data[0];
            key = top.Key;
            int last = --_count;
            if (last >= 0)
            {
                _data[0] = _data[last];
                SiftDown(0);
            }
            return top.Item;
        }

        private void SiftUp(int i)
        {
            var n = _data[i];
            int p = (i - 1) >> 1;
            while (i > 0 && n.Key < _data[p].Key)
            {
                _data[i] = _data[p];
                i = p; p = (i - 1) >> 1;
            }
            _data[i] = n;
        }

        private void SiftDown(int i)
        {
            var n = _data[i];
            int half = _count >> 1;
            while (i < half)
            {
                int c = (i << 1) + 1;
                int r = c + 1;
                if (r < _count && _data[r].Key < _data[c].Key) c = r;
                if (_data[c].Key >= n.Key) break;
                _data[i] = _data[c];
                i = c;
            }
            _data[i] = n;
        }
    }
}
