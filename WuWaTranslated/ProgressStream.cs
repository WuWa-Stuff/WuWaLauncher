using System.IO;

namespace WuWaTranslated;

public delegate void ProgressReportDelegate(long current, long? total);

public class ProgressStream : Stream
{
    private readonly Stream _destStream;
    private readonly ProgressReportDelegate _progress;
    private long? _totalLength;
    private long _current = 0;

    public override bool CanTimeout => _destStream.CanTimeout;
    public override bool CanRead => _destStream.CanRead;
    public override bool CanSeek => _destStream.CanSeek;
    public override bool CanWrite => _destStream.CanWrite;
    public override long Length => _destStream.Length;
    public override long Position
    {
        get => _destStream.Position;
        set => _destStream.Position = value;
    }

    public ProgressStream(Stream destinationStream, ProgressReportDelegate progressCallback)
    {
        _destStream = destinationStream;
        _progress = progressCallback;
    }
    
    private void ReportProgress(long count)
    {
        _current += count;
        _progress.Invoke(_current, _totalLength);
    }

    public override void Flush()
        => _destStream.Flush();

    public override int Read(byte[] buffer, int offset, int count)
        => _destStream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin)
        => _destStream.Seek(offset, origin);

    public override void SetLength(long value)
    {
        _totalLength = value;
        _destStream.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ReportProgress(count);
        _destStream.Write(buffer, offset, count);
    }

    protected override void Dispose(bool disposing)
    {
        _destStream.Dispose();
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        await _destStream.DisposeAsync();
        await base.DisposeAsync();
    }
}