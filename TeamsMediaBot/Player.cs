namespace TeamsMediaBot;

using System.Collections.Concurrent;
using Demuxer;
using Microsoft.Skype.Bots.Media;

public class Player : IAsyncDisposable
{
    private const int MinLengthOfBuffersInSeconds = 2;
    private readonly Timer _timer;
    private readonly int _audioFps;
    private readonly int _videoFps;
    private readonly ConcurrentQueue<AbstractFrame> _audioQueue = new();
    private readonly ConcurrentQueue<AbstractFrame> _videoQueue = new();
    private readonly IAudioSocket _audioSocket;
    private readonly IVideoSocket _videoSocket;
    private int _count;
    private int _executionsToSkip;

    public Player(IAudioSocket audioSocket, IVideoSocket videoSocket, int videoFps)
    {
        _audioSocket = audioSocket;
        _videoSocket = videoSocket;
        _audioFps = 50;
        _videoFps = videoFps;
        _timer = new Timer(SendBuffers);
        _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1).Divide(_videoFps));
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
    }

    // TODO: probably better if (_count % _audioFps == 0) EmitAudioFrame(); else if (_count % _videoFps == 0) EmitVideoFrame();
    private void SendBuffers(object? state)
    {
        if (_executionsToSkip > 0)
        {
            _executionsToSkip--;
        }
        else if (_audioQueue.Count < MinLengthOfBuffersInSeconds * _audioFps || _videoQueue.Count < MinLengthOfBuffersInSeconds * _videoFps)
        {
            _executionsToSkip += _videoFps;
        }
        else
        {
            if (_videoQueue.TryDequeue(out var videoFrame))
            {
                _videoSocket.Send(MapVideo(videoFrame));
            }
            if (_count == 0)
            {
                for (var i = 0; i < 50; i++)
                {
                    if (_audioQueue.TryDequeue(out var audioFrame))
                    {
                        _audioSocket.Send(MapAudio(audioFrame));
                    }
                }
            }
            _count = (_count + 1) % _videoFps;
        }
    }

    public async ValueTask DisposeAsync() => await _timer.DisposeAsync();

    private static AudioBuffer MapAudio(AbstractFrame frame) => new(frame, AudioFormat.Pcm16K);

    private static VideoBuffer MapVideo(AbstractFrame frame) => new(frame, VideoFormat.NV12_1920x1080_15Fps);
}
