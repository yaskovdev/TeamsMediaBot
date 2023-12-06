namespace TeamsMediaBot;

using Demuxer;
using Microsoft.Skype.Bots.Media;

public class AudioBuffer : AudioMediaBuffer
{
    private readonly Frame _source;
    private int _disposed;

    public AudioBuffer(Frame source, AudioFormat format)
    {
        _source = source;
        Data = _source.Data;
        Length = (long)_source.Size;
        AudioFormat = format;
        Timestamp = source.Timestamp.Ticks;
    }

    protected override void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _source.Dispose();
        }
    }
}
