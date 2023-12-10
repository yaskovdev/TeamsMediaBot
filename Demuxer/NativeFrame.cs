namespace Demuxer;

public class NativeFrame : AbstractFrame
{
    public NativeFrame(FrameType type, IntPtr data, ulong size, TimeSpan timestamp) : base(type, data, size, timestamp)
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            NativeDemuxerApi.DeleteFrameBuffer(Data);
        }
    }
}
