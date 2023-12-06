namespace Demuxer;

public class Resampler : IResampler
{
    private readonly IntPtr _resampler = NativeResamplerApi.CreateResampler();

    public void WriteFrame(byte[] bytes)
    {
        NativeResamplerApi.WriteFrame(_resampler, bytes, bytes.Length); // TODO: use SizeParamIndex
    }

    public Frame ReadFrame()
    {
        var length = 0;
        var data = NativeResamplerApi.ReadFrame(_resampler, ref length);
        return new Frame(FrameType.Audio, data, (ulong)length, TimeSpan.Zero);
    }

    public void Dispose()
    {
        NativeResamplerApi.DeleteResampler(_resampler);
    }
}
