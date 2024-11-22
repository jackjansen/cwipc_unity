using System;
using Unity.Collections;
using UnityEngine;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class NetworkPointCloudReader : AbstractPointCloudPreparer
    {
        private AsyncTCPPCReader reader;
        private AbstractPointCloudDecoder decoder;
        private QueueThreadSafe decoderQueue;
        private QueueThreadSafe myQueue;
        private cwipc.pointcloud currentPointCloud;
        Unity.Collections.NativeArray<cwipc.point> currentNativePointArray;
        [Tooltip("URL for pointcloud source server (tcp://host:port)")]
        public string url;
        [Tooltip("If true the pointclouds received are compressed")]
        public bool compressed;
        

        const float allocationFactor = 1.3f;

        public override long currentTimestamp
        {
            get
            {
                if (currentPointCloud == null) return 0;
                return currentPointCloud.timestamp();
            }
        }

        public override FrameMetadata? currentMetadata
        {
            get
            {
                if (currentPointCloud == null) return null;
                return currentPointCloud.metadata;
            }
        }

        private void Start()
        {
            myQueue = new QueueThreadSafe($"{Name()}.queue");
            decoderQueue = new QueueThreadSafe($"{Name()}.decoderQueue");
            InitReader();
        }

        public void Stop()
        {
            reader?.Stop();
            reader = null;
            decoder?.Stop();
        }

        private void InitReader()
        {
            if (reader != null)
            {
                Debug.LogError($"{Name()}: already initialized");
                return;
            }
            string fourcc = compressed ? "cwi1" : "cwi0";
            StreamSupport.IncomingTileDescription[] tileDescriptions = new StreamSupport.IncomingTileDescription[1]
            {
                new StreamSupport.IncomingTileDescription
                {
                    name="0",
                    outQueue=decoderQueue
                }
            };
            reader = new AsyncTCPPCReader(url, fourcc, tileDescriptions);
            if (compressed)
            {
                decoder = new AsyncPCDecoder(decoderQueue, myQueue);
            }
            else
            {
                decoder = new AsyncPCNullDecoder(decoderQueue, myQueue);
            }
        }

        private void OnDestroy()
        {
            Stop();
            if (currentNativePointArray.IsCreated)
            {
                currentNativePointArray.Dispose();
            }
        }

        public override int GetComputeBuffer(ref ComputeBuffer computeBuffer)
        {
            lock(this)
            {
                if (currentPointCloud == null) return 0;
                unsafe
                {
                    //
                    // Get the point cloud data into an unsafe native array.
                    //
                    int currentSizeInBytes = currentPointCloud.get_uncompressed_size();
                    const int sizeofPoint = sizeof(float) * 4;
                    int nPoints = currentSizeInBytes / sizeofPoint;
                    // xxxjack if currentCellsize is != 0 it is the size at which the points should be displayed
                    if (nPoints > currentNativePointArray.Length)
                    {
                        if (currentNativePointArray.Length != 0) currentNativePointArray.Dispose();
                        currentNativePointArray = new Unity.Collections.NativeArray<cwipc.point>(currentSizeInBytes, Unity.Collections.Allocator.Persistent);
                    }
                    if (currentSizeInBytes > 0)
                    {
                        System.IntPtr currentBuffer = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(currentNativePointArray);

                        int ret = currentPointCloud.copy_uncompressed(currentBuffer, currentSizeInBytes);
                        if (ret * sizeofPoint != currentSizeInBytes)
                        {
                            Debug.Log($"PointCloudPreparer decompress size problem: currentSizeInBytes={currentSizeInBytes}, copySize={ret * 16}, #points={ret}");
                            Debug.LogError("Programmer error while rendering a participant.");
                        }
                    }
                    //
                    // Copy the unsafe native array to the computeBuffer
                    //
                    if (computeBuffer == null || computeBuffer.count < nPoints)
                    {
                        int dampedSize = (int)(nPoints * allocationFactor);
                        if (computeBuffer != null) computeBuffer.Release();
                        computeBuffer = new ComputeBuffer(dampedSize, sizeofPoint);
                    }
                    computeBuffer.SetData(currentNativePointArray, 0, 0, nPoints);
                    return nPoints;
                }
            }
        }
        override public int GetPositionsAndColors(ref Vector3[] pointPositions, ref Color[] pointColors)
        {
            if (currentPointCloud == null) return 0;
            int nPoints;
            unsafe
            {
                //
                // Get the point cloud data into an unsafe native array.
                //
                int currentSizeInBytes = currentPointCloud.get_uncompressed_size();
                const int sizeofPoint = sizeof(float) * 4;
                nPoints = currentSizeInBytes / sizeofPoint;
                // xxxjack if currentCellsize is != 0 it is the size at which the points should be displayed
                if (nPoints != currentNativePointArray.Length)
                {
                    if (currentNativePointArray.Length != 0) currentNativePointArray.Dispose();
                    currentNativePointArray = new Unity.Collections.NativeArray<cwipc.point>(nPoints, Unity.Collections.Allocator.Persistent);
                }
                if (currentSizeInBytes > 0)
                {
                    System.IntPtr currentBuffer = (System.IntPtr)Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility.GetUnsafePtr(currentNativePointArray);

                    int ret = currentPointCloud.copy_uncompressed(currentBuffer, currentSizeInBytes);
                    if (ret * sizeofPoint != currentSizeInBytes)
                    {
                        Debug.Log($"PointCloudPreparer decompress size problem: currentSizeInBytes={currentSizeInBytes}, copySize={ret * 16}, #points={ret}");
                        Debug.LogError("Programmer error while rendering a participant.");
                    }
                }
            }

            NativeArray<cwipc.point> points = currentNativePointArray;
            if (points == null || points.Length == 0)
            {
                return 0;
            }
            nPoints = points.Length;
            // Ensure arrays are correctly sized
            if (pointPositions == null || pointPositions.Length != nPoints)
            {
                pointPositions = new Vector3[nPoints];
            }
            if (pointColors == null || pointColors.Length != nPoints)
            {
                pointColors = new Color[nPoints];
            }
            for (int i = 0; i < nPoints; i++)
            {
                pointPositions[i].x = points[i].x;
                pointPositions[i].y = points[i].y;
                pointPositions[i].z = points[i].z;
                pointColors[i].r = points[i].r / 255.0f;
                pointColors[i].g = points[i].g / 255.0f;
                pointColors[i].b = points[i].b / 255.0f;
                pointColors[i].a = 1;
            }
            return nPoints;
        }

        public override float GetPointSize()
        {
            if (currentPointCloud == null) return 0;
            float pointSize = currentPointCloud.cellsize();
            return pointSize;
        }

        public override long getQueueDuration()
        {
            return myQueue.QueuedDuration();
        }

        public override bool LatchFrame()
        {
            if (currentPointCloud != null)
            {
                currentPointCloud.free();
                currentPointCloud = null;
            }
            currentPointCloud = (cwipc.pointcloud)myQueue.TryDequeue(0);
            return currentPointCloud != null;
        }

        public override string Name()
        {
            return $"{GetType().Name}";
        }

        public override void Synchronize()
        {
            
        }

        public override bool EndOfData()
        {
            return myQueue == null || (myQueue.IsClosed() && myQueue.Count() == 0);
        }

    }
}
