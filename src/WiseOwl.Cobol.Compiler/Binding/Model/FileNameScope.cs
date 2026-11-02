// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>The file-names visible in one source element (<c>DataBinder.VisibleFiles</c>): its own files
/// (<c>DataBinder.FilesByName</c>) over its containers' GLOBAL files (<c>InheritedGlobals.Files</c>, nearest
/// container first), an own file-name hiding a container's of the same spelling (§8.4.6.2.1 3) a)). The ONE lookup every
/// reader of a file-name a statement or clause writes asks, in the data phases and the procedure division alike.</summary>
public readonly struct FileNameScope(IReadOnlyDictionary<string, FileModel> own, IReadOnlyDictionary<string, FileModel> inherited)
{
    private readonly IReadOnlyDictionary<string, FileModel> _own = own;
    private readonly IReadOnlyDictionary<string, FileModel> _inherited = inherited;

    /// <summary>The file <paramref name="name"/> names here: the element's own, else the nearest container's GLOBAL one.</summary>
    public bool TryGetValue(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out FileModel? file) =>
        _own.TryGetValue(name, out file) || _inherited.TryGetValue(name, out file);

    /// <summary>True when <paramref name="name"/> is a file-name visible here.</summary>
    public bool ContainsKey(string name) => _own.ContainsKey(name) || _inherited.ContainsKey(name);

    /// <summary>Every visible file: the element's own in declaration order, then each container GLOBAL file no own
    /// file-name hides, nearest container first.</summary>
    public IEnumerable<FileModel> Values => Visible(_own, _inherited);

    private static IEnumerable<FileModel> Visible(IReadOnlyDictionary<string, FileModel> own,
        IReadOnlyDictionary<string, FileModel> inherited)
    {
        foreach (var file in own.Values) yield return file;
        foreach (var (name, file) in inherited)
            if (!own.ContainsKey(name))
                yield return file;
    }
}
