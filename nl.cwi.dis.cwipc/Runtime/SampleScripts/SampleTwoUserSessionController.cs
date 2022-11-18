using System.Collections;
using System.Net;
using UnityEngine;
using Cwipc;

public class SampleTwoUserSessionController : MonoBehaviour
{
    [Tooltip("Single machine session: use localhost for sender and receiver")]
    [SerializeField] bool singleMachineSession = true;
    [Header("For 2-machine sessions give both hostnames. Each instance will find its own.")]
    [Tooltip("Host name or IP address")]
    [SerializeField] string firstHost;
    [Tooltip("Host name or IP address")]
    [SerializeField] string secondHost;
    [Tooltip("Self: capturer, self-view, compressor, transmitter GameObject")]
    [SerializeField] PointCloudPipelineSimple selfPipeline;
    [Tooltip("Other:, receiver, decompressor, view GameObject")]
    [SerializeField] PointCloudPipelineSimple otherPipeline;
    [Tooltip("Whether to use compression in this session")]
    [SerializeField] bool useCompression = true;

    // Start is called before the first frame update
    void Start()
    {
        Initialize();
        InitializeSelf();
        InitializeOther();
    }

    protected void Initialize()
    {
        if (singleMachineSession)
        {
            firstHost = "localhost";
            secondHost = "localhost";
        }
        // See if we need to swap the hostnames (if we are second)
        IPHostEntry ourHostEntry = Dns.GetHostEntry(Dns.GetHostName());
        IPHostEntry secondHostEntry = Dns.GetHostEntry(secondHost);
        bool secondIsOurs = false;
        foreach(var ip1 in ourHostEntry.AddressList)
        {
            foreach(var ip2 in secondHostEntry.AddressList)
            {
                if (ip1.Equals(ip2))
                {
                    secondIsOurs = true;
                }
            }
        }
        if (secondIsOurs)
        {
            string swap = secondHost;
            secondHost = firstHost;
            firstHost = swap;
        }
        // Disable other until we have all the information needed.
        otherPipeline.gameObject.SetActive(false);
    }

    protected void InitializeSelf()
    {
        AbstractPointCloudSink transmitter = selfPipeline.transmitter;
        if (transmitter == null) Debug.LogError($"SampleTowUserSessionController: transmitter is null for {selfPipeline}");
        transmitter.sinkType = AbstractPointCloudSink.SinkType.TCP;
        transmitter.outputUrl = $"tcp://{firstHost}:4303";
        transmitter.compressedOutputStreams = useCompression;
        Debug.Log($"SampleTwoUserSessionController: initialized self: transmitter on {firstHost}");

    }

    protected void InitializeOther()
    {

        PointCloudPipelineSimple receiver = otherPipeline;
        if (receiver == null) Debug.LogError($"SampleTowUserSessionController: receiver is null for {otherPipeline}");
        receiver.sourceType = PointCloudPipelineSimple.SourceType.TCP;
        receiver.inputUrl = $"tcp://{secondHost}:4303";
        receiver.compressedInputStream = useCompression;
        Debug.Log($"SampleTwoUserSessionController: initialized other: receiver for {secondHost}");
        otherPipeline.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
