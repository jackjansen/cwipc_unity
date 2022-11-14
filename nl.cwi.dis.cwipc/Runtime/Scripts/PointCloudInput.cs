using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    public class PointCloudInput : PointCloudOutput
    {
        protected QueueThreadSafe TransmitterInputQueue;
        protected AsyncWorker PCencoder;
        protected AsyncTCPWriter PCtransmitter;

        [Header("Transmission settings")]
        [Tooltip("Specifies TCP server to create as sink, in the form tcp://host:port")]
        [SerializeField] protected string outputUrl;
        [Tooltip("Insert a compressed pointcloud encoder into the output stream")]
        [SerializeField] protected bool compressedOutputStream;
        [Tooltip("For compressed streams: how many bits in the octree. Higher numbers are higher quality")]
        [SerializeField] protected int octreeBits;

        protected override void InitializeTransmitter()
        {
            //
            // Create queue from reader to encoder and queue from encoder to transmitter.
            // The first one is declared in our base class, and will be picked up by its
            // Initialize method.
            //
            ReaderEncoderQueue = new QueueThreadSafe("ReaderEncoderQueue", 2, true);
            TransmitterInputQueue = new QueueThreadSafe("TransmitterInputQueue", 2, false);
            //
            // Create transmitter.
            //
            string fourcc = compressedOutputStream ? "cwi1" : "cwi0";

            StreamSupport.OutgoingStreamDescription[] transmitterDescriptions = new StreamSupport.OutgoingStreamDescription[1]
            {
                new StreamSupport.OutgoingStreamDescription
                {
                    name="single",
                    tileNumber=0,
                    qualityIndex=0,
                    orientation=Vector3.zero,
                    inQueue=TransmitterInputQueue
                }
            };
            AsyncPCEncoder.EncoderStreamDescription[] encoderDescriptions = new AsyncPCEncoder.EncoderStreamDescription[1]
            {
                new AsyncPCEncoder.EncoderStreamDescription
                {
                    octreeBits=octreeBits,
                    tileNumber=0,
                    outQueue=TransmitterInputQueue
                }
            };
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
