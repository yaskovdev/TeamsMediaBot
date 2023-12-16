namespace Demuxer;

using System.Runtime.InteropServices;

public class BlockingCircularBuffer : IBlockingBuffer
{
    private const int EndOfFile = -1;

    private readonly object _lock = new();
    private readonly byte[] _buffer;
    private int _head;
    private int _tail;
    private int _size;
    private bool _disposed;

    public BlockingCircularBuffer(int capacity)
    {
        _buffer = new byte[capacity];
    }

    public void Write(string packet)
    {
        lock (_lock) // TODO: SemaphoreSlim is also an option (see how BlockingCollection is implemented)
        {
            if (packet.Length > _buffer.Length)
            {
                Console.WriteLine("The packet is bigger than the buffer capacity, skipping the packet");
                return;
            }

            if (_size + packet.Length > _buffer.Length)
            {
                if (_disposed)
                {
                    return;
                }
                Monitor.Wait(_lock);
            }

            var firstPart = _buffer.Length - _tail < packet.Length ? _buffer.Length - _tail : packet.Length;
            Copy(packet, 0, _buffer, _tail, firstPart);

            if (packet.Length - firstPart > 0)
            {
                Copy(packet, _buffer.Length - _tail, _buffer, 0, packet.Length - firstPart);
            }

            _tail = (_tail + packet.Length) % _buffer.Length;
            _size += packet.Length;
            Monitor.PulseAll(_lock);
        }
    }

    public int Read(IntPtr data, int size)
    {
        lock (_lock)
        {
            if (_size == 0)
            {
                if (_disposed)
                {
                    return EndOfFile;
                }
                Monitor.Wait(_lock);
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

    /// <summary>
    /// It is guaranteed that once this method returns all the threads are unblocked and no more threads will be blocked.
    /// </summary>
    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            Monitor.PulseAll(_lock);
        }
    }

    private static void Copy(string source, int sourceIndex, byte[] destination, int destinationIndex, int length)
    {
        for (var i = 0; i < length; i++)
        {
            destination[destinationIndex + i] = (byte)source[sourceIndex + i];
        }
    }
}
