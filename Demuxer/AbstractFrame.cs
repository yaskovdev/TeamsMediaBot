namespace Demuxer;

public abstract class AbstractFrame : IDisposable
{
    private int _disposed;

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

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    protected abstract void Dispose(bool disposing);
}
