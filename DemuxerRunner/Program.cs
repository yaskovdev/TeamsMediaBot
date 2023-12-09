namespace DemuxerRunner;

using System.Reflection;
using System.Runtime.InteropServices;
using Demuxer;
using ProtoBuf;

internal static class Program
{
    public static void Main(string[] args)
    {
        using var resampler = new Resampler();
        List<Chunk> chunks = ReadChunks();
        using var output = File.Open(@"c:\dev\experiment3\output.pcm", FileMode.Create);
        foreach (var chunk in chunks)
        {
            var handle = GCHandle.Alloc(chunk.Buffer, GCHandleType.Pinned);
            try
            {
                resampler.WriteFrame(handle.AddrOfPinnedObject(), chunk.Buffer.Length, (int)chunk.Timestamp.TotalMilliseconds);
                while (true)
                {
                    var frame = resampler.ReadFrame();
                    if (frame.Data == IntPtr.Zero)
                    {
                        break;
                    }
                    Console.WriteLine("Read frame of length " + frame.Size + " with timestamp in ms " + (int)frame.Timestamp.TotalMilliseconds);
                    var buffer = ToArray(frame.Data, (int)frame.Size);
                    output.Write(buffer);
                }
            }
            finally
            {
                handle.Free();
            }
        }
    }

    private static byte[] ToArray(IntPtr data, int length)
    {
        var buffer = new byte[length];
        Marshal.Copy(data, buffer, 0, length);
        return buffer;
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
