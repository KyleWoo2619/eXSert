```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26120.5770)
AMD Ryzen 9 7950X3D 4.20GHz, 1 CPU, 8 logical and 8 physical cores
.NET SDK 9.0.306
  [Host] : .NET 9.0.10 (9.0.1025.47515), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2  

```
| Method            | Mean | Error |
|------------------ |-----:|------:|
| PushAllThenPopAll |   NA |    NA |

Benchmarks with issues:
  BinaryHeapBenchmark.PushAllThenPopAll: .NET Framework 4.7.2(Runtime=.NET Framework 4.7.2)
