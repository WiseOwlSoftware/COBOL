// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// The files ONE bound unit declares (ISO §12.4.5 SELECT, §13.4.5 FD, §13.4.6 SD; docs/rearchitecture/DESIGN-architecture-review.md
/// §8.3 (1); kb/Work PB2296 M6): a value slice of <see cref="DataBindState"/> whose writers are the two halves that declare a
/// file, the FILE-CONTROL binder (the SELECT entry) and the FILE SECTION walk (an FD or SD that names a file no SELECT
/// specified, §13.4.5.3 SR1 / §13.4.6.3 SR1). A container's GLOBAL files are NOT here: they are
/// <see cref="InheritedGlobals.Files"/>, and <see cref="DataBindState.VisibleFiles"/> reads both, this unit's first (§8.4.6.2.1 3).
/// </summary>
public sealed class FileSlice
{
    private readonly List<FileModel> _files = [];
    private readonly Dictionary<string, FileModel> _filesByName = new(CobolNames.Comparer);

    /// <summary>All SELECTed files (the SELECT clause joined with its FD records), in source order. The bind-phase
    /// file-connector qualification mutates the <see cref="FileModel"/> ELEMENTS, which a read-only list does not prevent —
    /// element immutability is a later data-model-track item.</summary>
    public IReadOnlyList<FileModel> Declared => _files;

    /// <summary>The files keyed by COBOL file-name (case-insensitive), for the binder to resolve OPEN/READ/CLOSE targets and
    /// to map a WRITE/REWRITE record-name back to its owning file. THIS unit's own files only: a lookup that must see a
    /// container's GLOBAL file reads <see cref="DataBindState.VisibleFiles"/>.</summary>
    public IReadOnlyDictionary<string, FileModel> FilesByName => _filesByName;

    /// <summary>Declare <paramref name="file"/> under <paramref name="name"/>: a SELECT entry's, or the model an FD or SD that no
    /// SELECT specified is bound to so that its records still resolve. The later declaration of a spelling replaces the earlier
    /// in <see cref="FilesByName"/>, as the SELECT binder's duplicate screen has already refused it.</summary>
    internal void Declare(string name, FileModel file)
    {
        _files.Add(file);
        _filesByName[name] = file;
    }
}
