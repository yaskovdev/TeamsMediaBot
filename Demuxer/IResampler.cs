namespace Demuxer;

public interface IResampler : IDisposable
{
    void WriteFrame(byte[] bytes);

    Frame ReadFrame();
}
