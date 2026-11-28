// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using WiseOwl.Cobol.Compiler.Binding.Bound;
using WiseOwl.Cobol.Compiler.Binding.Model;
using WiseOwl.Cobol.Compiler.Symbols;

namespace WiseOwl.Cobol.Compiler.Binding;

/// <summary>An OCCURRENCE of the symbol table (docs/rearchitecture/DESIGN-listing.md section 6.7.1): an opaque number, 1-based in the
/// compilation group, made when an identification funnel records a reference the program wrote; <c>default</c> is none.</summary>
internal readonly record struct OccurrenceId(int Value)
{
    /// <summary>Whether this is an occurrence (the default is none).</summary>
    public bool IsSet => Value > 0;
}

public sealed partial class SymbolTable
{
    internal sealed partial class EntryStore
    {
        /// <summary>One recorded occurrence, mutable only while its statement is classified (<see cref="Classify"/>).</summary>
        private sealed class Occurrence(SymbolId entry, SymbolPosition at, int length)
        {
            public SymbolId Entry { get; } = entry;
            public SymbolPosition At { get; } = at;
            public int Length { get; } = length;
            public SymbolUse Use { get; set; } = SymbolUse.Read;
            public SymbolPosition Statement { get; set; }
            public string? Verb { get; set; }
            public bool Classified { get; set; }
        }

        // ⛔ THE ONE STORE OF REFERENCES (section 6.7.1: "there is no ledger beside it"): null unless the listing asked for the
        // cross-reference, so a compilation without one allocates nothing here (section 9).
        private List<Occurrence>? _occurrences;
        private Dictionary<SymbolPosition, OccurrenceId>? _byPosition;
        private Dictionary<object, OccurrenceId>? _linked;     // by the INSTANCE of the SITE the bound tree holds: a place, or an index-name receiver
        private List<object>? _linkOrder;   // the linked sites in the order linked: a statement's are those since its mark

        /// <summary>The end of the order places are linked in: a statement holds it as its mark (<see cref="LinkedSince"/>).</summary>
        internal int LinkMark => _linkOrder?.Count ?? 0;

        /// <summary>The sites linked since <paramref name="mark"/>: the places and index-name receivers the statement's references were
        /// built as.</summary>
        internal IEnumerable<object> LinkedSince(int mark)
        {
            for (int i = mark; i < (_linkOrder?.Count ?? 0); i++) yield return _linkOrder![i];
        }

        /// <summary>Whether the statement funnel also checks that every store position pairs with an occurrence
        /// (<see cref="VerifyPairing"/>); only the completeness oracle asks (<c>EditionContext.VerifiesStorePairing</c>).</summary>
        internal bool VerifiesPairing { get; private set; }

        /// <summary>Whether the group's identifications record occurrences (<c>EditionContext.RecordsReferences</c>).</summary>
        internal bool RecordsReferences => _occurrences is not null;

        /// <summary>Record occurrences from now on: the bind sets it once, before any element binds, when the listing reads the
        /// cross-reference (DESIGN-listing.md section 9).</summary>
        internal void RecordReferences(bool verifyPairing = false)
        {
            VerifiesPairing = verifyPairing;
            _occurrences ??= [];
            _byPosition ??= [];
            _linked ??= new(ReferenceEqualityComparer.Instance);
            _linkOrder ??= [];
        }

        /// <summary>The global append order's current end: a statement holds it as its MARK and classifies exactly the occurrences
        /// appended since (<see cref="Classify"/>).</summary>
        internal int OccurrenceMark => _occurrences?.Count ?? 0;

        /// <summary>Record that the program wrote a reference to <paramref name="entry"/> at <paramref name="at"/>, of
        /// <paramref name="length"/> characters. ⛔ ONE OCCURRENCE PER WRITTEN REFERENCE, keyed by where it is written: a site that
        /// identifies one written word twice (a probe and its resolution both committing, a funnel asked twice for a re-asked formal)
        /// records it once (section 6.7.1), so the occurrence of a position is the first identification's. Nothing is recorded when the
        /// group does not record references, for a name of no entry, or at no position.</summary>
        internal OccurrenceId Record(SymbolId entry, SymbolPosition at, int length)
        {
            if (_occurrences is null || !entry.IsSet || !at.IsSet) return default;
            if (_byPosition!.TryGetValue(at, out var known)) return known;
            _occurrences.Add(new Occurrence(entry, at, length));
            var id = new OccurrenceId(_occurrences.Count);
            _byPosition.Add(at, id);
            return id;
        }

        /// <summary>The occurrence recorded at <paramref name="at"/>, or none.</summary>
        internal OccurrenceId OccurrenceAt(SymbolPosition at) =>
            _byPosition is not null && _byPosition.TryGetValue(at, out var id) ? id : default;

        /// <summary>Link the place INSTANCE a builder made for a written reference to its occurrence (section 6.7.1), so the statement
        /// funnel finds the occurrence of each store position by walking from the stored place inward to the first linked
        /// instance. A place already linked (a builder may hand back a shared instance) is copied first: a new reference, equal by
        /// value, so value comparisons of places elsewhere are unchanged.</summary>
        internal T Link<T>(T place, OccurrenceId occurrence) where T : Place
        {
            if (_linked is null || !occurrence.IsSet) return place;
            if (_linked.ContainsKey(place)) place = place with { };
            _linked[place] = occurrence;
            _linkOrder!.Add(place);
            return place;
        }

        /// <summary>Link the index-name receiver node a binder made for a written index-name (an index-name's cell is held by its field
        /// name, no place stands for it: section 6.7.1, "Store kind") to its occurrence, as <see cref="Link{T}"/> links a place. The node is
        /// made for the one reference, so it is never already linked.</summary>
        internal SetIndexTarget Link(SetIndexTarget target, OccurrenceId occurrence)
        {
            if (_linked is null || !occurrence.IsSet) return target;
            _linked[target] = occurrence;
            _linkOrder!.Add(target);
            return target;
        }

        /// <summary>The occurrence of the reference <paramref name="site"/> was built for: the first site linked to an occurrence, walking
        /// inward from <paramref name="site"/> through the places it decorates.</summary>
        internal OccurrenceId LinkedOccurrence(object? site)
        {
            if (_linked is null) return default;
            for (var p = site; p is not null; p = (p as PlaceDecorator)?.Inner)
                if (_linked.TryGetValue(p, out var id)) return id;
            return default;
        }

        /// <summary>Whether the statement that took <paramref name="mark"/> has an occurrence to classify: one appended since that no
        /// nested statement classified.</summary>
        internal bool AnyToClassify(int mark)
        {
            if (_occurrences is null) return false;
            for (int i = mark; i < _occurrences.Count; i++)
                if (!_occurrences[i].Classified) return true;
            return false;
        }

        /// <summary>Whether <paramref name="site"/> (a place or an index-name receiver) stands for an occurrence still to classify: its linked occurrence was appended since
        /// <paramref name="mark"/> and no nested statement classified it.</summary>
        internal bool Awaits(object site, int mark) =>
            LinkedOccurrence(site) is { IsSet: true } id && id.Value > mark && !_occurrences![id.Value - 1].Classified;

        /// <summary>
        /// ⛔ THE STATEMENT FUNNEL'S CLASSIFICATION (DESIGN-listing.md section 6.7.1, "Store kind"): every occurrence appended since
        /// <paramref name="mark"/> and not yet classified (a nested statement classified its own first) is
        /// <paramref name="statement"/>'s, written in it with its <paramref name="verb"/>; an occurrence whose linked place stands at
        /// one of <paramref name="storePositions"/> takes that position's kind (<c>Write</c> and <c>WriteInPart</c> written,
        /// <c>ReadWrite</c> modified), and every other is read.
        /// </summary>
        internal void Classify(int mark, WiseOwl.Cobol.Editions.DiagnosticCursor statement, string verb,
            IEnumerable<(object Site, StoreKind Kind)> storePositions)
        {
            if (_occurrences is null) return;
            foreach (var (site, kind) in storePositions)
                if (LinkedOccurrence(site) is { IsSet: true } id && id.Value > mark && _occurrences[id.Value - 1] is { Classified: false } o)
                    o.Use = kind switch
                    {
                        StoreKind.Write or StoreKind.WriteInPart => SymbolUse.Written,
                        StoreKind.ReadWrite => SymbolUse.Modified,
                        _ => o.Use,
                    };
            for (int i = mark; i < _occurrences.Count; i++)
                if (_occurrences[i] is { Classified: false } o)
                {
                    o.Statement = PositionOf(statement);
                    o.Verb = verb;
                    o.Classified = true;
                }
        }

        /// <summary>THE PAIRING TEST's check (only when <see cref="VerifiesPairing"/>): each of <paramref name="storePositions"/>, the
        /// places the statement's own operands hold that it stores through, must pair with an occurrence of the statement
        /// (<see cref="Awaits"/>); one that does not is a written operand whose place a binder rebuilt by value or built with no link, kept in
        /// <see cref="UnpairedStorePositions"/>. Asked before the statement classifies.</summary>
        internal void VerifyPairing(int mark, WiseOwl.Cobol.Editions.DiagnosticCursor statement, string verb,
            IEnumerable<(object Site, StoreKind Kind)> storePositions)
        {
            foreach (var (site, _) in storePositions)
                if (!Awaits(site, mark))
                    _unpaired.Add((PositionOf(statement), verb, site switch
                    {
                        Place place => place.NamedItem?.CobolName ?? place.GetType().Name,
                        SetIndexTarget index => index.IndexField,
                        _ => site.GetType().Name,
                    }));
        }

        private readonly List<(SymbolPosition Statement, string Verb, string Operand)> _unpaired = [];

        /// <summary>Each store position a classified statement's written operands occupy that pairs with no occurrence of the statement
        /// (DESIGN-listing.md section 6.7.1, the pairing test): the statement's position and verb and the operand's name. Empty when every
        /// store position's place is linked to its reference's occurrence; filled only when <see cref="VerifiesPairing"/>.</summary>
        internal IReadOnlyList<(SymbolPosition Statement, string Verb, string Operand)> UnpairedStorePositions => _unpaired;

        /// <summary>Every occurrence of <paramref name="entry"/>, in the order recorded, as the finished table holds it.</summary>
        internal IReadOnlyList<SymbolOccurrence> OccurrencesOf(SymbolId entry)
        {
            if (_occurrences is null) return [];
            List<SymbolOccurrence>? found = null;
            foreach (var o in _occurrences)
                if (o.Entry == entry) (found ??= []).Add(new SymbolOccurrence(o.At, o.Length, o.Use, o.Statement, o.Verb));
            return found ?? (IReadOnlyList<SymbolOccurrence>)[];
        }

        /// <summary>Every occurrence, in the order recorded, with its entry: the completeness oracle's view (Conformance's
        /// <c>ReferenceOccurrenceDriftTests</c>) and the finish's.</summary>
        internal IEnumerable<(SymbolId Entry, SymbolOccurrence Occurrence)> Occurrences()
        {
            foreach (var o in _occurrences ?? [])
                yield return (o.Entry, new SymbolOccurrence(o.At, o.Length, o.Use, o.Statement, o.Verb));
        }
    }

    /// <summary>⛔ NO LISTING ASKED, NO LISTING WORK (section 9): record the reference <paramref name="written"/> to the data-name or condition-name
    /// <paramref name="candidate"/>, which asks for its entry (<see cref="DataEntryOf"/>, a lookup that fails loudly for a candidate with none)
    /// ONLY when the group records references. Every funnel that identifies a data item or condition-name records through here, never by
    /// <c>Record(DataEntryOf(...))</c>, so a compilation that asked for no listing does no entry lookup and cannot fail on one
    /// (<c>ListingOffDoesNoWorkDriftTests</c>).</summary>
    private OccurrenceId RecordData(object candidate, ParserRuleContext written, bool report) =>
        report && _entries.RecordsReferences ? Record(DataEntryOf(candidate), written, report) : default;

    /// <summary><see cref="RecordData(object, ParserRuleContext, bool)"/> for a qualified reference.</summary>
    private OccurrenceId RecordData(object candidate, WrittenQualifiedName written, bool report) =>
        report && _entries.RecordsReferences ? Record(DataEntryOf(candidate), written, report) : default;

    /// <summary>Record the reference <paramref name="written"/> (a context: its first token's position, its characters as written)
    /// to <paramref name="entry"/>, when <paramref name="report"/> says it is the reference itself and not a probe.</summary>
    internal OccurrenceId Record(SymbolId entry, ParserRuleContext written, bool report) =>
        report && _entries.RecordsReferences ? _entries.Record(entry, PositionOf(written.Start), LengthOf(written)) : default;

    /// <summary>Record the reference written as <paramref name="token"/>.</summary>
    internal OccurrenceId Record(SymbolId entry, IToken token, bool report) =>
        report && _entries.RecordsReferences ? _entries.Record(entry, PositionOf(token), token.Text.Length) : default;

    /// <summary>Record the reference written as a word inside a token (<see cref="WrittenWord"/>).</summary>
    internal OccurrenceId Record(SymbolId entry, WrittenWord written, bool report) =>
        report && _entries.RecordsReferences
            ? _entries.Record(entry, new SymbolPosition(written.Token.Line, written.Token.Column + written.Start), written.Length)
            : default;

    /// <summary>Record the qualified reference <paramref name="written"/> at its head, its length reaching its last qualifier.</summary>
    internal OccurrenceId Record(SymbolId entry, WrittenQualifiedName written, bool report)
    {
        if (!report || !_entries.RecordsReferences) return default;
        var last = written.Qualifiers.Count > 0 ? written.Qualifiers[^1] : written.Head;
        return _entries.Record(entry, PositionOf(written.Head), SpanLength(written.Head, last));
    }

    // Each written REDEFINES clause's data-name-2, by its redefiner, while the forest is built: the clause is resolved by its placement
    // after the forest is composed (LinkRedefinitionsPass, §2023:13.18.44.3 SR4), and only then is its occurrence's entry known.
    private Dictionary<DataItem, Frontend.Parsing.CobolParserCore.DataReferenceContext>? _redefinesWritten;

    /// <summary>Note where <paramref name="redefiner"/>'s REDEFINES clause writes data-name-2, for its occurrence once it is linked.</summary>
    internal void NoteRedefinesWritten(DataItem redefiner, Frontend.Parsing.CobolParserCore.DataReferenceContext written)
    {
        if (_entries.RecordsReferences) (_redefinesWritten ??= new(ReferenceEqualityComparer.Instance))[redefiner] = written;
    }

    /// <summary>Record the occurrence of <paramref name="redefiner"/>'s data-name-2 at the entry the link made its target.</summary>
    internal void RecordRedefinition(DataItem redefiner)
    {
        if (_redefinesWritten is not null && _redefinesWritten.TryGetValue(redefiner, out var written) && redefiner.RedefinesTarget is { } target)
            RecordData(target, written, report: true);
    }

    /// <summary>The occurrence recorded at <paramref name="written"/>'s first token, or none: what a place builder links the place it
    /// makes for that reference to.</summary>
    internal OccurrenceId OccurrenceOf(ParserRuleContext written) => _entries.OccurrenceAt(PositionOf(written.Start));

    /// <summary>The occurrence recorded at <paramref name="head"/>, or none.</summary>
    internal OccurrenceId OccurrenceOf(IToken head) => _entries.OccurrenceAt(PositionOf(head));

    /// <summary>The characters a context is written in: from its first token to the end of its last, when both are on one line;
    /// its first token's length otherwise (a reference continued onto another line).</summary>
    private static int LengthOf(ParserRuleContext written) =>
        written.Stop is { } stop && stop.TokenIndex >= written.Start.TokenIndex ? SpanLength(written.Start, stop) : written.Start.Text.Length;

    private static int SpanLength(IToken first, IToken last) =>
        first.Line == last.Line && last.StopIndex >= first.StartIndex && last.Column >= first.Column
            ? last.Column - first.Column + last.Text.Length
            : first.Text.Length;
}
