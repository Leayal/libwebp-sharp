using System;
using System.Collections.Generic;
using System.Text;

namespace libwebp_sharp
{
    public enum WebpPreset
    {
        Default,
        Photo,
        Picture,
        Drawing,
        Icon,
        Text
    }

    public enum CompressionMethod
    {
        Lossy,
        NearLossless,
        Lossless
    }

    [Flags]
    public enum MetadataType
    {
        None = 0,
        Exif = 1 << 0,
        Icc = 1 << 1,
        Xmp = 1 << 2,
        All = Exif | Icc | Xmp
    }

    public enum AlphaFilter
    {
        Fast,
        None,
        Best
    }

    public enum LosslessPreset : byte
    {
        /// <summary>
        /// Fastest
        /// </summary>
        Level0,
        Level1,
        Level2,
        Level3,
        Level4,
        Level5,
        Level6,
        Level7,
        Level8,
        /// <summary>
        /// Slowest
        /// </summary>
        Level9,
        Fastest = Level0,
        Slowest = Level9
    }
}
