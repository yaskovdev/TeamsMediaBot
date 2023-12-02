namespace Demuxer;

public class Frame : IDisposable
{
    public FrameType Type { get; }

    public IntPtr Data { get; }

    public ulong Size { get; }

    public TimeSpan Timestamp { get; }

    public Frame(FrameType type, IntPtr data, ulong size, TimeSpan timestamp)
    {
        Type = type;
        Data = data;
        Size = size;
        Timestamp = timestamp;
    }

    public void Dispose() => NativeDemuxerApi.DeleteFrameBuffer(Data);
}
