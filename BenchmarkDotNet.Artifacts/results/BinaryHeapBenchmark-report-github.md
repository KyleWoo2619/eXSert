```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26120.5770)
AMD Ryzen 9 7950X3D 4.20GHz, 1 CPU, 8 logical and 8 physical cores
  [Host]               : .NET Framework 4.8.1 (4.8.9310.0), X64 RyuJIT VectorSize=256
  .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9310.0), X64 RyuJIT VectorSize=256

Job=.NET Framework 4.7.2  Runtime=.NET Framework 4.7.2  

```
| Method             | Mean     | Error    | StdDev   |
|------------------- |---------:|---------:|---------:|
| PushAllThenPopAll  | 12.57 ms | 0.079 ms | 0.074 ms |
| PushPopInterleaved | 11.95 ms | 0.021 ms | 0.020 ms |
