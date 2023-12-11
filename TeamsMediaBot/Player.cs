namespace TeamsMediaBot;

using Demuxer;
using Microsoft.Skype.Bots.Media;

public class Player : IAsyncDisposable
{
    private const int AudioFps = 50;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly PeriodicTimer _timer;
    private readonly Queue<AbstractFrame> _audioQueue = new(); // TODO: should the queues be concurrent or better to rely on the synchronization?
    private readonly Queue<AbstractFrame> _videoQueue = new();
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
        while (await _timer.WaitForNextTickAsync()) // TODO: try using a normal while (_disposed == 0) cycle and current time.
        {
            try
            {
                await _semaphore.WaitAsync();
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
                _tick++;
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
            _timer.Dispose();
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
