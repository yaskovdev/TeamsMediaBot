namespace Demuxer;

using System.Runtime.InteropServices;

public class CircularBuffer
{
    private readonly byte[] _buffer;
    private int _head;
    private int _tail;

    public int Size { get; private set; }

    public CircularBuffer(int capacity)
    {
        _buffer = new byte[capacity];
    }

    public void Write(IntPtr data, int size)
    {
        if (Size + size > _buffer.Length)
        {
            throw new NotImplementedException("Increase buffer size");
        }

        var firstPart = _buffer.Length - _tail < size ? _buffer.Length - _tail : size;
        Marshal.Copy(data, _buffer, _tail, firstPart);

        if (size - firstPart > 0)
        {
            Marshal.Copy(data + (_buffer.Length - _tail), _buffer, 0, size - firstPart);
        }

        _tail = (_tail + size) % _buffer.Length;
        Size += size;
    }

    public void Read(IntPtr data, int size)
    {
        if (Size == 0)
        {
            return;
        }

        var numberOfBytesToCopy = Math.Min(Size, size);
        var firstPart = _buffer.Length - _head < numberOfBytesToCopy ? _buffer.Length - _head : numberOfBytesToCopy;
        Marshal.Copy(_buffer, _head, data, firstPart);
        if (numberOfBytesToCopy - firstPart > 0)
        {
            Marshal.Copy(_buffer, 0, data + _buffer.Length - _head, numberOfBytesToCopy - firstPart);
        }

        _head = (_head + numberOfBytesToCopy) % _buffer.Length;
        Size -= numberOfBytesToCopy;
    }
}
