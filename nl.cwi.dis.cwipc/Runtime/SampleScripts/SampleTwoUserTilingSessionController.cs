using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cwipc;

public class SampleTwoUserTilingSessionController : SampleTwoUserSessionController
{
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
        Debug.Log($"SampleTwoUserSessionController: initialized other: receiver for {secondHost}");
        otherPipeline.gameObject.SetActive(true);
    }
}
