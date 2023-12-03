namespace TeamsMediaBot;

using Demuxer;
using Microsoft.Skype.Bots.Media;

public class VideoBuffer : VideoMediaBuffer
{
    private readonly Frame _source;
    private int _disposed;

    public VideoBuffer(Frame source, VideoFormat format)
    {
        _source = source;
        Data = _source.Data;
        Length = (long)_source.Size;
        VideoFormat = format;
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
