using UnityEngine;

namespace EnemyBehavior.Density
{
 public sealed class DensityGrid : MonoBehaviour
 {
 public static DensityGrid Instance { get; private set; }

 [SerializeField] Vector2 worldOrigin = new Vector2(-50f, -50f);
 [SerializeField] Vector2 worldSize = new Vector2(100f,100f);
 [SerializeField] float cellSize =1.0f;
 [SerializeField] float decayPerSecond =0.5f;

 private int _w, _h;
 private float[] _cells;

 void Awake()
 {
 Instance = this;
 _w = Mathf.Max(1, Mathf.CeilToInt(worldSize.x / cellSize));
 _h = Mathf.Max(1, Mathf.CeilToInt(worldSize.y / cellSize));
 _cells = new float[_w * _h];
 }

 void Update()
 {
 UpdateGrid(Time.deltaTime);
 }

 public void Stamp(Vector3 pos, float radius, float weight)
 {
 int cx = Mathf.FloorToInt((pos.x - worldOrigin.x) / cellSize);
 int cz = Mathf.FloorToInt((pos.z - worldOrigin.y) / cellSize);
 int r = Mathf.CeilToInt(radius / cellSize);
 int xmin = Mathf.Clamp(cx - r,0, _w -1);
 int xmax = Mathf.Clamp(cx + r,0, _w -1);
 int zmin = Mathf.Clamp(cz - r,0, _h -1);
 int zmax = Mathf.Clamp(cz + r,0, _h -1);
 for (int z = zmin; z <= zmax; z++)
 {
 for (int x = xmin; x <= xmax; x++)
 {
 int idx = z * _w + x;
 // simple disk falloff
 float dx = (x - cx) * cellSize;
 float dz = (z - cz) * cellSize;
 float d = Mathf.Sqrt(dx*dx + dz*dz);
 if (d <= radius)
 {
 float fall =1f - (d / radius);
 _cells[idx] += weight * fall;
 }
 }
 }
 }

 public void UpdateGrid(float dt)
 {
 float k = Mathf.Exp(-decayPerSecond * dt);
 for (int i =0;i<_cells.Length;i++) _cells[i] *= k;
 }

 public float SampleCost(Vector3 pos)
 {
 float fx = (pos.x - worldOrigin.x) / cellSize;
 float fz = (pos.z - worldOrigin.y) / cellSize;
 int x0 = Mathf.FloorToInt(fx);
 int z0 = Mathf.FloorToInt(fz);
 if (x0 <0 || z0 <0 || x0 >= _w-1 || z0 >= _h-1) return0f;
 float tx = fx - x0; float tz = fz - z0;
 float c00 = _cells[z0 * _w + x0];
 float c10 = _cells[z0 * _w + (x0+1)];
 float c01 = _cells[(z0+1) * _w + x0];
 float c11 = _cells[(z0+1) * _w + (x0+1)];
 float cx0 = Mathf.Lerp(c00, c10, tx);
 float cx1 = Mathf.Lerp(c01, c11, tx);
 return Mathf.Lerp(cx0, cx1, tz);
 }
 }
}
