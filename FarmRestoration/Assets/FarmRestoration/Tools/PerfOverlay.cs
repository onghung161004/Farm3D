using System.Text;
using System.IO;
using UnityEngine;
using Unity.Profiling;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FarmRestoration.Tools
{
    public class PerfOverlay : MonoBehaviour
    {
        public bool showOverlay = true;
        public bool logToCsv = true;

        ProfilerRecorder drawCallsRecorder;
        ProfilerRecorder setPassCallsRecorder;
        ProfilerRecorder batchesRecorder;
        ProfilerRecorder trianglesRecorder;
        ProfilerRecorder verticesRecorder;
        ProfilerRecorder systemMemoryRecorder;
        ProfilerRecorder totalMemoryRecorder;
        ProfilerRecorder mainThreadTimeRecorder;
        ProfilerRecorder gpuTimeRecorder;

        private int frameCount = 0;
        private float dt = 0.0f;
        private float fps = 0.0f;
        private float lowestFps = 9999f;
        
        private string csvPath;

        void OnEnable()
        {
            drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            setPassCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            trianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
            systemMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            totalMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            gpuTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time", 15);

            csvPath = Path.Combine(Application.persistentDataPath, "perf_log.csv");
            if (logToCsv && !File.Exists(csvPath))
            {
                File.WriteAllText(csvPath, "Time,FPS,FrameTime,DrawCalls,SetPassCalls,Batches,Triangles,Vertices,SystemMemory(MB),TotalMemory(MB)\n");
            }
        }

        void OnDisable()
        {
            drawCallsRecorder.Dispose();
            setPassCallsRecorder.Dispose();
            batchesRecorder.Dispose();
            trianglesRecorder.Dispose();
            verticesRecorder.Dispose();
            systemMemoryRecorder.Dispose();
            totalMemoryRecorder.Dispose();
            mainThreadTimeRecorder.Dispose();
            gpuTimeRecorder.Dispose();
        }

        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.f3Key.wasPressedThisFrame) showOverlay = !showOverlay;
                if (Keyboard.current.f12Key.wasPressedThisFrame) TakeScreenshot();
            }
#else
            if (Input.GetKeyDown(KeyCode.F3)) showOverlay = !showOverlay;
            if (Input.GetKeyDown(KeyCode.F12)) TakeScreenshot();
#endif

            frameCount++;
            dt += Time.unscaledDeltaTime;
            if (dt > 1.0f)
            {
                fps = frameCount / dt;
                if (fps < lowestFps) lowestFps = fps;
                
                if (logToCsv)
                {
                    float frameTime = (float)(mainThreadTimeRecorder.LastValue * 1e-6);
                    string line = $"{Time.time:F1},{fps:F1},{frameTime:F2},{drawCallsRecorder.LastValue},{setPassCallsRecorder.LastValue},{batchesRecorder.LastValue},{trianglesRecorder.LastValue},{verticesRecorder.LastValue},{systemMemoryRecorder.LastValue / (1024 * 1024)},{totalMemoryRecorder.LastValue / (1024 * 1024)}\n";
                    File.AppendAllText(csvPath, line);
                }

                frameCount = 0;
                dt -= 1.0f;
            }
        }

        void TakeScreenshot()
        {
            string dir = Path.Combine(Directory.GetCurrentDirectory(), "docs", "Screens");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string filename = Path.Combine(dir, $"Screenshot_{System.DateTime.Now:yyyyMMdd_HHmmss}.png");
            ScreenCapture.CaptureScreenshot(filename);
            Debug.Log("Screenshot saved to " + filename);
        }

        void OnGUI()
        {
            if (!showOverlay) return;

            float frameTime = (float)(mainThreadTimeRecorder.LastValue * 1e-6);
            float gpuTime = (float)(gpuTimeRecorder.LastValue * 1e-6);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"FPS: {fps:F1} (1% Low: {lowestFps:F1})");
            sb.AppendLine($"CPU Frame Time: {frameTime:F2} ms");
            sb.AppendLine($"GPU Frame Time: {gpuTime:F2} ms");
            sb.AppendLine($"Batches: {batchesRecorder.LastValue}");
            sb.AppendLine($"SetPass: {setPassCallsRecorder.LastValue}");
            sb.AppendLine($"Draw Calls: {drawCallsRecorder.LastValue}");
            sb.AppendLine($"Triangles: {trianglesRecorder.LastValue}");
            sb.AppendLine($"Vertices: {verticesRecorder.LastValue}");
            sb.AppendLine($"Sys RAM: {systemMemoryRecorder.LastValue / (1024 * 1024)} MB");
            sb.AppendLine($"Total RAM: {totalMemoryRecorder.LastValue / (1024 * 1024)} MB");
            sb.AppendLine($"CSV: {csvPath}");

            GUI.color = Color.green;
            GUI.skin.label.fontSize = 18;
            GUI.Label(new Rect(10, 10, 400, 400), sb.ToString());
        }
    }
}
