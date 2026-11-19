// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>
/// The files ONE bound unit declares (ISO §2023:12.4.5 SELECT, §2023:13.4.5 FD, §2023:13.4.6 SD;
/// docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1); kb/Work PB2296 M6): a value slice of <see cref="DataBindState"/> whose writers are the two halves that declare a
/// file, the FILE-CONTROL binder (the SELECT entry) and the FILE SECTION walk (an FD or SD that names a file no SELECT
/// specified, §2023:13.4.5.3 SR1 / §2023:13.4.6.3 SR1). A container's GLOBAL files are NOT here: they are
/// inherited through <c>SymbolTable.InheritFiles</c>; the file-NAMES of both are <c>SymbolTable</c>'s (its file lookup reads this
/// unit's first, §2023:8.4.6.2.1 3)).
/// </summary>
public sealed class FileSlice
{
    private readonly List<FileModel> _files = [];

    /// <summary>All SELECTed files (the SELECT clause joined with its FD records), in source order. The bind-phase
    /// file-connector qualification mutates the <see cref="FileModel"/> ELEMENTS, which a read-only list does not prevent —
    /// element immutability is a later data-model-track item.</summary>
    public IReadOnlyList<FileModel> Declared => _files;

    /// <summary>Appends a file in the order declared: a SELECT entry's, or the model an FD or SD that no SELECT specified is
    /// bound to so that its records still resolve. Its file-name joins the name table through <c>SymbolTable.Declare</c>, the
    /// one caller.</summary>
    internal void Declare(FileModel file) => _files.Add(file);
}
