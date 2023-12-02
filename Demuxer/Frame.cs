namespace Demuxer;

public class Frame : IDisposable
{
    public FrameType Type { get; }

    public TimeSpan Timestamp { get; }

    public IntPtr Data { get; }

    public ulong Size { get; }

    public Frame(FrameType type, ulong size, TimeSpan timestamp, IntPtr data)
    {
        Type = type;
        Timestamp = timestamp;
        Data = data;
        Size = size;
    }

    public void Dispose() => NativeDemuxerApi.DeleteFrameBuffer(Data);
}
