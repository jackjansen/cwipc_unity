using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    abstract public class BasePointCloudTransmitter : MonoBehaviour
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
        [SerializeField] protected bool compressedOutputStreams;
        [Tooltip("For compressed streams: how many bits in the octree. Higher numbers are higher quality")]
        [SerializeField] protected int defaultOctreeBits;

        protected QueueThreadSafe ReaderEncoderQueue;
        protected AsyncWorker PCencoder;
        protected AsyncTCPWriter PCtransmitter;

        public QueueThreadSafe InitializeTransmitterQueue()
        {
            ReaderEncoderQueue = new QueueThreadSafe("ReaderEncoderQueue", 2, true);
            return ReaderEncoderQueue;
        }

        abstract public void InitializeTransmitter(PointCloudTileDescription[] tileDescriptions);


        protected void OnDestroy()
        {
            PCencoder?.StopAndWait();
            PCtransmitter?.StopAndWait();
        }
    }
}