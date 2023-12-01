namespace Demuxer;

public interface IBlockingStream : IDisposable
{
    void Write(byte[] packet);

    int Read(IntPtr buffer, int size);
}
