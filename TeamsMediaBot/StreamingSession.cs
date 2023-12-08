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
    private readonly IResampler _resampler;
    private readonly IAudioSocket _audioSocket;
    private readonly IVideoSocket _videoSocket;
    private readonly TaskCompletionSource<bool> _audioSocketActive = new();
    private readonly TaskCompletionSource<bool> _videoSocketActive = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    // private bool _disposed;
    // private AudioVideoFramePlayer? _player;

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
        _ = StartStreaming(); // TODO: log exception if any
    }

    private async Task StartStreaming()
    {
        Console.WriteLine("Waiting for the socket to become active");
        await _audioSocketActive.Task;
        // var playerSettings = new AudioVideoFramePlayerSettings(new AudioSettings(20), new VideoSettings(), 0);
        // _player = new AudioVideoFramePlayer((AudioSocket)_audioSocket, (VideoSocket)_videoSocket, playerSettings);
        Console.WriteLine("Creating buffers");
        List<AudioMediaBuffer> buffers = DummyAudioPlayer.CreateAudioMediaBuffers();
        int i = 0;
        while (true)
        {
            Console.WriteLine("Sending buffers");
            foreach (var buffer in buffers.Take(new Range(50 * i, 50 * (i + 1) - 1)))
            {
                _audioSocket.Send(buffer);
            }
            await Task.Delay(1000);
            i += 1;
        }
        // await _player.EnqueueBuffersAsync(buffers, ImmutableList<VideoMediaBuffer>.Empty);
        // while (true)
        // {
        //     try
        //     {
        //         await _semaphore.WaitAsync();
        //         if (!_disposed)
        //         {
        //             var frame = _demuxer.ReadFrame();
        //             if (frame.Type == FrameType.Video)
        //             {
        //                 // await _player.EnqueueBuffersAsync(ImmutableList<AudioMediaBuffer>.Empty.Add(buffers[i++]), ImmutableList<VideoMediaBuffer>.Empty.Add(MapVideo(frame)));
        //                 if (i == buffers.Count)
        //                 {
        //                     i = 0;
        //                 }
        //             }
        //             else
        //             {
        //                 // Console.WriteLine("Sending audio " + frame.Size);
        //                 _audioSocket.Send(MapAudio(frame));
        //                 // Console.WriteLine("Done sending audio");
        //                 // _resampler.WriteFrame(frame.Data, (int)frame.Size, (int)frame.Timestamp.TotalMilliseconds);
        //                 // while (true)
        //                 // {
        //                 //     var resampledAudio = _resampler.ReadFrame();
        //                 //     if (resampledAudio.Data == IntPtr.Zero)
        //                 //     {
        //                 //         break;
        //                 //     }
        //                 //     Console.WriteLine("Sending audio " + frame.Timestamp);
        //                 //     await _player.EnqueueBuffersAsync(ImmutableList<AudioMediaBuffer>.Empty.Add(MapAudio(frame)), ImmutableList<VideoMediaBuffer>.Empty);
        //                 // }
        //             }
        //         }
        //         else
        //         {
        //             return;
        //         }
        //     }
        //     finally
        //     {
        //         _semaphore.Release();
        //     }
        // }
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
            // _disposed = true;
            _semaphore.Release();
            _semaphore.Dispose();
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

    private static VideoBuffer MapVideo(Frame frame) => new(frame, VideoFormat.NV12_1920x1080_15Fps);

    private static AudioBuffer MapAudio(Frame frame) => new(frame, AudioFormat.Pcm16K);
}
