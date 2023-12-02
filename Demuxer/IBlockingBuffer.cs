namespace Demuxer;

public interface IBlockingBuffer : IDisposable
{
    void Write(byte[] packet);

    int Read(IntPtr buffer, int size);
}
