namespace Demuxer;

using System.Runtime.InteropServices;

public class Resampler : IResampler
{
    private static readonly Frame EmptyFrame = new(FrameType.Audio, IntPtr.Zero, 0, TimeSpan.Zero);

    private const int OutputPacketLengthMs = 20;
    private const int OutputPacketSize = OutputPacketLengthMs * 32;
    private readonly IntPtr _resampler = NativeResamplerApi.CreateResampler();
    private readonly CircularBuffer _outputBuffer = new(512 * 1024);
    private int _outputCount;
    private int _disposed;

    public void WriteFrame(IntPtr bytes, int length)
    {
        var metadata = new FrameMetadata();
        var resampledFrame = NativeResamplerApi.ResampleFrame(_resampler, bytes, length, ref metadata);
        try
        {
            _outputBuffer.Write(resampledFrame, (int)metadata.Size);
        }
        finally
        {
            NativeResamplerApi.DeleteFrameBuffer(resampledFrame);
        }
    }

    public AbstractFrame ReadFrame()
    {
        if (_outputBuffer.Size >= OutputPacketSize)
        {
            var data = Marshal.AllocHGlobal(OutputPacketSize);
            _outputBuffer.Read(data, OutputPacketSize);
            var frame = new Frame(FrameType.Audio, data, OutputPacketSize, TimeSpan.FromMilliseconds(_outputCount * OutputPacketLengthMs));
            _outputCount++;
            return frame;
        }
        return EmptyFrame;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            NativeResamplerApi.DeleteResampler(_resampler);
        }
    }
}
