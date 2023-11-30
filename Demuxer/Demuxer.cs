namespace Demuxer;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

public class Demuxer : IDemuxer
{
    private static readonly Frame EmptyFrame = new(FrameType.Video, 0, TimeSpan.Zero, Array.Empty<byte>());

    private readonly IBlockingStream _stream;

    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable", Justification = "The callback scope must be bigger than the scope of the native demuxer")]
    private readonly Callback _callback;

    private readonly IntPtr _demuxer;

    public Demuxer(IBlockingStream stream)
    {
        _stream = stream;
        _callback = Callback;
        _demuxer = NativeDemuxerApi.CreateDemuxer(_callback);
    }

    /// <summary>
    /// If the returned array is empty, then there is not enough source data. Write more packets.
    /// </summary>
    public Frame ReadFrame()
    {
        var data = new byte[1920 * 1080 * 3 / 2]; // TODO: check the size and do not hardcode
        var metadata = new FrameMetadata();
        var status = NativeDemuxerApi.ReadFrame(_demuxer, data, ref metadata);
        return status == 0 ? new Frame(metadata.Type, metadata.Size, TimeSpan.FromMilliseconds(metadata.Timestamp), data) : EmptyFrame;
    }

    public void Dispose()
    {
        NativeDemuxerApi.DeleteDemuxer(_demuxer);
    }

    private int Callback(IntPtr buffer, int size)
    {
        var bytes = _stream.Read(size);
        Marshal.Copy(bytes, 0, buffer, bytes.Length);
        return bytes.Length;
    }
}
