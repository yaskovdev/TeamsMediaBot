namespace TeamsMediaBot;

using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Skype.Bots.Media;

public class DummyAudioPlayer
{
    public static List<AudioMediaBuffer> CreateAudioMediaBuffers()
    {
        var audioMediaBuffers = new List<AudioMediaBuffer>();
        var referenceTime = 0;

        // packet size of 20 ms
        var numberOfTicksInOneAudioBuffers = 20 * 10000;

        using var fs = File.Open(GetResourcePath(@"Resources\Audio.pcm"), FileMode.Open);
        var bytesToRead = new byte[640];

        while (fs.Read(bytesToRead, 0, bytesToRead.Length) >= 640)
        {
            // here we want to create buffers of 20MS with PCM 16Khz
            IntPtr unmanagedBuffer = Marshal.AllocHGlobal(640);
            Marshal.Copy(bytesToRead, 0, unmanagedBuffer, 640);
            var audioBuffer = new AudioSendBuffer(unmanagedBuffer, 640, AudioFormat.Pcm16K, referenceTime);
            audioMediaBuffers.Add(audioBuffer);
            referenceTime += numberOfTicksInOneAudioBuffers;
        }

        return audioMediaBuffers;
    }

    private static string GetResourcePath(string name)
    {
        var location = Assembly.GetExecutingAssembly().Location;
        var uriBuilder = new UriBuilder(location);
        var path = Uri.UnescapeDataString(uriBuilder.Path);
        return Path.Combine(Path.GetDirectoryName(path) ?? "", name);
    }
}
