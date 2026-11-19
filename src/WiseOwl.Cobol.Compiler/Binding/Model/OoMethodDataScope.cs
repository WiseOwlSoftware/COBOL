// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Runtime;

namespace WiseOwl.Cobol.Binding.Model;

/// <summary>The method-local NAME SCOPE of one METHOD's data (ISO §2023:11.7 GR5 — a method-local name SHADOWS the
/// same name in object data; and it is INVISIBLE to sibling methods, the legacy trap-#6 cross-wiring guard made
/// structural): the per-method overlay <see cref="ReferenceResolver"/> and the condition-name lookup consult
/// FIRST while binding this method's statements. Built by <see cref="OoDataBinder.BindMethodData"/>, activated
/// per-pc by <c>StatementBinder.BindClassBody</c> through <see cref="ProcedureBindState.ActiveMethodScope"/>.</summary>
public sealed class OoMethodDataScope
{
    public Dictionary<string, List<DataItem>> ByName { get; } = new(CobolNames.Comparer);
    public Dictionary<string, List<Condition88>> Conditions { get; } = new(CobolNames.Comparer);

    /// <summary>The method's own INDEX-NAME declarations (M2-OO-1h step 4; §2023:11.7.4 GR5 — index-names are
    /// method-private: two methods each with <c>INDEXED BY IX</c> get DISTINCT cells, and a method IX shadows an
    /// object IX with its OWN cell — never one of the unit's <see cref="ForestSlice.IndexNames"/>).</summary>
    public IndexNameRegistry IndexNames { get; } = new();


    /// <summary>The method's own TYPE DECLARATIONS, in source order (kb/Work PB3192). A type-name declared in a method definition
    /// is a LOCAL name: §2023:8.4.6.2.2 makes global only a type-name "described with a GLOBAL clause" or "declared in a source
    /// element for an object definition", and §2023:13.18.27.3 SR4 bars GLOBAL in a method. So it is invisible to its object and
    /// to its sibling methods (§2023:8.4.6.2.1 "A local name may be referenced only in the source element in which it is
    /// declared"), it may repeat a type-name of its object without a duplicate, and inside the method it hides its object's
    /// type-name of the spelling (§2023:11.7.4 GR5). The type-names are the symbol table's (<c>SymbolTable.DeclareType</c>, which
    /// adds each declaration here).</summary>
    public IReadOnlyList<DataItem> TypeDeclarations => _typeDeclarations;
    private readonly List<DataItem> _typeDeclarations = [];

    /// <summary>Append one of the method's type declarations: <c>SymbolTable.DeclareType</c>'s, once it has keyed the type-name.</summary>
    internal void AddTypeDeclaration(DataItem template) => _typeDeclarations.Add(template);

    /// <summary>Whether this method declares <paramref name="word"/> in one of THIS SCOPE'S name indexes: a data-name, a
    /// condition-name or an index-name. The method's named CAPACITY registers (§2023:13.18.38.3 SR30 makes the register a
    /// data-name of the method that declares its table), type-names, constant-names, and the words its data division defines
    /// before any entry binds are the symbol table's (<c>SymbolTable.MethodDeclaresWord</c>; kb/Work PB2402, PB3192), and THE question
    /// "does this method declare the word" (§2023:11.7.4 GR5: "the use of that word in this method refers to the declaration in
    /// this method") is <c>DataBinder.MethodDeclares</c>, which asks both. ⛔ EVERY NAME INDEX OF THIS SCOPE, asked here
    /// beside the indexes themselves: when the registers moved into the method's own scope (kb/Work PB3165), the
    /// hand-written list in the binder kept asking the class's register index and a method's register stopped hiding a
    /// symbolic-character (kb/Work PB3154 review R2-1, the PB3165 merge-queue bounce). <c>GlobalNameClassDriftTests</c>
    /// finds this scope's name indexes by reflection and holds this predicate to every one.</summary>
    internal bool IndexesDeclare(string word)
    {
        return ByName.ContainsKey(word) || Conditions.ContainsKey(word) || IndexNames.Declares(word);
    }
}
