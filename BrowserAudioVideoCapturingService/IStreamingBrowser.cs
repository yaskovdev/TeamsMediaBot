namespace BrowserAudioVideoCapturingService;

using Demuxer;
using PuppeteerSharp;

public interface IStreamingBrowser
{
    public Task<IBrowser> LaunchInstance(IBlockingStream stream);
}
