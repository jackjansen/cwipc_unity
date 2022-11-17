using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cwipc
{
    using EncoderStreamDescription = StreamSupport.EncoderStreamDescription;
    using OutgoingStreamDescription = StreamSupport.OutgoingStreamDescription;


    public class PointCloudSelfPipelineSimple : PointCloudPipelineSimple
    {
        [Header("Transmission settings")]
        [Tooltip("Transmitter to use (if any)")]
        [SerializeField] protected PointCloudTransmitSimple _transmitter;
        protected override PointCloudTransmitSimple transmitter { get { return _transmitter; } }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
