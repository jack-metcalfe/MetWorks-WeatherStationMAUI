namespace Utility;
public class ReadOnlyStream : Stream
{
    private readonly Stream _inner;

    public ReadOnlyStream(Stream inner) => _inner = inner;
    public StreamReader ToStreamReader(Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return new StreamReader(this, encoding);
    }
    public override bool CanRead => _inner.CanRead;
    public override bool CanWrite => false;
    public override bool CanSeek => _inner.CanSeek;

    public override long Length => _inner.Length;
    public override long Position { get => _inner.Position; set => _inner.Position = value; }

    public override void Flush() => throw new NotSupportedException();
    public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
    public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

