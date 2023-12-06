namespace Demuxer;

public class Resampler : IResampler
{
    private readonly IntPtr _resampler = NativeResamplerApi.CreateResampler();

    public void WriteFrame(IntPtr bytes, int length, int timestamp)
    {
        NativeResamplerApi.WriteFrame(_resampler, bytes, length, timestamp);
    }

    public Frame ReadFrame()
    {
        var metadata = new FrameMetadata();
        var data = NativeResamplerApi.ReadFrame(_resampler, ref metadata);
        return new Frame(FrameType.Audio, data, metadata.Size, TimeSpan.FromMilliseconds(metadata.Timestamp));
    }

    public void Dispose()
    {
        NativeResamplerApi.DeleteResampler(_resampler);
    }
}
