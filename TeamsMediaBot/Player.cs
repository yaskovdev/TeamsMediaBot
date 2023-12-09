namespace TeamsMediaBot;

using Demuxer;

public class Player : IAsyncDisposable
{
    private const int MinLengthOfBuffersInSeconds = 2;
    private readonly Timer _timer;
    private readonly int _audioFps;
    private readonly int _videoFps;
    private readonly Queue<Frame> _audioFrames = new();
    private readonly Queue<Frame> _videoFrames = new();
    private int _count;
    private int _executionsToSkip;

    public Player(int videoFps)
    {
        _audioFps = 50;
        _videoFps = videoFps;
        _timer = new Timer(SendBuffers);
        _timer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1).Divide(_videoFps));
    }

    public void Enqueue(Frame frame)
    {
        if (frame.Type == FrameType.Audio)
        {
            _audioFrames.Enqueue(frame);
        }
        else if (frame.Type == FrameType.Video)
        {
            _videoFrames.Enqueue(frame);
        }
    }

    // TODO: probably better if (_count % _audioFps == 0) EmitAudioFrame(); else if (_count % _videoFps == 0) EmitVideoFrame();
    private void SendBuffers(object? state)
    {
        if (_audioFrames.Count < MinLengthOfBuffersInSeconds * _audioFps || _videoFrames.Count < MinLengthOfBuffersInSeconds * _videoFps)
        {
            _executionsToSkip += _videoFps;
        }

        if (_executionsToSkip > 0)
        {
            _executionsToSkip--;
        }
        else
        {
            if (_count == 0)
            {
                // send 50 of 20 ms audio frames in a loop
            }
            Console.WriteLine();
            _count = (_count + 1) % _videoFps;
        }
    }

    public async ValueTask DisposeAsync() => await _timer.DisposeAsync();
}
