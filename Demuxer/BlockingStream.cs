namespace Demuxer;

using System.Runtime.InteropServices;

public class BlockingStream : IBlockingStream
{
    private const int MaxSize = 512 * 1024;

    private readonly object _lock = new();
    private readonly byte[] _buffer = new byte[MaxSize];
    private int _buffer_size;
    private int _offset;
    private int _disposed;

    public void Write(byte[] packet)
    {
        lock (_lock)
        {
            if (_buffer_size + packet.Length > MaxSize)
            {
                Monitor.Wait(_lock);
            }
            if (_disposed == 0)
            {
                Array.Copy(_buffer, _offset, _buffer, 0, _buffer_size - _offset);
                _buffer_size -= _offset;
                _offset = 0;
                Array.Copy(packet, 0, _buffer, _buffer_size, packet.Length);
                _buffer_size += packet.Length;
                Monitor.PulseAll(_lock);
            }
        }
    }

    public int Read(IntPtr buffer, int size)
    {
        lock (_lock)
        {
            if (_buffer_size - _offset == 0)
            {
                Monitor.Wait(_lock);
            }
            if (_disposed == 0)
            {
                var numberOfBytesToCopy = Math.Min(_buffer_size - _offset, size);
                Marshal.Copy(_buffer, _offset, buffer, numberOfBytesToCopy);
                _offset += numberOfBytesToCopy;
                Monitor.PulseAll(_lock);
                return numberOfBytesToCopy;
            }
            return -1;
        }
    }

    // TODO: probably better to call it StopAcceptingPacketsAndUnlockThreads
    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = 1;
            Monitor.PulseAll(_lock);
        }
    }
}
