namespace TeamsMediaBot;

using System.Runtime.InteropServices;
using Microsoft.Skype.Bots.Media;

public class VideoBuffer : VideoMediaBuffer
{
    private GCHandle _handle;

    public VideoBuffer(byte[] buffer, long timestamp)
    {
        _handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        Data = _handle.AddrOfPinnedObject();
        Length = buffer.Length;
        Timestamp = timestamp;
        VideoFormat = VideoFormat.NV12_1920x1080_15Fps; // TODO: use H.264 (check older version of README.md how to get the test data)
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // _handle.Free();
        }
    }
}
