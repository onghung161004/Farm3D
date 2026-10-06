using UnityEngine;
using UnityEngine.Rendering;

namespace FarmRestoration
{
    /// <summary>Places a non-walkable terrain sample beyond the existing valley.</summary>
    [DisallowMultipleComponent]
    public sealed class DistantTerrainBackdrop : MonoBehaviour
    {
        [SerializeField] private GameObject terrainPrefab;
        [SerializeField] private TerrainData fittedTerrainData;
        [SerializeField] private Vector3 localOrigin = new Vector3(-350f, -35f, 100f);
        [SerializeField] private Vector3 backdropSize = new Vector3(700f, 220f, 300f);

        private GameObject backdrop;
        private bool createdBackdrop;
        private TerrainData originalData;
        private TerrainData copiedData;
        private TerrainLayer[] copiedLayers;

        private void Awake()
        {
            // The open scene can keep its old serialized values until Unity reloads it.
            if (localOrigin == new Vector3(-260f, -12f, 180f))
                localOrigin = new Vector3(-350f, -35f, 100f);
            if (backdropSize == new Vector3(520f, 220f, 230f))
                backdropSize = new Vector3(700f, 220f, 300f);

            if (terrainPrefab == null || terrainPrefab.GetComponent<Terrain>() == null)
            {
                Debug.LogWarning("Distant mountain terrain prefab is missing; the playable valley is unchanged.", this);
                return;
            }

            Transform existing = transform.Find("DistantRealisticTerrainBackdrop");
            createdBackdrop = existing == null;
            backdrop = createdBackdrop ? Instantiate(terrainPrefab, transform, false) : existing.gameObject;
            backdrop.name = "DistantRealisticTerrainBackdrop";
            backdrop.transform.localPosition = localOrigin;
            backdrop.transform.localRotation = Quaternion.identity;
            backdrop.transform.localScale = Vector3.one;

            Terrain terrain = backdrop.GetComponent<Terrain>();
            if (terrain == null)
            {
                Debug.LogWarning("Distant terrain object has no Terrain component.", backdrop);
                return;
            }
            TerrainCollider collider = backdrop.GetComponent<TerrainCollider>();
            if (collider != null)
            {
                collider.enabled = false;
                if (createdBackdrop) Destroy(collider);
            }

            originalData = terrain.terrainData;
            if (fittedTerrainData != null) terrain.terrainData = fittedTerrainData;
            if (terrain.terrainData == null)
            {
                Debug.LogWarning("Distant terrain has no heightmap data.", backdrop);
                return;
            }
            if (terrain.terrainData.size != backdropSize)
            {
                copiedData = Instantiate(terrain.terrainData);
                copiedData.name = "DistantMountainTerrainData_Runtime";
                copiedData.size = backdropSize;
                TerrainLayer[] sourceLayers = copiedData.terrainLayers;
                copiedLayers = new TerrainLayer[sourceLayers.Length];
                for (int i = 0; i < sourceLayers.Length; i++)
                {
                    if (sourceLayers[i] == null) continue;
                    copiedLayers[i] = Instantiate(sourceLayers[i]);
                    copiedLayers[i].tileSize = new Vector2(backdropSize.x, backdropSize.z);
                }
                copiedData.terrainLayers = copiedLayers;
                FlattenFrontEdge(copiedData, backdropSize, localOrigin);
                terrain.terrainData = copiedData;
            }

            terrain.enabled = true;
            terrain.drawTreesAndFoliage = false;
            terrain.treeDistance = 0f;
            terrain.detailObjectDistance = 0f;
            terrain.heightmapPixelError = 18f;
            terrain.shadowCastingMode = ShadowCastingMode.Off;
            terrain.allowAutoConnect = false;
        }

        private void OnDestroy()
        {
            if (backdrop != null && createdBackdrop) Destroy(backdrop);
            else if (backdrop != null && originalData != null)
            {
                Terrain terrain = backdrop.GetComponent<Terrain>();
                if (terrain != null) terrain.terrainData = originalData;
            }
            if (copiedData != null) Destroy(copiedData);
            if (copiedLayers == null) return;
            foreach (TerrainLayer layer in copiedLayers) if (layer != null) Destroy(layer);
        }

        public static void FlattenFrontEdge(TerrainData data, Vector3 size, Vector3 origin)
        {
            int resolution = data.heightmapResolution;
            float[,] heights = data.GetHeights(0, 0, resolution, resolution);
            int blendRows = Mathf.Max(1, Mathf.RoundToInt((70f / size.z) * (resolution - 1)));
            float edgeHeight = (-1.5f - origin.y) / size.y;
            for (int z = 0; z < blendRows; z++)
            {
                float t = z / (float)blendRows;
                float blend = t * t * (3f - 2f * t);
                for (int x = 0; x < resolution; x++)
                    heights[z, x] = Mathf.Lerp(edgeHeight, heights[z, x], blend);
            }
            data.SetHeights(0, 0, heights);
        }
    }
}
