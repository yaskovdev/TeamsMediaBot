namespace Demuxer;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal delegate int Callback(IntPtr message, int size);

[SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty", Justification = "Set up in native code")]
internal struct FrameMetadata
{
    public FrameType Type { get; }

    public ulong Size { get; }

    public long Timestamp { get; }
}

internal static partial class NativeDemuxerApi
{
    [LibraryImport("NativeDemuxer.dll", EntryPoint = "demuxer_create")]
    internal static partial IntPtr CreateDemuxer(Callback callback);

    [LibraryImport("NativeDemuxer.dll", EntryPoint = "demuxer_read_frame")]
    internal static partial IntPtr ReadFrame(IntPtr demuxer, ref FrameMetadata metadata);

    [LibraryImport("NativeDemuxer.dll", EntryPoint = "demuxer_frame_buffer_delete")]
    internal static partial void DeleteFrameBuffer(IntPtr buffer);

    [LibraryImport("NativeDemuxer.dll", EntryPoint = "demuxer_delete")]
    internal static partial void DeleteDemuxer(IntPtr demuxer);
}

internal static partial class NativeResamplerApi
{
    [LibraryImport("NativeDemuxer.dll", EntryPoint = "resampler_create")]
    internal static partial IntPtr CreateResampler();

    [LibraryImport("NativeDemuxer.dll", EntryPoint = "resampler_resample_frame")]
    internal static partial IntPtr ResampleFrame(IntPtr resampler, IntPtr srcFrame, int srcLength, ref FrameMetadata dstMetadata);

    [LibraryImport("NativeDemuxer.dll", EntryPoint = "resampler_frame_buffer_delete")]
    internal static partial void DeleteFrameBuffer(IntPtr buffer);

    [LibraryImport("NativeDemuxer.dll", EntryPoint = "resampler_delete")]
    internal static partial void DeleteResampler(IntPtr resampler);
}
