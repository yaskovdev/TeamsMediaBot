namespace TeamsMediaBot;

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
    private int _disposed;

    public Player(IAudioSocket audioSocket, IVideoSocket videoSocket)
    {
        _audioSocket = audioSocket;
        _videoSocket = videoSocket;
    }

    public async Task Enqueue(AbstractFrame frame)
    {
        try
        {
            await _semaphore.WaitAsync();
            if (frame.Type == FrameType.Audio)
            {
                _audioQueue.Enqueue(frame);
            }
            else if (frame.Type == FrameType.Video)
            {
                _videoQueue.Enqueue(frame);
            }
        }
        finally
        {
            _semaphore.Release();
        }

        if (Interlocked.Exchange(ref _playing, 1) == 0)
        {
            _ = StartPlaying(); // TODO: log possible exceptions
        }
    }

    private async Task StartPlaying()
    {
        await Task.Delay(TimeSpan.FromSeconds(4)); // TODO: probably wait for a specific queue length, not just for the hardcoded number of seconds.
        var stopwatch = Stopwatch.StartNew();
        while (_disposed == 0)
        {
            try
            {
                await _semaphore.WaitAsync();
                var audioFrame = Forward(_audioQueue, stopwatch.Elapsed);
                if (audioFrame is not null)
                {
                    _audioSocket.Send(MapAudio(audioFrame));
                }
                var videoFrame = Forward(_videoQueue, stopwatch.Elapsed);
                if (videoFrame is not null)
                {
                    _videoSocket.Send(MapVideo(videoFrame));
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
            _disposed = 1;
        }
        finally
        {
            _semaphore.Release();
        }
        _semaphore.Dispose();
    }

    private static AbstractFrame? Forward(Queue<AbstractFrame> queue, TimeSpan time)
    {
        AbstractFrame? frame = null;
        while (queue.TryPeek(out var head) && head.Timestamp <= time)
        {
            if (frame is not null)
            {
                Console.WriteLine($"Skipping a frame of type {frame.Type}"); // TODO: now when the cycle runs as fast as possible there is probably no need to skip anything, since it will likely keep up
                frame.Dispose();
            }
            queue.TryDequeue(out frame);
        }
        return frame;
    }

    private static AudioBuffer MapAudio(AbstractFrame frame) => new(frame, AudioFormat.Pcm16K);

    private static VideoBuffer MapVideo(AbstractFrame frame) => new(frame, VideoFormat.NV12_1920x1080_15Fps);
}
