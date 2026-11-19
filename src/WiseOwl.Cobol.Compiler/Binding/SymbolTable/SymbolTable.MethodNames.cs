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
        /// <summary>The method's own type declarations by type-name: a type-name declared in a method definition is a LOCAL name
        /// (§2023:8.4.6.2.2 makes global only a type-name "described with a GLOBAL clause" or "declared in a source element for an
        /// object definition"), and inside the method it hides its object's type-name of the spelling.</summary>
        public Dictionary<CobolName, DataItem> TypeNames { get; } = [];

        /// <summary>Whether one of the method's tables declares <paramref name="word"/>.</summary>
        public bool Declares(CobolName word) => TypeNames.ContainsKey(word);
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
