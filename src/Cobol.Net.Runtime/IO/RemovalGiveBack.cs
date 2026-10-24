// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Runtime.IO;

/// <summary>
/// ⛔ WHAT A DELETE FILE UNDER COMMIT CONTROL GAVE BACK TO REMOVE THE FILE, PUT BACK UNLESS THE REMOVAL LANDS
/// (<c>FileConnector.GiveBackForRemoval</c>; DESIGN-runtime-library.md 2.2.2.14; kb/Work PB3492, review finding R1-1). The
/// connector's kept handle goes first, for the host's probe of GR15's conflict; its Table 19 file lock goes once the capture
/// is about to run (<see cref="SuspendFileLock"/>), because a column the host publishes through a handle on the file refuses
/// the removal. Disposed before <see cref="Removed"/> — the DELETE FILE was unsuccessful anywhere before the file was removed —
/// both are taken back, so the file, and the run unit's transaction on it, stay as the CLOSE under commit control left them;
/// <see cref="Removed"/> makes the give-back final. One instance per attempt; settled exactly once.
/// </summary>
internal abstract class RemovalGiveBack : IDisposable
{
    private bool _settled;

    /// <summary>Give the kept Table 19 file lock back too, for the removal's instant (<c>RunUnitFileLock.Suspend</c>).</summary>
    internal abstract void SuspendFileLock();

    /// <summary>The removal did not land: the handle and the file lock taken back as the CLOSE kept them.</summary>
    private protected abstract void TakeBack();

    /// <summary>The removal landed: what was given back is gone for good.</summary>
    private protected abstract void Landed();

    /// <summary>The file was removed: the give-back is final, and <see cref="Dispose"/> puts nothing back.</summary>
    internal void Removed()
    {
        if (_settled) return;
        _settled = true;
        Landed();
    }

    /// <summary>Put back what was given back, unless <see cref="Removed"/> made it final.</summary>
    public void Dispose()
    {
        if (_settled) return;
        _settled = true;
        TakeBack();
    }
}
