namespace Demuxer;

public interface IResampler : IDisposable
{
    void WriteFrame(IntPtr bytes, int length, int timestamp);

    Frame ReadFrame();
}
