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
    /// <summary>The COBOL file-name (the SELECT / FD name; the runtime registry key).</summary>
    public required string CobolName { get; init; }

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

    /// <summary>True once an FD was matched to this SELECT (a SELECT with no FD is an error the front-end already
    /// diagnoses; here it simply has no records and is never opened with data).</summary>
    public bool HasFd { get; set; }

    /// <summary>The record area's character-image width (the max over the FD's records).</summary>
    public int RecordWidth => Records.Count == 0 ? 0 : Records.Max(r => r.ImageWidth);

    /// <summary>True for either sequential shape (the only organizations this slice can OPEN/READ/WRITE).</summary>
    public bool IsSequential => Organization is FileOrganization.Sequential or FileOrganization.LineSequential;
}
