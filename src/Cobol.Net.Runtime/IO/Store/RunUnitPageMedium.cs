// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Microsoft.Win32.SafeHandles;

namespace CobolNet.Runtime.IO.Store;

/// <summary>⛔ ONE RUN UNIT'S PAGE I/O ON ONE PHYSICAL FILE (docs/rearchitecture/DESIGN-runtime-library.md §2.2.1.7,
/// "Within a run unit"; kb/Work PB3333): the handles of every connector of the run unit attached to the file's store, each
/// the connector's own §9.1.15 file lock (kb/Work PB771 — no second handle is ever taken), and the page I/O of the store
/// goes through one of them positionally (<see cref="HandlePageMedium"/>). A WRITABLE handle is used whenever one is
/// attached, so a store whose connectors are a reader and a writer writes through the writer's; every handle of a run
/// unit names the same physical file, so which one carries a read changes nothing a page can show.
/// <para>A connector attaches its handle when its OPEN attaches it to the store and detaches it when its CLOSE (or a
/// failed OPEN) detaches it, so the medium never uses a handle its connector has given back.</para></summary>
internal sealed class RunUnitPageMedium : IPageMedium
{
    private readonly List<(SafeFileHandle Handle, bool Writable)> _handles = [];
    private HandlePageMedium? _through;

    /// <summary>Whether a writable handle is attached.</summary>
    public bool HasWritable => _handles.Any(h => h.Writable);

    /// <summary>Whether any handle is attached.</summary>
    public bool HasHandle => _handles.Count > 0;

    /// <summary>Whether <paramref name="handle"/> is attached.</summary>
    public bool Holds(SafeFileHandle handle) => _handles.Exists(h => ReferenceEquals(h.Handle, handle));

    /// <summary>Attach a connector's handle.</summary>
    public void Attach(SafeFileHandle handle, bool writable)
    {
        _handles.Add((handle, writable));
        Choose();
    }

    /// <summary>Detach a connector's handle (a handle never attached is ignored).</summary>
    public void Detach(SafeFileHandle handle)
    {
        int at = _handles.FindIndex(h => ReferenceEquals(h.Handle, handle));
        if (at < 0) return;
        _handles.RemoveAt(at);
        Choose();
    }

    private void Choose()
    {
        var chosen = _handles.FindIndex(h => h.Writable) is var w and >= 0 ? _handles[w] : _handles.FirstOrDefault();
        _through = chosen.Handle is { } handle ? new HandlePageMedium(handle) : null;
    }

    private HandlePageMedium Through =>
        _through ?? throw new InvalidOperationException("no connector of the run unit holds the physical file open");

    /// <inheritdoc/>
    public long Length => Through.Length;

    /// <inheritdoc/>
    public void Read(uint page, Span<byte> destination) => Through.Read(page, destination);

    /// <inheritdoc/>
    public void Write(uint page, ReadOnlySpan<byte> source) => Through.Write(page, source);

    /// <inheritdoc/>
    public void Sync() => Through.Sync();

    /// <inheritdoc/>
    public void SetLength(long bytes) => Through.SetLength(bytes);
}
