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

        if (Interlocked.Exchange(ref _playing, 1) == 0)
        {
            _ = StartPlaying(); // TODO: log possible exceptions
        }
    }

    private async Task StartPlaying()
    {
        await Task.Delay(TimeSpan.FromSeconds(4)); // TODO: probably wait for a specific queue length, not just for the hardcoded number of seconds.
        while (await _timer.WaitForNextTickAsync())
        {
            var audioFrame = Forward(_audioQueue, Now);
            if (audioFrame is not null)
            {
                _audioSocket.Send(MapAudio(audioFrame));
            }
            var videoFrame = Forward(_videoQueue, Now);
            if (videoFrame is not null)
            {
                _videoSocket.Send(MapVideo(videoFrame));
            }
            Interlocked.Increment(ref _tick);
        }
    }

    public void Dispose() => _timer.Dispose(); // TODO: need to synchronize with StartPlaying() to guarantee that once Dispose() returns no frames will be sent to sockets

    private static AbstractFrame? Forward(ConcurrentQueue<AbstractFrame> queue, TimeSpan time)
    {
        AbstractFrame? frame = null;
        while (queue.TryPeek(out var head) && head.Timestamp <= time)
        {
            if (frame is not null)
            {
                Console.WriteLine($"Skipping a frame of type {frame.Type}");
                frame.Dispose();
            }
            queue.TryDequeue(out frame);
        }
        return frame;
    }

    private static AudioBuffer MapAudio(AbstractFrame frame) => new(frame, AudioFormat.Pcm16K);

    private static VideoBuffer MapVideo(AbstractFrame frame) => new(frame, VideoFormat.NV12_1920x1080_15Fps);
}
