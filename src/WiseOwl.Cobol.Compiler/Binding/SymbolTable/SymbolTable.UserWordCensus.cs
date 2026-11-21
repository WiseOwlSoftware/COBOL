// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Editions;

namespace WiseOwl.Cobol.Binding;

public sealed partial class SymbolTable
{
    /// <summary>A rule the one declaration funnel of a user-defined word (<see cref="DataBinder.DeclareUserWord"/>) refuses a word
    /// by, and reports once per word in this binder's elements: a TYPE expansion re-registers a clone of its template's words.</summary>
    internal enum DeclarationRefusal
    {
        /// <summary>§2023:8.3.2.2: the word contains no basic letter or extended letter.</summary>
        Letterless,

        /// <summary>§2023:8.3.2.1 rule 5 with §2023:12.3.8.3 SR12 / SR13: an intrinsic-function-name a REPOSITORY function-specifier
        /// identifies.</summary>
        RepositoryIntrinsic,
    }

    private readonly HashSet<CobolName> _letterlessRefused = [];
    private readonly HashSet<CobolName> _repositoryIntrinsicRefused = [];

    /// <summary>True the first time <paramref name="word"/> is refused under <paramref name="refusal"/> in this table, so the funnel
    /// reports it; false for a repeat of the same name.</summary>
    internal bool FirstRefusal(DeclarationRefusal refusal, string word) => (refusal switch
    {
        DeclarationRefusal.Letterless => _letterlessRefused,
        DeclarationRefusal.RepositoryIntrinsic => _repositoryIntrinsicRefused,
        _ => throw new ArgumentOutOfRangeException(nameof(refusal), refusal, "not a declaration refusal of the funnel"),
    }).Add(Key(word));

    /// <summary>The census of ONE source element behind ISO §2023:8.3.2.2's "<i>Within a source element, a given user-defined
    /// word may be used as only one type of user-defined word</i>": the type each word was first recorded as, and the
    /// words already reported as two types. A program's binder is one source element and owns one census; a class
    /// binder also hosts its METHODS, each a source element of its own (§2023:10.5, "A source element is a source unit
    /// excluding any contained source units"), so each method has its own census, which the class table keeps for its symbol
    /// (<see cref="Compiler.Oo.OoClassTable.CensusOf(Model.OoMethodSymbol)"/>; kb/Work PB2921: one shared census refused a METHOD-ID spelled like its
    /// CLASS-ID, and a condition-name in one method spelled like a data-name in another, both COBOL2692 on legal
    /// source). Each element's census is the one <see cref="Compiler.Oo.OoClassTable.CensusOf"/> keeps for it. Recorded
    /// by <see cref="DataBinder.DeclareUserWord"/> for every declaration, and by
    /// <see cref="Compiler.Oo.OoClassTable.RecordReferenceUse"/> for every reference to an object-class-name,
    /// interface-name or parameter-name, including one another element declares (a method's class, a contained
    /// program's container; kb/Work PB3132), and for every reference to a name a configuration section declares
    /// (<see cref="DataBinder.RecordConfigurationNameUse"/>, kb/Work PB3153: a contained program's class condition over its
    /// container's class-name). A table of the names of one element, so it is the symbol table's (docs/rearchitecture/DESIGN-listing.md
    /// section 6.7.1: a table keyed by a COBOL name is <see cref="SymbolTable"/>'s), keyed by <see cref="CobolName"/>; it holds no
    /// symbol, only the type each word was used as, so its members take the word as text.</summary>
    internal sealed class UserWordCensus
    {
        private readonly Dictionary<CobolName, UserWordKind> _firstType = [];
        private readonly HashSet<CobolName> _conflictReported = [];

        /// <summary>Record one declaration or one use of <paramref name="word"/> as <paramref name="kind"/>. §2023:8.3.2.2 says
        /// "used": a word an element references as a type another element declares (the class-name a method INVOKEs, a
        /// class a contained program's container's REPOSITORY names) is used in the referring element as that type, so a
        /// METHOD-ID or a paragraph-name spelled like that class-name is two types in the referring source element
        /// (kb/Work PB3132), while the same word in an element that never references the class-name is legal (§2023:8.4.6.1;
        /// kb/Work PB2921). Returns the type the word was FIRST recorded as when this record is a §2023:8.3.2.2 conflict not
        /// yet reported; null for a first record, a pair <see cref="UserWordKinds.MayBeOneWord"/> admits at <paramref name="edition"/> (the same type
        /// twice included: a TYPE expansion re-registers a clone of its template's words, and a class-name is referenced
        /// as often as it is written), or a word already reported, so a third record does not repeat the refusal.</summary>
        public UserWordKind? Record(string word, UserWordKind kind, EditionInfo edition)
        {
            var key = Key(word);
            if (_firstType.TryAdd(key, kind)) return null;
            var first = _firstType[key];
            return UserWordKinds.MayBeOneWord(first, kind, edition) || !_conflictReported.Add(key) ? null : first;
        }

        /// <summary>THE one wording of a §2023:8.3.2.2 conflict (COBOL2692), for the declaration funnel and the method
        /// reference record alike: <paramref name="first"/> is the type the word was first recorded as.</summary>
        public static string ConflictMessage(string word, UserWordKind first, UserWordKind second, EditionInfo edition) =>
            $"'{word}' names two types of user-defined word in this source element ({first.Spelling()} and "
            + $"{second.Spelling()}) — a given user-defined word may be used as only one type of user-defined word "
            + edition.Cite(
                "(ISO §1985:IV.4.2.2.1.1: \"All user-defined words, except segment-numbers and level-numbers, can belong to one and only "
                + "one of these disjoint sets\"); the exceptions are the segment-numbers and the level-numbers, and the one set of "
                + "condition-names, data-names and record-names",
                "(ISO §2023:8.3.2.2); the exceptions are a compilation-variable-name, a level-number that matches a paragraph-name or "
                + "section-name, and the constant-name / data-name / property-name / record-key-name / record-name group")
            + (UserWordKinds.IsCopyName(first) || UserWordKinds.IsCopyName(second)
                ? "; below COBOL-2002 a COPY statement's text-name and library-name are user-defined words of disjoint sets of "
                  + "their own (§1985:IV.4.2.2.1.1: \"All user-defined words, except segment-numbers and level-numbers, can belong to "
                  + "one and only one of these disjoint sets\")"
                : "");

        private readonly Dictionary<(CobolName Word, UserWordKind Kind), int> _unqualifiedDeclarations = [];

        /// <summary>Record one declaration of a word that can be referenced only unqualified
        /// (<see cref="UserWordKinds.DeclaredUnqualifiable"/>, a switch's condition-name; kb/Work PB3216) and return how
        /// many this source element has now declared: 1 for the first, more for a repeat that
        /// <c>DataBinder.DeclareUserWord</c> refuses (ISO §2023:8.4.2.1).</summary>
        public int DeclareUnqualified(string word, UserWordKind kind) =>
            _unqualifiedDeclarations[(Key(word), kind)] = _unqualifiedDeclarations.GetValueOrDefault((Key(word), kind)) + 1;

        /// <summary>How many unqualifiable declarations of <paramref name="word"/> as <paramref name="kind"/> this source
        /// element has made.</summary>
        public int UnqualifiedDeclarations(string word, UserWordKind kind) =>
            _unqualifiedDeclarations.GetValueOrDefault((Key(word), kind));

        /// <summary>True when <paramref name="word"/> was first recorded in this source element as
        /// <paramref name="kind"/>.</summary>
        public bool IsDeclaredAs(string word, UserWordKind kind) =>
            _firstType.TryGetValue(Key(word), out var declared) && declared == kind;
    }
}
