// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace CobolNet.Binding;

/// <summary>The file organization (ISO/IEC 1989:2023 §12.4.5.10). This slice implements the two sequential shapes;
/// relative/indexed are later G5 slices (registered loud).</summary>
public enum FileOrganization { Sequential, LineSequential, Relative, Indexed }

/// <summary>The access mode (ISO §12.4.5.3). Sequential is the default and the only mode the sequential slice needs.</summary>
public enum FileAccessMode { Sequential, Random, Dynamic }

/// <summary>
/// A bound file connector (COBOLNET_DESIGN §8): the SELECT clause's properties joined with the FD's record
/// description(s). The FD's record area is a typed field — for multiple <c>01</c>s under one FD they SHARE one
/// storage area (ISO §9.1.2 / §13.18 — the file-edge analogue of REDEFINES), modeled by synthesizing each secondary
/// record as a REDEFINES of the first so the existing <see cref="RedefinesClass"/> tier machinery makes them alias
/// (the singular-pattern rule — no second storage-sharing mechanism). A WRITE/READ moves the record's character
/// image across the on-disk edge.
/// </summary>
public sealed class FileModel
{
    /// <summary>The COBOL file-name (the SELECT / FD name). At emission the multi-unit driver QUALIFIES it with
    /// the owning program's path ("PROG::FILE") to namespace the run-unit-global runtime registry — a file
    /// connector is internal to its program (ISO §8.6.3; two IC-suite units both declare PRINT-FILE). Name
    /// resolution is finished by then (bound nodes hold FileModel references), so the rename is emit-only.</summary>
    public required string CobolName { get; set; }

    /// <summary>The ASSIGN target text — a literal's decoded value or a data-name; resolved to a host path at run
    /// time by <c>CobolFile.ResolveHostPath</c>. Defaults to the file-name when no ASSIGN clause is present.</summary>
    public string AssignTarget { get; set; } = "";

    /// <summary>The file organization (default SEQUENTIAL).</summary>
    public FileOrganization Organization { get; set; } = FileOrganization.Sequential;

    /// <summary>The access mode (default SEQUENTIAL).</summary>
    public FileAccessMode AccessMode { get; set; } = FileAccessMode.Sequential;

    /// <summary>True for a SELECT OPTIONAL file.</summary>
    public bool Optional { get; set; }

    /// <summary>The FILE STATUS data-name as written, resolved to <see cref="FileStatusItem"/> post-build; null if none.</summary>
    public string? FileStatusName { get; set; }

    /// <summary>The resolved FILE STATUS data item (set post-build), or null if the file has no FILE STATUS clause.</summary>
    public DataItem? FileStatusItem { get; set; }

    /// <summary>The FD's record description(s), in declaration order. The first is the canonical storage area; every
    /// other shares it (synthesized REDEFINES).</summary>
    public List<DataItem> Records { get; } = [];

    /// <summary>The RECORD KEY data-name as written (ISO §12.4.5.12), resolved post-build to <see cref="RecordKeyItem"/>;
    /// null when absent (the clause is required for ORGANIZATION INDEXED).</summary>
    public string? RecordKeyName { get; set; }

    /// <summary>The resolved prime RECORD KEY item — a data item within the file's record (ISO §12.4.5.12 SR2).</summary>
    public DataItem? RecordKeyItem { get; set; }

    /// <summary>The ALTERNATE RECORD KEY clauses as written, in declaration order: data-name + WITH DUPLICATES
    /// (ISO §12.4.5.6); resolved post-build into <see cref="AlternateKeys"/>.</summary>
    public List<(string Name, bool Duplicates)> AlternateKeyNames { get; } = [];

    /// <summary>The resolved alternate keys, in declaration order (the runtime key index is the list index).</summary>
    public List<(DataItem Item, bool Duplicates)> AlternateKeys { get; } = [];

    /// <summary>The RELATIVE KEY data-name as written (ISO §12.4.5.13), resolved post-build; the item lives OUTSIDE
    /// the file's record (SR3) and holds the 1-based relative record number (GR1).</summary>
    public string? RelativeKeyName { get; set; }

    /// <summary>The resolved RELATIVE KEY item (an unsigned integer item, ISO §12.4.5.13 SR2).</summary>
    public DataItem? RelativeKeyItem { get; set; }

    /// <summary>True once an FD was matched to this SELECT (a SELECT with no FD is an error the front-end already
    /// diagnoses; here it simply has no records and is never opened with data).</summary>
    public bool HasFd { get; set; }

    /// <summary>True when this file is described by an SD (sort-merge file description, ISO §13.4.6): it has no
    /// host storage — only SORT/MERGE/RELEASE/RETURN may reference it (SR3/SR4); its runtime store is the
    /// in-memory <c>CobolSort</c> buffer.</summary>
    public bool IsSortMerge { get; set; }

    /// <summary>The RECORD clause's variable-length model (ISO §13.18.43 — RECORD IS VARYING / RECORD CONTAINS m
    /// TO n), or null when the records are fixed-length. All the record verbs consume it: WRITE/REWRITE/RELEASE
    /// take the record length from the DEPENDING item (GR13a) or the record's own size (GR13b/c) and fail with
    /// I-O status '44' outside [min,max] (GR14); READ/RETURN restore the just-read record's length (GR15).</summary>
    public VaryingRecordInfo? Varying { get; set; }

    /// <summary>The resolved RECORD VARYING … DEPENDING ON data item (set post-build, like
    /// <see cref="FileStatusItem"/>); null when fixed-length or no DEPENDING phrase.</summary>
    public DataItem? VaryingDependingItem { get; set; }

    /// <summary>The variable-length minimum record size (ISO §13.18.43 GR9 — an unstated minimum defaults to the
    /// smallest record described for the file, where an occurs-depending table contributes its MINIMUM
    /// occurrences per GR8a); −1 for fixed-length records.</summary>
    public int VaryMin => Varying is { } v ? v.Min ?? (Records.Count == 0 ? 1 : Records.Min(MinRecordSize)) : -1;

    /// <summary>The variable-length maximum record size (ISO §13.18.43 GR10 — an unstated maximum defaults to the
    /// largest record described for the file; an ODO table allocates its maximum, GR8b); −1 for fixed-length
    /// records.</summary>
    public int VaryMax => Varying is { } v ? v.Max ?? Math.Max(1, RecordWidth) : -1;

    /// <summary>The minimum byte size of one record description (ISO §13.18.43 GR8a): the sum over non-redefining
    /// content with every occurs-depending table at its MINIMUM occurrence count (a bare <c>RECORD IS VARYING</c>
    /// over an ODO record — RL211A — has minimum 120, not the 140 max allocation <see cref="DataItem.ImageWidth"/>
    /// reports).</summary>
    private static int MinRecordSize(DataItem item) =>
        item.IsElementary ? item.ImageWidth
        : item.Children.Where(c => c.RedefinesTargetName is null)
            .Sum(c => MinRecordSize(c) * (c.OccursSpec is { DependingName: not null } od ? od.Min : c.Occurs ?? 1));

    /// <summary>The record area's character-image width (the max over the FD's records).</summary>
    public int RecordWidth => Records.Count == 0 ? 0 : Records.Max(r => r.ImageWidth);

    /// <summary>The record description whose view spans the WHOLE record area — the largest one (ISO §13.4.2: the
    /// record area's size is that of the largest record description). Reading a record makes it available in the
    /// whole area, so every area-wide store/read (sequential and keyed READ, sort RETURN) must go through THIS
    /// record's view — a shorter <c>Records[0]</c> window would truncate the splice (ST111A's 50/75/100 FD,
    /// RL106A's 56/102 pair). Null when the FD has no record description.</summary>
    public DataItem? AreaRecord => Records.Count == 0 ? null : Records.MaxBy(r => r.ImageWidth);

    /// <summary>True for either sequential shape (the only organizations this slice can OPEN/READ/WRITE).</summary>
    public bool IsSequential => Organization is FileOrganization.Sequential or FileOrganization.LineSequential;
}

/// <summary>The variable-length record model of a RECORD clause (ISO §13.18.43): the declared minimum/maximum
/// record sizes (null when unstated) and the <c>VARYING … DEPENDING ON</c> data-name (null when none — a
/// <c>RECORD CONTAINS m TO n</c> file varies without a length register).</summary>
public sealed record VaryingRecordInfo(int? Min, int? Max, string? DependingName);
