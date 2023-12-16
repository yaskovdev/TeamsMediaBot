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
            _playerTask = Task.Run(StartPlaying);;
        }
        Interlocked.Increment(ref _count);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            Console.WriteLine("Waiting for the player to stop");
            if (_playerTask is not null) await _playerTask;
            Console.WriteLine("Player stopped"); // TODO: also dequeue all the items and dispose of them
        }
    }

    private void StartPlaying()
    {
        // TODO: probably wait for a specific queue length before starting playing
        var stopwatch = Stopwatch.StartNew();
        while (true)
        {
            try
            {
                Dequeue(_audioQueue, stopwatch.Elapsed);
                Dequeue(_videoQueue, stopwatch.Elapsed);
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
        stopwatch.Stop();
    }

    private void Dequeue(ConcurrentQueue<AbstractFrame> queue, TimeSpan time)
    {
        if (_disposed == 1) throw new ObjectDisposedException(nameof(Player));
        while (queue.TryPeek(out var head) && head.Timestamp <= time)
        {
            if (queue.TryDequeue(out var frame))
            {
                Send(frame);
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

    private void Send(AbstractFrame frame)
    {
        if (frame.Type == FrameType.Audio) _audioSocket.Send(new AudioBuffer(frame, AudioFormat.Pcm16K));
        else if (frame.Type == FrameType.Video) _videoSocket.Send(new VideoBuffer(frame, _videoFormat));
    }
}
