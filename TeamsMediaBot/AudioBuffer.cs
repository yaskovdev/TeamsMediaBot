namespace TeamsMediaBot;

using Demuxer;
using Microsoft.Skype.Bots.Media;

public class AudioBuffer : AudioMediaBuffer
{
    private readonly AbstractFrame _source;

    public AudioBuffer(AbstractFrame source, AudioFormat format)
    {
        _source = source;
        Data = _source.Data;
        Length = (long)_source.Size;
        AudioFormat = format;
        Timestamp = source.Timestamp.Ticks;
    }

    protected override void Dispose(bool disposing)
    {
        _source.Dispose();
    }
}
