using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    using EncoderStreamDescription = StreamSupport.EncoderStreamDescription;
    using OutgoingStreamDescription = StreamSupport.OutgoingStreamDescription;
    public class PointCloudInputTiled : PointCloudOutput
    {
        protected QueueThreadSafe[] TransmitterInputQueues;
        protected AsyncWorker PCencoder;
        protected AsyncTCPWriter PCtransmitter;

        [Header("Transmission settings")]
        [Tooltip("Specifies TCP server to create as sink, in the form tcp://host:port, for first tile stream. Subsequent tiles get port incremented.")]
        [SerializeField] protected string outputUrl;
        [Tooltip("Insert a compressed pointcloud encoder into the output stream")]
        [SerializeField] protected bool compressedOutputStream;
        [Tooltip("Default octreeBits for compressor (if not overridden by encoderDescriptions")]
        [SerializeField] protected int defaultOctreeBits;
        [Tooltip("Output Stream Descriptions")]
        [SerializeField] protected OutgoingStreamDescription[] transmitterDescriptions;
        [Tooltip("Output encoder parameters. Number and order must match transmitterDescriptions")]
        [SerializeField] protected EncoderStreamDescription[] encoderDescriptions;
        
        protected override void InitializeTransmitter()
        {
            //
            // Override descriptions if not already initialized.
            //

            if (transmitterDescriptions == null || transmitterDescriptions.Length == 0)
            {
                Debug.Log($"PointCloudInputTiled: creating default transmitterDescriptions");
                transmitterDescriptions = new OutgoingStreamDescription[1]
                {
                new OutgoingStreamDescription
                {
                    name="single",
                    tileNumber=0,
                    qualityIndex=0,
                    orientation=Vector3.zero
                }
                };
            }
            if (encoderDescriptions == null || encoderDescriptions.Length == 0)
            {
                Debug.Log($"PointCloudInputTiled: creating default encoderDescriptions");
                encoderDescriptions = new EncoderStreamDescription[1]
                {
                new EncoderStreamDescription
                {
                    octreeBits=defaultOctreeBits,
                    tileNumber=0
                }
                };
            }
            //
            // Create queue from reader to encoder and queues from encoder to transmitter.
            // The first one is declared in our base class, and will be picked up by its
            // Initialize method.
            //
            // The encoders and transmitters are tied together using their unique queue.
            //
            ReaderEncoderQueue = new QueueThreadSafe("ReaderEncoderQueue", 2, true);
            TransmitterInputQueues = new QueueThreadSafe[transmitterDescriptions.Length];
            for(int i= 0; i < transmitterDescriptions.Length; i++)
            {
                var name = transmitterDescriptions[i].name;
                // Note that it is a bit unclear whether to drop or not for the transmitter queue.
                // Not dropping means that all encoders and transmitters will hang if there is no
                // consumer for a specific tile. But dropping means that we may miss (on the receiver side)
                // one tile, and therefore have done a lot of encoding and decoding and transmission for nothing.
                var queue = new QueueThreadSafe($"TransmitterInputQueue#{name}", 2, true);
                TransmitterInputQueues[i] = queue;
                transmitterDescriptions[i].inQueue = queue;
                encoderDescriptions[i].outQueue = queue;
            }
            //
            // Create transmitter.
            //
            string fourcc = compressedOutputStream ? "cwi1" : "cwi0";
            //
            // Create Encoder
            //
            if (compressedOutputStream)
            {
                PCencoder = new AsyncPCEncoder(ReaderEncoderQueue, encoderDescriptions);
            }
            else
            {
                PCencoder = new AsyncPCNullEncoder(ReaderEncoderQueue, encoderDescriptions);
            }
            PCtransmitter = new AsyncTCPWriter(outputUrl, fourcc, transmitterDescriptions);
        }

        void InitializeEncoder()
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

        protected override void OnDestroy()
        {
            base.OnDestroy();
            PCencoder?.StopAndWait();
            PCtransmitter?.StopAndWait();
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
