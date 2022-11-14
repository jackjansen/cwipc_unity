using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    public class PointCloudOutput : MonoBehaviour
    {
        enum SourceType
        {
            Synthetic,
            Realsense,
            Kinect,
            Prerecorded,
            TCP,
        };
        [Tooltip("Type of source to create")]
        [SerializeField] private SourceType sourceType;
        [Tooltip("Renderer to use")]
        [SerializeField] protected PointCloudRenderer PCrenderer;

        [Header("Settings shared by (some) sources")]
        [Tooltip("Frame rate wanted")]
        [SerializeField] private float framerate = 15;
        [Tooltip("Rendering cellsize, if not specified in pointcloud")]
        [SerializeField] private float Preparer_DefaultCellSize = 1.0f;
        [Tooltip("Multiplication factor for pointcloud cellsize")]
        [SerializeField] private float Preparer_CellSizeFactor = 1.0f;

        [Header("Type: Synthetic settings")]
        [Tooltip("Number of points per cloud")]
        [SerializeField] private int Synthetic_NPoints = 8000;

        [Header("Type: Realsense/Kinect settings")]
        [Tooltip("Camera configuration filename")]
        [SerializeField] private string configFileName;
        [Tooltip("If non-zero: voxelize captured pointclouds to this cellsize")]
        [SerializeField] private float voxelSize;

        [Header("Type: prerecorded")]
        [Tooltip("Path of directory with pointcloud files")]
        [SerializeField] private string directoryPath;

        [Header("Type: TCP")]
        [Tooltip("Specifies TCP server, in the form tcp://host:port")]
        [SerializeField] private string url;
        [Tooltip("Insert a compressed pointcloud decoder into the stream")]
        public bool compressedStream;

        QueueThreadSafe ReaderOutputQueue;
        QueueThreadSafe RendererInputQueue;
        protected AsyncPointCloudReader PCcapturer;
        protected AsyncReader PCreceiver;
        protected AsyncFilter PCdecoder;
        protected AsyncPointCloudPreparer PCpreparer;

        // Start is called before the first frame update

        void Start()
        {
            InitializePipeline(); 
        }

        void InitializePipeline()
        {
            ReaderOutputQueue = new QueueThreadSafe("ReaderOutputQueue", 2, true);
            InitializeReader();
            if (RendererInputQueue == null)
            {
                RendererInputQueue = ReaderOutputQueue;
            }
            PCpreparer = new AsyncPointCloudPreparer(ReaderOutputQueue, Preparer_DefaultCellSize, Preparer_CellSizeFactor);
            PCrenderer.SetPreparer(PCpreparer);
        }

        void InitializeReader()
        {
            switch(sourceType)
            {
                case SourceType.Synthetic:
                    PCcapturer = new AsyncSyntheticReader(framerate, Synthetic_NPoints, ReaderOutputQueue);
                    break;
                case SourceType.Realsense:
                    PCcapturer = new AsyncRealsenseReader(configFileName, voxelSize, framerate, ReaderOutputQueue);
                    break;
                case SourceType.Kinect:
                    PCcapturer = new AsyncRealsenseReader(configFileName, voxelSize, framerate, ReaderOutputQueue);
                    break;
                case SourceType.Prerecorded:
                    //PCreceiver = new AsyncPrerecordedReader(directoryPath, voxelSize, framerate, ReaderOutputQueue);
                    break;
                case SourceType.TCP:
                    string fourcc = compressedStream ? "cwi1" : "cwi0";
                    RendererInputQueue = new QueueThreadSafe("DecoderOutputQueue", 2, false);
                    PCreceiver = new AsyncTCPReader(url, fourcc, ReaderOutputQueue);
                    if (compressedStream)
                    {
                        PCdecoder = new AsyncPCDecoder(ReaderOutputQueue, RendererInputQueue);
                    }
                    else
                    {
                        PCdecoder = new AsyncPCNullDecoder(ReaderOutputQueue, RendererInputQueue);
                    }
                    break;
            }
        }

        private void OnDestroy()
        {
            PCcapturer?.StopAndWait();
            PCreceiver?.StopAndWait();
            PCdecoder?.StopAndWait();
            PCpreparer.StopAndWait();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
