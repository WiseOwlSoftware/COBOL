// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>Which type-name a data description entry carries as written text, for <see cref="SymbolTable.TypeDeclarationOf"/>: the
/// entry holds the word, and the table reads it in the scope of the element whose entry writes it.</summary>
internal enum TypeNameCarried
{
    /// <summary>The TYPE clause as written (<see cref="DataItem.WrittenTypeName"/>, §2023:13.18.57).</summary>
    TypeClause,
    /// <summary>The type the entry is typed by once its TYPE clause is expanded, which a copy of the entry keeps
    /// (<see cref="DataItem.TypeName"/>).</summary>
    Typed,
    /// <summary>A restricted pointer's <c>USAGE POINTER TO type-name</c> (<see cref="PicInfo.RestrictedTypeName"/>,
    /// §2023:13.18.60.3 SR11).</summary>
    RestrictedPointer,
}

public sealed partial class SymbolTable
{
    /// <summary>This element's own type declarations by type-name (§2023:13.18.58), the first of a spelling kept (a second is the
    /// duplicate <see cref="DeclareType"/> refuses). The declarations themselves, in source order, are the forest's
    /// (<see cref="ForestSlice.TypeDeclarations"/>).</summary>
    private readonly Dictionary<CobolName, DataItem> _ownTypeNames = [];

    // A METHOD's own type declarations are its MethodNames.TypeNames (SymbolTable.MethodNames.cs).

    /// <summary>The GLOBAL type declarations of every containing program, the nearest container's declaration of a name kept
    /// (§2023:13.18.58.4 GR3; §2023:8.4.6.2.2; kb/Work PB1303). Kept apart from <see cref="_ownTypeNames"/>: an own declaration
    /// must neither collide with an inherited one (§2023:8.4.6.2.1 3) a)) nor be walked, emitted or re-validated as this unit's.</summary>
    private readonly Dictionary<CobolName, DataItem> _inheritedTypeNames = [];

    /// <summary>The members of every type declaration of this element a TYPE clause references, by name: §2023:8.4.2.2.1 rule 4's
    /// "other definitions" (<see cref="WithLapsedTypeMembers"/>). Built once, when <see cref="Passes.ExpandTypesPass.Run"/> has
    /// completed every declaration (<see cref="IndexLapsedTypeMembers"/>); null until then, when they are walked live (a SAME AS
    /// operand resolved while the declarations are being completed, whose copies are still arriving).</summary>
    private Dictionary<CobolName, List<DataItem>>? _lapsedTypeMembers;

    /// <summary>Declares a type declaration in the table of the source element <paramref name="scope"/> names (a method's
    /// type-name is local to the method, §2023:8.4.6.2.2; kb/Work PB3192), and the declaration joins that element's list in
    /// source order. False when the spelling is already declared there (§2023:13.18.58: a type-name shall be unique).</summary>
    internal bool DeclareType(DataItem template, string name, Scope scope)
    {
        var table = scope.Method is { } method ? MethodNamesOf(method).TypeNames : _ownTypeNames;
        if (!table.TryAdd(Key(name), template)) return false;
        if (scope.Method is { } m) m.AddTypeDeclaration(template);
        else _data.State.Forest.AddTypeDeclaration(template);
        return true;
    }

    /// <summary>The container's own GLOBAL type declarations join this element's inherited ones (§2023:13.18.58.4 GR3), called for
    /// each container nearest first, so the nearest container's declaration of a name is kept.</summary>
    internal void InheritTypeDeclarations(SymbolTable container)
    {
        foreach (var (name, declaration) in container._ownTypeNames)
            if (declaration.HasGlobalClause)
                _inheritedTypeNames.TryAdd(name, declaration);
    }

    /// <summary>The type declaration the type-name <paramref name="carrier"/> holds as text names (<paramref name="which"/>), in the
    /// scope of the element whose entry writes it, or null when it names none (§2023:13.18.57.3 SR1: type-name-1 "shall be the
    /// name of a type declaration"). A name a SYMBOL carries is resolved by the table from the symbol
    /// (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, slice 6").</summary>
    internal DataItem? TypeDeclarationOf(DataItem carrier, TypeNameCarried which) =>
        (which switch
        {
            TypeNameCarried.TypeClause => carrier.WrittenTypeName,
            TypeNameCarried.Typed => carrier.TypeName,
            TypeNameCarried.RestrictedPointer => carrier.Pic?.RestrictedTypeName,
            _ => throw new ArgumentOutOfRangeException(nameof(which), which, "a type-name an entry carries"),
        }) is { } name
            ? TypeDeclarationNamed(Key(name), _data.ScopeOf(carrier.Root))
            : null;

    /// <summary>Whether <paramref name="template"/> is the type declaration its own element's table holds under its name (a
    /// declaration refused as a duplicate is not, and is never completed).</summary>
    internal bool IsDeclaredTypeDeclaration(DataItem template)
    {
        var method = _data.ScopeOf(template).Method;
        var table = method is null ? _ownTypeNames : _methodNames.GetValueOrDefault(method)?.TypeNames;
        return table is not null && table.TryGetValue(Key(template.CobolName!), out var own) && ReferenceEquals(own, template);
    }

    /// <summary>⛔ THE ONE TYPE-NAME LOOKUP, in the source element <paramref name="scope"/> names: a METHOD's own declaration first
    /// (kb/Work PB3192), and none of its object's when the method declares the word as anything else, since "the declaration in
    /// the containing object definition is inaccessible to this method" (§2023:11.7.4 GR5); then this element's own, else the
    /// nearest containing program's GLOBAL one.</summary>
    private DataItem? TypeDeclarationNamed(CobolName key, Scope scope)
    {
        if (scope.Method is { } m)
        {
            if (_methodNames.TryGetValue(m, out var own) && own.TypeNames.TryGetValue(key, out var declaration)) return declaration;
            if (_data.MethodDeclares(m, key.Word)) return null;
        }
        return _ownTypeNames.GetValueOrDefault(key) ?? _inheritedTypeNames.GetValueOrDefault(key);
    }

    /// <summary>The type declarations visible in <paramref name="scope"/>, in <see cref="TypeDeclarationNamed"/>'s order: a method's
    /// own, this element's, a container's GLOBAL one.</summary>
    private IEnumerable<DataItem> VisibleTypeDeclarations(Scope scope)
    {
        IEnumerable<DataItem> visible = _ownTypeNames.Values.Concat(_inheritedTypeNames.Values);
        return scope.Method is { } m && _methodNames.TryGetValue(m, out var own) ? own.TypeNames.Values.Concat(visible) : visible;
    }

    /// <summary>The type declarations of <paramref name="method"/>'s element (this element's when null), in declaration order.</summary>
    private IEnumerable<DataItem> TypeDeclarationsOf(OoMethodDataScope? method) =>
        method is null ? _ownTypeNames.Values : _methodNames.GetValueOrDefault(method)?.TypeNames.Values ?? Enumerable.Empty<DataItem>();

    /// <summary>Builds the index of the lapsed type-declaration members (<see cref="_lapsedTypeMembers"/>), once, when
    /// <see cref="Passes.ExpandTypesPass.Run"/> has completed every declaration of this element.</summary>
    internal void IndexLapsedTypeMembers()
    {
        if (_lapsedTypeMembers is not null)
            throw new InvalidOperationException("the lapsed type members are indexed once, by ExpandTypesPass (kb/Work PB2296)");
        var index = new Dictionary<CobolName, List<DataItem>>();
        foreach (var declaration in _ownTypeNames.Values.Where(d => d.ReferencedByTypeClause))
            foreach (var member in DataItem.DescendantsOf(declaration))
                if (member.CobolName is { } n)
                {
                    if (!index.TryGetValue(Key(n), out var list)) index[Key(n)] = list = [];
                    list.Add(member);
                }
        _lapsedTypeMembers = index;
    }

    /// <summary>⛔ ISO §2023:8.4.2.2.1 RULE 4, IN THE ONE PLACE EVERY DATA-NAME LOOKUP READS (kb/Work PB1476). "Qualification of a
    /// user-defined name is required unless … 4) Any other definition of the name is subordinate to a type declaration entry for
    /// which the type-name is not referenced in any TYPE clause in the source unit." A type declaration's members are off the
    /// name index (§2023:13.18.58.4 GR1), which IS the exemption while the type is unreferenced; once any TYPE clause names it
    /// (<see cref="DataItem.ReferencedByTypeClause"/>) its members are "other definitions" again, so they join
    /// <paramref name="storage"/> (the name's storage definitions, never empty) as candidates that COUNT for the uniqueness
    /// verdict and never resolve alone (<c>DataBinder.QualifiedCandidates</c>). A member a candidate is a copy of
    /// (<see cref="DataItem.ClonedFrom"/>, to its written end) is that candidate's own definition, not another one: <c>01 Z TYPE
    /// T.</c> makes a reference to T's member through Z's one copy unique, as §2023:13.18.58.4 GR1 says ("If there is more than
    /// one such group, qualification with the name of the group is necessary"). Returns <paramref name="storage"/> itself when
    /// nothing lapses, so the common lookup allocates nothing. <paramref name="method"/> is the METHOD whose tier found
    /// <paramref name="storage"/>, whose own type declarations are the other definitions there, or null for this element's
    /// (kb/Work PB3192: a method's type declarations are its own source element's).</summary>
    private List<DataItem> WithLapsedTypeMembers(CobolName name, List<DataItem> storage, OoMethodDataScope? method)
    {
        List<DataItem>? merged = null;
        foreach (var member in LapsedTypeMembers(name, method))
            if (!storage.Exists(s => IsCopyOf(s, member)))
                (merged ??= [.. storage]).Add(member);
        return merged ?? storage;
    }

    /// <summary>The INDEX-NAME arm of <see cref="WithLapsedTypeMembers"/>: an INDEXED BY name written on a member of a referenced
    /// type declaration is "another definition of the name" (§2023:8.4.2.2.1 rule 4) exactly as the member's data-name is, so it
    /// COUNTS against <paramref name="declared"/> (the reference's qualified index survivors) when <paramref name="qualifies"/>
    /// reaches it, and only while at least one declared survivor exists (such a member declares no index cell to resolve to). A
    /// member a survivor's table is a copy of is that survivor's own definition (kb/Work PB1476).</summary>
    private NameCandidates<IndexDeclaration> WithLapsedTypeIndexes(CobolName name, NameCandidates<IndexDeclaration> declared,
                                                                   Func<IndexDeclaration, bool> qualifies, OoMethodDataScope? method)
    {
        if (declared.Count == 0) return declared;
        List<IndexDeclaration> lapsed = [];
        foreach (var declaration in TypeDeclarationsOf(method))
        {
            if (!declaration.ReferencedByTypeClause) continue;
            foreach (var table in DataItem.DescendantsOf(declaration))
                if (table.IndexNames.Exists(n => Key(n) == name)
                    && !declared.Any(d => IsCopyOf(d.Table, table))
                    && new IndexDeclaration(name.Word, table, "") is var member && qualifies(member))
                    lapsed.Add(member);
        }
        return declared.Including(lapsed);
    }

    private static bool IsCopyOf(DataItem copy, DataItem definition)
    {
        for (var from = copy.ClonedFrom; from is not null; from = from.ClonedFrom)
            if (ReferenceEquals(from, definition)) return true;
        return false;
    }

    /// <summary>The members of every type declaration a TYPE clause references, by name (<see cref="WithLapsedTypeMembers"/>): the
    /// index once it is built, else (a SAME AS operand resolved while the declarations are being completed) walked live. A
    /// METHOD's declarations (<paramref name="method"/>, kb/Work PB3192) are few and always walked live.</summary>
    private IEnumerable<DataItem> LapsedTypeMembers(CobolName name, OoMethodDataScope? method)
    {
        if (method is null && _lapsedTypeMembers is { } index)
            return index.TryGetValue(name, out var hits) ? hits : [];
        return TypeDeclarationsOf(method).Where(d => d.ReferencedByTypeClause)
            .SelectMany(DataItem.DescendantsOf).Where(m => m.CobolName is { } n && Key(n) == name);
    }

    /// <summary>The CONDITION-NAME arm of <see cref="WithLapsedTypeMembers"/>: §2023:8.4.2.2.1 rule 4 is about "the name" of any
    /// user-defined word, and a type declaration's level-88 entries are "part of the type declaration of that type"
    /// (§2023:13.18.58.4 GR1), off the condition-name index exactly as its data members are off the data-name index. Once the type
    /// is referenced by a TYPE clause they COUNT against <paramref name="registered"/> (never empty), except one a registered
    /// condition-name is a copy of (<see cref="Condition88.ClonedFrom"/>); the condition binder lets such a member count but
    /// never resolve alone (kb/Work PB1476).</summary>
    private List<Condition88> WithLapsedTypeConditions(CobolName name, List<Condition88> registered, OoMethodDataScope? method)
    {
        List<Condition88>? merged = null;
        foreach (var declaration in TypeDeclarationsOf(method))
        {
            if (!declaration.ReferencedByTypeClause) continue;
            foreach (var holder in DataItem.DescendantsOf(declaration).Prepend(declaration))
                foreach (var member in holder.Own88s)
                {
                    if (Key(member.Name) != name || registered.Exists(c => IsCopyOf(c, member))) continue;
                    (merged ??= [.. registered]).Add(member);
                }
        }
        return merged ?? registered;

        static bool IsCopyOf(Condition88 copy, Condition88 definition)
        {
            for (var from = copy.ClonedFrom; from is not null; from = from.ClonedFrom)
                if (ReferenceEquals(from, definition)) return true;
            return false;
        }
    }
}
