namespace TeamsMediaBot;

using Demuxer;
using Microsoft.Skype.Bots.Media;

public class VideoBuffer : VideoMediaBuffer
{
    private readonly Frame _source;

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
        if (disposing)
        {
            _source.Dispose();
        }
    }
}
