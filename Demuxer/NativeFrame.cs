namespace Demuxer;

public class NativeFrame : AbstractFrame
{
    private int _disposed;

    public NativeFrame(FrameType type, IntPtr data, ulong size, TimeSpan timestamp) : base(type, data, size, timestamp)
    {
    }

    public override void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            NativeDemuxerApi.DeleteFrameBuffer(Data);
        }
    }
}
