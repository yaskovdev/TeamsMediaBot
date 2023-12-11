namespace TeamsMediaBot;

using System.Collections.Concurrent;
using Demuxer;
using Microsoft.Skype.Bots.Media;

public class Player : IDisposable
{
    private const int AudioFps = 50;
    private readonly PeriodicTimer _timer;
    private readonly ConcurrentQueue<AbstractFrame> _audioQueue = new(); // TODO: should the queues be concurrent or better to rely on the synchronization?
    private readonly ConcurrentQueue<AbstractFrame> _videoQueue = new();
    private readonly IAudioSocket _audioSocket;
    private readonly IVideoSocket _videoSocket;
    private int _playing;
    private int _tick;

    private static TimeSpan Interval => TimeSpan.FromSeconds(1) / AudioFps;

    private TimeSpan Now => Interval * _tick;

    public Player(IAudioSocket audioSocket, IVideoSocket videoSocket)
    {
        _audioSocket = audioSocket;
        _videoSocket = videoSocket;
        _timer = new PeriodicTimer(Interval);
    }

    public void Enqueue(AbstractFrame frame)
    {
        if (frame.Type == FrameType.Audio)
        {
            _audioQueue.Enqueue(frame);
        }
        else if (frame.Type == FrameType.Video)
        {
            _videoQueue.Enqueue(frame);
        }

        if (Interlocked.Exchange(ref _playing, 1) == 0) // TODO: probably wait for a longer queue, not just for the very first enqueued frame.
        {
            _ = StartPlaying();
        }
    }

    private async Task StartPlaying()
    {
        while (await _timer.WaitForNextTickAsync())
        {
            while (_audioQueue.TryPeek(out var next) && next.Timestamp <= Now) // TODO: DRY
            {
                _audioQueue.TryDequeue(out _);
                _audioSocket.Send(MapAudio(next)); // TODO: probably send only the last one to be able to keep up? And dispose of the others. Same for video.
            }
            while (_videoQueue.TryPeek(out var next) && next.Timestamp <= Now)
            {
                _videoQueue.TryDequeue(out _);
                _videoSocket.Send(MapVideo(next));
            }
            Interlocked.Increment(ref _tick);
        }
    }

    public void Dispose() => _timer.Dispose(); // TODO: need to synchronize with StartPlaying() to guarantee that once Dispose() returns no frames will be sent to sockets

    private static AudioBuffer MapAudio(AbstractFrame frame) => new(frame, AudioFormat.Pcm16K);

    private static VideoBuffer MapVideo(AbstractFrame frame) => new(frame, VideoFormat.NV12_1920x1080_15Fps);
}
