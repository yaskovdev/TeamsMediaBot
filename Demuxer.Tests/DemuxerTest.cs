namespace Demuxer.Tests;

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices;
using ProtoBuf;

[TestClass]
public class DemuxerTest
{
    private const int ExpectedNumberOfFrames = 1521;

    [TestMethod]
    [Timeout(3000)]
    public void ShouldDemuxChunkedMedia()
    {
        var chunks = ReadChunks();
        using var blockingBuffer = new BlockingCircularBuffer(512 * 1024 * 1024);
        Task.Run(() => EnqueueChunks(chunks, blockingBuffer));
        using var instanceUnderTest = new Demuxer(blockingBuffer);
        var frames = ReadFrames(instanceUnderTest);
        try
        {
            var tempDirectory = CreateTemporaryDirectory();
            var videoFile = Path.Combine(tempDirectory.FullName, "Video.nv12");
            using var outputVideoStream = File.Create(videoFile);
            foreach (var frame in frames)
            {
                if (frame.Type == FrameType.Video)
                {
                    var buffer = new byte[frame.Size];
                    Marshal.Copy(frame.Data, buffer, 0, buffer.Length);
                    outputVideoStream.Write(buffer);
                }
            }
            Console.WriteLine(videoFile);
        }
        finally
        {
            frames.ToImmutableList().ForEach(it => it.Dispose());
        }
    }

    private static IImmutableList<AbstractFrame> ReadFrames(IDemuxer instanceUnderTest)
    {
        var frames = new List<AbstractFrame>();
        for (var i = 0; i < ExpectedNumberOfFrames; i++)
        {
            frames.Add(instanceUnderTest.ReadFrame());
        }
        return frames.ToImmutableList();
    }

    private static void EnqueueChunks(IImmutableList<Chunk> chunks, IBlockingBuffer buffer)
    {
        foreach (var chunk in chunks)
        {
            buffer.Write(Encode(chunk.Buffer));
        }
    }

    private static string Encode(byte[] source) => new(source.Select(it => (char)it).ToArray());

    private static IImmutableList<Chunk> ReadChunks()
    {
        var chunks = new List<Chunk>();
        using var inputStream = new FileStream(GetResourcePath(@"Resources\Chunks.bin"), FileMode.Open);
        while (true)
        {
            var chunk = Serializer.DeserializeWithLengthPrefix<Chunk?>(inputStream, PrefixStyle.Base128);
            if (chunk is null)
            {
                break;
            }
            chunks.Add(chunk);
        }
        return chunks.ToImmutableList();
    }

    private static string GetResourcePath(string name)
    {
        var location = Assembly.GetExecutingAssembly().Location;
        var uriBuilder = new UriBuilder(location);
        var path = Uri.UnescapeDataString(uriBuilder.Path);
        return Path.Combine(Path.GetDirectoryName(path) ?? "", name);
    }

    private static DirectoryInfo CreateTemporaryDirectory()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        return Directory.CreateDirectory(tempDirectory);
    }
}
