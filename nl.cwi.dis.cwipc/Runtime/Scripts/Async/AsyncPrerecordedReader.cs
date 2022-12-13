using System;
using UnityEngine;

namespace Cwipc
{
    public class AsyncPrerecordedReader : AsyncPrerecordedBaseReader, ITileDescriptionProvider
    {
        

        public AsyncPrerecordedReader(string _dirname, float _voxelSize, float _frameRate, QueueThreadSafe _outQueue, QueueThreadSafe _out2Queue = null)
        : base(_dirname, _voxelSize, _frameRate)
        {
        	newTimestamps = true;
            Add(null, _outQueue, _out2Queue);
            // Check that the tileconfig.json is compatible with this reader
            if (qualitySubdirs != null || tileSubdirs != null)
            {
                Debug.LogError($"{Name()}: Directory {_dirname} has per-tile pointclouds, incompatible with this reader");
            }
            Start();
        }

        public override PointCloudTileDescription[] getTiles()
        {
            return tileInfo;
        }

        public override void ReportCurrentTimestamp(long curIndex)
        {
            return;
        }
    }
  
}
