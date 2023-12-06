namespace Demuxer.Tests;

using ProtoBuf;

[ProtoContract]
public class Chunk
{
    [ProtoMember(1)]
    public byte[] Buffer { get; set; }
}
