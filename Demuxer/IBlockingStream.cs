namespace Demuxer;

public interface IBlockingStream
{
    void Write(byte[] packet);

    byte[] Read(int size);
}
