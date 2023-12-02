namespace Demuxer.Tests;

using System.Runtime.InteropServices;
using Shouldly;

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
            instanceUnderTest.Write(new byte[] { 1, 2, 3 });
            instanceUnderTest.Write(new byte[] { 4 });
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
            instanceUnderTest.Write(new byte[] { 1 });
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
        const int capacity = 1;
        var buffer = new byte[capacity];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            var instanceUnderTest = new BlockingCircularBuffer(1);
            instanceUnderTest.Write(new byte[] { 0 });
            instanceUnderTest.Read(handle.AddrOfPinnedObject(), 1);
            instanceUnderTest.Write(new byte[] { 0 });
        }
        finally
        {
            handle.Free();
        }
    }
}
