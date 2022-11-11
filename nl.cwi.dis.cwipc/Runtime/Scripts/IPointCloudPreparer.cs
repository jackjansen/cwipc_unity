using UnityEngine;

namespace Cwipc
{
    using Timestamp = System.Int64;
    using Timedelta = System.Int64;

    /// <summary>
    /// Provide media objects (pointclouds, audio, video) to renderers or other consumers.
    /// This interface is provided by media stream readers and decoders and such.
    /// Base interface handles synchronization between mutiple media streams.
    /// </summary>
    public interface IPreparer
    {
        /// <summary>
        /// Prepare synchronizer. Called on all preparers in a group so they can determine possible
        /// range of frame to display.
        /// </summary>
        public void Synchronize();

        /// <summary>
        /// Lock the synchronizer. Called on all preparers so they can determine which frame they will return.
        /// After this the various Get methods of the per-media-type interface subclasses can be used to obtain the
        /// data.
        /// </summary>
        /// <returns>False if no suitable frame is available</returns>
        public bool LatchFrame();

        /// <summary>
        /// Returns input queue length: how much input data is available for this preparer.
        /// Mainly for statstics printing.
        /// </summary>
        /// <returns>Input queue duration in milliseconds</returns>
        public Timedelta getQueueDuration();
    }

    /// <summary>
    /// Provide pointclouds to a renderer (or other consumer).
    /// </summary>
    public interface IPointcloudPreparer : IPreparer
    {
        /// <summary>
        /// Store pointcloud data of frame locked by LatchFrame in a ComputeBuffer.
        /// ComputBuffer must be pre-allocated and will be increased in size to make the data fit.
        /// </summary>
        /// <param name="computeBuffer">Where the pointcloud data is stored.</param>
        /// <returns>Number of points in the pointcloud</returns>
        public int GetComputeBuffer(ref ComputeBuffer computeBuffer);

        /// <summary>
        /// Return size (in meters) of a single cell/point in the current pointcloud.
        /// Used for rendering the points at the correct size.
        /// </summary>
        /// <returns>Size of a cell</returns>
        public float GetPointSize();
    }

    abstract public class AbstractPointCloudPreparer : MonoBehaviour, IPointcloudPreparer
    {
        abstract public void Synchronize();
        abstract public bool LatchFrame();
        abstract public int GetComputeBuffer(ref ComputeBuffer computeBuffer);
        abstract public float GetPointSize();
        abstract public Timedelta getQueueDuration();
    }
}