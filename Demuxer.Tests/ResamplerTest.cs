namespace Demuxer.Tests;

using System.Reflection;
using System.Runtime.InteropServices;

[TestClass]
public class ResamplerTest
{
    [TestMethod]
    public void ShouldResampleAudioFrame()
    {
        using var instanceUnderTest = new Resampler();
        var bytes = File.ReadAllBytes(GetResourcePath(@"Resources\AudioFrame.pcm"));
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            instanceUnderTest.WriteFrame(handle.AddrOfPinnedObject(), bytes.Length, 0);
            while (true)
            {
                var frame = instanceUnderTest.ReadFrame();
                if (frame.Data == IntPtr.Zero)
                {
                    break;
                }
                Console.WriteLine("Read frame of length " + frame.Size);
            }
        }
        finally
        {
            handle.Free();
        }
    }

    private static string GetResourcePath(string name)
    {
        var location = Assembly.GetExecutingAssembly().Location;
        var uriBuilder = new UriBuilder(location);
        var path = Uri.UnescapeDataString(uriBuilder.Path);
        return Path.Combine(Path.GetDirectoryName(path) ?? "", name);
    }
}
