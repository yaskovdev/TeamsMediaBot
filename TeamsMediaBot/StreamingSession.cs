namespace TeamsMediaBot;

using BrowserAudioVideoCapturingService;
using Demuxer;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Skype.Bots.Media;
using PuppeteerSharp;

public class StreamingSession : IAsyncDisposable
{
    private readonly Task<IBrowser> _launchBrowserTask;
    private readonly Task _streamingTask;
    private readonly IBlockingBuffer _buffer;
    private readonly IDemuxer _demuxer;
    private readonly IResampler _resampler;
    private readonly TaskCompletionSource<bool> _audioSocketActive = new();
    private readonly TaskCompletionSource<bool> _videoSocketActive = new();
    private readonly Player _player;
    private int _disposed;

    public StreamingSession(ILocalMediaSession mediaSession, VideoFormat videoFormat)
    {
        _buffer = new BlockingCircularBuffer(512 * 1024 * 1024); // TODO: apparently, the browser still tries to write to the buffer even if no more data can fit. Should be fixed. Increasing the buffer size as a temporary solution
        var browserLauncher = new BrowserLauncher();
        _launchBrowserTask = browserLauncher.LaunchInstance(videoFormat.Width, videoFormat.Height, (int)videoFormat.FrameRate, _buffer.Write);
        _demuxer = new Demuxer(_buffer);
        _resampler = new Resampler();
        var audioSocket = mediaSession.AudioSocket;
        audioSocket.AudioSendStatusChanged += OnAudioSendStatusChanged;
        var videoSocket = mediaSession.VideoSockets[0];
        videoSocket.VideoSendStatusChanged += OnVideoSendStatusChanged;
        _player = new Player(audioSocket, videoSocket, videoFormat);
        _streamingTask = StartStreaming();
    }

    private async Task StartStreaming()
    {
        Console.WriteLine("Waiting for the sockets to become active");
        await Task.WhenAll(_audioSocketActive.Task, _videoSocketActive.Task);
        Console.WriteLine("Sockets are active");
        while (true)
        {
            if (_disposed == 1)
            {
                return;
            }
            var frame = _demuxer.ReadFrame();
            if (frame.Type == FrameType.Video)
            {
                _player.Enqueue(frame);
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
                    _player.Enqueue(resampledAudio);
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            var browser = await _launchBrowserTask;
            await browser.StopCapturing();
            await browser.DisposeAsync();
            Console.WriteLine("Waiting for streaming to finish before disposing of the rest of the dependencies");
            await _streamingTask;
            _buffer.Dispose();
            _resampler.Dispose();
            _demuxer.Dispose();
            await _player.DisposeAsync();
        }
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
