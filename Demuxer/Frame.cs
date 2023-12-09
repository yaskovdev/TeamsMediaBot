namespace Demuxer;

using System.Runtime.InteropServices;

public class Frame : AbstractFrame
{
    private int _disposed;

    public Frame(FrameType type, IntPtr data, ulong size, TimeSpan timestamp) : base(type, data, size, timestamp)
    {
    }

    public override void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            Marshal.FreeHGlobal(Data);
        }
    }
}
