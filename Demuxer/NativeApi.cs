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

internal static class NativeDemuxerApi
{
    [DllImport("NativeDemuxer.dll", EntryPoint = "create_demuxer")]
    internal static extern IntPtr CreateDemuxer(Callback callback);

    [DllImport("NativeDemuxer.dll", EntryPoint = "demuxer_read_frame")]
    internal static extern IntPtr ReadFrame(IntPtr demuxer, ref FrameMetadata metadata);

    [DllImport("NativeDemuxer.dll", EntryPoint = "delete_frame_buffer")]
    internal static extern void DeleteFrameBuffer(IntPtr buffer);

    [DllImport("NativeDemuxer.dll", EntryPoint = "delete_demuxer")]
    internal static extern void DeleteDemuxer(IntPtr demuxer);
}

internal static class NativeResamplerApi
{
    [DllImport("NativeDemuxer.dll", EntryPoint = "resampler_create")]
    internal static extern IntPtr CreateResampler();

    [DllImport("NativeDemuxer.dll", EntryPoint = "resampler_resample_frame")]
    internal static extern IntPtr ResampleFrame(IntPtr resampler, IntPtr srcFrame, int srcLength, ref FrameMetadata dstMetadata);

    [DllImport("NativeDemuxer.dll", EntryPoint = "resampler_delete")]
    internal static extern void DeleteResampler(IntPtr resampler);
}
