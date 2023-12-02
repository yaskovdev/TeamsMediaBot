﻿namespace TeamsMediaBot;

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
    private readonly IBlockingStream _stream;
    private readonly IDemuxer _demuxer;
    private readonly IVideoSocket _videoSocket;
    private readonly TaskCompletionSource<bool> _videoSocketActive = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private int _disposed;
    private AudioVideoFramePlayer? _player;

    public StreamingSession(ILocalMediaSession mediaSession)
    {
        _stream = new BlockingStream(512 * 1024);
        var browserLauncher = new BrowserLauncher();
        _launchBrowserTask = browserLauncher.LaunchInstance(_stream);
        _demuxer = new Demuxer(_stream);
        _videoSocket = mediaSession.VideoSockets[0];
        _videoSocket.VideoSendStatusChanged += OnVideoSendStatusChanged;
        var audioSocket = mediaSession.AudioSocket;
        audioSocket.AudioSendStatusChanged += OnAudioSendStatusChanged;
        _ = StartStreaming();
    }

    private async Task StartStreaming()
    {
        await _videoSocketActive.Task;
        var playerSettings = new AudioVideoFramePlayerSettings(new AudioSettings(20), new VideoSettings(), 1000);
        _player = new AudioVideoFramePlayer(null, (VideoSocket)_videoSocket, playerSettings);
        _player.LowOnFrames += OnLowOnFrames;
        while (true)
        {
            try
            {
                await _semaphore.WaitAsync(); // TODO: test how it performs against normal lock and no lock at all
                if (_disposed == 0)
                {
                    var frame = _demuxer.ReadFrame();
                    if (frame.Type == FrameType.Video)
                    {
                        await _player.EnqueueBuffersAsync(ImmutableList<AudioMediaBuffer>.Empty, ImmutableList<VideoMediaBuffer>.Empty.Add(Map(frame)));
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
            _stream.Dispose();
            _demuxer.Dispose();
            var browser = await _launchBrowserTask;
            await browser.StopCapturing();
            await browser.DisposeAsync();
        }
        finally
        {
            _disposed = 1;
            _semaphore.Release();
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

    private static void OnLowOnFrames(object? sender, LowOnFramesEventArgs e)
    {
        Console.WriteLine($"Player is low on {e.MediaType} frames, remaining media length in ms is {e.RemainingMediaLengthInMS}");
    }

    private static VideoSendBuffer Map(Frame frame) =>
        new(frame.Data.ToArray(), (uint)frame.Data.Count, VideoFormat.NV12_1920x1080_15Fps, frame.Timestamp.Ticks);
}