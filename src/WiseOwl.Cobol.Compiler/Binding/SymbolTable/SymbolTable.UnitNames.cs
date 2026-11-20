// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

public sealed partial class SymbolTable
{
    /// <summary>
    /// The FUNCTION-ID units of the compilation group by their user-function-name, THE COMPILATION GROUP'S TIER of the table beside
    /// its OO half, <see cref="ClassNames"/> (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b, remainder slice 5"; kb/Work
    /// PB5932). A function definition or prototype is a source unit of the group, and a REPOSITORY <c>FUNCTION word</c> entry of any
    /// source element that follows it names it by its WORD (§2023:8.4.6.7), so the words are the group's, not one element's. A
    /// program unit's own entry is <see cref="DeclareProgram"/>'s; the group's program units are searched by their EXTERNALIZED
    /// names (§2023:12.3.8.4 GR10), the operating environment's names and no COBOL word, so they are no table here.
    /// <para>Two questions are asked of the words: whether a unit carries a word (the candidates of determination D-R3 steps 2 and 3,
    /// <see cref="GroupRepositoryProvider.FunctionsByWord"/>), and the earlier unit of the same kind (definition or prototype) whose
    /// word a unit repeats (COBOL1508, <c>BinderDriver.CheckFunctionUnits</c>).</para>
    /// </summary>
    internal sealed class UnitNames
    {
        /// <summary>The group's FUNCTION-ID units under their user-function-name, each list in source order.</summary>
        private readonly Dictionary<CobolName, List<BoundUnit>> _functions = [];

        /// <summary>The function units of <paramref name="units"/> (the group's, in source order), keyed by word: the table the
        /// same-word screen reads (<see cref="EarlierOfSameWord"/>).</summary>
        internal UnitNames(IReadOnlyList<BoundUnit> units)
        {
            foreach (var u in units)
                if (u.IsFunction)
                {
                    var key = Key(u.Name);
                    if (!_functions.TryGetValue(key, out var list)) _functions[key] = list = [];
                    list.Add(u);
                }
        }

        /// <summary>Whether the user-function-name of the function unit <paramref name="unit"/> is <paramref name="word"/>: what a
        /// REPOSITORY <c>FUNCTION word</c> entry may name (§2023:8.4.6.7). The word reaches the group through the repository's
        /// providers as text (<see cref="IRepositoryProvider.FunctionsByWord"/>), so the question is the table's name equality over
        /// the group's units, which the provider walks in its own order (§2023:12.3.8.4 GR11 a) then b)).</summary>
        internal static bool NamesFunction(BoundUnit unit, string word) => Key(unit.Name) == Key(word);

        /// <summary>The first function unit, in source order, of <paramref name="unit"/>'s own kind (a definition for a definition, a
        /// prototype for a prototype) whose user-function-name is <paramref name="unit"/>'s, when that is an EARLIER unit than
        /// <paramref name="unit"/>: the word is then repeated. A prototype and a definition of one word are the pair §2023:10.6.2 SR3
        /// legislates for, so the kinds are never compared with each other.</summary>
        internal BoundUnit? EarlierOfSameWord(BoundUnit unit)
        {
            foreach (var u in _functions[Key(unit.Name)])
                if (u.IsPrototype == unit.IsPrototype)
                    return ReferenceEquals(u, unit) ? null : u;
            return null;
        }
    }
}
