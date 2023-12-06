namespace DemuxerRunner;

using System.Reflection;
using System.Runtime.InteropServices;
using Demuxer;
using ProtoBuf;

internal static class Program
{
    public static void Main(string[] args)
    {
        using var instanceUnderTest = new Resampler();
        List<Chunk> chunks = ReadChunks();
        using var output = File.Open(@"c:\dev\experiment3\output.pcm", FileMode.Create);
        foreach (var chunk in chunks)
        {
            instanceUnderTest.WriteFrame(chunk.Buffer);
            while (true)
            {
                var frame = instanceUnderTest.ReadFrame();
                if (frame.Data == IntPtr.Zero)
                {
                    break;
                }
                Console.WriteLine("Read frame of length " + frame.Size);
                var buffer = new byte[frame.Size];
                Marshal.Copy(frame.Data, buffer, 0, (int)frame.Size);
                output.Write(buffer);
            }
        }
    }

    private static List<Chunk> ReadChunks()
    {
        using var input = File.Open(GetResourcePath(@"Resources\AudioChunks.bin"), FileMode.Open);
        var chunks = new List<Chunk>();
        while (true)
        {
            var chunk = Serializer.DeserializeWithLengthPrefix<Chunk?>(input, PrefixStyle.Base128);
            if (chunk is null)
            {
                break;
            }
            chunks.Add(chunk);
        }
        return chunks;
    }

    private static string GetResourcePath(string name)
    {
        var location = Assembly.GetExecutingAssembly().Location;
        var uriBuilder = new UriBuilder(location);
        var path = Uri.UnescapeDataString(uriBuilder.Path);
        return Path.Combine(Path.GetDirectoryName(path) ?? "", name);
    }
}
