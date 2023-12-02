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

    public Frame ReadFrame()
    {
        var metadata = new FrameMetadata();
        var data = NativeDemuxerApi.ReadFrame(_demuxer, ref metadata);
        return new Frame(metadata.Type, metadata.Size, TimeSpan.FromMilliseconds(metadata.Timestamp), data);
    }

    public void Dispose()
    {
        NativeDemuxerApi.DeleteDemuxer(_demuxer);
    }
}
