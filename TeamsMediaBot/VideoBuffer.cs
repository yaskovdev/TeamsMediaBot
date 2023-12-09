namespace TeamsMediaBot;

using Demuxer;
using Microsoft.Skype.Bots.Media;

public class VideoBuffer : VideoMediaBuffer
{
    private readonly AbstractFrame _source;

    public VideoBuffer(AbstractFrame source, VideoFormat format)
    {
        _source = source;
        Data = _source.Data;
        Length = (long)_source.Size;
        VideoFormat = format;
        Timestamp = source.Timestamp.Ticks;
    }

    protected override void Dispose(bool disposing)
    {
        _source.Dispose();
    }
}
