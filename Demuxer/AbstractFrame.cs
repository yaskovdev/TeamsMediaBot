namespace Demuxer;

public abstract class AbstractFrame : IDisposable
{
    public FrameType Type { get; }

    public IntPtr Data { get; }

    public ulong Size { get; }

    public TimeSpan Timestamp { get; }

    protected AbstractFrame(FrameType type, IntPtr data, ulong size, TimeSpan timestamp)
    {
        Type = type;
        Data = data;
        Size = size;
        Timestamp = timestamp;
    }

    public abstract void Dispose();
}
