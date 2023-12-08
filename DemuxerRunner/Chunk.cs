namespace DemuxerRunner;

using ProtoBuf;

[ProtoContract]
public class Chunk
{
    [ProtoMember(1)]
    public byte[] Buffer { get; set; }

    [ProtoMember(2)]
    public TimeSpan Timestamp { get; set; }
}
