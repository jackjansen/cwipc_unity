using System;
using System.Resources;
using Unity.Collections;
using UnityEngine;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    public class StreamedPointCloudReader : AbstractPointCloudSource
    {
        new private AsyncTCPReader reader;
        private AsyncPCDecoder decoder;
        private QueueThreadSafe myReaderQueue;
        private QueueThreadSafe myDecoderQueue;
        private cwipc.pointcloud currentPointCloud;
        Unity.Collections.NativeArray<cwipc.point> currentNativePointArray;

        [Tooltip("URL to play back compressed point clouds from")]
        public string url;
        
        [Tooltip("Point size to use if a point cloud does not contain a pointsize")]
        [SerializeField] float defaultPointSize = 0;

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

        private void Awake()
        {
            myReaderQueue = new QueueThreadSafe($"{Name()}.readerQueue");
            myDecoderQueue = new QueueThreadSafe($"{Name()}.decoderQueue");
        }

        new public void Start()
        {
            InitReader();
        }

        new public void Stop()
        {
            decoder?.Stop();
            decoder = null;
            reader?.Stop();
            reader = null;
            if (myDecoderQueue != null) {
                BaseMemoryChunk chunk;
                do {
                    chunk = myDecoderQueue.TryDequeue(0);
                    if (chunk != null) {
                        chunk.free();
                    }
                } while(chunk != null);
                myDecoderQueue = null;
            }
            if (myReaderQueue != null) {
                BaseMemoryChunk chunk;
                do {
                    chunk = myReaderQueue.TryDequeue(0);
                    if (chunk != null) {
                        chunk.free();
                    }
                } while(chunk != null);
                myReaderQueue = null;
            }
        }

        private void InitReader()
        {
            if (reader != null || decoder != null)
            {
                Stop();
            }
            string fourcc = "cwi1";
            reader = new AsyncTCPReader(url, fourcc, myReaderQueue);
            decoder = new AsyncPCDecoder(myReaderQueue, myDecoderQueue);
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
                    //
                    // Copy the unsafe native array to the computeBuffer
                    //
                    if (computeBuffer == null || computeBuffer.count < nPoints)
                    {
                        int dampedSize = (int)(nPoints * allocationFactor);
                        if (computeBuffer != null) computeBuffer.Release();
                        computeBuffer = new ComputeBuffer(dampedSize, sizeofPoint);
                    }
                    NativeArray<byte> currentNativeByteArray = currentNativePointArray.Reinterpret<byte>(16);
                    computeBuffer.SetData(currentNativeByteArray, 0, 0, nPoints*16);
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
            if (pointSize == 0) pointSize = defaultPointSize;
            return pointSize;
        }

        public override long getQueueDuration()
        {
            return myDecoderQueue.QueuedDuration();
        }

        public override bool LatchFrame()
        {
            if (currentPointCloud != null)
            {
                currentPointCloud.free();
                currentPointCloud = null;
            }
            currentPointCloud = (cwipc.pointcloud)myDecoderQueue.TryDequeue(0);
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
            return myDecoderQueue == null || (myDecoderQueue.IsClosed() && myDecoderQueue.Count() == 0);
        }

    }
}
