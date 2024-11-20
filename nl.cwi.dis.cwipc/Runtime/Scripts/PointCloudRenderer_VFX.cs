
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;
using System;

namespace Cwipc
{
    /// <summary>
    /// MonoBehaviour that renders pointclouds with VFX Graph.
    /// </summary>
    public class PointCloudRenderer_VFX : MonoBehaviour
    {
        ComputeBuffer pointBuffer = null;
        int pointCount = 0;
        [Header("Settings")]
        [Tooltip("Source of pointclouds. Can (and must) be empty if set dynamically through script.")]
        public AbstractPointCloudPreparer pointcloudSource;
        public IPointCloudPreparer preparer;

        [Header("VFX Settings")]
        public VisualEffect vfxGraph; // Reference to your VFX Graph
        public PointCloud_VFX pc_VFX;

        [Header("Events")]
        [Tooltip("Event emitted when the first point cloud is displayed")]
        public UnityEvent started;
        [Tooltip("Event emitted when the last point cloud has been displayed")]
        public UnityEvent finished;
        private bool started_emitted = false;
        private bool finished_emitted = false;

        [Header("Introspection (for debugging)")]
        [Tooltip("Renderer temporarily paused by a script")]
        [SerializeField] bool paused = false;

        private Vector3[] positions = null;
        private Color[] colors = null;


        // Start is called before the first frame update
        void Start()
        {
            if (started == null)
            {
                started = new UnityEvent();
            }
            if (finished == null)
            {
                finished = new UnityEvent();
            }
            pointBuffer = new ComputeBuffer(1, sizeof(float) * 4);

            if (pointcloudSource != null)
            {
                SetPreparer(pointcloudSource);
            }

            if (vfxGraph == null)
            {
                Debug.LogError("PointCloudSource or VFX Graph is not assigned.");
            }
        }

        public void PausePlayback(bool _paused)
        {
            paused = _paused;
        }

        public void SetPreparer(IPointCloudPreparer _preparer)
        {
            if (_preparer == null)
            {
                Debug.LogError($"Programmer error: attempt to set null preparer");
            }
            if (preparer != null)
            {
                Debug.LogError($"Programmer error: attempt to set second preparer");
            }
            preparer = _preparer;
        }

        private void Update()
        {
            if (preparer == null) return;
            preparer.Synchronize();
        }

        private void LateUpdate()
        {
            if (preparer == null) return;
            if (paused) return;

            bool fresh = preparer.LatchFrame();
            if (fresh)
            {
                if (!started_emitted)
                {
                    started_emitted = true;
                    started.Invoke();
                }

                pointCount = preparer.GetComputeBuffer(ref pointBuffer);

                if (pointBuffer == null || !pointBuffer.IsValid())
                {
                    Debug.LogError($"Invalid pointBuffer");
                    return;
                }

                pointCount = ExtractDataFromComputeBuffer(pointBuffer, ref positions, ref colors);
                Debug.Log("Pass to VFX : " + pointCount + " points");
                pc_VFX.PassToVFX(positions, colors);
            }
            else
            {
                if (!finished_emitted && preparer.EndOfData())
                {
                    finished_emitted = true;
                    finished.Invoke();
                }
            }
        }

        public void OnDestroy()
        {
            if (pointBuffer != null)
            {
                pointBuffer.Release();
                pointBuffer = null;
            }
        }

        private int ExtractDataFromComputeBuffer(ComputeBuffer computeBuffer, ref Vector3[] pointPositions, ref Color[] pointColors)
        {
            if (computeBuffer == null || computeBuffer.count == 0)
            {
                pointPositions = new Vector3[0];
                pointColors = new Color[0];
                return 0;
            }

            // Get the point count from the compute buffer
            int nPoints = computeBuffer.count;

            // Ensure arrays are correctly sized
            if (pointPositions == null || pointPositions.Length != nPoints)
            {
                pointPositions = new Vector3[nPoints];
            }
            if (pointColors == null || pointColors.Length != nPoints)
            {
                pointColors = new Color[nPoints];
            }

            // Allocate a buffer to retrieve data from the compute buffer
            byte[] bufferData = new byte[nPoints * sizeof(float) * 4];

            // Get the data from the compute buffer
            computeBuffer.GetData(bufferData);

            const int sizeofPoint = sizeof(float) * 4; // x, y, z, and packed color

            // Extract points and colors from the buffer
            for (int i = 0; i < nPoints; i++)
            {
                // Extract position (x, y, z) from the buffer
                float x = BitConverter.ToSingle(bufferData, i * sizeofPoint);
                float y = BitConverter.ToSingle(bufferData, i * sizeofPoint + sizeof(float));
                float z = BitConverter.ToSingle(bufferData, i * sizeofPoint + 2 * sizeof(float));

                // Extract packed color data (RGBA)
                uint packedColor = BitConverter.ToUInt32(bufferData, i * sizeofPoint + 3 * sizeof(float));

                // Populate the position array
                pointPositions[i] = new Vector3(x, y, z);

                // Decode and populate the color array
                float r = ((packedColor >> 16) & 0xFF) / 255.0f;
                float g = ((packedColor >> 8) & 0xFF) / 255.0f;
                float b = (packedColor & 0xFF) / 255.0f;
                pointColors[i] = new Color(r, g, b, 1.0f);
            }

            return nPoints;
        }

    }
}

/*
 *  Asynchron Test
 * 
 * 
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;
using System;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Cwipc
{
    /// <summary>
    /// MonoBehaviour that renders pointclouds with VFX Graph.
    /// </summary>
    public class PointCloudRenderer_VFX : MonoBehaviour
    {
        ComputeBuffer pointBuffer = null;
        int pointCount = 0;
        [Header("Settings")]
        [Tooltip("Source of pointclouds. Can (and must) be empty if set dynamically through script.")]
        public AbstractPointCloudPreparer pointcloudSource;
        public IPointCloudPreparer preparer;

        [Header("VFX Settings")]
        public VisualEffect vfxGraph; // Reference to your VFX Graph
        public PointCloud_VFX pc_VFX;

        [Header("Events")]
        [Tooltip("Event emitted when the first point cloud is displayed")]
        public UnityEvent started;
        [Tooltip("Event emitted when the last point cloud has been displayed")]
        public UnityEvent finished;
        private bool started_emitted = false;
        private bool finished_emitted = false;

        [Header("Introspection (for debugging)")]
        [Tooltip("Renderer temporarily paused by a script")]
        [SerializeField] bool paused = false;

        private Vector3[] positions;
        private Color[] colors;
        byte[] bufferData;

        Thread ThreadWorker;
        private readonly object lockObject = new object();
        private CancellationTokenSource cancellationTokenSource;
        private bool newFrameAvailable = false;


        // Start is called before the first frame update
        void Start()
        {
            if (started == null)
            {
                started = new UnityEvent();
            }
            if (finished == null)
            {
                finished = new UnityEvent();
            }
            pointBuffer = new ComputeBuffer(1, sizeof(float) * 4);

            if (pointcloudSource != null)
            {
                SetPreparer(pointcloudSource);
            }

            if (vfxGraph == null)
            {
                Debug.LogError("PointCloudSource or VFX Graph is not assigned.");
            }

            if (ThreadWorker == null || !ThreadWorker.IsAlive)
            {
                cancellationTokenSource = new CancellationTokenSource();
                ThreadWorker = new Thread(() => DoWork(cancellationTokenSource.Token));
                ThreadWorker.IsBackground = true;
                ThreadWorker.Start();
            }
        }

        public void PausePlayback(bool _paused)
        {
            paused = _paused;
        }

        public void SetPreparer(IPointCloudPreparer _preparer)
        {
            if (_preparer == null)
            {
                Debug.LogError($"Programmer error: attempt to set null preparer");
            }
            if (preparer != null)
            {
                Debug.LogError($"Programmer error: attempt to set second preparer");
            }
            preparer = _preparer;
        }

        private void Update()
        {
            if (paused) return;

            lock (lockObject)
            {
                preparer.Synchronize();
                pointCount = preparer.GetComputeBuffer(ref pointBuffer);

                if (newFrameAvailable)
                {
                    newFrameAvailable = false;
                    Debug.Log("Pass to VFX : " + pointCount + " points");
                    
                    pc_VFX.PassToVFX(positions, colors);
                    
                    if (!started_emitted)
                    {
                        started_emitted = true;
                        started.Invoke();
                    }
                }
                else
                {
                    if (!finished_emitted && preparer.EndOfData())
                    {
                        finished_emitted = true;
                        finished.Invoke();
                    }
                }
            }
        }

        void DoWork(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    lock (lockObject)
                    {
                        bool fresh = preparer.LatchFrame();

                        if (fresh)
                        {
                            pointCount = ExtractDataFromComputeBuffer(pointBuffer, ref positions, ref colors);
                            newFrameAvailable = true;
                        }
                    }

                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"DoWork error: {ex.Message}");
            }
            finally
            {
                Cleanup();
            }
        }


        void Cleanup()
        {
            cancellationTokenSource?.Dispose(); // Clean up the CancellationTokenSource
            cancellationTokenSource = null;
        }

        public void OnDestroy()
        {
            cancellationTokenSource?.Cancel(); // Request cancellation
            ThreadWorker?.Join(); // Wait for the thread to finish
            Cleanup();

            if (pointBuffer != null)
            {
                pointBuffer.Release();
                pointBuffer = null;
            }
        }

        private int ExtractDataFromComputeBuffer(ComputeBuffer computeBuffer, ref Vector3[] pointPositions, ref Color[] pointColors)
        {
            if (computeBuffer == null || computeBuffer.count == 0)
            {
                pointPositions = new Vector3[0];
                pointColors = new Color[0];
                return 0;
            }

            // Get the point count from the compute buffer
            int nPoints = computeBuffer.count;

            // Ensure arrays are correctly sized
            if (pointPositions == null || pointPositions.Length != nPoints)
            {
                pointPositions = new Vector3[nPoints];
            }
            if (pointColors == null || pointColors.Length != nPoints)
            {
                pointColors = new Color[nPoints];
            }

            // Allocate a buffer to retrieve data from the compute buffer
            byte[] bufferData = new byte[nPoints * sizeof(float) * 4];

            // Get the data from the compute buffer
            computeBuffer.GetData(bufferData);

            const int sizeofPoint = sizeof(float) * 4; // x, y, z, and packed color

            // Extract points and colors from the buffer
            for (int i = 0; i < nPoints; i++)
            {
                // Extract position (x, y, z) from the buffer
                float x = BitConverter.ToSingle(bufferData, i * sizeofPoint);
                float y = BitConverter.ToSingle(bufferData, i * sizeofPoint + sizeof(float));
                float z = BitConverter.ToSingle(bufferData, i * sizeofPoint + 2 * sizeof(float));

                // Extract packed color data (RGBA)
                uint packedColor = BitConverter.ToUInt32(bufferData, i * sizeofPoint + 3 * sizeof(float));

                // Populate the position array
                pointPositions[i] = new Vector3(x, y, z);

                // Decode and populate the color array
                float r = ((packedColor >> 16) & 0xFF) / 255.0f;
                float g = ((packedColor >> 8) & 0xFF) / 255.0f;
                float b = (packedColor & 0xFF) / 255.0f;
                pointColors[i] = new Color(r, g, b, 1.0f);
            }

            return nPoints;
        }
        private int ExtractDataFromBytes(byte[] BytesData, ref Vector3[] pointPositions, ref Color[] pointColors)
        {
            if (BytesData == null)
            {
                pointPositions = new Vector3[0];
                pointColors = new Color[0];
                return 0;
            }

            // Get the point count from the compute buffer
            int nPoints = BytesData.Length / (sizeof(float) * 4);

            // Ensure arrays are correctly sized
            if (pointPositions == null || pointPositions.Length != nPoints)
            {
                pointPositions = new Vector3[nPoints];
            }
            if (pointColors == null || pointColors.Length != nPoints)
            {
                pointColors = new Color[nPoints];
            }

            const int sizeofPoint = sizeof(float) * 4; // x, y, z, and packed color

            // Extract points and colors from the buffer
            for (int i = 0; i < nPoints; i++)
            {
                // Extract position (x, y, z) from the buffer
                float x = BitConverter.ToSingle(BytesData, i * sizeofPoint);
                float y = BitConverter.ToSingle(BytesData, i * sizeofPoint + sizeof(float));
                float z = BitConverter.ToSingle(BytesData, i * sizeofPoint + 2 * sizeof(float));

                // Extract packed color data (RGBA)
                uint packedColor = BitConverter.ToUInt32(BytesData, i * sizeofPoint + 3 * sizeof(float));

                // Populate the position array
                pointPositions[i] = new Vector3(x, y, z);

                // Decode and populate the color array
                float r = ((packedColor >> 16) & 0xFF) / 255.0f;
                float g = ((packedColor >> 8) & 0xFF) / 255.0f;
                float b = (packedColor & 0xFF) / 255.0f;
                pointColors[i] = new Color(r, g, b, 1.0f);
            }

            return nPoints;
        }

    }
}

*/