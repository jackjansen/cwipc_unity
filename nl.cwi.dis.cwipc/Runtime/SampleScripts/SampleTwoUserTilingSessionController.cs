using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cwipc;
using System.Threading;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

public class SampleTwoUserTilingSessionController : SampleTwoUserSessionController
{
    [Tooltip("Disable tiling for this session")]
    [SerializeField] protected bool untiledSession = false;
    [Tooltip("For compressed session: levels of octree depth to compress to")]
    [SerializeField] protected int[] octreeDepths = new int[] { 10 };
    [Header("Introspection")]
    [SerializeField] private StreamSupport.PointCloudNetworkTileDescription ourTileDescription;
    [SerializeField] private StreamSupport.PointCloudNetworkTileDescription theirTileDescription;

    protected SimpleSocketReceiver controlReceiver;
    protected SimpleSocketSender controlSender;

    private void OnDestroy()
    {
        controlSender?.Stop();
        controlSender = null;
        controlReceiver?.Stop();
        controlReceiver = null;
    }

    private void Update()
    {
        if (controlReceiver != null)
        {
            string msg = controlReceiver.Receive();
            if (msg != null) {
                // xxxjack handle message
                Debug.Log($"SampleTwoUserTilingSessionController: Received message \"{msg}\"");
                theirTileDescription = JsonUtility.FromJson<StreamSupport.PointCloudNetworkTileDescription>(msg);
                streamDescriptionReceived = true;
            }
        }
        if (otherInitialized) return;
        if (streamDescriptionReceived)
        {
            otherInitialized = true;
            InitializeOther();
        }
    }

    /// <summary>
    /// Initialize the base class (host names), then create the server to and client to
    /// communicate control information with the other side.
    /// </summary>
    protected override void Initialize()
    {
        streamDescriptionReceived = false;
        base.Initialize();
        string senderUrl = $"tcp://{firstHost}:4300";
        string receiverUrl = $"tcp://{secondHost}:4300";
        controlSender = new SimpleSocketSender(senderUrl);
        controlReceiver = new SimpleSocketReceiver(receiverUrl);
    }

    /// <summary>
    /// Initialize our pointcloud pipeline.
    /// </summary>
    protected override void InitializeSelf()
    {
        PointCloudSelfPipelineTiled pipeline = selfPipeline.GetComponent<PointCloudSelfPipelineTiled>();
        AbstractPointCloudSink transmitter = pipeline?.transmitter;
        if (transmitter == null) Debug.LogError($"SampleTowUserSessionController: transmitter is null for {selfPipeline}");
        transmitter.sinkType = AbstractPointCloudSink.SinkType.TCP;
        transmitter.outputUrl = $"tcp://{firstHost}:4303";
        transmitter.compressedOutputStreams = useCompression;
        Debug.Log($"SampleTwoUserSessionController: initialized self: transmitter on {firstHost}");
        //
        // Send message to other side, describg our tiling and compression streams
        //
        PointCloudTileDescription[] tilesToTransmit = pipeline.getTiles();
        if (tilesToTransmit == null)
        {
            // If there is no tile information we assume a single tile.
            tilesToTransmit = new PointCloudTileDescription[1]
            {
                new PointCloudTileDescription()
                {
                    cameraMask=0,
                    cameraName="untiled",
                    normal=Vector3.zero
                }
            };
        }
        if (untiledSession)
        {
            // If we want an untiled session we assume tile 0 is the untiled pointcloud stream.
            tilesToTransmit = tilesToTransmit[..1];
        }
        else if(tilesToTransmit.Length > 1)
        {
            // We assume tile 0 is the untiled representation and remove it.
            tilesToTransmit = tilesToTransmit[1..];
        }
        ourTileDescription = StreamSupport.CreateNetworkTileDescription(tilesToTransmit, octreeDepths);
        string message = JsonUtility.ToJson(ourTileDescription);
        Debug.Log($"xxxjack send message: {message}");
        controlSender.Send(message);

    }

    /// <summary>
    /// Initialize the other pointcloud pipeline and enable it.
    /// </summary>
    protected override void InitializeOther()
    {

        PointCloudPipelineTiled receiver = otherPipeline.GetComponent<PointCloudPipelineTiled>();
        if (receiver == null) Debug.LogError($"SampleTowUserSessionController: receiver is null for {otherPipeline}");
        receiver.sourceType = PointCloudPipelineTiled.SourceType.TCP;
        receiver.inputUrl = $"tcp://{secondHost}:4303";
        receiver.compressedInputStream = useCompression;
        StreamSupport.IncomingTileDescription[] incomingTileDescription = StreamSupport.CreateIncomingTileDescription(theirTileDescription);
        receiver.SetTileDescription(incomingTileDescription);
        Debug.Log($"SampleTwoUserSessionController: initialized other: receiver for {secondHost}");
        otherPipeline.gameObject.SetActive(true);
    }

    protected class SimpleSocketReceiver : AsyncTCPReader
    {
        QueueThreadSafe myReceiveQueue = new QueueThreadSafe("SimpleSocketReceiver", 1, false);

        public SimpleSocketReceiver(string _url) : base(_url)
        {
            receivers = new ReceiverInfo[]
            {
                new ReceiverInfo()
                {
                    outQueue=myReceiveQueue,
                    host=url.Host,
                    port=url.Port,
                    fourcc=0x60606060
                }
            };
            Start();
        }

        public string Receive()
        {
            BaseMemoryChunk packet = myReceiveQueue.TryDequeue(0);
            if (packet == null) return null;
            string packetString = System.Runtime.InteropServices.Marshal.PtrToStringUTF8(packet.pointer, packet.length);
            return packetString;
        }
    }

    protected class SimpleSocketSender : AsyncTCPWriter
    {
        QueueThreadSafe mySendQueue = new QueueThreadSafe("SimpleSocketSender", 1, false);

        public SimpleSocketSender(string _url) : base()
        {
            Uri url = new Uri(_url);
            descriptions = new TCPStreamDescription[]
            {
                new TCPStreamDescription()
                {
                    host=url.Host,
                    port=url.Port,
                    fourcc=0x60606060,
                    inQueue=mySendQueue
                }
            };
            Start();
        }

        public override void Stop()
        {
            if (!mySendQueue.IsClosed())
            {
                mySendQueue.Close();
            }
         }

        public void Send(string message)
        {
            byte[] messageBytes = System.Text.UTF8Encoding.UTF8.GetBytes(message);
            NativeMemoryChunk packet = new NativeMemoryChunk(messageBytes.Length);
            System.Runtime.InteropServices.Marshal.Copy(messageBytes, 0, packet.pointer, packet.length);
            mySendQueue.Enqueue(packet);
        }
    }
}
