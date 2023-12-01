namespace Demuxer;

public interface IBlockingStream
{
    void Write(byte[] packet);

    int Read(IntPtr buffer, int size);
}
