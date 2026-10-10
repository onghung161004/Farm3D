using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace FarmRestoration.Editor.Tools
{
    public class BaselineScanner
    {
        [MenuItem("Tools/Farm/Baseline/Scan Scenes")]
        public static void ScanScenes()
        {
            string outPath = Path.Combine("docs", "Baseline", "scene_stats.csv");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Scene,Renderers,Triangles,Vertices,Materials,Shaders,Lights,Particles,Colliders,Rigidbodies,Terrains");

            string[] scenes = AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath).ToArray();
            foreach (string scenePath in scenes)
            {
                if (scenePath.Contains("Packages")) continue;
                
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                
                var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                int triCount = 0;
                int vertCount = 0;
                HashSet<Material> mats = new HashSet<Material>();
                HashSet<Shader> shaders = new HashSet<Shader>();

                foreach (var r in renderers)
                {
                    if (r is MeshRenderer mr)
                    {
                        MeshFilter mf = mr.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            triCount += mf.sharedMesh.triangles.Length / 3;
                            vertCount += mf.sharedMesh.vertexCount;
                        }
                    }
                    else if (r is SkinnedMeshRenderer smr)
                    {
                        if (smr.sharedMesh != null)
                        {
                            triCount += smr.sharedMesh.triangles.Length / 3;
                            vertCount += smr.sharedMesh.vertexCount;
                        }
                    }
                    
                    foreach (var m in r.sharedMaterials)
                    {
                        if (m != null)
                        {
                            mats.Add(m);
                            if (m.shader != null) shaders.Add(m.shader);
                        }
                    }
                }

                int lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                int particles = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                int colliders = Object.FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                int rbs = Object.FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                int terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;

                sb.AppendLine($"{Path.GetFileNameWithoutExtension(scenePath)},{renderers.Length},{triCount},{vertCount},{mats.Count},{shaders.Count},{lights},{particles},{colliders},{rbs},{terrains}");
            }
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"Scanned {scenes.Length} scenes. Output: {outPath}");
        }

        [MenuItem("Tools/Farm/Baseline/Scan Assets")]
        public static void ScanAssets()
        {
            string outPath = Path.Combine("docs", "Baseline", "asset_stats.csv");
            string top20Path = Path.Combine("docs", "Baseline", "asset_top20.md");
            
            var textures = AssetDatabase.FindAssets("t:Texture2D").Select(AssetDatabase.GUIDToAssetPath).Where(p => p.StartsWith("Assets")).ToArray();
            var models = AssetDatabase.FindAssets("t:Model").Select(AssetDatabase.GUIDToAssetPath).Where(p => p.StartsWith("Assets")).ToArray();
            var materials = AssetDatabase.FindAssets("t:Material").Select(AssetDatabase.GUIDToAssetPath).Where(p => p.StartsWith("Assets")).ToArray();
            var audios = AssetDatabase.FindAssets("t:AudioClip").Select(AssetDatabase.GUIDToAssetPath).Where(p => p.StartsWith("Assets")).ToArray();

            // Texture stats
            long totalTexMem = 0;
            var texList = new List<(string path, long size, string format, bool mipmap)>();
            foreach (var t in textures)
            {
                TextureImporter ti = AssetImporter.GetAtPath(t) as TextureImporter;
                if (ti != null)
                {
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(t);
                    if (tex == null) continue;
                    long mem = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex);
                    totalTexMem += mem;
                    texList.Add((t, mem, tex.format.ToString(), tex.mipmapCount > 1));
                }
            }
            var top20Tex = texList.OrderByDescending(x => x.size).Take(20).ToList();

            // Model stats
            var modelList = new List<(string path, int tris)>();
            foreach (var m in models)
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(m);
                if (go == null) continue;
                int tris = 0;
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh != null) tris += mf.sharedMesh.triangles.Length / 3;
                }
                foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.sharedMesh != null) tris += smr.sharedMesh.triangles.Length / 3;
                }
                modelList.Add((m, tris));
            }
            var top20Model = modelList.OrderByDescending(x => x.tris).Take(20).ToList();

            // Mat stats
            HashSet<Shader> usedShaders = new HashSet<Shader>();
            foreach (var m in materials)
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(m);
                if (mat != null && mat.shader != null) usedShaders.Add(mat.shader);
            }

            // Audio stats
            long totalAudioMem = 0;
            foreach (var a in audios)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(a);
                if (clip != null) totalAudioMem += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(clip);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Category,Count,Memory/Size,Notes");
            sb.AppendLine($"Textures,{textures.Length},{totalTexMem / (1024*1024)} MB,");
            sb.AppendLine($"Models,{models.Length},-,");
            sb.AppendLine($"Materials,{materials.Length},-,Unique Shaders: {usedShaders.Count}");
            sb.AppendLine($"Audio,{audios.Length},{totalAudioMem / (1024*1024)} MB,");
            File.WriteAllText(outPath, sb.ToString());

            StringBuilder sbMd = new StringBuilder();
            sbMd.AppendLine("## Top 20 Textures (by Memory)");
            sbMd.AppendLine("| Path | Memory (KB) | Format | Mipmap |");
            sbMd.AppendLine("|---|---|---|---|");
            foreach (var t in top20Tex) sbMd.AppendLine($"| {t.path} | {t.size / 1024} | {t.format} | {t.mipmap} |");
            
            sbMd.AppendLine("\n## Top 20 Models (by Triangles)");
            sbMd.AppendLine("| Path | Triangles |");
            sbMd.AppendLine("|---|---|");
            foreach (var m in top20Model) sbMd.AppendLine($"| {m.path} | {m.tris} |");
            File.WriteAllText(top20Path, sbMd.ToString());
            
            Debug.Log($"Scanned Assets. Output: {outPath} and {top20Path}");
        }

        public static void BatchScanAll()
        {
            ScanScenes();
            ScanAssets();
        }
    }
}
