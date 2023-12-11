namespace Demuxer;

using System.Runtime.InteropServices;

public class Resampler : IResampler
{
    private const int OutputPacketLengthMs = 20;
    private const int OutputPacketSize = OutputPacketLengthMs * 32;
    private readonly IntPtr _resampler = NativeResamplerApi.CreateResampler();
    private readonly CircularBuffer _outputBuffer = new(512 * 1024);
    private int _outputCount;
    private int _disposed;

    public void WriteFrame(IntPtr bytes, int length) => NativeResamplerApi.WriteFrame(_resampler, bytes, length);

    public AbstractFrame ReadFrame()
    {
        while (true)
        {
            var metadata = new FrameMetadata();
            var data = NativeResamplerApi.ReadFrame(_resampler, ref metadata);
            if (data == IntPtr.Zero)
            {
                break;
            }
            _outputBuffer.Write(data, (int)metadata.Size);
        }
        if (_outputBuffer.Size >= OutputPacketSize)
        {
            var data = Marshal.AllocHGlobal(OutputPacketSize);
            _outputBuffer.Read(data, OutputPacketSize);
            var frame = new Frame(FrameType.Audio, data, OutputPacketSize, TimeSpan.FromMilliseconds(_outputCount * OutputPacketLengthMs));
            _outputCount++;
            return frame;
        }
        return new Frame(FrameType.Audio, IntPtr.Zero, 0, TimeSpan.Zero);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            NativeResamplerApi.DeleteResampler(_resampler);
        }
    }
}
