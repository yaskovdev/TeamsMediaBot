namespace TeamsMediaBot;

using BrowserAudioVideoCapturingService;
using Demuxer;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Skype.Bots.Media;
using PuppeteerSharp;

public class StreamingSession : IAsyncDisposable
{
    private readonly Task<IBrowser> _launchBrowserTask;
    private readonly IBlockingBuffer _buffer;
    private readonly IDemuxer _demuxer;
    private readonly IResampler _resampler;
    private readonly IAudioSocket _audioSocket;
    private readonly IVideoSocket _videoSocket;
    private readonly TaskCompletionSource<bool> _audioSocketActive = new();
    private readonly TaskCompletionSource<bool> _videoSocketActive = new();
    private readonly Player _player;
    private bool _disposed;

    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public StreamingSession(ILocalMediaSession mediaSession)
    {
        _buffer = new BlockingCircularBuffer(512 * 1024);
        var browserLauncher = new BrowserLauncher();
        _launchBrowserTask = browserLauncher.LaunchInstance(_buffer.Write);
        _demuxer = new Demuxer(_buffer);
        _resampler = new Resampler();
        _audioSocket = mediaSession.AudioSocket;
        _audioSocket.AudioSendStatusChanged += OnAudioSendStatusChanged;
        _videoSocket = mediaSession.VideoSockets[0];
        _videoSocket.VideoSendStatusChanged += OnVideoSendStatusChanged;
        _player = new Player(_audioSocket, _videoSocket); // TODO: read FPS from config, same in BrowserAudioVideoCapturingService.Constants
        _ = StartStreaming(); // TODO: log exception if any
    }

    private async Task StartStreaming()
    {
        Console.WriteLine("Waiting for the sockets to become active");
        await Task.WhenAll(_audioSocketActive.Task, _videoSocketActive.Task);
        Console.WriteLine("Sockets are active");
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
                        await _player.Enqueue(frame);
                    }
                    else if (frame.Type == FrameType.Audio)
                    {
                        _resampler.WriteFrame(frame.Data, (int)frame.Size);
                        while (true)
                        {
                            var resampledAudio = _resampler.ReadFrame();
                            if (resampledAudio.Data == IntPtr.Zero)
                            {
                                break;
                            }
                            await _player.Enqueue(resampledAudio);
                        }
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
            _resampler.Dispose();
            _demuxer.Dispose();
            var browser = await _launchBrowserTask;
            await browser.StopCapturing();
            await browser.DisposeAsync();
        }
        finally
        {
            _disposed = true;
            _semaphore.Release();
        }
        _semaphore.Dispose();
    }

    private void OnAudioSendStatusChanged(object? sender, AudioSendStatusChangedEventArgs args)
    {
        if (args.MediaSendStatus == MediaSendStatus.Active)
        {
            _audioSocketActive.TrySetResult(true);
        }
    }

    private void OnVideoSendStatusChanged(object? sender, VideoSendStatusChangedEventArgs args)
    {
        if (args.MediaSendStatus == MediaSendStatus.Active)
        {
            _videoSocketActive.TrySetResult(true);
        }
    }
}
