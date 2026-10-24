// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Runtime.Exceptions;

namespace CobolNet.Runtime.IO;

/// <summary>
/// The sort/merge statement runtime (ISO/IEC 1989:2023 §14.9.40 SORT / §14.9.24 MERGE): one store per sort-merge (SD)
/// file, keyed by COBOL file-name, holding the statement's procedure phase (<see cref="SortStore"/>) and its records
/// (<see cref="SortWork"/> — an external merge sort in BOUNDED memory, never the whole file: kb/Work PB3339,
/// docs/CONFORMANCE.md §3 D-SWK). Records cross this boundary as their character image (a <see cref="string"/> at the
/// record's released length) — the typed record ↔ image conversion stays in the generated code, exactly like
/// <see cref="CobolFile"/> (the substrate stays typed; COBOLNET_DESIGN §8.2: the sort store holds serialized images,
/// key windows are computed at compile time). The compiler emits
/// <c>Init(keys) → {Release… | NextInput+Release…} → Sort|Merge → {Return… (Rewind per GIVING file)} → Close</c>
/// around each SORT/MERGE statement — the three GR9 phases. The keys come with <c>Init</c> because the store orders
/// records while the release phase runs, whenever they outgrow the memory limit.
/// </summary>
public static class CobolSort
{
    /// <summary>⛔ THE COMPARISON CLASS of one key — the ONE thing that selects a key's comparator (ISO
    /// §14.9.40.4 GR5 / §14.9.24.4 GR5: "The alphanumeric collating sequence that applies to the comparison of key
    /// data items of class alphabetic and class alphanumeric, and the national collating sequence that applies to
    /// the comparison of key data items of class national, are each separately determined …" — TWO sequences,
    /// chosen by the KEY's class, and neither of them names class numeric or class boolean). GR8/GR19 defer the
    /// comparison itself to the relation-condition rules, so each arm is one of those clauses:
    /// §8.8.4.2.7 alphanumeric, §8.8.4.2.9 national, §8.8.4.2.8 boolean (by VALUE, never collated),
    /// §8.8.4.2.4 numeric (algebraic, never collated).</summary>
    public enum KeyClass
    {
        /// <summary>Class alphabetic / alphanumeric, and every GROUP key (§8.8.4.2.1 makes a group operand
        /// class alphanumeric): the byte window compared under the GR5 alphanumeric sequence.</summary>
        Alphanumeric,

        /// <summary>Class national (§8.8.4.2.9): the window's UTF-16BE byte PAIRS decode back to national
        /// character positions (§13.18.60.4 GR8, D-N1) and compare under the GR5 NATIONAL sequence.</summary>
        National,

        /// <summary>Class boolean (§8.8.4.2.8 — "a comparison of their boolean value, regardless of their usage"):
        /// the packed window compares position-wise with NO collating sequence.</summary>
        Boolean,

        /// <summary>Class numeric (§8.8.4.2.4): the window decodes through the key's own <see cref="NumProfile"/>
        /// and compares ALGEBRAICALLY — a collating sequence never applies.</summary>
        Numeric,
    }

    /// <summary>One compile-time key descriptor (ISO §14.9.40.3 SR6a/SR6e — keys are fixed BYTE windows of
    /// the SD record; the same positions are the key in EVERY record): the window (<paramref name="Offset"/>,
    /// <paramref name="Length"/>, both in bytes — a national position is two of them), the direction (GR8a/b),
    /// and the <see cref="KeyClass"/> that selects the comparator.
    /// <para>⛔ A KEY THAT FOLLOWS A VARIABLE-LENGTH MEMBER (kb/Work PB1025; docs/CONFORMANCE.md §3 D-KWV) carries
    /// its record type's <paramref name="Layout"/>, and <paramref name="Offset"/> is then its offset in the record's
    /// FIXED run: the record image the store holds is contiguous (§8.5.1.11.2), so the key's byte positions in THIS
    /// record are found by the same take step that decomposes it (<see cref="CobolContiguousLayout.Position"/>).
    /// Null for every key no variable-length member precedes — its window is the same in every record.</para></summary>
    public readonly record struct Key(int Offset, int Length, bool Descending, KeyClass Class, NumProfile Profile,
        CobolContiguousLayout? Layout = null)
    {
        /// <summary>The key's first byte position in <paramref name="image"/> — located through the record's own
        /// EXTENT TABLE when it was released with one that describes it (D-FRA (v); kb/Work PB1053), exactly where a
        /// RETURN of the record decomposes it.</summary>
        public int At(string image, RecordExtents? extents = null) =>
            Layout is { } l ? l.Position(image, Offset, extents) : Offset;
    }

    /// <summary>⛔ THE SORT/MERGE STATEMENT'S OWN RAISES ARE MARKED HERE (kb/Work PB1036). The six entries the
    /// statement itself executes — <see cref="Init(string, Key[], CobolCollation?, CobolCollation?)"/>, <see cref="FileNotOpen"/>,
    /// the implicit <see cref="Release(string, string, int, int, int, RecordExtents?)"/>, <see cref="Sort"/>,
    /// <see cref="Merge"/> and the implicit <see cref="Return"/> — run their bodies under this exception FILTER, which
    /// sets <see cref="CobolFatalException.RaisedBySortMerge"/> and declines the catch, so the condition unwinds with its
    /// stack intact. ISO §14.6.13.1.3 2) ("If the executed statement is a MERGE or SORT statement, then the rules for
    /// those statements apply") keys on the statement that EXECUTED, and only this code knows it: an EC-SORT-MERGE-*
    /// condition, but equally an EC-LOCALE-* condition a locale-collated key raises while the sequence phase compares
    /// records. The RELEASE and RETURN statements' OWN tests are deliberately not marked — the executed statement there
    /// is the RELEASE or the RETURN — and neither is anything raised by a statement of an input or output procedure.
    /// ⛔ But the SEQUENCING those statements trigger is marked (kb/Work PB3339): a RELEASE that fills the memory limit
    /// makes the store sort a run, and a RETURN reads the merge of runs, and both are the SORT's or MERGE's own sequence
    /// operation done incrementally (<see cref="SortWork"/>), not the RELEASE's or the RETURN's.</summary>
    private static bool RaisedByTheStatement(CobolFatalException e)
    {
        e.RaisedBySortMerge = true;
        return false;
    }

    /// <summary>The ambient run unit's sort-merge stores (kb/Work PB1570) — run-unit state, never process state.</summary>
    private static SortFileTable Stores => RunUnit.Current.SortFiles;

    private static SortStore Get(string name) => Stores.Get(name);

    /// <summary>Begin a SORT/MERGE statement on <paramref name="name"/>: a fresh, empty store (ISO §14.9.40 GR9a — the
    /// release phase starts; re-executing a SORT on the same SD reuses the connector with a clean store), ordered by
    /// <paramref name="keys"/> — the compile-time key descriptors (§14.9.40.3 SR6e), given here rather than to the
    /// sequence phase because the store orders a run whenever the release phase fills its memory limit (kb/Work
    /// PB3339) — and by the statement's TWO GR5-resolved COLLATING SEQUENCES (the statement's own
    /// alphabet-name-1 / alphabet-name-2, else the program collating sequences, else null = native): each is
    /// SNAPSHOTTED here, at statement start (ISO §14.6.6 r5 — "A locale switch during execution of a SORT or MERGE
    /// statement has no effect on the processing of that SORT or MERGE statement" — a SET LOCALE in an INPUT
    /// PROCEDURE must not move the sequence the sort phase uses), and <see cref="Sort"/> / <see cref="Merge"/> use
    /// the snapshots. §14.9.40.4 GR5 determines the two SEPARATELY: <paramref name="collation"/> applies to key
    /// data items of class alphabetic and alphanumeric, <paramref name="national"/> to those of class national.
    /// <para>⛔ EC-SORT-MERGE-ACTIVE is tested HERE, before the store is touched (kb/Work PB1036): §14.9.40.4 GR10
    /// ("If the range of the input procedure causes the execution of any MERGE, RETURN, or format 1 SORT
    /// statements, the EC-SORT-MERGE-ACTIVE exception condition is set to exist"), GR13 and §14.9.24.4 GR8 (the
    /// output procedure's MERGE, RELEASE or format 1 SORT) all forbid a SORT/MERGE while any procedure of an
    /// executing SORT/MERGE runs — whatever file it names, in THIS run unit (kb/Work PB1570). A raise therefore leaves an enclosing statement on the
    /// same file intact. With checking off the results are undefined (GR10) and the statement runs.</para></summary>
    public static void Init(string name, Key[] keys, CobolCollation? collation = null, CobolCollation? national = null)
    {
        try { InitCore(name, keys, collation, national); }
        catch (CobolFatalException e) when (RaisedByTheStatement(e)) { }
    }

    /// <summary>Register the statement that has just begun on <paramref name="name"/> as a FORMAT 1 SORT, with the
    /// declarative scope (<c>__useActive</c>) of the program instance or method invocation executing it — null when that
    /// scope has no EC-model declaratives. Emitted first in the body of EVERY format 1 SORT: §14.9.40.4 GR17 terminates
    /// the active SORT when ANY USE procedure invoked while it is active does not complete normally
    /// (<see cref="AfterUseProcedure"/>; kb/Work PB1177), and the procedure may be another element's — a CALLed program's
    /// or a containing program's GLOBAL one — whatever declaratives the SORT's own scope has. A MERGE registers none — its
    /// rules (§14.9.24.4) carry no such sentence. <see cref="Init(string, Key[], CobolCollation?, CobolCollation?)"/> clears
    /// the registration, so a store reused by the next statement on the file starts with none.</summary>
    public static void RegisterFormat1Sort(string name, object? useScope) =>
        Get(name).Format1 = new Format1SortRegistration(useScope, Stores.NextOrdinal());

    private static void InitCore(string name, Key[] keys, CobolCollation? collation, CobolCollation? national)
    {
        if (ExceptionState.SortMergeActiveChecking && Stores.AnyInPhase([ProcedurePhase.Input, ProcedurePhase.Output]))
            ExceptionState.SortMergeActiveError($"SORT/MERGE on {name}: executed within the range of an input or "
                + "output procedure of an executing SORT/MERGE statement (ISO §14.9.40.4 GR10 / GR13, §14.9.24.4 GR8)");
        var f = Get(name);
        f.Begin(new SortWork(Stores.Memory, keys, collation?.Snapshot(), national?.Snapshot()));
        f.LastReturnedLength = 0;
        f.LastReturnedExtents = null;
        f.Phase = ProcedurePhase.None;
        f.Format1 = null;
        f.AtEndReached = false;
    }

    /// <summary>Enter the SORT's INPUT PROCEDURE (<paramref name="output"/> false) or the SORT/MERGE's OUTPUT
    /// PROCEDURE (true) — emitted immediately before the bounded dispatch of the procedure range. The input phase
    /// ends when <see cref="Sort"/> begins the sequence phase; the output phase ends at <see cref="Close"/>.</summary>
    public static void EnterProcedure(string name, bool output)
    {
        var f = Get(name);
        f.Phase = output ? ProcedurePhase.Output : ProcedurePhase.Input;
        f.AtEndReached = false;
    }

    /// <summary>⛔ §14.9.40.4 GR17, ASKED AFTER EVERY DECLARATIVE (kb/Work PB1177): "If a USE procedure invoked while a
    /// format 1 SORT statement is active does not complete normally, the SORT statement is terminated." The generated
    /// <c>__RunUse</c> of every EC-model element hands every DECLARATIVE's action here, with its own declarative scope
    /// (<paramref name="useScope"/>), once the declarative has finished. When the action is a completion that is not
    /// normal (<see cref="DispatchResult.CompletedNotNormally"/>: a RESUME, or a fall-off §14.6.13.1.2 1) marks) and a
    /// format 1 SORT is active in the run unit (<see cref="RegisterFormat1Sort"/>), the SORT is terminated:
    /// <see cref="SortTerminationSignal"/> unwinds to the statement's landing, whatever invoked the procedure — an
    /// implicit USING/GIVING transfer, a statement of the input or output procedure, a statement of a program CALLed or a
    /// method INVOKEd from either, or another USE procedure running within any of them (GR10, GR13: the range includes
    /// "all statements that are executed as the result of a transfer of control in the range" and "all statements in
    /// declarative procedures that are executed as a result of the execution of statements in the range"). The whole
    /// range ends with the statement, so the activations it entered end too (their boundaries unwind:
    /// <c>ProgramTable.Activate</c>, the method activation's <c>finally</c>).
    /// <para>⚠ DETERMINATION (docs/CONFORMANCE.md §3, kb/Work PB1177): the landing carries the action only for the
    /// SORT's OWN declaratives (the registered scope). Another element's RESUME AT procedure-name names a procedure of an
    /// activation the termination has ended, so it lands as <see cref="DispatchResult.NotNormal"/>, at the statement's
    /// end. Otherwise — no SORT active, or a normal completion — the action goes back to the raise site unchanged.</para></summary>
    public static int AfterUseProcedure(object useScope, int action)
    {
        if (!DispatchResult.CompletedNotNormally(action) || Stores.ActiveFormat1Sort() is not { } active)
            return action;
        var (name, sort) = active;
        throw new SortTerminationSignal(name, ReferenceEquals(sort.UseScope, useScope) ? action : DispatchResult.NotNormal);
    }

    /// <summary>⛔ §14.9.40.4 GR17 FOR A USE PROCEDURE THAT ENDS IN EXIT PROGRAM OR GOBACK (kb/Work PB1177) — a
    /// completion that is not normal (§14.6.13.1.2 1)) that leaves the declarative by its own signal, so
    /// <see cref="AfterUseProcedure"/> never sees it. The generated <c>__RunUse</c> asks this as a declining exception
    /// FILTER on the element's return signal, with its declarative scope (<paramref name="useScope"/>); always false.
    /// When the active format 1 SORT is the element's OWN (its registered scope), the return leaves the SORT statement
    /// through its <c>finally</c>, which ends it — nothing to record. When it is another element's — one whose range
    /// CALLed or INVOKEd this one — the return goes back to the activator inside that range, so the SORT is terminated
    /// when the activation has returned (<c>SortFileTable.AfterActivationReturn</c>).</summary>
    public static bool UseProcedureExited(object useScope)
    {
        if (Stores.ActiveFormat1Sort() is { } active && !ReferenceEquals(active.Registration.UseScope, useScope))
            Stores.TerminateOnActivationReturn(active.Name);
        return false;
    }

    /// <summary>The activating site's §14.9.40.4 GR17 question once the return has completed, §14.9.23.4 GR8 has placed
    /// the result and the propagation pickup has run - see <c>SortFileTable.AfterActivationReturn</c> (kb/Work PB1177,
    /// PB3246); a CALL, an INVOKE and the host boundary all ask it.</summary>
    public static void AfterActivationReturn() => Stores.AfterActivationReturn();

    /// <summary>The RELEASE STATEMENT (ISO §14.9.32.4). GR1: "A RELEASE statement may be executed only when it is
    /// within the range of an input procedure being executed by a SORT statement that references the file-name
    /// associated with record-name-1. If it is executed at any other time, the EC-FLOW-RELEASE exception condition
    /// is set to exist." The test precedes the release, so a raise leaves the record unreleased; with checking off
    /// nothing is raised (§14.6.13.1.1) and the record is released as GR2 describes — a store with no executing
    /// SORT is discarded by the next <see cref="Init(string, Key[], CobolCollation?, CobolCollation?)"/>. The implicit
    /// USING transfer (§14.9.40.4 GR12 b) is not a RELEASE statement and uses <see cref="Release"/> (kb/Work PB349).
    /// <para>Two more tests precede the release (kb/Work PB1036). <b>EC-SORT-MERGE-ACTIVE</b>: a RELEASE within
    /// the range of any executing output procedure (§14.9.40.4 GR13, §14.9.24.4 GR8). ⚠ DETERMINATION: the same
    /// RELEASE also fails GR1, and §14.6.13.1.1 leaves which one is set to exist undefined ("if more than one
    /// exception is detected during the execution of a statement, the one that is set to exist is undefined");
    /// -ACTIVE, the rule written for exactly this range, is tested first, and GR1's general "any other time" only
    /// when -ACTIVE is not raised (docs/CONFORMANCE.md §3 D-SMA). <b>EC-SORT-MERGE-RELEASE</b>: the record's
    /// size — <paramref name="size"/>, the §13.18.43.4 GR13 a) DEPENDING ON value, else the image's own length
    /// (GR13 b)/c)) — outside <paramref name="min"/>..<paramref name="max"/>: "If a RELEASE statement is being
    /// executed, the EC-SORT-MERGE-RELEASE exception condition is set to exist and the execution of the RELEASE
    /// statement is unsuccessful" (§13.18.43.4 GR14 b) / GR19 b)), so a raise releases nothing. The record is
    /// sliced to <paramref name="size"/> HERE, after the test, never by an emitted reference modification — a
    /// DEPENDING ON value past the record area would otherwise raise EC-BOUND-REF-MOD, a condition no rule of the
    /// RELEASE names. With checking off the record releases at <paramref name="size"/> exactly as the lenient
    /// reference modification sliced it before (<see cref="Released"/>).</para></summary>
    /// <param name="extents">The record's EXTENT TABLE when a variable-length group record is released
    /// (determination D-FRA (v); kb/Work PB1053) — it travels with the record through the sort, as a file frame
    /// carries it.</param>
    /// <returns><see langword="true"/> when the record was released to the sort file — the condition this
    /// implementation gates the "also available as a record of other files referenced in the same SAME RECORD AREA
    /// clause" store on (§14.9.32.4 GR3 states no success condition of its own; WRITE and REWRITE name one,
    /// §14.9.51.4 GR4 and §14.9.35.4 GR6 — kb/Work PB1195); <see langword="false"/> when no SORT is executing and the
    /// record is discarded.</returns>
    public static bool ReleaseStatement(string name, string image, int min, int max, int? size = null,
        RecordExtents? extents = null)
    {
        if (ExceptionState.SortMergeActiveChecking && Stores.AnyInPhase([ProcedurePhase.Output]))
            ExceptionState.SortMergeActiveError($"RELEASE for sort file {name}: executed within the range of an "
                + "output procedure of an executing SORT/MERGE statement (ISO §14.9.40.4 GR13, §14.9.24.4 GR8)");
        var f = Stores.Find(name);
        if (f is null || f.Phase != ProcedurePhase.Input)
            ExceptionState.FlowReleaseError($"RELEASE for sort file {name}: not within the range of an input "
                + "procedure being executed by a SORT statement that references it (ISO §14.9.32.4 GR1)");
        image ??= "";
        int bytes = size ?? image.Length;
        if (bytes < min || bytes > max)
            ExceptionState.SortMergeReleaseError($"RELEASE for sort file {name}: a {bytes}-byte record is outside "
                + $"the record range {min} to {max} (ISO §13.18.43.4 GR14 b) / GR19 b))");
        if (f is null) return false;   // no SORT executing: there is no sort file to release to — never a stranded store
        var frame = new StoredFrame(size is { } s ? Released(image, s) : image, extents);
        try { f.Work.Add(frame); }   // a full chunk is sorted into a run here: the SORT's sequencing, marked as its own
        catch (CobolFatalException e) when (RaisedByTheStatement(e)) { }
        return true;
    }

    /// <summary>The record area image at the §13.18.43.4 GR13 a) length <paramref name="size"/> — the lenient
    /// reference modification <c>(1:size)</c> this slicing replaced, kept byte-identical for checking off: a
    /// negative size is the whole area, zero is the zero-length record, and a size past the area space-extends.</summary>
    private static string Released(string image, int size) =>
        size < 0 ? image
        : size == 0 ? ""
        : size <= image.Length ? image[..size]
        : image.PadRight(size);

    /// <summary>The RETURN STATEMENT (ISO §14.9.34.4). GR1: a RETURN "may be executed only when it is within the
    /// range of an output procedure being executed by a MERGE or SORT statement that references file-name-1. If it
    /// is executed at any other time, the EC-FLOW-RETURN exception condition is set to exist." GR3: "After the
    /// execution of imperative-statement-1 in the AT END phrase, no RETURN statement may be executed as part of
    /// the current output procedure. If such a RETURN statement is executed, the EC-SORT-MERGE-RETURN exception
    /// condition is set to exist and the results of the execution of the RETURN statement are undefined." Both
    /// tests precede the retrieval; with checking off the store's deterministic answer stands (at end again, or
    /// at end for a file with no executing statement). The implicit GIVING transfer uses <see cref="Return"/>.
    /// <para>EC-SORT-MERGE-ACTIVE is tested first (kb/Work PB1036): a RETURN within the range of any executing
    /// input procedure (§14.9.40.4 GR10), the same determination as <see cref="ReleaseStatement"/>'s — the rule
    /// written for that range wins over GR1's general "any other time" (docs/CONFORMANCE.md §3 D-SMA).</para></summary>
    public static bool ReturnStatement(string name, out string image)
    {
        if (ExceptionState.SortMergeActiveChecking && Stores.AnyInPhase([ProcedurePhase.Input]))
            ExceptionState.SortMergeActiveError($"RETURN for sort-merge file {name}: executed within the range of "
                + "an input procedure of an executing SORT statement (ISO §14.9.40.4 GR10)");
        var f = Stores.Find(name);
        if (f is null || f.Phase != ProcedurePhase.Output)
            ExceptionState.FlowReturnError($"RETURN for sort-merge file {name}: not within the range of an output "
                + "procedure being executed by a MERGE or SORT statement that references it (ISO §14.9.34.4 GR1)");
        else if (f.AtEndReached)
            ExceptionState.SortMergeReturnError($"RETURN for sort-merge file {name}: executed after the at end "
                + "condition in the current output procedure (ISO §14.9.34.4 GR3)");
        if (f is null)
        {
            image = "";   // no statement executing: nothing to make available — the at end path
            return false;
        }
        bool got = Return(name, out image);
        if (!got && f.Phase == ProcedurePhase.Output) f.AtEndReached = true;
        return got;
    }

    /// <summary>Mark the start of the next USING stream (MERGE only): records released after this call belong to
    /// the next file-name-2/-3 in statement order — the tie-break order ISO §14.9.24 GR4 prescribes.</summary>
    public static void NextInput(string name) => Get(name).Work.NextStream();

    /// <summary>⛔ THE SHORT-RECORD FILL OF A SORT/MERGE TRANSFER (§14.9.40.4 GR7 / GR16, §14.9.24.4 GR2 / GR13; kb/Work
    /// PB1140): a record with fewer character positions than the fixed length of the file it moves to <i>"is space
    /// filled on the right to that fixed length"</i> — <paramref name="national"/> says which space
    /// (<c>FileModel.ShortRecordFillNational</c>, decided once at compile time from the file's own record
    /// descriptions). A record carrying an EXTENT TABLE is a variable-length group whose fixed form
    /// (<c>CobolContiguousLayout.ToFixedForm</c>, D-FRA (vi)) places and pads each member itself, so it is returned
    /// unchanged — padding its contiguous image would stop the table from describing it.</summary>
    public static string FillTo(string image, int width, bool national, RecordExtents? extents = null) =>
        extents?.Layout is not null ? image : RecordFill.Fit(image, width, national);

    /// <summary>RELEASE one record image at its released length (ISO §14.9.32 GR2; §14.9.40 GR12b for the implicit
    /// USING release) — the RELEASE STATEMENT's §14.9.32.4 GR1 test lives in <see cref="ReleaseStatement"/>, and
    /// this implicit release is not a RELEASE statement. Its one test is the size rule of §14.9.40.4 GR12 b) /
    /// §14.9.24.4 GR7 b) (kb/Work PB1036): "If the size of the record read from the file referenced by file-name-2
    /// is larger than the largest record allowed in the file description entry for file-name-1, the
    /// EC-SORT-MERGE-RELEASE exception condition is set to exist and the execution of the SORT statement is
    /// terminated", and the smaller-than-smallest twin when file-name-1 has variable-length records — so the
    /// caller passes <paramref name="min"/> 0 for a fixed-length sort-merge file. <paramref name="readSize"/> is
    /// the size the record had when it was READ, not the image's length after the fixed-length fit. A raise
    /// releases nothing and the SORT/MERGE statement's guard terminates the statement (§14.6.13.1.3 2)); with
    /// checking off the store accepts the record as the transfer shaped it. <paramref name="extents"/> is the
    /// record's EXTENT TABLE when a variable-length group record is released (D-FRA (v); kb/Work PB1053).</summary>
    public static void Release(string name, string image, int readSize, int min, int max, RecordExtents? extents = null)
    {
        try { ReleaseCore(name, image, readSize, min, max, extents); }
        catch (CobolFatalException e) when (RaisedByTheStatement(e)) { }
    }

    private static void ReleaseCore(string name, string image, int readSize, int min, int max, RecordExtents? extents)
    {
        if (readSize < min || readSize > max)
            ExceptionState.SortMergeReleaseError($"implicit release to sort-merge file {name}: a {readSize}-byte "
                + $"record is outside the record range {min} to {max} (ISO §14.9.40.4 GR12 b), §14.9.24.4 GR7 b))");
        Get(name).Work.Add(new StoredFrame(image ?? "", extents));
    }

    /// <summary>⛔ THE SORT/MERGE STATEMENT'S OWN ASSOCIATION (§12.4.5.3 GR3; kb/Work PB1097): <i>"The association
    /// occurs at the time of execution of an OPEN, SORT, or MERGE statement that referenced file-name-1"</i>, and
    /// when the content of data-name-1 is not consistent <i>"the OPEN, SORT, or MERGE statement is unsuccessful"</i>.
    /// A sort-merge file (§13.4.6) is the statement's <see cref="SortWork"/> (memory, spilling to anonymous temporary
    /// run files: docs/CONFORMANCE.md §3 D-SWK), never a host connector, so nothing else asks the question for it; the rule is <see cref="FileConnector.AssociationFailure"/>, the very one every OPEN
    /// applies (DOC-A.1-73). <see langword="false"/> = the association cannot be made: the statement is
    /// unsuccessful, which §3.176 defines as an attempt that "does not result in the execution of all the
    /// operations specified by that statement" — the emitted statement leaves before any phase runs. The sort-merge
    /// file entry has no FILE STATUS clause (§12.4.5.1 Format 4), so there is no I-O status to set.</summary>
    /// <param name="assign">The executing element's specification: data-name-1's content.</param>
    public static bool AssociationMade(string assign) =>
        FileConnector.AssociationFailure(assign, dynamic: true, out _) is null;

    /// <summary>The EC-SORT-MERGE-FILE-OPEN test for one USING or GIVING file (kb/Work PB1036), rendered where the
    /// rule places it: SORT at the start of the phase that processes the file — §14.9.40.4 GR9 a) "If the file
    /// referenced by file-name-2 is in an open mode when this phase commences" and its GR9 c) twin for
    /// file-name-3 — and MERGE for every USING and GIVING file "at the start of execution of the MERGE
    /// statement" (§14.9.24.4 GR7 / GR12). The test precedes the implicit OPEN, so a raise leaves the program's
    /// own connector exactly as it was. With checking off the results are undefined (SORT GR9) and the transfer
    /// proceeds: its implicit OPEN answers '41', which the transfer rule disposes of (kb/Work PB993).</summary>
    public static void FileNotOpen(string name, string file)
    {
        try { FileNotOpenCore(name, file); }
        catch (CobolFatalException e) when (RaisedByTheStatement(e)) { }
    }

    private static void FileNotOpenCore(string name, string file)
    {
        if (CobolFile.OpenModeIfOpen(file) is { } mode)
            ExceptionState.SortMergeFileOpenError($"SORT/MERGE on {name}: the USING/GIVING file {file} is open "
                + $"({mode}) (ISO §14.9.40.4 GR9, §14.9.24.4 GR7 / GR12)");
    }

    /// <summary>The sequence phase (ISO §14.9.40 GR9b): a STABLE key sort (<see cref="SortWork.Sequence"/> — in memory
    /// when the records fit, else the merge of the sorted runs the release phase wrote; kb/Work PB3339). Stability
    /// realizes GR3's DUPLICATES IN ORDER (equal keys keep USING-file / RELEASE order); without the phrase the relative
    /// order is undefined (GR4), so the stable result is conformant there too — <paramref name="duplicatesInOrder"/> is
    /// accepted for the call-site's traceability.</summary>
    public static void Sort(string name, bool duplicatesInOrder)
    {
        try { SortCore(name, duplicatesInOrder); }
        catch (CobolFatalException e) when (RaisedByTheStatement(e)) { }
    }

    private static void SortCore(string name, bool duplicatesInOrder)
    {
        _ = duplicatesInOrder;   // stability is unconditional — GR3 satisfied, GR4 (undefined) safely refined
        var f = Get(name);
        f.Phase = ProcedurePhase.None;   // the sequence phase ends the input procedure (§14.9.40.4 GR9 a/b)
        f.Work.Sequence();
    }

    /// <summary>The merge operation (ISO §14.9.24 GR1): a k-way merge of the pre-sorted USING streams. Equal keys
    /// take the record from the EARLIEST stream first, and within one stream the records keep their file order —
    /// exactly GR4a/GR4b (<see cref="SortWork"/>: from memory when the streams fit, else from their runs).
    /// <para>EC-SORT-MERGE-SEQUENCE (kb/Work PB1036): §14.9.24.4 GR6 — "If the records in the file referenced by
    /// file-name-2 and file-name-3 are not ordered as described in the ASCENDING or DESCENDING KEY clauses and the
    /// collating sequence associated with the MERGE statement, the EC-SORT-MERGE-SEQUENCE exception condition is
    /// set to exist, all files associated with the MERGE statement are closed, and the results of the merge
    /// operation are undefined." Each stream is checked as its records arrive, against the SAME key comparison the
    /// merge uses, so "ordered" means exactly what the merge assumes; the raise is here, before the merge, for the
    /// first stream in statement order that is out of order. Every USING file was closed by its implicit CLOSE before
    /// this phase and no GIVING file is opened until after it, so "all files … are closed" already holds. With checking
    /// off the check is skipped and the merge yields a deterministic stream-merge order, conformant within
    /// "undefined".</para></summary>
    public static void Merge(string name)
    {
        try { MergeCore(name); }
        catch (CobolFatalException e) when (RaisedByTheStatement(e)) { }
    }

    private static void MergeCore(string name)
    {
        var f = Get(name);
        if (ExceptionState.SortMergeSequenceChecking)
            foreach (var (stream, record) in f.Work.OrderViolations())
                ExceptionState.SortMergeSequenceError($"MERGE on {name}: record {record} of USING file {stream} is out "
                    + "of KEY order (ISO §14.9.24.4 GR6)");
        f.Work.Sequence();
    }

    /// <summary>RETURN the next record in key order (ISO §14.9.34 GR3): <see langword="true"/> with the record's
    /// image at its own length, or <see langword="false"/> at end — the UNCHECKED primitive the implicit GIVING
    /// transfer uses; the RETURN statement's GR1 / GR3 tests live in <see cref="ReturnStatement"/>. A post-end
    /// call deterministically reports at-end again (a conformant refinement of GR3's "undefined"). Reading the merge of
    /// runs is the statement's own sequencing, so it runs under the statement's mark (<see cref="RaisedByTheStatement"/>).</summary>
    public static bool Return(string name, out string image)
    {
        var f = Get(name);
        StoredFrame returned = default;
        bool got = false;
        try { got = f.Work.Next(out returned); }
        catch (CobolFatalException e) when (RaisedByTheStatement(e)) { }
        if (!got)
        {
            image = "";
            f.LastReturnedLength = 0;
            f.LastReturnedExtents = null;
            return false;   // at end — the record area's content is undefined (GR3); the caller leaves it as-is
        }
        image = returned.Image;
        f.LastReturnedLength = image.Length;
        f.LastReturnedExtents = returned.Extents;
        return true;
    }

    /// <summary>The length of the most recently RETURNed record — the value a varying SD's RECORD VARYING
    /// DEPENDING ON item receives (ISO §13.18.43 GR15: each returned record restores its own length).</summary>
    public static int LastReturnedLength(string name) => Get(name).LastReturnedLength;

    /// <summary>The EXTENT TABLE the most recently RETURNed record was released with (docs/CONFORMANCE.md §3 D-FRA
    /// (v); kb/Work PB1053) — what an out-of-line variable-length group record decomposes it by, and what the
    /// implicit GIVING WRITE frames it with. Null when the record carries none.</summary>
    public static RecordExtents? LastReturnedExtents(string name) => Get(name).LastReturnedExtents;

    /// <summary>Rewind the return phase to the first record — emitted before EACH GIVING file's write-out, so
    /// every file-name-3/-4 receives the FULL sorted/merged result (ISO §14.9.40 GR15 / §14.9.24 GR12).</summary>
    public static void Rewind(string name) => Get(name).Work.Rewind();

    /// <summary>End the SORT/MERGE statement: drop the store, closing (and so deleting) every temporary run file it
    /// holds (the sort file has no persistent storage — ISO §9 sort-merge file model: only RELEASE/RETURN/SORT/MERGE
    /// ever reference it; docs/CONFORMANCE.md §3 D-SWK (b)).</summary>
    public static void Close(string name) => Stores.Remove(name);
}
