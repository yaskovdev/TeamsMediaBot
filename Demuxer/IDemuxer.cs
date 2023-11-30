namespace Demuxer;

public interface IDemuxer : IDisposable
{
    Frame ReadFrame();
}
