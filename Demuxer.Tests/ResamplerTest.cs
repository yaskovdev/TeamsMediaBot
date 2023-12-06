namespace Demuxer.Tests;

using System.Reflection;

[TestClass]
public class ResamplerTest
{
    [TestMethod]
    public void ShouldResampleAudioFrame()
    {
        using var instanceUnderTest = new Resampler();
        var bytes = File.ReadAllBytes(GetResourcePath(@"Resources\AudioFrame.pcm"));
        instanceUnderTest.WriteFrame(bytes);
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

    private static string GetResourcePath(string name)
    {
        var location = Assembly.GetExecutingAssembly().Location;
        var uriBuilder = new UriBuilder(location);
        var path = Uri.UnescapeDataString(uriBuilder.Path);
        return Path.Combine(Path.GetDirectoryName(path) ?? "", name);
    }
}
