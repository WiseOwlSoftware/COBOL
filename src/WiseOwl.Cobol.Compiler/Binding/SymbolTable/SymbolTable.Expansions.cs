// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Compiler.Oo;
using WiseOwl.Cobol.Frontend.Parsing;

namespace WiseOwl.Cobol.Compiler.Binding;

using Core = CobolParserCore;

public sealed partial class SymbolTable
{
    /// <summary>
    /// THE PARAMETERIZED EXPANSION'S NAMES (§2023:9.3.12, §2023:12.3.8.4 GR5 and GR8; <see cref="OoExpansion"/>): THE COMPILATION
    /// GROUP'S TIER of the table, before its class table exists (docs/rearchitecture/DESIGN-listing.md section 6.7.1, "Step 6b,
    /// remainder slice 5", which records the tier decided). A parameterized class or interface definition and every expansion a
    /// REPOSITORY specifier asks for are class and interface definitions of the GROUP: their names share the group's one namespace of
    /// object-class-names and interface-names (§2023:8.4.6.4, §2023:8.3.2.2), and an expansion's name is its identity in the group
    /// (§2023:9.3.12). The expansion runs before <see cref="ClassNames"/> is built, because it makes the definitions that table is
    /// built from, so its names are a table of their own here, made by <see cref="OoExpansion.Expand"/>, handed to
    /// <see cref="OoClassTable"/> in the expansion's result, and asked thereafter only whether a word names a parameterized definition
    /// (§2023:12.3.8.4 GR1) or one some expansion re-binds.
    /// <para>The words a definition writes are no element's table either: a formal parameter-name is scoped to its definition
    /// (§2023:11.3.4 GR6, §2023:11.6.4 GR4), a REPOSITORY specifier's declared name to its paragraph (§2023:12.3.8.3 SR4, SR7). Their
    /// questions (<see cref="SpecifierDeclaring"/>, <see cref="RepeatedFormals"/>, <see cref="IsFormal"/>,
    /// <see cref="Substitution"/>) are questions of the parse tree, asked before any element's table exists, answered here with the
    /// table's one name equality as <see cref="EndMarkerAgrees"/> is.</para>
    /// </summary>
    internal sealed class ExpansionNames
    {
        /// <summary>The names of a group with no parameterized definition and no EXPANDS phrase: nothing is parameterized or
        /// expanded. Never written to.</summary>
        internal static readonly ExpansionNames None = new();

        /// <summary>The group's written parameterized definitions, by their name.</summary>
        private readonly Dictionary<CobolName, OoExpansion.Skeleton> _skeletons = [];

        /// <summary>The parameterized definitions another group published that an EXPANDS phrase named, by the target word.</summary>
        private readonly Dictionary<CobolName, OoExpansion.Skeleton> _importedSkeletons = [];

        /// <summary>Every name an actual parameter may resolve to as a class (the written plain classes and every class expansion
        /// name) and as an interface.</summary>
        private readonly HashSet<CobolName> _classes = [];
        private readonly HashSet<CobolName> _interfaces = [];

        /// <summary>The distinct expansions, by name (§2023:9.3.12 identity).</summary>
        private readonly Dictionary<CobolName, OoExpansion.Expansion> _expansions = [];

        /// <summary>The parameterized definitions some expansion re-binds.</summary>
        private readonly HashSet<CobolName> _expanded = [];

        /// <summary>Declare a written parameterized definition; false when its name is a parameterized definition's already.</summary>
        internal bool DeclareSkeleton(OoExpansion.Skeleton skeleton) => _skeletons.TryAdd(Key(skeleton.Name), skeleton);

        /// <summary>The written parameterized definition the written word <paramref name="word"/> names (an EXPANDS phrase's target,
        /// a specifier's declared name), or null.</summary>
        internal OoExpansion.Skeleton? WrittenSkeleton(ParserRuleContext? word) =>
            word is null || _skeletons.Count == 0 ? null : _skeletons.GetValueOrDefault(Key(word.GetText()));

        /// <summary>The written parameterized definitions, in the order declared.</summary>
        internal IEnumerable<OoExpansion.Skeleton> WrittenSkeletons => _skeletons.Values;

        /// <summary>Whether the group writes any parameterized definition.</summary>
        internal bool HasWrittenSkeletons => _skeletons.Count != 0;

        /// <summary>The published parameterized definition an EXPANDS phrase already read for the target it writes,
        /// <paramref name="word"/>, or null.</summary>
        internal OoExpansion.Skeleton? ImportedSkeleton(ParserRuleContext word) => _importedSkeletons.GetValueOrDefault(Key(word.GetText()));

        /// <summary>Record the published parameterized definition read for the EXPANDS target <paramref name="word"/>.</summary>
        internal void DeclareImportedSkeleton(ParserRuleContext word, OoExpansion.Skeleton skeleton) =>
            _importedSkeletons[Key(word.GetText())] = skeleton;

        /// <summary>Whether <paramref name="word"/> names a parameterized definition, written in the group or read from the
        /// repository: one that "may be specified only in the REPOSITORY paragraph" (§2023:12.3.8.4 GR1).</summary>
        internal bool IsParameterized(string word) =>
            (_skeletons.Count != 0 || _importedSkeletons.Count != 0)
            && (_skeletons.ContainsKey(Key(word)) || _importedSkeletons.ContainsKey(Key(word)));

        /// <summary>Declare <paramref name="word"/> as a class (or, <paramref name="isInterface"/>, an interface) an actual parameter
        /// may name: a written plain definition, or an expansion's name.</summary>
        internal void DeclareDefinition(string word, bool isInterface) => (isInterface ? _interfaces : _classes).Add(Key(word));

        /// <summary>Whether <paramref name="word"/> is declared as a class (or, <paramref name="isInterface"/>, an interface) an
        /// actual parameter may name.</summary>
        internal bool DeclaresDefinition(string word, bool isInterface) => (isInterface ? _interfaces : _classes).Contains(Key(word));

        /// <summary>Declare <paramref name="expansion"/> under its name, or answer the expansion that name already declares (one name
        /// is one class instance, §2023:9.3.12).</summary>
        internal OoExpansion.Expansion? DeclareExpansion(OoExpansion.Expansion expansion) =>
            _expansions.TryAdd(Key(expansion.Name), expansion) ? null : _expansions[Key(expansion.Name)];

        /// <summary>Whether two expansions' actual parameters are the same names, in order (§2023:9.3.12: expansions "with the same
        /// actual parameters are the same class instance").</summary>
        internal static bool SameActuals(IReadOnlyList<string> actuals, IReadOnlyList<string> others) =>
            actuals.Count == others.Count && actuals.Select(Key).SequenceEqual(others.Select(Key));

        /// <summary>Record that some expansion re-binds the parameterized definition <paramref name="word"/>.</summary>
        internal void MarkExpanded(string word) => _expanded.Add(Key(word));

        /// <summary>Whether some expansion re-binds the parameterized definition <paramref name="word"/>: it is then checked through
        /// its expansions, never also as itself (one fact, one diagnostic).</summary>
        internal bool IsExpanded(string word) => _expanded.Count != 0 && _expanded.Contains(Key(word));

        // ---- questions of a definition's own words, asked of the parse tree --------------------------------------------------------

        /// <summary>The class-specifier or interface-specifier of <paramref name="entries"/> (one REPOSITORY paragraph's) that DECLARES
        /// <paramref name="word"/> (object-class-name-1 or interface-name-2), the first in the order written, or null.
        /// <paramref name="specifierOnly"/> skips an entry with an EXPANDS phrase (an expansion, not a specifier of a written class);
        /// <paramref name="isInterface"/>, when set, admits only an entry of that kind.</summary>
        internal static Core.RepositoryEntryContext? SpecifierDeclaring(IEnumerable<Core.RepositoryEntryContext> entries, string word,
            bool specifierOnly = false, bool? isInterface = null)
        {
            var key = Key(word);
            foreach (var r in entries)
                if ((!specifierOnly || r.expandsPhrase() is null)
                    && (isInterface is not { } kind || (r.INTERFACE() is not null) == kind)
                    && DeclaredWord(r) is { } declared && Key(declared.GetText()) == key)
                    return r;
            return null;
        }

        /// <summary>The word a CLASS or INTERFACE specifier DECLARES (object-class-name-1, interface-name-2), as the parser read it;
        /// null for every other specifier.</summary>
        internal static ParserRuleContext? DeclaredWord(Core.RepositoryEntryContext r) =>
            (ParserRuleContext?)r.className() ?? r.interfaceName();

        /// <summary>The parameter-names of a USING clause written after an earlier one of the same name, in the order written (a
        /// given parameter-name "shall not appear more than once", §2023:11.3.3 SR9, §2023:11.6.3 SR7).</summary>
        internal static IEnumerable<Core.OoParameterNameContext> RepeatedFormals(IEnumerable<Core.OoParameterNameContext> formals)
        {
            var seen = new HashSet<CobolName>();
            foreach (var f in formals)
                if (!seen.Add(Key(f.GetText()))) yield return f;
        }

        /// <summary>Whether <paramref name="word"/> is one of the parameter-names <paramref name="formals"/>.</summary>
        internal static bool IsFormal(IReadOnlyList<Core.OoParameterNameContext> formals, string word)
        {
            var key = Key(word);
            foreach (var f in formals)
                if (Key(f.GetText()) == key) return true;
            return false;
        }

        /// <summary>The replacement of each formal parameter-name of an expansion's definition by its actual parameter
        /// (§2023:12.3.8.4 GR5, GR8): <see cref="FormalSubstitution.ActualFor"/> answers the actual a word of the definition stands for.</summary>
        internal static FormalSubstitution Substitution(IReadOnlyList<Core.OoParameterNameContext> formals, IReadOnlyList<string> actuals)
        {
            var map = new Dictionary<CobolName, string>();
            for (int i = 0; i < formals.Count; i++) map[Key(formals[i].GetText())] = actuals[i];
            return new FormalSubstitution(map);
        }
    }

    /// <summary>One expansion's formal-to-actual replacement (<see cref="ExpansionNames.Substitution"/>).</summary>
    internal sealed class FormalSubstitution
    {
        private readonly Dictionary<CobolName, string> _actuals;

        internal FormalSubstitution(Dictionary<CobolName, string> actuals) => _actuals = actuals;

        /// <summary>The actual parameter that replaces <paramref name="word"/> when it is a formal parameter-name, else null.</summary>
        internal string? ActualFor(string word) => _actuals.GetValueOrDefault(Key(word));
    }
}
