// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.Messaging;

/// <summary>
/// ⛔ A MESSAGE SERVER'S LOG, WRITTEN BY SEVERAL WRITERS (docs/rearchitecture/DESIGN-message-control.md §5.4.5; kb/Work PB4745): <c>FileMode.Append</c> only seeks to the end at open and then
/// writes at the stream's own tracked offset (neither <c>FILE_APPEND_DATA</c> on Windows nor <c>O_APPEND</c> on Unix), so two streams opened on one log overwrite each other. This stream
/// is write-only and positions at the end of the file before EVERY write, under one lock, and a process opens ONE of them per log and shares it between its writers (the requestor's
/// drains of the started command's two pipes); across processes the position-then-write pair is what remains, which keeps each write whole except for a write that begins in the
/// instant another process's write is between its own seek and its end.
/// </summary>
internal sealed class ServerLog : Stream
{
    private readonly FileStream _file;
    private readonly object _gate = new();

    private ServerLog(FileStream file) => _file = file;

    /// <summary>Opens (creating) the log at <paramref name="path"/>, shared with its other writers and readers. It is no COBOL file connector's handle (a message server's log is outside
    /// the file facility, §2023:9.1), so <c>HostFile</c> is not its opener.</summary>
    internal static ServerLog Open(string path) =>
        new(new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete));

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        lock (_gate)
        {
            _file.Seek(0, SeekOrigin.End);
            _file.Write(buffer);
            _file.Flush();
        }
    }

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        Write(buffer.Span);
        return ValueTask.CompletedTask;
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) _file.Dispose();
        base.Dispose(disposing);
    }
}
