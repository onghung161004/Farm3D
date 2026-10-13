using UnityEngine;

namespace FarmRestoration
{
    /// <summary>Draws cultivated cells as one terrain-following, softly blended garden surface.</summary>
    [DisallowMultipleComponent]
    public sealed class GardenSoilSurface : MonoBehaviour
    {
        [SerializeField] private Material soilMaterial;

        private const float CellSize = 1.25f;
        private const float Feather = 0.34f;
        private const int Subdivisions = 4;
        private FarmPlot[] plots;
        private Mesh soilMesh;
        private MeshCollider terrain;

        public void Configure(Material material)
        {
            soilMaterial = material;
            if (Application.isPlaying && isActiveAndEnabled && soilMesh == null) Initialize();
        }

        private void OnEnable() => Initialize();

        private void Initialize()
        {
            if (soilMesh != null) return;
            if (soilMaterial == null)
            {
                Debug.LogWarning("Garden soil material is missing; keeping the original plot visuals.", this);
                return;
            }

            plots = GetComponentsInChildren<FarmPlot>(true);
            if (plots.Length == 0)
            {
                Debug.LogWarning("Garden soil has no farm plots to render.", this);
                return;
            }
            GameObject ground = GameObject.Find("WalkableValleyTerrain");
            terrain = ground == null ? null : ground.GetComponent<MeshCollider>();
            if (terrain == null)
            {
                Debug.LogWarning("Garden soil could not find the valley terrain collider.", this);
                return;
            }

            Transform child = transform.Find("SoftSoilMesh");
            if (child == null)
            {
                child = new GameObject("SoftSoilMesh", typeof(MeshFilter), typeof(MeshRenderer)).transform;
                child.SetParent(transform, false);
            }
            child.GetComponent<MeshRenderer>().sharedMaterial = soilMaterial;
            soilMesh = new Mesh { name = name + " Soil Surface" };
            child.GetComponent<MeshFilter>().sharedMesh = soilMesh;
            foreach (FarmPlot plot in plots) plot.StateChanged += OnPlotChanged;
            Rebuild();
        }

        private void OnDisable()
        {
            if (plots != null)
                foreach (FarmPlot plot in plots)
                    if (plot != null) plot.StateChanged -= OnPlotChanged;
            if (soilMesh != null)
            {
                if (Application.isPlaying) Destroy(soilMesh);
                else DestroyImmediate(soilMesh);
            }
            soilMesh = null;
        }

        private void OnPlotChanged(FarmPlot plot) => Rebuild();

        private void Rebuild()
        {
            if (soilMesh == null || terrain == null || plots == null) return;
            float minX = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxZ = float.MinValue;
            foreach (FarmPlot plot in plots)
            {
                Vector3 p = plot.transform.position;
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
            }
            int width = Mathf.RoundToInt((maxX - minX) / CellSize) + 1;
            int depth = Mathf.RoundToInt((maxZ - minZ) / CellSize) + 1;
            if (width * depth > 256) return;
            bool[,] active = new bool[width, depth];
            FarmPlot[,] cells = new FarmPlot[width, depth];

            foreach (FarmPlot plot in plots)
            {
                Vector3 position = plot.transform.position;
                int x = Mathf.RoundToInt((position.x - minX) / CellSize);
                int z = Mathf.RoundToInt((position.z - minZ) / CellSize);
                if (x < 0 || x >= width || z < 0 || z >= depth) continue;
                cells[x, z] = plot;
                active[x, z] = plot.CurrentState != FarmPlotState.Untilled;
            }

            const int side = Subdivisions + 1;
            Vector3[] vertices = new Vector3[width * depth * side * side];
            Vector2[] uv = new Vector2[vertices.Length];
            Color[] colors = new Color[vertices.Length];
            int[] triangles = new int[width * depth * Subdivisions * Subdivisions * 6];
            int vertex = 0, triangle = 0;
            for (int z = 0; z < depth; z++)
            for (int x = 0; x < width; x++)
            {
                FarmPlot plot = cells[x, z];
                Vector3 plotCenter = plot == null ? new Vector3(minX + x * CellSize, 0f, minZ + z * CellSize)
                    : plot.transform.position;
                bool visible = active[x, z];
                bool wet = visible && plot.CurrentState == FarmPlotState.Watered;
                int start = vertex;
                for (int row = 0; row <= Subdivisions; row++)
                for (int column = 0; column <= Subdivisions; column++)
                {
                    float dx = (column / (float)Subdivisions - 0.5f) * CellSize;
                    float dz = (row / (float)Subdivisions - 0.5f) * CellSize;
                    float worldX = plotCenter.x + dx, worldZ = plotCenter.z + dz;
                    Ray ray = new Ray(new Vector3(worldX, 50f, worldZ), Vector3.down);
                    float height = terrain.Raycast(ray, out RaycastHit hit, 100f) ? hit.point.y : plotCenter.y - 0.08f;
                    vertices[vertex] = transform.InverseTransformPoint(new Vector3(worldX, height + 0.035f, worldZ));
                    uv[vertex] = new Vector2(worldX * 0.48f, worldZ * 0.48f);
                    float alpha = visible ? 1f : 0f;
                    if (x == 0 || !active[x - 1, z]) alpha *= Mathf.Clamp01((dx + CellSize * 0.5f) / Feather);
                    if (x == width - 1 || !active[x + 1, z]) alpha *= Mathf.Clamp01((CellSize * 0.5f - dx) / Feather);
                    if (z == 0 || !active[x, z - 1]) alpha *= Mathf.Clamp01((dz + CellSize * 0.5f) / Feather);
                    if (z == depth - 1 || !active[x, z + 1]) alpha *= Mathf.Clamp01((CellSize * 0.5f - dz) / Feather);
                    colors[vertex] = wet ? new Color(0.79f, 0.84f, 0.91f, alpha) : new Color(1f, 1f, 1f, alpha);
                    vertex++;
                }
                for (int row = 0; row < Subdivisions; row++)
                for (int column = 0; column < Subdivisions; column++)
                {
                    int a = start + row * side + column, b = a + 1, c = a + side, d = c + 1;
                    triangles[triangle++] = a; triangles[triangle++] = c; triangles[triangle++] = b;
                    triangles[triangle++] = b; triangles[triangle++] = c; triangles[triangle++] = d;
                }
            }

            soilMesh.Clear();
            soilMesh.vertices = vertices;
            soilMesh.uv = uv;
            soilMesh.colors = colors;
            soilMesh.triangles = triangles;
            soilMesh.RecalculateNormals();
            soilMesh.RecalculateBounds();
        }
    }
}
