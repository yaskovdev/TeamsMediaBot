namespace DemuxerTests;

using System.Runtime.InteropServices;
using Demuxer;

[TestClass]
public class BlockingStreamTest
{
    [TestMethod]
    [Timeout(3000)]
    public void ShouldAcceptWriteGivenEnoughSpace()
    {
        const int bufferSize = 1;
        var buffer = new byte[bufferSize];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            var instanceUnderTest = new BlockingStream(1);
            instanceUnderTest.Write(new byte[] { 0 });
            instanceUnderTest.Read(handle.AddrOfPinnedObject(), bufferSize);
            instanceUnderTest.Write(new byte[] { 0 });
        }
        finally
        {
            handle.Free();
        }
    }
}
