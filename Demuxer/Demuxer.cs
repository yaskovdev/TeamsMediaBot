namespace Demuxer;

using System.Diagnostics.CodeAnalysis;

public class Demuxer : IDemuxer
{
    private static readonly NativeFrame EmptyFrame = new(FrameType.Audio, IntPtr.Zero, 0, TimeSpan.Zero);

    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable", Justification = "The callback scope must be bigger than the scope of the native demuxer")]
    private readonly Callback _callback;

    private readonly IntPtr _demuxer;

    private int _disposed;

    public Demuxer(IBlockingBuffer buffer)
    {
        _callback = buffer.Read;
        _demuxer = NativeDemuxerApi.CreateDemuxer(_callback);
    }

    /// <summary>
    /// Returns an empty frame if an end of stream was reached. 
    /// </summary>
    public AbstractFrame ReadFrame()
    {
        var metadata = new FrameMetadata();
        var data = NativeDemuxerApi.ReadFrame(_demuxer, ref metadata);
        return data == IntPtr.Zero ? EmptyFrame : new NativeFrame(metadata.Type, data, metadata.Size, TimeSpan.FromMilliseconds(metadata.Timestamp));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            NativeDemuxerApi.DeleteDemuxer(_demuxer);
        }
    }
}
