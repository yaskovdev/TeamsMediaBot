namespace TeamsMediaBot;

using System.Collections.Immutable;
using BrowserAudioVideoCapturingService;
using Demuxer;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Skype.Bots.Media;
using PuppeteerSharp;

public class StreamingSession : IAsyncDisposable
{
    private readonly Task<IBrowser> _launchBrowserTask;
    private readonly IDemuxer _demuxer;
    private readonly IVideoSocket _videoSocket;
    private AudioVideoFramePlayer? _player;
    private readonly IList<VideoMediaBuffer> _videoBuffers = new List<VideoMediaBuffer>();

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
        StartStreaming();
    }

    private void StartStreaming()
    {
        while (true)
        {
            var frame = _demuxer.ReadFrame(); // TODO: can be called after _demuxer.Dispose() and cause an exception
            if (frame.Data.Count == 0)
            {
                break;
            }
            if (frame.Type == FrameType.Video)
            {
                _videoBuffers.Add(new VideoBuffer(frame.Data.ToArray(), frame.Timestamp.Ticks));
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
            Thread.Sleep(1000);
            var playerSettings = new AudioVideoFramePlayerSettings(new AudioSettings(20), new VideoSettings(), 1000);
            _player = new AudioVideoFramePlayer(null, (VideoSocket)_videoSocket, playerSettings);
            _player.LowOnFrames += OnLowOnFrames;
            _player.EnqueueBuffersAsync(ImmutableList<AudioMediaBuffer>.Empty, _videoBuffers);
        }
    }

    private void OnAudioSendStatusChanged(object? sender, AudioSendStatusChangedEventArgs args)
    {
        if (args.MediaSendStatus == MediaSendStatus.Active)
        {
        }
    }

    private void OnLowOnFrames(object? sender, LowOnFramesEventArgs e)
    {
        _ = _player?.EnqueueBuffersAsync(ImmutableList<AudioMediaBuffer>.Empty, _videoBuffers);
    }
}
