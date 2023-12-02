namespace Demuxer;

public interface IBlockingBuffer : IDisposable
{
    void Write(string packet);

    int Read(IntPtr buffer, int size);
}
