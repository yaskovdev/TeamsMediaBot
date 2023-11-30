namespace Demuxer;

public interface IDemuxer
{
    void WritePacket(byte[] packet);

    Frame ReadFrame();
}
