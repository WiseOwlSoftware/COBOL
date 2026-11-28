// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Runtime.Exceptions;

namespace WiseOwl.Cobol.Compiler.Binding.Bound;

/// <summary>The selection tiers of ISO §2023:14.9.49.4 GR3a to GR3g, in GR3's order: a declarative is selected by the
/// FIRST tier that has a match, and source order decides only within a tier. A USE statement and a Format-3 PERFORM's WHEN
/// phrase are selected by the same tiers: §2023:14.9.28.4 GR17, "The rules for determining a match are specified in General
/// rules 3a to 3g of the USE statement". The ordinal IS the priority (a sorts first).</summary>
public enum ExceptionSelectionTier
{
    /// <summary>GR3 a): a bare file-name, any EC-I-O condition associated with the file.</summary>
    FileName,
    /// <summary>GR3 b): an open mode, any EC-I-O condition of a file open, or being opened, in that mode.</summary>
    OpenMode,
    /// <summary>GR3 c): a level-3 exception-name paired with a file-name.</summary>
    FileLevel3,
    /// <summary>GR3 d): a level-2 exception-name paired with a file-name.</summary>
    FileLevel2,
    /// <summary>GR3 e): a level-3 exception-name.</summary>
    Level3,
    /// <summary>GR3 f): a level-2 exception-name.</summary>
    Level2,
    /// <summary>GR3 g): the level-1 exception-name EC-ALL.</summary>
    EcAll,
}

/// <summary>One exception-name selection operand, the shape a Format-3 USE entry (§2023:14.9.49.2,
/// <c>exception-name-1 | exception-name-2 FILE file-name-2</c>) and a Format-3 PERFORM WHEN operand (§2023:14.9.28.2,
/// the same pair plus the bare <c>file-name-1</c> of WHEN EXCEPTION) share: an <paramref name="Ec"/> exception-name in the
/// catalog's own spelling, or null for a bare file-name; the paired or bare <paramref name="File"/>; and its GR3
/// <paramref name="Tier"/>, decided once, here, from the name's catalog level (kb/Work PB2723). Built only through
/// <see cref="ForName"/> and <see cref="ForFile"/>, so a tier never disagrees with its operand.</summary>
public sealed record BoundExceptionSelector
{
    public string? Ec { get; }
    public FileModel? File { get; }
    public ExceptionSelectionTier Tier { get; }

    private BoundExceptionSelector(string? ec, FileModel? file, ExceptionSelectionTier tier) =>
        (Ec, File, Tier) = (ec, file, tier);

    /// <summary>An exception-name the catalog resolved (<paramref name="name"/>), with its paired file when one is written.
    /// The tier is GR3 c) or d) with a file and e), f) or g) without one, by the name's level in the three-level hierarchy of
    /// §2023:14.6.13.1.1, whose one level-1 name is EC-ALL. A file pairs only with an EC-I-O name (§2023:14.9.49.3 SR13,
    /// §2023:14.9.28.3 SR16), which is level 2 or 3, so a level-1 name with a file is a binder defect and fails loudly.</summary>
    public static BoundExceptionSelector ForName(EcInfo name, FileModel? file) => new(name.Name, file, (name.Level, file) switch
    {
        (3, not null) => ExceptionSelectionTier.FileLevel3,
        (2, not null) => ExceptionSelectionTier.FileLevel2,
        (3, null) => ExceptionSelectionTier.Level3,
        (2, null) => ExceptionSelectionTier.Level2,
        (1, null) => ExceptionSelectionTier.EcAll,
        _ => throw new InvalidOperationException(
            $"exception-name '{name.Name}' (level {name.Level}) paired with a file has no §2023:14.9.49.4 GR3 tier"),
    });

    /// <summary>A bare file-name operand (WHEN EXCEPTION file-name-1, GR3 a).</summary>
    public static BoundExceptionSelector ForFile(FileModel file) => new(null, file, ExceptionSelectionTier.FileName);
}
