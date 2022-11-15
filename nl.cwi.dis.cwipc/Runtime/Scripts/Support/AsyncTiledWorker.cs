using System;
using UnityEngine;

namespace Cwipc
{
    /// <summary>
    /// Abstract class for an AsyncWorker that is expected to optionally produce tiled frames.
    /// Only really implemented for tiled pointcloud capturers, readers and receivers.
    /// </summary>
    public abstract class AsyncTiledWorker : AsyncWorker, ITileDescriptionProvider
    {


        public AsyncTiledWorker() : base()
        {
        }
        /// <summary>
        /// Return array of tiles produced  by this reader.
        /// </summary>
        /// <returns></returns>
        virtual public PointCloudTileDescription[] getTiles()
        {
            return null;
        }
    }
}
