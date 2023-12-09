namespace Demuxer;

using System.Diagnostics.CodeAnalysis;

public class Demuxer : IDemuxer
{
    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable", Justification = "The callback scope must be bigger than the scope of the native demuxer")]
    private readonly Callback _callback;

    private readonly IntPtr _demuxer;

    public Demuxer(IBlockingBuffer buffer)
    {
        _callback = buffer.Read;
        _demuxer = NativeDemuxerApi.CreateDemuxer(_callback);
    }

    public AbstractFrame ReadFrame()
    {
        var metadata = new FrameMetadata();
        var data = NativeDemuxerApi.ReadFrame(_demuxer, ref metadata);
        return new NativeFrame(metadata.Type, data, metadata.Size, TimeSpan.FromMilliseconds(metadata.Timestamp)); // TODO: use the same approach with timestamps as in the resampler?
    }

    public void Dispose()
    {
        NativeDemuxerApi.DeleteDemuxer(_demuxer);
    }
}
