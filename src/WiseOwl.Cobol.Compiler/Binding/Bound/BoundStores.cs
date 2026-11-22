// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using WiseOwl.Cobol.Runtime;

using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Bound;

/// <summary>How a statement touches a given item through its STORE positions (the polarity that selects the
/// §2023:8.4.3.9.4 general rule): <see cref="None"/> → the occurrence is purely SENDING (GR1, get only);
/// <see cref="Write"/> → a WRITE-only receiving position (GR2, set only — the get method is NOT invoked);
/// <see cref="ReadWrite"/> → an in-place read-modify-write position (GR3, get before + set after);
/// <see cref="WriteInPart"/> → a receiving position that stores only PART of the item and keeps the rest (ISO
/// §2023:14.9.43.4 GR7 for STRING; §2023:8.4.3.3.4 GR5 for a reference-modified receiver), so the kept portion is the
/// property's value: get before when the class has a get method, set after — and a class with NO get method is
/// not an error (the position is a RECEIVING one, §2023:14.9.43.3 SR10, so §2023:8.4.3.9.3 SR3 asks nothing of it).</summary>
public enum StoreKind { None, Write, ReadWrite, WriteInPart }

/// <summary>What a store query asks of a statement that names one operand and stores into SELECTED items within it
/// (MOVE, ADD and SUBTRACT CORRESPONDING; INITIALIZE): the operand the statement WROTE, or the items it actually stores
/// (kb/Work PB5890). Every other statement stores what it names, and both views agree on it.</summary>
internal enum StoreView
{
    /// <summary>The receiving operand as written: the receiving group of a CORRESPONDING statement, identifier-1 of INITIALIZE.
    /// The property-temp query asks it (the temp IS the written operand) and the debug query asks it (X3.23-1985 USE FOR
    /// DEBUGGING general rule 6 c "explicitly references"; rule 9: a group reference does not reference its subordinates).</summary>
    Operands,

    /// <summary>The storage the statement alters: the receiving item of each corresponding pair (§1985:VI.6.19.4 GR1 "selected
    /// items within identifier-1 are moved to selected items within identifier-2"), and the elementary stores INITIALIZE's
    /// expansion makes after its category selection, exclusions and default (§1985:VI.6.17.4 GR2 to GR5). The control-data
    /// query asks it (§1985:XIII.4.9.3 SR4 "must not alter the value of any control data item").</summary>
    Storage,
}

/// <summary>
/// The bound-tree STORE analysis: does a statement write through a Place rooted at a given
/// <see cref="DataItem"/>? First client: the object-property reference desugar (ISO §2023:8.4.3.9.4 GR1–GR3,
/// deep-dive D-P2) — a reference is a RECEIVING occurrence exactly when its synthesized temp is stored by
/// the statement it desugared out of (GR2/GR3), so the SET accessor is invoked only then; a misclassified
/// "sending" would skip a required SET (silently lost store), a misclassified "receiving" would run a
/// side-effecting SET the spec says is not invoked — hence the walk is a TOTAL explicit taxonomy over every
/// <c>BoundStatement</c> (the 2026-07-04 15-agent survey, scratchpad <c>bound_stores_classification.md</c>:
/// 119 nodes, 46 store-bearing; every store field verified against the emitter). It is the exhaustive
/// generated <see cref="IBoundStatementVisitor{T}"/> (PHASE-07 Step 6c), so a new bound-statement leaf is a
/// COMPILE error here, and its answer is a non-nullable <see cref="StoreKind"/>: EVERY leaf classifies EVERY
/// Place it carries, so there is no "outside the taxonomy" answer for a caller to stage (kb/Work PB1275 — thirteen
/// leaves used to answer null, and the property desugar turned each into COBOL0843, refusing an object or
/// pointer property in every SET format although §2023:8.4.3.9.3 SR5/SR6 admit it wherever a data item of its
/// description may stand).
/// </summary>
/// <remarks>
/// Scope facts that keep this walk small and honest:
/// - Only PLACE stores can hit a property temp. Non-Place stores (generated index/ALTER fields, external
///   switches, report-engine counters, run-unit exception state, <c>BoundTableSort.Storage</c>) can never target
///   the temp — a temp is a synthesized simple local (and a property subject cannot carry OCCURS, COBOL0842), so
///   those arms are <c>None</c> here. The items a file connector stores (the record area, FILE STATUS, RELATIVE KEY,
///   DEPENDING ON: <see cref="FileConnectorStore"/>) are data items, and <see cref="StoresInto"/> asks them through
///   the places its caller passes (kb/Work PB5885).
/// - <see cref="Place.Item"/> is identity-compared: every Place wrapper (RefMod/alias/view) forwards
///   <c>Item</c> to the underlying item, so a windowed write through <c>RefModPlace</c> still classifies.
/// - The child-statement recursion (<c>Kids</c>) is DEFENSIVELY total over conditional-phrase bodies (SIZE
///   ERROR / AT END / INVALID KEY / ON EXCEPTION / ON OVERFLOW / AT EOP), IF/EVALUATE arms, inline-PERFORM
///   bodies, SEARCH arms, <c>BoundEcChecked.Inner</c> and <c>BoundSequence.Steps</c>. In practice a property
///   temp is statement-local: every nested statement is bound through its OWN <c>BindStatement</c> and so
///   drains its own property ops (<c>StatementBinder.BindStatement</c>), so the temp lives in the carrying
///   statement's DIRECT operands/condition, never a separately-wrapped nested body — which is why arms that
///   omit the recursion (e.g. <c>BoundKeyedDelete</c>) are equivalent, not buggy. Out-of-line PERFORM bodies
///   are pc ranges (not nested statements), so they are never walked.
/// </remarks>
public static class BoundStores
{
    /// <summary>The store polarity of <paramref name="item"/> in <paramref name="s"/> —
    /// <see cref="StoreKind.None"/> = provably never stored (a pure sending occurrence);
    /// <see cref="StoreKind.Write"/> / <see cref="StoreKind.ReadWrite"/> per the emitter-verified
    /// classification. A property temp occurs at exactly ONE Place in the tree, so the first store found is
    /// total.</summary>
    public static StoreKind StoreKindOf(BoundStatement s, DataItem item)
    {
        var visitor = new StoreKindVisitor(p => ReferenceEquals(p.Item, item), StoreView.Operands, crossingsStore: true);
        var kind = s.Accept(visitor);
        // A whole-receiver arm answered Write for a store into a WINDOW of the item: only that subset is a receiving
        // item (§2023:8.4.3.3.4 GR5), so the rest of the item is kept, which is the same polarity STRING's GR7 gives.
        return kind == StoreKind.Write && visitor.StoredThroughWindow ? StoreKind.WriteInPart : kind;
    }

    /// <summary>The polarity of a whole-receiver store through <paramref name="receiver"/>, the store the walk answers
    /// <see cref="StoreKind.Write"/> for: <see cref="StoreKind.WriteInPart"/> when the place is a reference-modified
    /// WINDOW of its item (<see cref="Place.DenotedItem"/> is null: §2023:8.4.3.3.4 GR5 "a unique data item that is a
    /// subset"), whose other positions are kept. The one statement of that rule: the walk asks it of every store
    /// position, and a binder that must know the polarity before its statement node exists asks it directly
    /// (MoveBinder.BindMoveOf, kb/Work PB370).</summary>
    public static StoreKind WholeReceiverStore(Place receiver) =>
        receiver.DenotedItem is null ? StoreKind.WriteInPart : StoreKind.Write;

    /// <summary>Does <paramref name="s"/>, or any statement nested in it, STORE into an item
    /// <paramref name="matches"/> accepts? The second client (kb/Work PB363, ISO §2023:14.9.49.3 SR11 "A USE BEFORE
    /// REPORTING procedure shall not alter the value of any control data item"): the same emitter-verified
    /// classification, asked of a SET of items instead of one, through the PLACE of each store so a window or one occurrence is not the whole item.
    /// <para>A BY REFERENCE CALL argument and an INVOKE write-back are NOT stores here: whether the callee alters
    /// the argument is not decidable from this source element, and a syntax rule that fired on the possibility
    /// would reject a conforming program. The OO property desugar counts them (it must run the SET accessor after
    /// a crossing), which is why the choice is a parameter of the walk and not of the node.</para>
    /// <para>The stores an input-output statement makes through its file connector ARE stores here, though the statement
    /// names only the file (kb/Work PB5885): <paramref name="connector"/> places them (<see cref="FileConnectorStore"/>) and
    /// each I-O arm says which its verb makes, so a READ alters its record area, the FILE STATUS item, the RELATIVE KEY item
    /// and the DEPENDING ON item as a MOVE to one of them does. The other two queries ask about the operands a statement
    /// writes and pass none.</para>
    /// <para>It asks the <see cref="StoreView.Storage"/> view (kb/Work PB5890): a CORRESPONDING statement or an INITIALIZE stores
    /// the items it selects within the operand it names, not the whole operand, so an item of the group it selects none of keeps
    /// its value.</para></summary>
    public static bool StoresInto(BoundStatement s, Func<Place, bool> matches,
        Func<FileModel, FileConnectorStore, IEnumerable<Place>> connector) =>
        s.Accept(new StoreKindVisitor(matches, StoreView.Storage, crossingsStore: false, connector: connector)) is not StoreKind.None;

    /// <summary>The place through which <paramref name="s"/> ITSELF — never a statement nested in it — stores into
    /// <paramref name="item"/>, or null when it does not. The third client (X3.23-1985 USE FOR DEBUGGING general rule 6 c, which
    /// runs a debugging section "immediately after the execution of any other COBOL statement that explicitly references and causes the
    /// content of the data item … to be changed"; rule 7 makes each imperative verb a statement of its own;
    /// docs/CONTROL_FLOW_DESIGN.md D20): the same emitter-verified classification as <see cref="StoresInto"/>, over the direct
    /// operands only. A BY REFERENCE CALL argument is not a store here, for <see cref="StoresInto"/>'s reason.</summary>
    public static Place? DirectStoreOf(BoundStatement s, DataItem item)
    {
        // "explicitly references": the data-name the operand is written with, under any decoration (Place.NamedItem, kb/Work
        // PB5801 review R1-1), so a store through a level-66 alias is a store referencing the alias, not the item it renames.
        var visitor = new StoreKindVisitor(p => ReferenceEquals(p.NamedItem, item), StoreView.Operands, crossingsStore: false, direct: true);
        return s.Accept(visitor) is StoreKind.None ? null : visitor.FirstHit;
    }

    /// <summary>⛔ THE STORE POSITIONS A STATEMENT'S WRITTEN OPERANDS OCCUPY (docs/rearchitecture/DESIGN-listing.md section 6.7.1,
    /// "Store kind"): each place <paramref name="s"/> ITSELF holds in an operand position (<see cref="BoundReferences.Of"/>, the
    /// instance the bound node holds, a decorated place as its outer place) that the exhaustive visitor stores through, with the kind
    /// of the store, asked of that INSTANCE as <see cref="StoreKindOf"/> asks of an item (a BY REFERENCE argument counted as
    /// <see cref="StoreKindOf"/> counts it, a store into a window <see cref="StoreKind.WriteInPart"/>). A statement nested in
    /// <paramref name="s"/> is a statement of its own and answers for its own operands. The places a file connector's callback builds
    /// (the record area, FILE STATUS, RELATIVE KEY, DEPENDING ON) are no written operand and are not asked. The symbol table's
    /// statement funnel reads it to classify each occurrence (<c>SymbolTable.EntryStore.Classify</c>). <paramref name="linked"/> are the
    /// places the statement's references were built as (the symbol table's links since the statement's mark), asked beside the places
    /// <see cref="BoundReferences.Of"/> finds, since a node may hold an operand's place where that walk does not enter (a tuple of
    /// <see cref="BoundSetConditions"/>: kb/Work PB5925's dated file <c>2026-11-20-step-7b-2-built</c>). This is the PAIRING TEST's walk (the symbol table asks it only when
    /// a test asks it to verify pairing, <c>SymbolTable.EntryStore.VerifyPairing</c>): the classification itself is
    /// <see cref="StorePositionsAmong"/>.</summary>
    internal static IEnumerable<(object Site, StoreKind Kind)> StorePositionsOf(BoundStatement s, IEnumerable<object>? linked = null)
    {
        // A written reference is held by the bound tree as a place, or, for an index-name (whose cell is a field name), as the
        // receiver node itself: the two kinds of SITE a store position can stand at (section 6.7.1, "Store kind"). The sites are
        // what the tree holds and what the symbol table linked, so a store position whose reference was never linked is still seen.
        var reach = BoundReferences.Of(s);
        var asked = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var site in ((IEnumerable<object>)reach.Places).Concat(reach.IndexTargets).Concat(linked ?? []))
        {
            if (!asked.Add(site)) continue;
            if (site is Place { NamedItem.IsCompilerTemp: true }) continue;   // a temporary the binder made is no operand the program wrote
            var at = site;
            var visitor = new StoreKindVisitor(p => ReferenceEquals(p, at), StoreView.Operands, crossingsStore: true, direct: true,
                matchesIndex: t => ReferenceEquals(t, at));
            var kind = s.Accept(visitor);
            if (kind == StoreKind.Write && visitor.StoredThroughWindow) kind = StoreKind.WriteInPart;
            if (kind is not StoreKind.None) yield return (site, kind);
        }
    }

    /// <summary>⛔ THE STORE POSITIONS AMONG THE SITES (a place, or an index-name receiver) <paramref name="wanted"/> NAMES (the classification's question, DESIGN-listing.md
    /// section 6.7.1, "Store kind"; section 9): each place instance <paramref name="s"/> stores through, with the kind of the store, asked
    /// of the instances the exhaustive visitor ITSELF asks about. One survey pass notes every wanted place an arm asks (the survey
    /// answers no, so no arm stops early), and only those are asked their kind, so a statement's reads cost nothing beyond the survey and
    /// no walk of the node's fields is made. Where <see cref="StorePositionsOf"/> starts from the places the statement holds, this starts
    /// from the places the visitor asks.</summary>
    internal static IEnumerable<(object Site, StoreKind Kind)> StorePositionsAmong(BoundStatement s, Func<object, bool> wanted)
    {
        List<object>? asked = null;
        bool Note(object site)
        {
            if (!wanted(site)) return false;
            asked ??= [];
            foreach (var known in asked)
                if (ReferenceEquals(known, site)) return false;
            asked.Add(site);
            return false;
        }
        s.Accept(new StoreKindVisitor(p => Note(p), StoreView.Operands, crossingsStore: true, direct: true, matchesIndex: t => Note(t)));
        if (asked is null) yield break;
        object? at = null;
        var visitor = new StoreKindVisitor(p => ReferenceEquals(p, at), StoreView.Operands, crossingsStore: true, direct: true,
            matchesIndex: t => ReferenceEquals(t, at));
        foreach (var site in asked)
        {
            at = site;
            visitor.Reset();
            var kind = s.Accept(visitor);
            if (kind == StoreKind.Write && visitor.StoredThroughWindow) kind = StoreKind.WriteInPart;
            if (kind is not StoreKind.None) yield return (site, kind);
        }
    }

    /// <summary>The per-node store classification (the former <c>StoreKindOf</c> switch, one arm per leaf) —
    /// the exhaustive <see cref="IBoundStatementVisitor{T}"/> over the bound statements, carrying the target
    /// <paramref name="item"/> so recursion is <c>child.Accept(this)</c>. <paramref name="view"/> says whether a statement that
    /// stores into selected items of the operand it names answers for the operand or for those items.</summary>
    private sealed class StoreKindVisitor(Func<Place, bool> matches, StoreView view, bool crossingsStore, bool direct = false,
        Func<FileModel, FileConnectorStore, IEnumerable<Place>>? connector = null,
        Func<SetIndexTarget, bool>? matchesIndex = null) : IBoundStatementVisitor<StoreKind>
    {
        /// <summary>The first <see cref="Place"/> a matching store went through, or null (<see cref="DirectStoreOf"/> reads it).</summary>
        public Place? FirstHit { get; private set; }

        /// <summary>Does the statement store through <paramref name="p"/> into a matching item? Every arm asks its store
        /// positions through here (or through a helper that does), so a store into a reference-modified WINDOW of the
        /// item (<see cref="WholeReceiverStore"/>) is noted once, for every present and future whole-receiver arm, in
        /// <see cref="StoredThroughWindow"/>.</summary>
        private bool Hit(Place? p)
        {
            if (p is null || !matches(p)) return false;
            FirstHit ??= p;
            if (WholeReceiverStore(p) is StoreKind.WriteInPart) StoredThroughWindow = true;
            return true;
        }

        /// <summary>True once a matching store went through a window of the item (see <see cref="Hit"/>); the visitor is
        /// one query's, single-threaded.</summary>
        public bool StoredThroughWindow { get; private set; }

        /// <summary>Start the next question of the same visitor afresh (<see cref="StorePositionsAmong"/> asks one visitor of each site).</summary>
        public void Reset() { FirstHit = null; StoredThroughWindow = false; }
        private bool TargetHit(BoundSetTarget? t) => t is SetPlaceTarget sp ? Hit(sp.Place) : IndexHit(t as SetIndexTarget);

        /// <summary>Does the statement store through the index-name receiver <paramref name="t"/>, which the query asks (only
        /// <see cref="StorePositionsOf"/> and <see cref="StorePositionsAmong"/> do; the item queries ask of data items)?</summary>
        private bool IndexHit(SetIndexTarget? t) => t is not null && matchesIndex is not null && matchesIndex(t);
        /// <summary>The RECEIVER of a bound <c>… INTO</c> implicit move (kb/Work PB348 — identifier-1 now rides
        /// the move rather than the I-O node). Asked of the move's own target list, which is the same single
        /// receiver the node used to hold.</summary>
        private bool IntoHit(BoundMove? m) => m is not null && m.Targets.Any(Hit);
        private bool ReceiversHit(IReadOnlyList<Receiver> rs) => rs.Any(r => Hit(r.Place));

        /// <summary>Does the statement store, through the connector of <paramref name="file"/>, into a matching item among
        /// <paramref name="stores"/>? Only <see cref="StoresInto"/> passes a <c>connector</c>: an item the statement does not name
        /// is never a property temp, nor an operand the statement explicitly references (X3.23-1985 USE FOR DEBUGGING general
        /// rule 6 c).</summary>
        private bool ConnectorHit(FileModel file, params ReadOnlySpan<FileConnectorStore> stores)
        {
            if (connector is null) return false;
            foreach (var store in stores)
                foreach (var place in connector(file, store))
                    if (Hit(place)) return true;
            return false;
        }

        /// <summary>The FILE STATUS item of each of <paramref name="files"/>: the I-O status is set by CLOSE, DELETE, OPEN, READ,
        /// REWRITE, START, UNLOCK and WRITE (§2023:9.1.13.1), and the item is updated with it (§2023:12.4.5.8.4 GR1).</summary>
        private bool StatusHit(IEnumerable<FileModel> files) => connector is not null && files.Any(f => ConnectorHit(f, FileConnectorStore.Status));

        /// <summary>What a READ stores through the connector of <paramref name="file"/>, on both organizations' arms: the record
        /// area (§2023:14.9.30.4 GR13), the FILE STATUS item (every input-output statement, §2023:9.1.13.1) and the DEPENDING ON item
        /// (§2023:13.18.43.4 GR15: "after the successful execution of a READ or RETURN statement"); and the RELATIVE KEY item only
        /// for a SEQUENTIAL read (<paramref name="sequential"/>): §1985:VIII.4.5.4 GR14 moves the relative record number into it on
        /// "the execution of a format 1 READ statement", and §2023:14.9.30.4 GR25 is the same rule, while a random read takes the
        /// item as its input (kb/Work PB5885 review R1-1).</summary>
        private bool ReadHit(FileModel file, bool sequential) =>
            ConnectorHit(file, FileConnectorStore.RecordArea, FileConnectorStore.Status, FileConnectorStore.Depending)
            || (sequential && ConnectorHit(file, FileConnectorStore.RelativeKey));

        // The statements NESTED in this one — a conditional phrase's imperative statements, an IF or EVALUATE arm, an inline
        // PERFORM body: each is a statement of its own. A found store dominates (the temp occurs exactly once). A DIRECT query
        // (the debug triggers, X3.23-1985 USE FOR DEBUGGING general rule 7) asks the statement ITSELF and never descends here.
        private StoreKind Kids(params IEnumerable<BoundStatement>?[] lists) => direct ? StoreKind.None : Steps(lists);

        // The steps of a container that is one source statement made of several nodes (BoundSequence's operand-evaluation steps,
        // BoundImplicitSeries's members): always descended, whatever the query.
        private StoreKind Steps(params IEnumerable<BoundStatement>?[] lists)
        {
            foreach (var list in lists)
                foreach (var child in list ?? [])
                    if (child.Accept(this) is not StoreKind.None and var kind) return kind;
            return StoreKind.None;
        }

        private StoreKind StoreOrKids(bool stored, StoreKind kind, params IEnumerable<BoundStatement>?[] lists)
            => stored ? kind : Kids(lists);

        /// <summary>A whole-receiver store into any of <paramref name="targets"/> — the SET formats that assign an
        /// object reference, a pointer, a program- or function-pointer or a saved locale, each storing its receivers
        /// whole and reading none of them.</summary>
        private StoreKind WriteIfAny(IEnumerable<Place> targets) => targets.Any(Hit) ? StoreKind.Write : StoreKind.None;

        private bool InitStores(IReadOnlyList<InitializeAction> actions)
        {
            foreach (var a in actions)
                switch (a)
                {
                    case InitializeStore st when matches(st.Target): return true;
                    case InitializeStoreValue sv when matches(sv.Target): return true;
                    case InitializeSetNull sn when matches(sn.Target): return true;
                    // §2023:14.9.20.4 GR4/GR6b's implicit SET stores into its TARGET (identifier-2 is the sender).
                    case InitializeSetFrom sf when matches(sf.Target): return true;
                    case InitializeLoop lp when InitStores(lp.Body): return true;
                    // A Format-2 (table) VALUE select stores into the receiver on EVERY arm it has (ISO
                    // §2023:14.9.20.4 GR5c.1.c) — the arms differ only in which literal reaches it, so any one of them
                    // answering yes is a store into this item.
                    case InitializeOccurrenceSelect sel when InitStores(
                        [.. sel.Arms.Select(x => x.Do), .. sel.Otherwise is { } o ? new[] { o } : []]): return true;
                }
            return false;
        }

        // ── Pure control / read-only / model-routed statements (survey "pure" list) ─────────────────────
        public StoreKind Visit(BoundUnsupported n) => StoreKind.None;
        public StoreKind Visit(BoundDeferredToExpansion n) => StoreKind.None;   // the expansion's statement stores, not this
        public StoreKind Visit(BoundRejected n) => StoreKind.None;
        public StoreKind Visit(BoundStop n) => StoreKind.None;
        public StoreKind Visit(BoundStopLiteral n) => StoreKind.None;
        public StoreKind Visit(BoundDisplay n) => StoreKind.None;
        // The communication facility's statements read and write the CD's whole area, and RECEIVE keeps the receiving item's unfilled positions (kb/Work PB4677).
        public StoreKind Visit(BoundCommunicationStatement n) => StoreOrKids(Hit(n.Area) || Hit(n.Into), StoreKind.ReadWrite, n.NoData, n.WithData);
        // The messaging facility's statements (kb/Work PB4743): SEND Format 1 stores the server tag it RETURNS, RECEIVE reads and may set its tag, stores the message into identifier-1 (keeping the positions a shorter message leaves) and its length.
        public StoreKind Visit(BoundSendToServer n) => StoreOrKids(Hit(n.Returning), StoreKind.Write, n.OnException, n.NotOnException);
        public StoreKind Visit(BoundSendResponse n) => Kids(n.OnException, n.NotOnException);
        public StoreKind Visit(BoundReceiveMessage n) => Hit(n.From) | Hit(n.Into) ? StoreKind.ReadWrite : StoreOrKids(Hit(n.Length), StoreKind.Write, n.OnException, n.NotOnException);
        public StoreKind Visit(BoundGoTo n) => StoreKind.None;
        public StoreKind Visit(BoundGoToDepending n) => StoreKind.None;
        public StoreKind Visit(BoundExitParagraph n) => StoreKind.None;
        public StoreKind Visit(BoundExitSection n) => StoreKind.None;
        public StoreKind Visit(BoundExitPerform n) => StoreKind.None;
        public StoreKind Visit(BoundNop n) => StoreKind.None;
        public StoreKind Visit(BoundCommitRollback n) => StoreKind.None;   // CONTINUE-equivalent (kb/Work PB137)
        public StoreKind Visit(BoundContinueAfter n) => StoreKind.None;   // reads its interval expr; stores nothing
        public StoreKind Visit(BoundNextSentence n) => StoreKind.None;
        public StoreKind Visit(BoundOpen n) => StatusHit(n.Files.Select(f => f.File)) ? StoreKind.Write : StoreKind.None;
        public StoreKind Visit(BoundClose n) => StatusHit(n.Files.Select(f => f.File)) ? StoreKind.Write : StoreKind.None;
        public StoreKind Visit(BoundInitiate n) => StoreKind.None;
        public StoreKind Visit(BoundGenerate n) => StoreKind.None;
        public StoreKind Visit(BoundSuppress n) => StoreKind.None;
        public StoreKind Visit(BoundTerminate n) => StoreKind.None;
        public StoreKind Visit(BoundRaise n) => StoreKind.None;
        public StoreKind Visit(BoundResume n) => StoreKind.None;
        public StoreKind Visit(BoundSetLastException n) => StoreKind.None;
        public StoreKind Visit(BoundGoToAlterable n) => StoreKind.None;
        public StoreKind Visit(BoundCancel n) => StoreKind.None;
        public StoreKind Visit(BoundExitProgram n) => StoreKind.None;
        public StoreKind Visit(BoundGoback n) => StoreKind.None;
        public StoreKind Visit(BoundMethodReturn n) => StoreKind.None;
        public StoreKind Visit(BoundAlter n) => StoreKind.None;
        public StoreKind Visit(BoundSetSwitches n) => StoreKind.None;
        // DELETE names no data receiver (its INVALID KEY body is separately wrapped); it sets the FILE STATUS item.
        public StoreKind Visit(BoundKeyedDelete n) => StatusHit([n.File]) ? StoreKind.Write : StoreKind.None;
        public StoreKind Visit(BoundSort n) => SortTransferHit(n.Using, n.Giving) ? StoreKind.Write : StoreKind.None;
        public StoreKind Visit(BoundMerge n) => SortTransferHit(n.Using, n.Giving) ? StoreKind.Write : StoreKind.None;

        /// <summary>The USING files are read by implicit READ statements (§2023:14.9.40.4 GR9a), each storing what an explicit
        /// READ stores, and the GIVING files are written as if by WRITE statements, each setting the FILE STATUS item and, for a
        /// relative file, the RELATIVE KEY item.</summary>
        private bool SortTransferHit(IReadOnlyList<FileModel> usingFiles, IReadOnlyList<FileModel> givingFiles) =>
            connector is not null
            && (usingFiles.Any(f => ReadHit(f, sequential: true)) || givingFiles.Any(f => ConnectorHit(f, FileConnectorStore.Status, FileConnectorStore.RelativeKey)));
        // The table SORT reads the table and places its occurrences back (ISO/IEC 1989:2023 §2023:14.9.40.4 GR24): data-name-2 and every
        // key data-name it writes are changed in place (kb/Work PB4855). Its storage is reached through n.Storage, the same table.
        public StoreKind Visit(BoundTableSort n)
        {
            bool hit = Hit(n.Written);
            foreach (var key in n.Keys) hit |= Hit(key.Written);
            return hit ? StoreKind.ReadWrite : StoreKind.None;
        }
        public StoreKind Visit(BoundUnlock n) => StatusHit([n.File]) ? StoreKind.Write : StoreKind.None;

        // ── Wrappers / containers ───────────────────────────────────────────────────────────────────────
        public StoreKind Visit(BoundSequence n) => Steps(n.Steps);
        // A multi-operand verb's implicit statements (ISO §2023:14.9.20.4 GR3 and siblings — BoundImplicitSeries):
        // a container exactly like BoundSequence for this query, since any member may carry the store.
        public StoreKind Visit(BoundImplicitSeries n) => Steps(n.Members);
        public StoreKind Visit(BoundEcChecked n) => n.Inner.Accept(this);
        public StoreKind Visit(BoundDebugTriggered n) => n.Inner.Accept(this);   // the USE FOR DEBUGGING wrapper is the same statement (D20)
        public StoreKind Visit(BoundActivationSite n) => n.Inner.Accept(this);
        public StoreKind Visit(BoundReceiverBrackets n) => n.Inner.Accept(this);   // the brackets' accessors are not stores of the statement's receivers
        public StoreKind Visit(BoundIf n) => Kids(n.Then, n.Else);
        public StoreKind Visit(BoundEvaluate n) => Kids([.. n.Whens.SelectMany(w => w.Statements)], n.Other);
        public StoreKind Visit(BoundInlinePerform n) => StoreOrKids(
            n.Control is PerformVarying pv && pv.Levels.Any(l => TargetHit(l.Var)),
            StoreKind.ReadWrite, n.Body);   // induction var: init + augment (GR12/GR13)
        public StoreKind Visit(BoundOutOfLinePerform n) =>
            n.Control is PerformVarying pv && pv.Levels.Any(l => TargetHit(l.Var))
                ? StoreKind.ReadWrite : StoreKind.None;
        public StoreKind Visit(BoundExceptionPerform n) => Kids(n.Imp1, n.FinallyBody);   // imp-2/3/4 are
        // appended pc-range paragraphs (analyzed at their own nodes); only imp-1 + FINALLY are inline here.

        // ── Data movement / arithmetic (polarity per the survey: in-place vs WRITE-only) ────────────────
        public StoreKind Visit(BoundMove n) => n.Targets.Any(Hit) ? StoreKind.Write : StoreKind.None;
        public StoreKind Visit(BoundAddTo n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.ReadWrite, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind Visit(BoundAddGiving n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind Visit(BoundSubtractFrom n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.ReadWrite, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind Visit(BoundSubtractGiving n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind Visit(BoundMultiplyBy n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.ReadWrite, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind Visit(BoundMultiplyGiving n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind Visit(BoundDivideInto n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.ReadWrite, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind Visit(BoundDivideGiving n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind Visit(BoundDivideRemainder n) => StoreOrKids(Hit(n.Quotient.Place) || Hit(n.Remainder), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind Visit(BoundCompute n) => StoreOrKids(ReceiversHit(n.Targets), StoreKind.Write, n.SizeError?.OnError, n.SizeError?.NotOnError);
        public StoreKind Visit(BoundComputeBoolean n) => n.Targets.Any(Hit) ? StoreKind.Write : StoreKind.None;   // §2023:14.9.8 F2 — no size-error phrase
        public StoreKind Visit(BoundPositionValue n) => Hit(n.Temp) ? StoreKind.Write : StoreKind.None;
        // The operand view asks the receiving group the statement wrote; the storage view asks the receiving item of each corresponding pair,
        // the only items the statement stores (§1985:VI.6.19.4 GR1; ADD and SUBTRACT CORRESPONDING store their numeric pairs alike), so an item
        // of the group with no corresponding item keeps its value (kb/Work PB5890).
        public StoreKind Visit(BoundCorresponding n) => StoreOrKids(
            view is StoreView.Operands ? Hit(n.ReceivingGroup) : n.Pairs.Any(p => Hit(p.Target)),
            n.Verb == CorrVerb.Move ? StoreKind.Write : StoreKind.ReadWrite,
            n.SizeError?.OnError, n.SizeError?.NotOnError);
        // The operand view asks identifier-1 as written (INITIALIZE G changes G, which no elementary store of the expansion names; the debug
        // triggers stop there: a group reference does not reference or change its subordinates, X3.23-1985 USE FOR DEBUGGING rule 9). The storage
        // view asks the expansion's stores, which are the items the statement initializes after its category selection, its exclusions and its
        // default (§1985:VI.6.17.4 GR2 to GR5), so an item of another category than REPLACING names keeps its value (kb/Work PB5890).
        public StoreKind Visit(BoundInitialize n) =>
            (view is StoreView.Operands ? Hit(n.Written) : InitStores(n.Actions)) ? StoreKind.Write : StoreKind.None;

        // VALIDATE reads its operand and stores nothing in stage one (§2023:14.9.50.4 GR5: the content of an invalid item does not change); DEFAULT and DESTINATION (stages S2) will.
        public StoreKind Visit(BoundValidate n) => StoreKind.None;

        // ── SET family ──────────────────────────────────────────────────────────────────────────────────
        public StoreKind Visit(BoundSetConditions n) => n.Sets.Any(x => Hit(x.Parent)) ? StoreKind.Write : StoreKind.None;
        public StoreKind Visit(BoundSetTo n) => n.Targets.Any(TargetHit) ? StoreKind.Write : StoreKind.None;
        public StoreKind Visit(BoundSetUpDown n) => n.Targets.Any(TargetHit) ? StoreKind.ReadWrite : StoreKind.None;
        public StoreKind Visit(BoundSetSize n) => Hit(n.Target) ? StoreKind.ReadWrite : StoreKind.None;   // reads current content, writes resized
        // Format 15 writes each receiver whole and reads none of them (§2023:14.9.39.4 GR32-GR36 — every value
        // is a property of the DESCRIPTION, never of the current content).
        public StoreKind Visit(BoundSetContent n) => n.Stores.Any(st => Hit(st.Target)) ? StoreKind.Write : StoreKind.None;

        // ── SEARCH ──────────────────────────────────────────────────────────────────────────────────────
        public StoreKind Visit(BoundSearch n) => StoreOrKids(TargetHit(n.AlsoVaried) || IndexHit(n.VaryingIndex), StoreKind.ReadWrite,
            n.AtEnd, [.. n.Whens.SelectMany(w => w.Statements)]);

        // ── ACCEPT / STRING / UNSTRING / INSPECT ────────────────────────────────────────────────────────
        public StoreKind Visit(BoundAccept n) => Hit(n.Target) ? StoreKind.Write : StoreKind.None;
        // identifier-4 (POINTER) is read (GR4) and written back, so it is read-modify-write. identifier-3 (INTO) is
        // a RECEIVING operand (§2023:14.9.43.3 SR10 — "The data item referenced by identifier-3 is the receiving
        // operand"), so a property WITH NO GET is legal there (§2023:8.4.3.9.3 SR3 asks for a GET only of a SENDING use;
        // classifying INTO as read-modify-write demanded one the rule does not — kb/Work PB1275). It is receiving
        // IN PART, though (GR7, below): WriteInPart takes the get method where the class has one.
        // STRING changes "only the portion of the data item referenced by identifier-3 that was referenced during the
        // execution" (§2023:14.9.43.4 GR7), so the item's earlier content outside that portion survives and the receiving
        // reference is WriteInPart: its get method (if the class has one) supplies that content (kb/Work PB2618).
        public StoreKind Visit(BoundStringStmt n) =>
            Hit(n.Pointer) ? StoreKind.ReadWrite
            : StoreOrKids(Hit(n.Into), StoreKind.WriteInPart, n.OnOverflow, n.NotOnOverflow);
        public StoreKind Visit(BoundUnstringStmt n) =>
            Hit(n.Pointer) || Hit(n.Tallying)
                ? StoreKind.ReadWrite                                   // GR11a/GR13 + GR14 read-then-add
                : StoreOrKids(n.Receivers.Any(r => Hit(r.Target) || Hit(r.DelimiterIn) || Hit(r.CountIn)),
                    StoreKind.Write, n.OnOverflow, n.NotOnOverflow);    // GR11c/d/e pure stores
        // identifier-1 is an OPERAND since PB10. A function-identifier target has no Place, so it can never be a
        // STORE — which is not a special case here but the same rule stated twice: the guard below already
        // requires REPLACING/CONVERTING, and §2023:8.4.3.2.3 SR1 bars a function-identifier from exactly those.
        public StoreKind Visit(BoundInspect n) =>
            (n.Replacing.Count > 0 || n.Converting is not null) && Hit((n.Target as BoundFieldOperand)?.Place)
                ? StoreKind.ReadWrite                                   // image read, modified, stored
                : n.Tallying.Any(tl => Hit(tl.Counter))
                    ? StoreKind.ReadWrite                               // GR11 counter accumulate
                    : StoreKind.None;

        // ── File I/O (FILE STATUS / record-area stores route through FileModel — never the temp) ─────────
        // The conditional-phrase recursion is TOTAL over the node's phrase bodies, so the sequential WRITE's
        // §2023:9.1.14 pair (bound under --permissive only — kb/Work PB691) is walked exactly as BoundKeyedWrite's is;
        // the two arms of one verb may not disagree about which bodies exist (feedback_two_arm_dispatch).
        public StoreKind Visit(BoundWrite n) => StoreOrKids((n.FromMove is not null && Hit(n.Record)) || StatusHit([n.File]),
            StoreKind.ReadWrite, n.AtEop, n.NotAtEop,                  // FROM-move then read as the image
            n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);          // the --permissive pair (PB691)
        // The INVALID KEY lists are walked on BOTH READ arms. They are §2023:14.9.30.2 Format-2 phrases and the
        // sequential arm reports COBOL1720 for them, but --permissive leaves the bind standing and
        // §2023:14.9.30.4 GR13c then RUNS the NOT INVALID KEY imperative — a store inside it is a real store, and
        // omitting the lists here is the same two-arm disagreement kb/Work PB334 was about.
        // A READ stores its INTO operand and, through the connector, what ReadHit names: the statement names only the file
        // (kb/Work PB5885).
        public StoreKind Visit(BoundRead n) => StoreOrKids(IntoHit(n.IntoMove) || ReadHit(n.File, sequential: true), StoreKind.Write,
            n.AtEnd, n.NotAtEnd, n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);
        public StoreKind Visit(BoundRewrite n) => StoreOrKids((n.FromMove is not null && Hit(n.Record)) || StatusHit([n.File]),
            StoreKind.ReadWrite, n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);   // the --permissive pair (PB691)
        public StoreKind Visit(BoundKeyedRead n) => StoreOrKids(IntoHit(n.IntoMove) || ReadHit(n.File, sequential: n.Kind != ReadKind.Random), StoreKind.Write,
            n.AtEnd, n.NotAtEnd, n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);
        // A sequential-access WRITE of a relative file moves the number of the record released into the RELATIVE KEY item
        // (§2023:14.9.51.4 GR29a); under random or dynamic access the item is the WRITE's input (GR29 b)).
        public StoreKind Visit(BoundKeyedWrite n) => StoreOrKids((n.FromMove is not null && Hit(n.Record)) || StatusHit([n.File])
                || (n.File.AccessMode == FileAccessMode.Sequential && ConnectorHit(n.File, FileConnectorStore.RelativeKey)),
            StoreKind.ReadWrite, n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);
        public StoreKind Visit(BoundKeyedRewrite n) => StoreOrKids((n.FromMove is not null && Hit(n.Record)) || StatusHit([n.File]),
            StoreKind.ReadWrite, n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);
        public StoreKind Visit(BoundKeyedDeleteFile n) => StoreOrKids(StatusHit([n.File]), StoreKind.Write, n.OnException, n.NotOnException);
        public StoreKind Visit(BoundKeyedStart n) => StoreOrKids(StatusHit([n.File]), StoreKind.Write, n.InvalidKey?.Invalid, n.InvalidKey?.NotInvalid);
        public StoreKind Visit(BoundRelease n) => n.FromMove is not null && Hit(n.Record) ? StoreKind.ReadWrite : StoreKind.None;
        // RETURN makes the record available in the record area (§2023:14.9.34.4 GR3) and sets the DEPENDING ON item
        // (§2023:13.18.43.4 GR15) through the connector, as READ does; an SD has no FILE STATUS or RELATIVE KEY item.
        public StoreKind Visit(BoundReturn n) => StoreOrKids(
            IntoHit(n.IntoMove) || ConnectorHit(n.File, FileConnectorStore.RecordArea, FileConnectorStore.Depending),
            StoreKind.Write, n.AtEnd, n.NotAtEnd);

        // ── CALL / INVOKE (BY REFERENCE crossings: copy-in + writeback = ReadWrite) ──────────────────────
        public StoreKind Visit(BoundCallProgram n) =>
            crossingsStore && n.Args.Any(a => a.Mode == CobolPassMode.Reference && Hit(a.Place))
                ? StoreKind.ReadWrite
                : StoreOrKids(Hit(n.Returning), StoreKind.Write, n.OnException, n.NotOnException);
        public StoreKind Visit(BoundInvoke n) =>
            crossingsStore && (n.Args?.Any(a => a.WriteBack && Hit(a.Source)) ?? false) ? StoreKind.ReadWrite
            : Hit(n.Returning) ? StoreKind.Write
            : StoreKind.None;

        // The universal (D10) INVOKE: every identifier-3 crosses BY REFERENCE (§2023:14.9.23.3 SR6) and is copied back,
        // exactly the typed path's write-back crossing; the RETURNING item is stored whole; the receiver and an
        // identifier-2 selector are only read.
        public StoreKind Visit(BoundInvokeUniversal n) =>
            crossingsStore && n.Args.Any(a => Hit(a.Source)) ? StoreKind.ReadWrite
            : Hit(n.Returning) ? StoreKind.Write
            : StoreKind.None;

        // INVOKE NULL (§2023:14.9.23.4 GR5): the statement is terminated before any argument crosses or any result is
        // delivered, so it stores nothing.
        public StoreKind Visit(BoundInvokeNull n) => StoreKind.None;

        // ── Pointer, object-reference and carrier stores (kb/Work PB1275 — these answered null, "outside the
        //    taxonomy", until every Place they carry was classified). Each SET format below stores its receivers
        //    whole and reads only its sender (§2023:14.9.39.4); its sender, a FREE / RAISE operand's read, an ALLOCATE
        //    size and a SET amount are SENDING occurrences. ──────────────────────────────────────────────────────
        public StoreKind Visit(BoundSetObjectRef n) => WriteIfAny(n.Targets);           // Format 5 (GR9/GR10)
        public StoreKind Visit(BoundObjectView n) => Hit(n.Target) ? StoreKind.Write : StoreKind.None;   // the view's temporary (§2023:8.4.3.5.4); identifier-1 is only read
        public StoreKind Visit(BoundSelfReference n) => Hit(n.Target) ? StoreKind.Write : StoreKind.None;   // SELF's temporary (§2023:8.4.3.8.4 GR1)
        public StoreKind Visit(BoundSetPointer n) =>                                     // Format 7 (GR12)
            WriteIfAny(n.Receivers.Select(r => r.Pointer).OfType<Place>());
        public StoreKind Visit(BoundSetProgramPointer n) => WriteIfAny(n.Targets);      // Format 9 (GR16)
        public StoreKind Visit(BoundSetEntry n) => WriteIfAny(n.Targets);               // Format 9, ENTRY sender (§2023:8.4.3.13)
        public StoreKind Visit(BoundSetFunctionPointer n) => WriteIfAny(n.Targets);     // Format 8 (GR14; kb/Work PB452)
        public StoreKind Visit(BoundSetFunctionAddress n) => WriteIfAny(n.Targets);     // Format 8, §2023:8.4.3.12 sender
        public StoreKind Visit(BoundSetMessageTag n) => WriteIfAny([n.Target]);         // Format 17 (GR40/GR41; kb/Work PB4740)
        public StoreKind Visit(BoundSaveLocale n) => Hit(n.Target) ? StoreKind.Write : StoreKind.None;   // Format 12 (GR26/GR27)
        public StoreKind Visit(BoundSetLocale n) => StoreKind.None;                    // Format 11 reads a saved pointer
        // Format 10: the address is moved BY the amount (GR20), so the receiver is read and written.
        public StoreKind Visit(BoundSetPointerUpDown n) => n.Targets.Any(Hit) ? StoreKind.ReadWrite : StoreKind.None;
        // ALLOCATE's RETURNING pointer is stored whole (§2023:14.9.3.4 GR2/GR4); the BASED item's own implicit pointer is
        // not a Place and so never a property temp.
        public StoreKind Visit(BoundAllocate n) => Hit(n.Returning) ? StoreKind.Write : StoreKind.None;
        // FREE reads each pointer to release its storage and then sets it to NULL (§2023:14.9.15.4 GR1a).
        public StoreKind Visit(BoundFree n) => n.Operands.Any(Hit) ? StoreKind.ReadWrite : StoreKind.None;
        public StoreKind Visit(BoundRaiseObject n) => StoreKind.None;                  // RAISE identifier-1 is sent
        // SET capacity names a dynamic-capacity TABLE (an AccessPath, never a Place) — and a property subject cannot
        // carry OCCURS (COBOL0842) — so it can store into no property temp.
        public StoreKind Visit(BoundSetCapacity n) => StoreKind.None;
        public StoreKind Visit(BoundHoldTableAll n) => StoreOrKids(Hit(n.Count), StoreKind.Write, [n.Copy]);   // writes its element temporaries (the Copy child) and the count
    }
}
