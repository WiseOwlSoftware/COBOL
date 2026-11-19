// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

public sealed partial class SymbolTable
{
    /// <summary>⛔ ONE METHOD'S OWN NAME TABLES (§2023:11.7.4 GR5: "If a given user-defined word is defined in the data division of
    /// this method definition and in the data division of the containing object definition, the use of that word in this method
    /// refers to the declaration in this method"): a method is a source element of its own, its names the nearest tier a
    /// reference in it reaches, invisible to its object and to its sibling methods (kb/Work PB3192). Every table of the method's
    /// data-name class lives here, and <see cref="Declares"/> asks each, so a method's declaration of a spelling in ANY of them
    /// hides the object's declaration of it (<see cref="MethodDeclaresWord"/>); <c>GlobalNameClassDriftTests</c> finds the tables
    /// by reflection and holds <see cref="Declares"/> to every one.</summary>
    private sealed class MethodNames
    {
        /// <summary>The method's own data-names → the items of each (a multimap, as the element's <c>_dataNames</c>): a
        /// method-local name REPLACES, never unions with, the object's (§2023:11.7.4 GR5), and it is invisible to sibling methods.</summary>
        public Dictionary<CobolName, List<DataItem>> DataNames { get; } = [];

        /// <summary>The method's own level-88 condition-names → the conditions of each.</summary>
        public Dictionary<CobolName, List<Condition88>> ConditionNames { get; } = [];

        /// <summary>The method's own type declarations by type-name: a type-name declared in a method definition is a LOCAL name
        /// (§2023:8.4.6.2.2 makes global only a type-name "described with a GLOBAL clause" or "declared in a source element for an
        /// object definition"), and inside the method it hides its object's type-name of the spelling.</summary>
        public Dictionary<CobolName, DataItem> TypeNames { get; } = [];

        /// <summary>The method's own OCCURS DYNAMIC <c>CAPACITY IN data-name-3</c> registers, each name to the method's dynamic tables
        /// that declare it (kb/Work PB3165): the register is "treated as though implicitly defined at the same level as the entry
        /// containing the OCCURS clause" (§2023:13.18.38.3 SR30), so it is a name of the method, method-private, shadowing an
        /// object-level name of the spelling. A list per name, as the element's (a TYPE / SAME AS copy reproduces it, kb/Work PB2988).</summary>
        public Dictionary<CobolName, List<DataItem>> CapacityRegisters { get; } = [];

        /// <summary>The method's own INDEX-NAME declarations (§2023:11.7.4 GR5: index-names are method-private, so two methods each
        /// with <c>INDEXED BY IX</c> get distinct cells, and a method IX shadows an object IX with its own cell).</summary>
        public IndexNameTable IndexNames { get; } = new();

        /// <summary>Whether one of the method's tables declares <paramref name="word"/>.</summary>
        public bool Declares(CobolName word)
        {
            return DataNames.ContainsKey(word) || ConditionNames.ContainsKey(word) || TypeNames.ContainsKey(word)
                || CapacityRegisters.ContainsKey(word) || IndexNames.Declares(word);
        }
    }

    /// <summary>Each METHOD's own name tables, by its name scope.</summary>
    private readonly Dictionary<OoMethodDataScope, MethodNames> _methodNames = new(ReferenceEqualityComparer.Instance);

    /// <summary>The method's tables, made on its first declaration.</summary>
    private MethodNames MethodNamesOf(OoMethodDataScope method)
    {
        if (!_methodNames.TryGetValue(method, out var names)) _methodNames[method] = names = new();
        return names;
    }
}
