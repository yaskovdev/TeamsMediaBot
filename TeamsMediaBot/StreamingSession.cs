namespace TeamsMediaBot;

using System.Collections.Immutable;
using BrowserAudioVideoCapturingService;
using Demuxer;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Skype.Bots.Media;
using PuppeteerSharp;
using Frame = Demuxer.Frame;

public class StreamingSession : IAsyncDisposable
{
    private readonly Task<IBrowser> _launchBrowserTask;
    private readonly IBlockingBuffer _buffer;
    private readonly IDemuxer _demuxer;
    private readonly IVideoSocket _videoSocket;
    private readonly TaskCompletionSource<bool> _videoSocketActive = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;
    private AudioVideoFramePlayer? _player;

    public StreamingSession(ILocalMediaSession mediaSession)
    {
        _buffer = new BlockingCircularBuffer(512 * 1024);
        var browserLauncher = new BrowserLauncher();
        _launchBrowserTask = browserLauncher.LaunchInstance(_buffer.Write);
        _demuxer = new Demuxer(_buffer);
        _videoSocket = mediaSession.VideoSockets[0];
        _videoSocket.VideoSendStatusChanged += OnVideoSendStatusChanged;
        var audioSocket = mediaSession.AudioSocket;
        audioSocket.AudioSendStatusChanged += OnAudioSendStatusChanged;
        _ = StartStreaming();
    }

    private async Task StartStreaming()
    {
        await _videoSocketActive.Task;
        var playerSettings = new AudioVideoFramePlayerSettings(new AudioSettings(0), new VideoSettings(), 0);
        _player = new AudioVideoFramePlayer(null, (VideoSocket)_videoSocket, playerSettings);
        while (true)
        {
            try
            {
                await _semaphore.WaitAsync();
                if (!_disposed)
                {
                    var frame = _demuxer.ReadFrame();
                    if (frame.Type == FrameType.Video)
                    {
                        await _player.EnqueueBuffersAsync(ImmutableList<AudioMediaBuffer>.Empty, ImmutableList<VideoMediaBuffer>.Empty.Add(Map(frame)));
                    }
                    else
                    {
                        frame.Dispose();
                    }
                }
                else
                {
                    return;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _semaphore.WaitAsync();
            _buffer.Dispose();
            _demuxer.Dispose();
            var browser = await _launchBrowserTask;
            await browser.StopCapturing();
            await browser.DisposeAsync();
        }
        finally
        {
            _disposed = true;
            _semaphore.Release();
            _semaphore.Dispose();
        }
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
        }
    }

    private static VideoBuffer Map(Frame frame) => new(frame, VideoFormat.NV12_1920x1080_15Fps);
}
