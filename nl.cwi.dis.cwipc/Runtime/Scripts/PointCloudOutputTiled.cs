using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    public class PointCloudOutputTiled : MonoBehaviour
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
        [SerializeField] protected GameObject PCrendererPrefab;
        
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

        [Header("Introspection/debugging")]
        [Tooltip("Renderers created")]
        [SerializeField] protected PointCloudRenderer[] PCrenderers;

        protected virtual bool enableOutput { get { return true; } }
        protected QueueThreadSafe ReaderRenderQueue;
        protected QueueThreadSafe RendererInputQueue;
        protected QueueThreadSafe ReaderEncoderQueue = null;
        protected AsyncPointCloudReader PCcapturer;
        protected AsyncReader PCreceiver;
        protected AsyncFilter PCdecoder;
        protected AsyncPointCloudPreparer[] PCpreparers;

        // Start is called before the first frame update

        void Start()
        {
            InitializePipeline(); 
        }

        protected virtual void InitializePipeline()
        {
            int nTiles = 1;

            if (enableOutput)
            {
                ReaderRenderQueue = new QueueThreadSafe("ReaderRenderQueue", 2, true);
            }
            InitializeTransmitterQueue();
            InitializeReader();
            InitializeTransmitter();
            if (RendererInputQueue == null)
            {
                RendererInputQueue = ReaderRenderQueue;
            }
            if (enableOutput)
            {
                PCpreparers = new AsyncPointCloudPreparer[nTiles];
                PCrenderers = new PointCloudRenderer[nTiles];
                int tileIndex = 0;
                // Instantiate
                GameObject newGameObject = Instantiate<GameObject>(PCrendererPrefab, transform);
                //newGameObject.SetActive(true);
                PointCloudRenderer newRendererObject = newGameObject.GetComponent<PointCloudRenderer>();
                AsyncPointCloudPreparer newPreparerObject = new AsyncPointCloudPreparer(RendererInputQueue, Preparer_DefaultCellSize, Preparer_CellSizeFactor);
                PCpreparers[tileIndex] = newPreparerObject;
                PCrenderers[tileIndex] = newRendererObject;
                newRendererObject.SetPreparer(newPreparerObject);
            }
            else
            {
            }
        }

        protected virtual void InitializeTransmitterQueue()
        {

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
            if (PCpreparers != null)
            {
                foreach(var p in PCpreparers)
                {
                    p.StopAndWait();
                }
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
