namespace TeamsMediaBot;

using BrowserAudioVideoCapturingService;
using Demuxer;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Skype.Bots.Media;
using PuppeteerSharp;

public class StreamingSession : IAsyncDisposable
{
    private readonly Task<IBrowser> _launchBrowserTask;
    private readonly IDemuxer _demuxer;
    private readonly TaskCompletionSource<bool> _videoSocketActive = new();
    private readonly TaskCompletionSource<bool> _audioSocketActive = new();
    private readonly IVideoSocket _videoSocket;

    public StreamingSession(ILocalMediaSession mediaSession)
    {
        var stream = new BlockingStream();
        var streamingBrowser = new StreamingBrowser();
        _launchBrowserTask = streamingBrowser.LaunchInstance(stream);
        _demuxer = new Demuxer(stream);
        _videoSocket = mediaSession.VideoSockets[0];
        _videoSocket.VideoSendStatusChanged += OnVideoSendStatusChanged;
        var audioSocket = mediaSession.AudioSocket;
        audioSocket.AudioSendStatusChanged += OnAudioSendStatusChanged;
        _ = StartStreaming();
    }

    private async Task StartStreaming()
    {
        await Task.WhenAll(_videoSocketActive.Task, _audioSocketActive.Task);
        while (true)
        {
            var frame = _demuxer.ReadFrame();
            if (frame.Data.Count == 0)
            {
                break;
            }
            if (frame.Type == FrameType.Video)
            {
                _videoSocket.Send(new VideoBuffer(frame.Data.ToArray(), frame.Timestamp.Ticks));
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _demuxer.Dispose();
        var browser = await _launchBrowserTask;
        await browser.DisposeAsync();
    }

    private void OnVideoSendStatusChanged(object? sender, VideoSendStatusChangedEventArgs args)
    {
        if (args.MediaSendStatus == MediaSendStatus.Active)
        {
            _videoSocketActive.TrySetResult(true);
        }
    }

    private void OnAudioSendStatusChanged(object? sender, AudioSendStatusChangedEventArgs args)
    {
        if (args.MediaSendStatus == MediaSendStatus.Active)
        {
            _audioSocketActive.TrySetResult(true);
        }
    }
}
