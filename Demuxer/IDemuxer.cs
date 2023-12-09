namespace Demuxer;

public interface IDemuxer : IDisposable
{
    AbstractFrame ReadFrame();
}
