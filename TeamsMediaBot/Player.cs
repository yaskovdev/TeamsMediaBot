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
    private readonly VideoFormat _videoFormat;
    private int _playing;
    private bool _disposed;
    private int _count;

    public Player(IAudioSocket audioSocket, IVideoSocket videoSocket, VideoFormat videoFormat)
    {
        _audioSocket = audioSocket;
        _videoSocket = videoSocket;
        _videoFormat = videoFormat;
    }

    public async Task Enqueue(AbstractFrame frame)
    {
        if (_count % 1000 == 0)
        {
            Console.WriteLine($"Audio queue size is {_audioQueue.Count}, video queue size is {_videoQueue.Count}");
        }

        await EnqueueInternal(frame);

        if (Interlocked.Exchange(ref _playing, 1) == 0)
        {
            StartPlaying().OnException(e => Console.WriteLine($"An exception happened during playing: {e}"));
        }
        Interlocked.Increment(ref _count);
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

    private async Task StartPlaying()
    {
        await Task.Delay(TimeSpan.FromSeconds(1)); // TODO: probably wait for a specific queue length, not just for the hardcoded number of seconds.
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                await Dequeue(_audioQueue, stopwatch.Elapsed);
                await Dequeue(_videoQueue, stopwatch.Elapsed);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
        stopwatch.Stop();
    }

    private async Task Dequeue(Queue<AbstractFrame> queue, TimeSpan time)
    {
        try
        {
            await _semaphore.WaitAsync();
            if (_disposed) throw new ObjectDisposedException(nameof(Player));
            while (queue.TryPeek(out var head) && head.Timestamp <= time)
            {
                Send(queue.Dequeue());
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task EnqueueInternal(AbstractFrame frame)
    {
        var queue = frame.Type switch
        {
            FrameType.Audio => _audioQueue,
            FrameType.Video => _videoQueue,
            _ => null
        };
        try
        {
            await _semaphore.WaitAsync();
            queue?.Enqueue(frame);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void Send(AbstractFrame frame)
    {
        if (frame.Type == FrameType.Audio) _audioSocket.Send(new AudioBuffer(frame, AudioFormat.Pcm16K));
        else if (frame.Type == FrameType.Video) _videoSocket.Send(new VideoBuffer(frame, _videoFormat));
    }
}
