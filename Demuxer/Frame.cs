namespace Demuxer;

using System.Runtime.InteropServices;

public class Frame : AbstractFrame
{
    public Frame(FrameType type, IntPtr data, ulong size, TimeSpan timestamp) : base(type, data, size, timestamp)
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Marshal.FreeHGlobal(Data);
        }
    }
}
