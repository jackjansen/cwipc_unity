using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    using IncomingTileDescription = StreamSupport.IncomingTileDescription;
    using IncomingStreamDescription = StreamSupport.IncomingStreamDescription;
    public class PointCloudOutputTiled : MonoBehaviour
    {
        public enum SourceType
        {
            TCP,
        };
        [Tooltip("Type of source to create")]
        [SerializeField] public SourceType sourceType;
        [Tooltip("Number of tiles to receive. Should match the number on the other side")]
        [SerializeField] protected int nTiles = 1;
        [Tooltip("Renderer to clone, one for each tile")]
        [SerializeField] protected GameObject PCrendererPrefab;
        
        [Header("Settings shared by (some) sources")]
        [Tooltip("Rendering cellsize, if not specified in pointcloud")]
        [SerializeField] protected float Preparer_DefaultCellSize = 1.0f;
        [Tooltip("Multiplication factor for pointcloud cellsize")]
        [SerializeField] protected float Preparer_CellSizeFactor = 1.0f;

        [Header("Source type: TCP")]
        [Tooltip("Specifies TCP server to contact for source, in the form tcp://host:port")]
        [SerializeField] protected string inputUrl;
        [Tooltip("Insert a compressed pointcloud decoder into the stream")]
        public bool compressedInputStream;

        [Header("Introspection/debugging")]
        [Tooltip("Renderers created")]
        [SerializeField] protected PointCloudRenderer[] PCrenderers;

        protected QueueThreadSafe[] ReaderDecoderQueues;
        protected QueueThreadSafe[] DecoderPreparerQueues;
        protected AsyncReader PCreceiver;
        protected AsyncFilter[] PCdecoders;
        protected AsyncPointCloudPreparer[] PCpreparers;

        // Start is called before the first frame update

        void Start()
        {
            InitializePipeline(); 
        }

        protected virtual void InitializePipeline()
        {
            //
            // Create the queues
            //
            ReaderDecoderQueues = new QueueThreadSafe[nTiles];
            DecoderPreparerQueues = new QueueThreadSafe[nTiles];
            //
            // Create the incoming tile/stream descriptions
            //
            string fourcc = compressedInputStream ? "cwi1" : "cwi0";
            IncomingTileDescription[] tileDescription = new IncomingTileDescription[nTiles];
            for (int i=0; i<nTiles; i++)
            {
                ReaderDecoderQueues[i] = new QueueThreadSafe($"ReaderDecoderQueue#{i}", 2, true);
                DecoderPreparerQueues[i] = new QueueThreadSafe($"DecoderPreparerQueue#{i}", 2, false);
                // xxxjack we make this up for now
                IncomingStreamDescription[] sds = new IncomingStreamDescription[1]
                {
                    new IncomingStreamDescription
                    {
                        streamIndex = i,
                        tileNumber = i,
                        orientation = Vector3.zero
                    }
                };
                tileDescription[i] = new IncomingTileDescription()
                {
                    name = $"tile#{i}",
                    tileNumber = i,
                    outQueue = ReaderDecoderQueues[i],
                    streamDescriptors = sds
                };
            }
            //
            // Create the receiver
            //
            PCreceiver = new AsyncTCPPCReader(inputUrl, fourcc, tileDescription);
            //
            // Create the decoders, preparers and renderers
            //
           
            PCpreparers = new AsyncPointCloudPreparer[nTiles];
            PCrenderers = new PointCloudRenderer[nTiles];
            for (int tileIndex = 0; tileIndex < nTiles; tileIndex++)
            {
                AsyncFilter newDecoderObject = CreateDecoder(ReaderDecoderQueues[tileIndex], DecoderPreparerQueues[tileIndex]);
                GameObject newGameObject = Instantiate<GameObject>(PCrendererPrefab, transform);
                PointCloudRenderer newRendererObject = newGameObject.GetComponent<PointCloudRenderer>();
                AsyncPointCloudPreparer newPreparerObject = new AsyncPointCloudPreparer(DecoderPreparerQueues[tileIndex], Preparer_DefaultCellSize, Preparer_CellSizeFactor);
                PCpreparers[tileIndex] = newPreparerObject;
                PCrenderers[tileIndex] = newRendererObject;
                newRendererObject.SetPreparer(newPreparerObject);
            }
        }

        
        AsyncFilter CreateDecoder(QueueThreadSafe inQueue, QueueThreadSafe outQueue)
        {
            if (compressedInputStream)
            {
                return new AsyncPCDecoder(inQueue, outQueue);
            }
            else
            {
                return new AsyncPCNullDecoder(inQueue, outQueue);
            }

        }

        protected virtual void OnDestroy()
        {
            PCreceiver?.StopAndWait();
            if (PCdecoders != null)
            {
                foreach(var d in PCdecoders)
                {
                    d.StopAndWait();
                }
            }
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
