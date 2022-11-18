using System;
using UnityEngine;

namespace Cwipc
{
    /// <summary>
    /// Structures and methods used to help implementing streaming pointclouds (and other media)
    /// across the net.
    /// </summary>
    public class StreamSupport
    {
        /// <summary>
        /// Helper method to convert 4 characters into a 32-bit 4CC.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        static public uint VRT_4CC(char a, char b, char c, char d)
        {
            return (uint)(a << 24 | b << 16 | c << 8 | d);
        }

        /// <summary>
        /// Structure describing a single outgoing (tile) stream.
        /// Can really only be used for tiled pointclouds.
        /// </summary>
        [Serializable]
        public struct OutgoingStreamDescription
        {
            /// <summary>
            /// Name, for debugging and for Dash manifest
            /// </summary>
            public string name;
            /// <summary>
            /// Index of this tile stream.
            /// </summary>
            public uint tileNumber;
            /// <summary>
            /// Index of the quality, for multi-quality streams.
            /// </summary>
            public int qualityIndex;
            /// <summary>
            /// Indication of the relative direction this tile points (relative to the pointcloud centroid)
            /// </summary>
            public Vector3 orientation;
            /// <summary>
            /// The queue on which the producer will produce pointclouds for this writer to transmit.
            /// </summary>
            public QueueThreadSafe inQueue;
        }

        /// <summary>
        /// Structure describing the parameters of a single encoder (possibly part of a multi-tile, multi-quality
        /// encoder group).
        /// </summary>
        [Serializable]
        public struct EncoderStreamDescription
        {
            /// <summary>
            /// Encoder parameter. Depth of the octree used during encoding. Compressed pointcloud will have at most 8**octreeBits points.
            /// </summary>
            public int octreeBits;
            /// <summary>
            /// Tile number to filter pointcloud on before encoding. 0 means no filtering.
            /// </summary>
            public int tileNumber;
            /// <summary>
            /// Output queue for this encoder, will usually be shared with the corresponding transmitter (as its input queue).
            /// </summary>
            public QueueThreadSafe outQueue;
        }

        /// <summary>
        /// Structure describing a single incoming (tiled, single quality) stream.
        /// Can really only be used for pointclouds.
        /// </summary>
        [Serializable]
        public struct IncomingStreamDescription
        {
            /// <summary>
            /// Index of the stream (in the underlying TCP or Dash protocol)
            /// </summary>
            public int streamIndex;
            /// <summary>
            /// Tile number for the pointclouds received on this stream.
            /// </summary>
            public int tileNumber;
            /// <summary>
            /// Indication of the relative direction this tile points (relative to the pointcloud centroid)
            /// </summary>
            public Vector3 orientation;
        }

        /// <summary>
        /// Structure describing a set of multi-quality streams for a single tile.
        /// </summary>
        [Serializable]
        public struct IncomingTileDescription
        {
            /// <summary>
            /// Name of the stream (for Dash manifest and for debugging/statistics printing)
            /// </summary>
            public string name;
            /// <summary>
            /// The queue on which frames for this stream will be deposited
            /// </summary>
            public QueueThreadSafe outQueue;
            /// <summary>
            /// Index of the tile
            /// </summary>
            public int tileNumber;
            /// <summary>
            /// Streams used for this tile (for its multiple qualities)
            /// </summary>
            public IncomingStreamDescription[] streamDescriptors;
        }

        /// <summary>
        /// Structure describing the available tiles, and what representations are available
        /// for each tile, how "good" each representation is and how much bandwidth it uses.
        /// </summary>
        [Serializable]
        public struct PointCloudNetworkTileDescription
        {
            /// <summary>
            /// Structure describing a single tile.
            /// </summary>
            [Serializable]
            public struct NetworkTileInformation
            {
                /// <summary>
                /// Orientation of the tile, relative to the centroid of the whole pointcloud.
                /// (0,0,0) for directionless.
                /// </summary>
                public Vector3 orientation;
                /// <summary>
                /// Structure describing a single stream (within a tile).
                /// </summary>
                [Serializable]
                public struct NetworkQualityInformation
                {
                    /// <summary>
                    /// Indication of how much bandwidth this stream requires.
                    /// </summary>
                    public float bandwidthRequirement;
                    /// <summary>
                    /// Indication of how "good" this stream is, visually. 0.0 is worst
                    /// quality, 1.0 is best quality. 
                    /// </summary>
                    public float representation;
                };
                /// <summary>
                /// Streams available for this tile (at various quality levels)
                /// </summary>
                public NetworkQualityInformation[] qualities;
            };
            /// <summary>
            /// All tiles for this aggregate pointcloud stream.
            /// </summary>
            public NetworkTileInformation[] tiles;
        };
    }
}