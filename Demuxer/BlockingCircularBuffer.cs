namespace Demuxer;

using System.Runtime.InteropServices;

public class BlockingCircularBuffer : IBlockingBuffer
{
    private readonly object _lock = new();
    private readonly byte[] _buffer;
    private int _head;
    private int _tail;
    private int _size;
    private int _disposed;

    public BlockingCircularBuffer(int capacity)
    {
        _buffer = new byte[capacity];
    }

    public void Write(byte[] value)
    {
        lock (_lock)
        {
            if (_size + value.Length > _buffer.Length) // TODO: what if packet.Length > _buffer.Length? Waiting won't help then.
            {
                Monitor.Wait(_lock);
            }

            if (_disposed == 1)
            {
                return;
            }

            var numToCopy = value.Length;
            var firstPart = _buffer.Length - _tail < numToCopy ? _buffer.Length - _tail : numToCopy;
            Array.Copy(value, 0, _buffer, _tail, firstPart);
            numToCopy -= firstPart;
            if (numToCopy > 0)
            {
                Array.Copy(value, _buffer.Length - _tail, _buffer, 0, numToCopy);
            }

            _tail = (_tail + value.Length) % _buffer.Length;
            _size += value.Length;
            Monitor.PulseAll(_lock);
        }
    }

    public int Read(IntPtr data, int size)
    {
        lock (_lock)
        {
            if (_size == 0)
            {
                Monitor.Wait(_lock);
            }

            if (_disposed == 1)
            {
                return -1;
            }

            var numberOfBytesToCopy = Math.Min(_size, size);
            var firstPart = _buffer.Length - _head < numberOfBytesToCopy ? _buffer.Length - _head : numberOfBytesToCopy;
            Marshal.Copy(_buffer, _head, data, firstPart);
            if (numberOfBytesToCopy - firstPart > 0)
            {
                Marshal.Copy(_buffer, 0, data + _buffer.Length - _head, numberOfBytesToCopy - firstPart);
            }

            _head = (_head + numberOfBytesToCopy) % _buffer.Length;
            _size -= numberOfBytesToCopy;

            Monitor.PulseAll(_lock);
            return numberOfBytesToCopy;
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
