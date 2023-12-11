﻿namespace TeamsMediaBot;

using System.Diagnostics;
using Demuxer;
using Microsoft.Skype.Bots.Media;

public class Player : IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Queue<AbstractFrame> _audioQueue = new();
    private readonly Queue<AbstractFrame> _videoQueue = new();
    private readonly IAudioSocket _audioSocket;
    private readonly IVideoSocket _videoSocket;
    private int _playing;
    private bool _disposed;
    private int _count;

    public Player(IAudioSocket audioSocket, IVideoSocket videoSocket)
    {
        _audioSocket = audioSocket;
        _videoSocket = videoSocket;
    }

    public async Task Enqueue(AbstractFrame frame)
    {
        if (_count % 1000 == 0)
        {
            Console.WriteLine($"Audio queue size is {_audioQueue.Count}, video queue size is {_videoQueue.Count}");
        }

        if (frame.Type == FrameType.Audio)
        {
            await Enqueue(_audioQueue, frame);
        }
        else if (frame.Type == FrameType.Video)
        {
            await Enqueue(_videoQueue, frame);
        }

        if (Interlocked.Exchange(ref _playing, 1) == 0)
        {
            _ = StartPlaying(); // TODO: log possible exceptions
        }
        Interlocked.Increment(ref _count);
    }

    private async Task StartPlaying()
    {
        await Task.Delay(TimeSpan.FromSeconds(4)); // TODO: probably wait for a specific queue length, not just for the hardcoded number of seconds.
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                await _semaphore.WaitAsync();
                if (_disposed) break;
                while (_audioQueue.TryPeek(out var head) && head.Timestamp <= stopwatch.Elapsed)
                {
                    _audioSocket.Send(MapAudio(_audioQueue.Dequeue()));
                }
            }
            finally
            {
                _semaphore.Release();
            }
            try
            {
                await _semaphore.WaitAsync();
                if (_disposed) break;
                while (_videoQueue.TryPeek(out var head) && head.Timestamp <= stopwatch.Elapsed)
                {
                    _videoSocket.Send(MapVideo(_videoQueue.Dequeue()));
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
        stopwatch.Stop();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await _semaphore.WaitAsync();
            _disposed = true;
        }
        finally
        {
            _semaphore.Release();
        }
        _semaphore.Dispose();
    }

    private async Task Enqueue(Queue<AbstractFrame> queue, AbstractFrame frame)
    {
        try
        {
            await _semaphore.WaitAsync();
            queue.Enqueue(frame);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static AudioBuffer MapAudio(AbstractFrame frame) => new(frame, AudioFormat.Pcm16K);

    private static VideoBuffer MapVideo(AbstractFrame frame) => new(frame, VideoFormat.NV12_1920x1080_15Fps);
}
