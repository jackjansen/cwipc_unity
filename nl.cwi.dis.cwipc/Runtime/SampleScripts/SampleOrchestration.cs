using System;
using System.Collections;
using System.Collections.Generic;
using Cwipc;
using UnityEngine;

public class SampleOrchestration : MonoBehaviour
{
    protected SimpleSocketReceiver controlReceiver;
    protected SimpleSocketSender controlSender;
    protected Action<string> callback;

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
            if (msg != null)
            {
                // xxxjack handle message
                Debug.Log($"SampleTwoUserTilingSessionController: Received message \"{msg}\"");
                if (callback != null)
                {
                    callback(msg);
                }
            }
        }
    }

    /// <summary>
    /// Initialize the base class (host names), then create the server to and client to
    /// communicate control information with the other side.
    /// </summary>
    public void Initialize(string senderUrl, string receiverUrl)
    {
        controlSender = new SimpleSocketSender(senderUrl);
        controlReceiver = new SimpleSocketReceiver(receiverUrl);
    }

    public void Send<T>(T message)
    {
        string msg = JsonUtility.ToJson(message);
        controlSender.Send(msg);
    }

    public void RegisterCallback<T>(Action<T> _callback)
    {
        callback = (string s) => _callback(JsonUtility.FromJson<T>(s));
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
