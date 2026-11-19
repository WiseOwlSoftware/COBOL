// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Frontend.Common;

/// <summary>
/// ⛔ THE ONE DECIDER OF HOW A COMPILATION'S DEBUG INFORMATION NAMES A SOURCE FILE (DESIGN-listing.md section 6.9). A backend names each
/// COBOL file its PDB holds sequence points in relative to the document root, the main source's directory, with '/' separators
/// (<see cref="NameOf"/>), so identical source compiles to an identical assembly in any directory (the PE names its PDB by a hash of the
/// PDB's content). Every reader of a document name — the listing's object-code map, a backend's diagnostic placed on its listing line,
/// a test — resolves it back to the file's full path with <see cref="PathOf"/>, against the root <see cref="RootOf"/> gives. One function
/// each way, so the writer and its readers cannot disagree.
/// </summary>
public static class SourceDocuments
{
    /// <summary>The document root of the compilation whose main source is <paramref name="sourcePath"/>: that file's directory, as a full
    /// path.</summary>
    public static string RootOf(string sourcePath) => Path.GetDirectoryName(Path.GetFullPath(sourcePath))!;

    /// <summary>The name a PDB gives <paramref name="file"/>: its path relative to <paramref name="root"/> with '/' separators, or its full
    /// path when there is no root. A line directive's file name has no escapes, so a path holding a quotation mark or a line end cannot
    /// be written; such a file is named with each of them replaced by an underscore, keeping its sequence points at a name no file has
    /// (and <see cref="PathOf"/> of it names no file the compilation read).</summary>
    public static string NameOf(string file, string? root)
    {
        string full = Path.GetFullPath(file);
        string name = root is null ? full : Path.GetRelativePath(Path.GetFullPath(root), full).Replace(Path.DirectorySeparatorChar, '/');
        return name.AsSpan().IndexOfAny("\"\r\n") < 0 ? name : name.Replace('"', '_').Replace('\r', '_').Replace('\n', '_');
    }

    /// <summary>The full path of the file a PDB names <paramref name="name"/> (<see cref="NameOf"/>): resolved against
    /// <paramref name="root"/>, or taken as it stands when there is no root (a full path already).</summary>
    public static string PathOf(string name, string? root) => root is null ? Path.GetFullPath(name) : Path.GetFullPath(name, root);
}
