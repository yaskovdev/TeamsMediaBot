namespace TeamsMediaBot;

using System.Runtime.InteropServices;
using Microsoft.Skype.Bots.Media;

public class AudioBuffer : AudioMediaBuffer
{
    private GCHandle _handle;

    public AudioBuffer(byte[] buffer, long timestamp)
    {
        _handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        Data = _handle.AddrOfPinnedObject();
        Length = buffer.Length;
        Timestamp = timestamp;
        AudioFormat = AudioFormat.Pcm16K;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _handle.Free();
        }
    }
}
