using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Collections.Generic;
using Microsoft.VSDiagnostics;

// Benchmark for a small array-backed binary heap implementation used for A* open sets.
// Exercises push/pop performance to guide choosing a heap implementation before optimizing A*.
[SimpleJob(RuntimeMoniker.Net472)]
[CPUUsageDiagnoser]
public class BinaryHeapBenchmark
{
    const int N = 100000;
    private int[] values;
    [GlobalSetup]
    public void Setup()
    {
        var rnd = new Random(42);
        values = new int[N];
        for (int i = 0; i < N; i++)
            values[i] = rnd.Next();
    }

    [Benchmark]
    public void PushAllThenPopAll()
    {
        var h = new BinaryHeap(N);
        for (int i = 0; i < N; i++)
            h.Push(i, values[i]);
        for (int i = 0; i < N; i++)
            h.Pop();
    }

    [Benchmark]
    public void PushPopInterleaved()
    {
        var h = new BinaryHeap(N / 4);
        int idx = 0;
        for (int i = 0; i < N; i++)
        {
            h.Push(i, values[i]);
            if ((i & 3) == 0 && h.Count > 0)
                h.Pop();
        }

        while (h.Count > 0)
            h.Pop();
    }

    // Minimal array-backed binary heap implementation for benchmarking.
    public class BinaryHeap
    {
        private int[] items;
        private float[] scores;
        private int count;
        public BinaryHeap(int capacity)
        {
            items = new int[Math.Max(capacity, 8)];
            scores = new float[items.Length];
            count = 0;
        }

        public int Count => count;

        public void Push(int item, float score)
        {
            if (count >= items.Length)
            {
                int newCap = Math.Max(items.Length * 2, 8);
                Array.Resize(ref items, newCap);
                Array.Resize(ref scores, newCap);
            }

            int i = count++;
            items[i] = item;
            scores[i] = score;
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (scores[parent] <= scores[i])
                    break;
                Swap(i, parent);
                i = parent;
            }
        }

        public int Pop()
        {
            int ret = items[0];
            count--;
            if (count > 0)
            {
                items[0] = items[count];
                scores[0] = scores[count];
                SiftDown(0);
            }

            return ret;
        }

        private void SiftDown(int i)
        {
            while (true)
            {
                int left = (i << 1) + 1;
                if (left >= count)
                    break;
                int right = left + 1;
                int smallest = left;
                if (right < count && scores[right] < scores[left])
                    smallest = right;
                if (scores[i] <= scores[smallest])
                    break;
                Swap(i, smallest);
                i = smallest;
            }
        }

        private void Swap(int a, int b)
        {
            var ti = items[a];
            items[a] = items[b];
            items[b] = ti;
            var ts = scores[a];
            scores[a] = scores[b];
            scores[b] = ts;
        }
    }
}