// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Generated;

namespace CobolNet.Binding;

using Core = CobolParserCore;

/// <summary>
/// Resolves a <c>dataReference</c> parse node to a <see cref="Place"/> — the single entry point every verb uses to
/// turn a COBOL operand into a typed C# lvalue (COBOLNET_DESIGN §3.4). Two phases:
/// <list type="number">
///   <item><b>Syntactic flatten</b> — walk <c>cobolWord dataReferenceSuffix*</c> into the base name, its OF/IN
///         qualifiers, and whether a subscript or reference-modification is present.</item>
///   <item><b>Semantic resolve</b> — resolve the (optionally qualified) name to a <see cref="DataItem"/> by
///         right-to-left narrowing, then build the member-access path.</item>
/// </list>
/// Returns <see langword="null"/> when the reference cannot be resolved in this slice — an unknown name, a special
/// register, or (until G2-1b) a subscripted / reference-modified reference — so the caller emits a loud
/// not-implemented guard rather than silently mis-binding.
/// </summary>
public sealed class ReferenceResolver(DataBinder data)
{
    /// <summary>Resolve <paramref name="dref"/> to a <see cref="Place"/>, or <see langword="null"/> if unsupported here.</summary>
    public Place? Resolve(Core.DataReferenceContext dref)
    {
        // Special registers (LINAGE-/LINE-/PAGE-COUNTER) have no cobolWord base — not handled in this slice.
        if (dref.cobolWord() is not { } baseWord) return null;
        string name = baseWord.GetText();

        var qualifiers = new List<string>();
        bool hasSubscriptOrRefMod = false;
        foreach (var suffix in dref.dataReferenceSuffix())
        {
            if (suffix.qualification() is { } q)
            {
                qualifiers.Add(q.cobolWord().GetText());
                if (q.subscriptPart().Length > 0 || q.refModPart().Length > 0) hasSubscriptOrRefMod = true;
            }
            else if (suffix.subscriptPart() is not null || suffix.refModPart() is not null)
            {
                hasSubscriptOrRefMod = true;
            }
        }
        // OCCURS subscripts + reference modification are G2-1b (the ported SUB_* interpreter).
        if (hasSubscriptOrRefMod) return null;

        DataItem? item = qualifiers.Count > 0 ? ResolveQualified(name, qualifiers) : ResolveUnqualified(name);
        return item is null ? null : new MemberPlace(AccessPath(item), item);
    }

    /// <summary>The item an unqualified name resolves to (first match; COBOL requires qualification to disambiguate).</summary>
    private DataItem? ResolveUnqualified(string name) =>
        data.ByName.TryGetValue(name, out var list) && list.Count > 0 ? list[0] : null;

    /// <summary>
    /// Resolve a qualified reference <c>name OF q[0] OF q[1] …</c> by right-to-left narrowing: resolve the
    /// outermost qualifier, walk inward through each qualifier, then find <paramref name="name"/> within the
    /// innermost qualifier's subtree (ISO §8.4.3.3 qualification).
    /// </summary>
    private DataItem? ResolveQualified(string name, List<string> qualifiers)
    {
        DataItem? scope = ResolveUnqualified(qualifiers[^1]);            // outermost qualifier
        for (int k = qualifiers.Count - 2; k >= 0 && scope is not null; k--)
            scope = FindDescendant(scope, qualifiers[k]);
        return scope is null ? null : FindDescendant(scope, name);
    }

    /// <summary>Find a descendant (direct or nested) of <paramref name="scope"/> with COBOL name <paramref name="name"/>.</summary>
    private static DataItem? FindDescendant(DataItem scope, string name)
    {
        foreach (var child in scope.Children)
        {
            if (string.Equals(child.CobolName, name, StringComparison.OrdinalIgnoreCase)) return child;
            if (FindDescendant(child, name) is { } found) return found;
        }
        return null;
    }

    /// <summary>The C# member-access path for an item: a static field at the root, else <c>Parent.Child</c> chained.</summary>
    private static string AccessPath(DataItem item) =>
        item.Parent is null ? item.CsName : AccessPath(item.Parent) + "." + item.CsName;
}
