using System;
using UnityEngine;

namespace Cwipc
{
    /// <summary>
    /// Abstract class for an AsyncWorker that is expected to optionally produce tiled frames.
    /// Only really implemented for tiled pointcloud capturers, readers and receivers.
    /// </summary>
    public abstract class AsyncTiledWorker : AsyncWorker
    {
        /// <summary>
        /// Structure defining a pointcloud tile.
        /// </summary>
        [Serializable]
        public struct TileInfo
        {
            /// <summary>
            /// Direction of this tile, as seen from the centroid of the pointcloud.
            /// </summary>
            public Vector3 normal;
            /// <summary>
            /// Name (or serial number, or other identifier) of the camera that created this tile.
            /// </summary>
            public string cameraName;
            /// <summary>
            /// 8-bit bitmask representing this tile in each point.
            /// </summary>
            public int cameraMask;
        }

        public AsyncTiledWorker() : base()
        {
        }
        /// <summary>
        /// Return array of tiles produced  by this reader.
        /// </summary>
        /// <returns></returns>
        virtual public TileInfo[] getTiles()
        {
            return null;
        }
    }
}
