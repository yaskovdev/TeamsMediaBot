namespace TeamsMediaBot;

using System.Collections.Concurrent;
using System.Diagnostics;
using Demuxer;
using Microsoft.Skype.Bots.Media;

public class Player : IAsyncDisposable
{
    private readonly ConcurrentQueue<AbstractFrame> _audioQueue = new();
    private readonly ConcurrentQueue<AbstractFrame> _videoQueue = new();
    private readonly IAudioSocket _audioSocket;
    private readonly IVideoSocket _videoSocket;
    private readonly VideoFormat _videoFormat;
    private int _playing;
    private int _disposed;
    private int _count;
    private volatile Task? _playerTask;

    public Player(IAudioSocket audioSocket, IVideoSocket videoSocket, VideoFormat videoFormat)
    {
        _audioSocket = audioSocket;
        _videoSocket = videoSocket;
        _videoFormat = videoFormat;
    }

    public void Enqueue(AbstractFrame frame)
    {
        if (_count % 1000 == 0)
        {
            Console.WriteLine($"Audio queue size is {_audioQueue.Count}, video queue size is {_videoQueue.Count}");
        }

        EnqueueInternal(frame);

        if (Interlocked.Exchange(ref _playing, 1) == 0)
        {
            _playerTask = Task.Run(StartPlaying);
        }
        Interlocked.Increment(ref _count);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            Console.WriteLine("Waiting for the player to empty the queues and stop");
            if (_playerTask is not null) await _playerTask;
            Console.WriteLine("Player stopped");
        }
    }

    private void StartPlaying()
    {
        // TODO: probably wait for a specific queue length before starting playing
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            Dequeue(_audioQueue, stopwatch.Elapsed);
            Dequeue(_videoQueue, stopwatch.Elapsed);
            if (_disposed == 1 && _audioQueue.IsEmpty && _videoQueue.IsEmpty)
            {
                break;
            }
        }
        stopwatch.Stop();
    }

    private void Dequeue(ConcurrentQueue<AbstractFrame> queue, TimeSpan time)
    {
        if (_disposed == 1)
        {
            EmptyQueue(queue);
        }
        else
        {
            while (queue.TryPeek(out var head) && head.Timestamp <= time)
            {
                if (queue.TryDequeue(out var frame))
                {
                    Send(frame);
                }
            }
        }
    }

    private void EnqueueInternal(AbstractFrame frame)
    {
        var queue = frame.Type switch
        {
            FrameType.Audio => _audioQueue,
            FrameType.Video => _videoQueue,
            _ => null
        };
        queue?.Enqueue(frame);
    }

    /// <summary>
    /// Catching exceptions here would not be needed if Microsoft.Graph.Communications.Calls could wait
    /// for the async dispose in <c>TeamsMediaBotService.OnUpdated</c> before disposing of the sockets.
    /// </summary>
    private void Send(AbstractFrame frame)
    {
        try
        {
            if (frame.Type == FrameType.Audio) _audioSocket.Send(new AudioBuffer(frame, AudioFormat.Pcm16K));
            else if (frame.Type == FrameType.Video) _videoSocket.Send(new VideoBuffer(frame, _videoFormat));
        }
        catch (ObjectDisposedException)
        {
            frame.Dispose();
        }
    }

    private static void EmptyQueue(ConcurrentQueue<AbstractFrame> queue)
    {
        while (queue.TryDequeue(out var head))
        {
            head.Dispose();
        }
    }
}
