namespace Demuxer;

using System.Diagnostics.CodeAnalysis;

public class Demuxer : IDemuxer
{
    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable", Justification = "The callback scope must be bigger than the scope of the native demuxer")]
    private readonly Callback _callback;

    private readonly IntPtr _demuxer;

    public Demuxer(IBlockingStream stream)
    {
        _callback = stream.Read;
        _demuxer = NativeDemuxerApi.CreateDemuxer(_callback);
    }

    /// <summary>
    /// If the returned array is empty, then there is not enough source data. Write more packets.
    /// </summary>
    public Frame ReadFrame()
    {
        var data = new byte[1920 * 1080 * 3 / 2]; // TODO: check the size and do not hardcode
        var metadata = new FrameMetadata();
        NativeDemuxerApi.ReadFrame(_demuxer, data, ref metadata);
        // TODO: Frame should have IntPtr that will be copied in TeamsMediaBot.StreamingSession.Map, then Frame can be disposed of using the native destructor
        // Or even create your own implementation instead of VideoSendBuffer to copy straight to the pointer that MediaPlatform will use (and free it after the use)
        return new Frame(metadata.Type, metadata.Size, TimeSpan.FromMilliseconds(metadata.Timestamp), data);
    }

    public void Dispose()
    {
        NativeDemuxerApi.DeleteDemuxer(_demuxer);
    }
}
