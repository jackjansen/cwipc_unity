using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    using EncoderStreamDescription = StreamSupport.EncoderStreamDescription;
    using OutgoingStreamDescription = StreamSupport.OutgoingStreamDescription;

    public class PointCloudTransmitSimple : MonoBehaviour
    {
        public enum SinkType
        {
            TCP,
        }
        [Tooltip("Type of output sink (protocol)")]
        [SerializeField] protected SinkType sinkType;
        [Tooltip("Specifies TCP server to create as sink, in the form tcp://host:port")]
        [SerializeField] protected string outputUrl;
        [Tooltip("Insert a compressed pointcloud encoder into the output stream")]
        [SerializeField] protected bool compressedOutputStream;
        [Tooltip("For compressed streams: how many bits in the octree. Higher numbers are higher quality")]
        [SerializeField] protected int octreeBits;

        protected QueueThreadSafe ReaderEncoderQueue;
        protected QueueThreadSafe TransmitterInputQueue;
        protected AsyncWorker PCencoder;
        protected AsyncTCPWriter PCtransmitter;

        public QueueThreadSafe InitializeTransmitterQueue()
        {
            ReaderEncoderQueue = new QueueThreadSafe("ReaderEncoderQueue", 2, true);
            return ReaderEncoderQueue;
        }

        public void InitializeTransmitter()
        {
            //
            // Create queue from reader to encoder and queue from encoder to transmitter.
            // The first one is declared in our base class, and will be picked up by its
            // Initialize method.
            //
            TransmitterInputQueue = new QueueThreadSafe("TransmitterInputQueue", 2, false);
            //
            // Create transmitter.
            //
            string fourcc = compressedOutputStream ? "cwi1" : "cwi0";

            OutgoingStreamDescription[] transmitterDescriptions = new OutgoingStreamDescription[1]
            {
                new OutgoingStreamDescription
                {
                    name="single",
                    tileNumber=0,
                    qualityIndex=0,
                    orientation=Vector3.zero,
                    inQueue=TransmitterInputQueue
                }
            };
            EncoderStreamDescription[] encoderDescriptions = new EncoderStreamDescription[1]
            {
                new EncoderStreamDescription
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


        protected void OnDestroy()
        {
            PCencoder?.StopAndWait();
            PCtransmitter?.StopAndWait();
        }
    }
}