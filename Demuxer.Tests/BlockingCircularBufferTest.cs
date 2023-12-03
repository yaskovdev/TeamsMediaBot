namespace Demuxer.Tests;

using System.Runtime.InteropServices;

[TestClass]
public class BlockingCircularBufferTest
{
    [TestMethod]
    [Timeout(3000)]
    public void ShouldReadWhatYouWrote()
    {
        const int capacity = 512 * 1024 * 1024;
        var instanceUnderTest = new BlockingCircularBuffer(capacity);
        var buffer = new byte[capacity];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            instanceUnderTest.Write(Encode(new byte[] { 1, 2, 3 }));
            instanceUnderTest.Write(Encode(new byte[] { 4 }));
            instanceUnderTest.Read(handle.AddrOfPinnedObject(), 2).ShouldBe(2);
            new ArraySegment<byte>(buffer)[..2].ShouldBe(new byte[] { 1, 2 });
            instanceUnderTest.Read(handle.AddrOfPinnedObject(), 2).ShouldBe(2);
            new ArraySegment<byte>(buffer)[..2].ShouldBe(new byte[] { 3, 4 });
        }
        finally
        {
            handle.Free();
        }
    }

    [TestMethod]
    [Timeout(3000)]
    public void ShouldReadFullyIfHasNotEnoughData()
    {
        const int capacity = 512 * 1024 * 1024;
        var instanceUnderTest = new BlockingCircularBuffer(capacity);
        var buffer = new byte[capacity];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            instanceUnderTest.Write(Encode(new byte[] { 1 }));
            instanceUnderTest.Read(handle.AddrOfPinnedObject(), 2).ShouldBe(1);
            new ArraySegment<byte>(buffer)[..1].ShouldBe(new byte[] { 1 });
        }
        finally
        {
            handle.Free();
        }
    }

    [TestMethod]
    [Timeout(3000)]
    public void ShouldAcceptWriteGivenEnoughSpace()
    {
        var buffer = new byte[1];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            var instanceUnderTest = new BlockingCircularBuffer(1);
            instanceUnderTest.Write(Encode(new byte[] { 0 }));
            instanceUnderTest.Read(handle.AddrOfPinnedObject(), 1);
            instanceUnderTest.Write(Encode(new byte[] { 0 }));
        }
        finally
        {
            handle.Free();
        }
    }

    [TestMethod]
    public void ShouldSkipPacketThadDoesNotFitIntoCapacity()
    {
        var buffer = new byte[2];
        var instanceUnderTest = new BlockingCircularBuffer(1);
        instanceUnderTest.Write(Encode(new byte[] { 1, 2 }));
        instanceUnderTest.Write(Encode(new byte[] { 3 }));
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            instanceUnderTest.Read(handle.AddrOfPinnedObject(), 1);
            new ArraySegment<byte>(buffer)[..1].ShouldBe(new byte[] { 3 });
        }
        finally
        {
            handle.Free();
        }
    }

    private static string Encode(byte[] source) => new(source.Select(it => (char)it).ToArray());
}
