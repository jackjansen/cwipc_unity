using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    public class PointCloudOutput : MonoBehaviour
    {
        public enum SourceType
        {
            Synthetic,
            Realsense,
            Kinect,
            Prerecorded,
            TCP,
        };
        [Tooltip("Type of source to create")]
        [SerializeField] public SourceType sourceType;
        [Tooltip("Renderer to use")]
        [SerializeField] protected PointCloudRenderer PCrenderer;

        [Header("Settings shared by (some) sources")]
        [Tooltip("Frame rate wanted")]
        [SerializeField] protected float framerate = 15;
        [Tooltip("Rendering cellsize, if not specified in pointcloud")]
        [SerializeField] protected float Preparer_DefaultCellSize = 1.0f;
        [Tooltip("Multiplication factor for pointcloud cellsize")]
        [SerializeField] protected float Preparer_CellSizeFactor = 1.0f;

        [Header("Source type: Synthetic settings")]
        [Tooltip("Number of points per cloud")]
        [SerializeField] protected int Synthetic_NPoints = 8000;

        [Header("Source type: Realsense/Kinect settings")]
        [Tooltip("Camera configuration filename")]
        [SerializeField] protected string configFileName;
        [Tooltip("If non-zero: voxelize captured pointclouds to this cellsize")]
        [SerializeField] protected float voxelSize;

        [Header("Source type: prerecorded")]
        [Tooltip("Path of directory with pointcloud files")]
        [SerializeField] protected string directoryPath;

        [Header("Source type: TCP")]
        [Tooltip("Specifies TCP server to contact for source, in the form tcp://host:port")]
        [SerializeField] protected string inputUrl;
        [Tooltip("Insert a compressed pointcloud decoder into the stream")]
        public bool compressedInputStream;

        protected QueueThreadSafe ReaderRenderQueue;
        protected QueueThreadSafe RendererInputQueue;
        protected QueueThreadSafe ReaderEncoderQueue = null;
        protected AsyncPointCloudReader PCcapturer;
        protected AsyncReader PCreceiver;
        protected AsyncFilter PCdecoder;
        protected AsyncPointCloudPreparer PCpreparer;

        // Start is called before the first frame update

        void Start()
        {
            InitializePipeline(); 
        }

        protected virtual void InitializePipeline()
        {
            ReaderRenderQueue = new QueueThreadSafe("ReaderRenderQueue", 2, true);
            InitializeTransmitter();
            InitializeReader();
            if (RendererInputQueue == null)
            {
                RendererInputQueue = ReaderRenderQueue;
            }
            PCpreparer = new AsyncPointCloudPreparer(RendererInputQueue, Preparer_DefaultCellSize, Preparer_CellSizeFactor);
            PCrenderer.SetPreparer(PCpreparer);
        }

        protected virtual void InitializeTransmitter()
        {

        }

        void InitializeReader()
        {
            switch(sourceType)
            {
                case SourceType.Synthetic:
                    PCcapturer = new AsyncSyntheticReader(framerate, Synthetic_NPoints, ReaderRenderQueue, ReaderEncoderQueue);
                    break;
                case SourceType.Realsense:
                    PCcapturer = new AsyncRealsenseReader(configFileName, voxelSize, framerate, ReaderRenderQueue, ReaderEncoderQueue);
                    break;
                case SourceType.Kinect:
                    PCcapturer = new AsyncRealsenseReader(configFileName, voxelSize, framerate, ReaderRenderQueue, ReaderEncoderQueue);
                    break;
                case SourceType.Prerecorded:
                    //PCreceiver = new AsyncPrerecordedReader(directoryPath, voxelSize, framerate, ReaderOutputQueue, ReaderEncoderQueue);
                    break;
                case SourceType.TCP:
                    InitializeDecoder();
                    break;
            }
        }

        void InitializeDecoder()
        {
            string fourcc = compressedInputStream ? "cwi1" : "cwi0";
            RendererInputQueue = new QueueThreadSafe("DecoderOutputQueue", 2, false);
            PCreceiver = new AsyncTCPReader(inputUrl, fourcc, ReaderRenderQueue);
            if (compressedInputStream)
            {
                PCdecoder = new AsyncPCDecoder(ReaderRenderQueue, RendererInputQueue);
            }
            else
            {
                PCdecoder = new AsyncPCNullDecoder(ReaderRenderQueue, RendererInputQueue);
            }

        }

        protected virtual void OnDestroy()
        {
            PCcapturer?.StopAndWait();
            PCreceiver?.StopAndWait();
            PCdecoder?.StopAndWait();
            PCpreparer?.StopAndWait();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
