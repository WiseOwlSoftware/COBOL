// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime;

/// <summary>
/// ⛔ THE NAMED BEHAVIOUR CHANGES BETWEEN ISO COBOL EDITIONS — the BEHAVIOUR twin of
/// <c>WiseOwl.Cobol.Editions.ConstructRegistry</c> (kb/Work PB344). A registry row there answers "does this edition
/// HAVE this construct?" and produces a diagnostic; a member here answers "which edition's RULE does this
/// construct obey?" and produces an ANSWER. Both questions arise on every edition-varying feature and they are
/// NOT the same question: the source of a behaviour change is legal at every edition, so no diagnostic can
/// carry it.
/// <para>⛔ <b>THE ADMISSION TEST FOR A MEMBER</b> (kb/Work PB344, earned by a member that had to be withdrawn).
/// Annex E is INFORMATIVE and, per §2023:E.1, lists "the substantive changes between <b>the previous COBOL
/// standard</b> and this Working Draft International Standard" — ONE prior edition, the 2014 one. An E.2 item is
/// therefore evidence about a prior edition's RULES only when it says what the prior rule STATED; where its
/// justification says instead that the previous standard "was not clear or missing processing" — i.e. the prior
/// text was SILENT, and each sub-item adds "This appears to be an error in previous standards" — there is no
/// prior behaviour to serve, silence is not a prohibition, and the 2023 rule stands at every edition. Item 19
/// a)/b) (the open-mode USE tier) failed that test and is NOT a member: the 1985 edition's own validation suite
/// requires the opposite of what a gate would have produced (NIST CCVS SQ122A/SQ136A/SQ137A/SQ138A/SQ148A).
/// Item 22 below passes it — "the rule itself stated that the first record would be retrieved".</para>
/// <para>Each member names one row of <c>docs/VERSION_CHANGE_REFERENCE.md</c> — Table 1 for a 2014 to 2023 change,
/// Table 7 for an earlier window (its edge derived, as that table says) — whose action is
/// <c>gate-behavior-by-dialect</c>. The row's <c>&lt;!-- gate:… --&gt;</c> marker is this member's
/// <see cref="DialectBehaviors.Row.Id"/>, which is how the drift test
/// (<c>DialectBehaviorRegistryDriftTests</c>) proves the two registers still agree — a member with no row, or a
/// row whose marker names no member, fails the build's test gate rather than rotting.</para>
/// <para>⛔ IT LIVES IN THE RUNTIME, NOT IN <c>WiseOwl.Cobol.Editions</c>, for the same reason
/// <c>SignEncoding</c> does: BOTH sides need it. The emitter folds a behaviour that is decided at COMPILE time
/// (it knows <c>--std</c>, so a suppressed branch is never rendered), and the generated program's connectors
/// ask it at RUN time for a behaviour that is decided inside the runtime's own walk — over the edition the
/// COMPILATION baked into them (<c>FileConnector.Edition</c>). <c>WiseOwl.Cobol.Editions</c> is a leaf that the
/// runtime does not reference, so putting it there would force either a project-reference inversion or a
/// SECOND copy of the table.</para>
/// </summary>
public enum DialectBehavior
{
    /// <summary>ISO/IEC 1989:2023 §2023:14.9.30.4 GR21 indexed rule d) 3 — "If no such record is found or PREVIOUS
    /// is specified and the previous operation on the file was an OPEN statement, the at end condition exists".
    /// 2023 CHANGED IT: Annex E.2 item 22, "READ PREVIOUS statement following an OPEN statement. Ensure that an
    /// at end condition occurs", whose justification records the PRIOR rule — "the rule itself stated that the
    /// first record would be retrieved" — so at 2002/2014 a READ PREVIOUS immediately after an OPEN makes the
    /// first existing record available, exactly as NEXT does. (The conflicting NOTE Annex E.2 also mentions is
    /// informative — this document is drafted to the ISO/IEC Directives Part 2, under which a note states no
    /// requirement — so the RULE is the prior standard's normative content.) INDEXED ONLY: the relative and
    /// sequential sub-rule blocks print rule b) unamended.</summary>
    IndexedReadPreviousAfterOpenAtEnd = 0,

    /// <summary>ISO/IEC 1989:2023 §2023:7.3.6.3 GR2 — "The implementor shall define and document which mode of
    /// arithmetic is to be used when evaluating compile-time arithmetic." 2023 CHANGED IT: Annex E.2 item 6 says
    /// the mode "is now explicitly implementor defined", and its justification records the PRIOR rule — "The
    /// previous COBOL Standard required the use of an arithmetic mode that is no longer supported" — so it passes
    /// the admission test above. The one arithmetic mode 2023 removed is Standard Arithmetic (E.2 item 21), so at
    /// 2002 and 2014 a compile-time arithmetic expression (CONSTANT AS, <c>&gt;&gt;DEFINE</c>,
    /// <c>&gt;&gt;IF</c>/<c>&gt;&gt;EVALUATE</c>) is evaluated in standard arithmetic, and from 2023 in the
    /// implementor's documented mode (docs/CONFORMANCE.md DOC-A.1-29). The consumer is
    /// <c>WiseOwl.Cobol.Frontend.Expressions.CompileTimeArithmetic.For</c> — kb/Work PB1592.</summary>
    CompileTimeArithmeticImplementorDefined = 1,

    /// <summary>ISO/IEC 1989:2023 §2023:9.1.15 / §2023:14.9.27.4 Table 19 — the FILE SHARING facility (Annex A.4.7, an OPTIONAL
    /// module): the sharing mode, the file lock "thereby preventing other run units from opening that file with
    /// incompatible sharing rules", the OPEN arbitration of Table 19 and the file sharing conflict condition whose I-O
    /// status is '61' (§2023:9.1.13.9 item 1). It passes the admission test above the other way round: not by an Annex E
    /// item (Annex E is the 2014 to 2023 window) but by §2023:A.4.1, <i>"Any associated syntax rules, general rules, other
    /// rules, exception conditions, and I-O status values are also optional"</i> — the facility is a 2002 module (the
    /// SHARING and LOCK MODE clauses are COBOL-2002 introductions, <c>COBOL0900</c> below 2002), so a COBOL-85
    /// program is compiled for a processor on which it is not in effect. At <c>--std 85</c> a file connector therefore
    /// has the implementor-defined sharing mode §2023:9.1.15 allows ("a mode completely specified by the implementor", which
    /// need not be one of the three): <b>no mode restricts another connector</b>. Two connectors of one run unit open the
    /// same physical file in any combination of modes and every OPEN answers '00' (the owner's determination of
    /// 2026-10-23, kb/Work PB670, option (i): it is GnuCOBOL's measured <c>-std=cobol85</c> behaviour, and it produces no
    /// status the 1985 edition does not define); a host refusal by a process outside the run unit answers '30'. The edge
    /// is DERIVED (docs/VERSION_CHANGE_REFERENCE.md Table 7 row 7.39). Consumers: <c>FileRegistry.SharedOpenAttempt</c>
    /// (the arbitration and the default mode) and <c>FileConnector.Open</c> (the cross-run-unit file lock and the
    /// host refusal's status), both through <c>FileConnector.SharingFacilityInEffect</c>.</summary>
    FileSharingFacility = 2,

    /// <summary>ISO/IEC 1989:2023 §2023:14.9.28.4 GR13 e) 2 a-c, TEST BEFORE: when an AFTER level's condition is
    /// satisfied the inner variable is set to its initialization value FIRST and the outer variable is augmented
    /// SECOND. §1985:VI.6.21.4 GR10 d) 1 (Figure 2 draws the same) has the opposite order: it augments the outer
    /// variable and THEN sets the inner one, so at <c>--std 85</c> an AFTER level whose FROM reads the level to its
    /// left starts from the augmented value (kb/Work PB5101, PB436). Both editions are QUOTED (VCR row 7.53), the
    /// 2002 text being worded as 2023 is. The consumer is <c>ControlFlowBinder.BindVarying</c>, which sets
    /// <c>PerformVarying.AugmentOuterFirst</c> for the emitter.</summary>
    PerformVaryingInitInnerBeforeAugmentOuter = 3,

    /// <summary>ISO/IEC 1989:2023 §2023:13.18.16.4 GR4 a) — while a control break's footings print, the control data items
    /// hold their PRIOR values, and "If the printing of the control footing causes a page advance and the page heading
    /// or page footing refers to a control data item, the value produced is similarly the prior value" (the 2002 text,
    /// 13.16.15.3 GR4 a, says the same, and 2023 keeps it). §1985:XIII.3.20.4 GR13 gives the prior values only to a
    /// control footing's own USE procedure and SOURCE clauses (a), and at TERMINATE to the control footings and the
    /// report footing (b), and GR13 c) says "All other data item references within report groups and their USE
    /// procedures access the current values that are contained within the data items at the time the report group is
    /// processed" — so at <c>--std 85</c> the page footing, page heading and OR PAGE control headings of a footing's
    /// page advance, and the TERMINATE page footing, see the CURRENT values (kb/Work PB5128). Both editions are QUOTED
    /// (VCR row 7.54). The consumer is <c>CobolReport.WithPageGroupControlValues</c>, over the edition the compilation
    /// baked into the report (<c>CobolReport.Edition</c>).</summary>
    ControlFootingPageGroupsSeePriorControlValues = 4,

    /// <summary>ISO/IEC 1989:2023 §2023:14.9.30.4 GR27a — '02' is set only when "an indexed file is being sequentially
    /// accessed" and the key of reference's next record has the same key; the 2002 text words it the same (a READ with the
    /// NEXT or PREVIOUS phrase). §1985:IX.1.3.4 GRL2.1 names no access mode: "For a READ statement, the key value for
    /// the current key of reference is equal to the value of the same key in the next record within the current key of
    /// reference", so at <c>--std 85</c> a random READ (format 2, a KEY phrase) of an alternate key that has duplicates
    /// also answers '02' (kb/Work PB5117). The consumer is <c>IndexedConnector.ReadRandom</c>.</summary>
    IndexedDuplicateKeyStatusSequentialOnly = 5,
    /// <summary>ISO/IEC 1989:2023 §2023:14.9.37.4 GR4 — a format 1 SEARCH whose search index, at the start of the execution,
    /// "corresponds to an occurrence number that is negative, zero, or greater than the highest permissible occurrence number"
    /// is unsuccessful at once (the 2002 transcription, SEARCH statement general rule 4 at specs/ISO_COBOL_2002.md line 20917, words it the same). §1985:VI.6.22.4 GR2 tests the UPPER bound
    /// alone: an index "greater than the highest permissible occurrence number for identifier-1" ends the search at once, and
    /// an index "not greater" starts the serial search by "evaluating the conditions in the order that they are written";
    /// §1985:VI.6.23.4 GR2 lets a PERFORM or SEARCH leave an index "outside the range of its associated table", which is how a
    /// program arrives at occurrence 0. So at <c>--std 85</c> an index below 1 starts the scan and is incremented to occurrence 1
    /// (kb/Work PB5121; VCR row 7.56). Both editions are QUOTED. The consumer is <c>SearchBinder.BindSearch</c>, which sets
    /// <c>BoundSearch.IndexBelowTableIsUnsuccessful</c> for <c>ControlFlowEmitter.EmitSerialScan</c>.</summary>
    SearchIndexBelowTableUnsuccessful = 6,

    /// <summary>ISO/IEC 1989:2023 §2023:14.9.22.4 GR19 with §2023:14.6.4 step 6 — a format 3 INSPECT "is interpreted and
    /// executed as though two successive INSPECT statements", but "Item identification of any identifier in the format 2
    /// statement is done only once before executing the format 1 statement", and item identification includes the "length
    /// evaluation for an occurs-depending group item" (the 2002 text, §2002:14.8.21.3 GR18 and §2002:14.5.4 step 6, says the
    /// same of a variable-length data item). So from 2002 the REPLACING half works over the length identifier-1 had when the
    /// statement began, whatever the TALLYING half did to the DEPENDING ON object. §1985:VI.6.18.4 GR17 fixes only "Subscripting
    /// associated with any identifier in the format 2 statement"; its length rule (§1985:VI.6.18.4 GR1 with §1985:VI.5.8.4 GR3:
    /// the value of data-name-1 "at the start of the operation") is read at the start of EACH of the two successive
    /// statements, so at <c>--std 85</c> the REPLACING half takes the length anew (kb/Work PB5119). The consumer is
    /// <c>InspectBinder</c>, which pins the DEPENDING ON object for the statement from 2002 and sets
    /// <c>BoundInspect.ReplacingTakesLengthAnew</c> below it.</summary>
    InspectFormat3LengthIdentifiedOnce = 7,

    /// <summary>ISO/IEC 1989:2023 §2023:13.18.54.4 GR4 — "If the associated size error indicator is set, an EC-REPORT-SUM-SIZE
    /// exception condition is set to exist and the printable item is filled with spaces" (the 2002 text, 13.16.52.3 GR4,
    /// words it the same). §1985:XIII.3.19.4 GR4 has no size error indicator: "the sum counter serves as a source data item.
    /// The report writer control system moves the data contained in the sum counter, according to the rules of the MOVE
    /// statement, to the printable item for presentation", so at <c>--std 85</c> a counter whose addition overflowed still
    /// presents the content it holds, and nothing is raised (kb/Work PB5248; VCR row 7.58). The consumer is
    /// <c>CobolReport.SumPresentable</c>, over the edition the compilation baked into the report.</summary>
    SumSizeErrorBlanksPrintableItem = 8,

    /// <summary>ISO/IEC 1989:2023 §2023:13.18.37.4 GR4 a) — the save location filled by an absolute NEXT GROUP "will have no
    /// effect at all if a TERMINATE is next executed for the report" (the 2002 text, 13.16.35.3 GR4 a), words it the same).
    /// §1985:XIII.3.10.8 GR3 gives every body group the saved-integer arm of the fit test, and §1985:XIII.4.7.4 GR1 has TERMINATE
    /// produce the control footings as body groups, so at <c>--std 85</c> the saved integer is still there for the first
    /// control footing TERMINATE produces: it takes the page advance and the placement the integer gives (kb/Work PB5245;
    /// VCR row 7.59). The consumer is <c>CobolReport.TerminateCore</c>.</summary>
    TerminateDiscardsSavedNextGroup = 9,

    /// <summary>ISO/IEC 1989:2023 §2023:13.18.28.4 GR1 — GROUP INDICATE "has the same effect as a PRESENT WHEN clause where the
    /// associated condition is true only on the first occasion that a GENERATE is issued for the current detail report
    /// group" after an INITIATE, a page advance or a control break (the 2002 text, 13.16.26.3 GR1, words it the same), so a
    /// GENERATE the USE procedure's SUPPRESS PRINTING inhibited still used the occasion up. §1985:XIII.3.13.4 GR1 keys the
    /// indication to "the first presentation of the detail report group", and §1985:XIII.4.6.4 GR3 says a suppressed group
    /// is not presented, so at <c>--std 85</c> only a GENERATE whose group was presented consumes it (kb/Work PB5246; VCR row
    /// 7.60). The consumer is <c>CobolReport.GenerateCore</c>.</summary>
    GroupIndicateConsumedByIssuedGenerate = 10,

    /// <summary>ISO/IEC 1989:2023 §2023:15.5.2 — a value in integer date form "shall be less than or equal to the value of
    /// FUNCTION INTEGER-OF-DATE (99991231), which is 3,067,671", and the YYYY of INTEGER-OF-DATE and INTEGER-OF-DAY "shall be an
    /// integer greater than 1600 and less than 10000" (§2023:15.46.3 AR1 a, §2023:15.47.3 AR1 a); the 2002 text words all three
    /// the same. The 1989 intrinsic function module bounds the year below only: §1989:A.2.16.3 AR1 a. "It must be an integer
    /// greater than 1600" (§1989:A.2.17.3 AR1 the same), and DATE-OF-INTEGER and DAY-OF-INTEGER take "a positive integer that
    /// represents a number of days succeeding December 31, 1600" (§1989:A.2.12.3 AR1, §1989:A.2.13.3 AR1). So at
    /// <c>--std 85</c> INTEGER-OF-DATE (100000101) is 3,067,672 and DATE-OF-INTEGER (3067672) is 100000101 (kb/Work PB5130;
    /// VCR row 7.61). Both editions are QUOTED. The consumer is <c>IntrinsicBinder.BindFunctionResult</c>, which sets
    /// <c>BoundIntrinsicCall.IntegerDateFormUnbounded</c> for the four <c>CobolDate</c> bodies.</summary>
    IntegerDateFormBounded = 11,

    /// <summary>ISO/IEC 1989:2023 §2023:15.68.3 AR2 — NUMVAL-C's argument-2 "shall not contain any of the digits 0 through 9;
    /// the characters '*', '+', '–', ',' , or '.'; or the two consecutive letters 'CR' or 'DB'" and "shall contain at least one
    /// non-space character"; the 2002 text words it the same. The 1989 intrinsic function module asks only its class,
    /// §1989:A.2.30.3 AR4 "Argument-2, if specified, must be a nonnumeric literal or alphanumeric data item", and AR1 makes cs
    /// "the string of one or more characters specified by argument-2", so at <c>--std 85</c> a currency string such as "Fr."
    /// or "CRC" is admitted and scanned as written (kb/Work PB5161; VCR row 7.62). Both editions are QUOTED. The consumers
    /// are <c>IntrinsicBinder.BindNumvalCFamily</c> (the literal screen, and <c>BoundIntrinsicCall.CurrencyContentUnrestricted</c>
    /// for a data item) and <c>CobolIntrinsics.NumvalCDec</c>.</summary>
    NumvalCCurrencyContentRules = 12,

    /// <summary>ISO/IEC 1989:2023 §2023:13.18.43.4 GR19 — "If the number of bytes in the logical record to be written is
    /// less than integer-4 or greater than integer-5", a WRITE or REWRITE of a file whose RECORD clause is Format 3
    /// (<c>RECORD CONTAINS integer-4 TO integer-5</c>) is unsuccessful (I-O status '44' on a WRITE/REWRITE, §2023:9.1.13.7
    /// item 4 a). The 2002 text carries the same rule (§2002:13.16.41.3 GR19 a: the "REWRITE or WRITE statement is
    /// unsuccessful"; its I-O status 44 item c names the fixed-or-variable-length format of the RECORD clause), whereas
    /// §1985:VII.1.3.5 names only "the RECORD IS VARYING clause", with Format 3's size "completely defined in the
    /// record description entry" (§1985:VII.3.8.4 GR14). So at <c>--std 85</c> a Format 3 range only documents the record
    /// sizes and no WRITE is refused for it (kb/Work PB5197). The consumer is <c>FileControlBinder.BindRecordClause</c>.</summary>
    RecordContainsRangeBindsRecordSize = 13,

    /// <summary>ISO/IEC 1989:2023 §2023:14.9.51.4 GR29 b) — a random or dynamic WRITE of a relative file whose key item holds
    /// "a value that is less than 1 or greater than the highest relative record number permitted for the file" is
    /// unsuccessful with I-O status '34'; the 2014 text carries the same sentence (§2014:14.9.47.3 GR26 b). The 2002 text
    /// (§2002:14.8.47.3 GR25 b) ends that rule after the '22' sentence and knows only the invalid key condition of a write
    /// "beyond the externally defined boundaries of the file", status '24' (§2002:14.8.47.3 GR29 b); the 1985 relative-file
    /// status list has no '34' at all and gives '24' for the same attempt (§1985:VIII.1.3.4 item (3) c) with the invalid key
    /// condition of §1985:VIII.1.3.5 (kb/Work PB5125). So below 2014 the same test answers '24' and the INVALID KEY phrase
    /// (or the USE procedure) runs; from 2014 it answers '34', the permanent error, and the phrase is skipped. The consumer
    /// is <c>RelativeConnector.Write</c>.</summary>
    RelativeWriteKeyOutOfRangeIsPermanentError = 14,

    /// <summary>ISO/IEC 1989:2023 §2023:14.9.22.4 GR9 b — an INSPECT operand's BEFORE phrase covers "the first character
    /// position that is eligible to participate up to, but not including, the first occurrence encountered of
    /// literal-2", so with an AFTER phrase too the BEFORE delimiter is looked for from the AFTER-derived position.
    /// §1985:VI.6.18.4 GR8 b ("from its leftmost character position up to, but not including, the first occurrence of
    /// literal-2 within the content of the data item referenced by identifier-1"), the 2002 text (14.8.21 GR8 b) and
    /// the 2014 text measure the BEFORE portion from the leftmost position whatever the AFTER phrase says, and the
    /// operand participates where the two portions intersect: AYAAXAA with AFTER "X" BEFORE "Y" tallies 0 below 2023,
    /// 2 from 2023 (kb/Work PB5120). The rewording came with BACKWARD (§2023:E.3.3 item 34), which needs a delimiter
    /// "encountered" in the scan's direction. The consumer is <c>InspectBinder.BindPhrases</c>, which sets
    /// <c>BoundInspect.BeforeFromLeftmost</c> for the emitter and so for <c>CobolInspect.Region</c>.</summary>
    InspectBeforeFromFirstEligiblePosition = 15,

    /// <summary>ISO/IEC 1989:2023 §2023:14.9.5.4 GR5 — "The program to be canceled shall not be in the active state or contain a
    /// program in the active state"; the 2002 text (§2002:14.8.5.3 GR5) words it the same. §1985:X.5.3.4 GR5 forbids less: "A program
    /// named in the CANCEL statement must not refer directly or indirectly to any program that has been called and has not yet
    /// executed an EXIT PROGRAM statement", and the run unit's main program is active without having been called, so at
    /// <c>--std 85</c> a CANCEL naming it is §1985:X.5.3.4 GR8's "No action is taken when an explicit or implicit CANCEL
    /// statement is executed naming a program that has not been called in this run unit" (kb/Work PB5241; VCR row 7.66).
    /// Both editions are QUOTED. The consumer is <c>CallBinder</c>'s CANCEL arm, which sets
    /// <c>BoundCancel.RefusesEveryActiveProgram</c> for <c>CallEmitter.EmitCancel</c> and so for
    /// <c>ProgramTable.Cancel</c>.</summary>
    CancelRefusesEveryActiveProgram = 16,

    /// <summary>ISO/IEC 1989:2023 §2023:8.8.1.2 GR6c — "If the value of an expression to be raised to a power is less than zero, the
    /// evaluation of the exponent shall result in an integer. Otherwise, the EC-SIZE-EXPONENTIATION exception condition is set to exist
    /// and the size error condition is raised"; the 2002 text (§2002:8.8.1.1) words it the same, and §2002:F.1 item 14 records it as a
    /// substantive change. §1985:VI.6.2.3 GR5c raises the size error condition only "If no real number exists as the result of the
    /// evaluation", so at <c>--std 85</c> <c>(-32) ** 0.2</c> is −2, the real fifth root of −32, and <c>(-4) ** 0.5</c> is still a size
    /// error (kb/Work PB5186; VCR row 7.67). Both editions are QUOTED. The consumer is <c>ExpressionBinder.BindPower</c>, which sets
    /// <c>BoundPower.NegativeBaseRealRoots</c> for <c>NumericRenderer.Power</c> and so for
    /// <c>CobolIntrinsics.PowNativeReal(double, CobolExact)</c> and <c>CobolExact.PowReal</c>.</summary>
    NegativeBaseRequiresIntegerExponent = 17,
}

/// <summary>
/// The <see cref="DialectBehavior"/> table and its ONE predicate. Indexed by the enum member's ordinal, so
/// <see cref="IsActive"/> is an array read — it sits inside the indexed connector's record-selection walk.
/// </summary>
public static class DialectBehaviors
{
    /// <summary>One behaviour change: the edition it took effect in, the
    /// <c>docs/VERSION_CHANGE_REFERENCE.md</c> Table 1 row that owns it, and the normative + informative
    /// citations the gate is written against.</summary>
    /// <param name="Behavior">The enum member this row describes (its ordinal IS the row's index).</param>
    /// <param name="Id">The kebab identifier, identical to the owning VCR row's <c>&lt;!-- gate:… --&gt;</c>
    /// marker.</param>
    /// <param name="ChangedIn">The FIRST edition whose rules state the NEW behaviour.</param>
    /// <param name="VcrRow">The <c>docs/VERSION_CHANGE_REFERENCE.md</c> change-table row number as the ledger prints it:
    /// <c>29</c> for a Table 1 row (the 2014 to 2023 window Annex E itemizes), <c>7.39</c> for a Table 7 row (any
    /// earlier window).</param>
    /// <param name="SpecRef">The normative clause the rule is written in.</param>
    /// <param name="AnnexRef">The informative annex item that records the change: an Annex E item for a 2023 change,
    /// the Annex A.4 optional module for a facility an earlier edition does not have.</param>
    /// <param name="Title">A one-line statement of what changed.</param>
    public readonly record struct Row(DialectBehavior Behavior, string Id, int ChangedIn, string VcrRow,
        string SpecRef, string AnnexRef, string Title);

    private static readonly Row[] Table =
    [
        new(DialectBehavior.IndexedReadPreviousAfterOpenAtEnd, "indexed-read-previous-after-open-at-end", 2023,
            "29", "14.9.30.4 GR21 d) 3", "E.2 item 22",
            "READ ... PREVIOUS immediately after an OPEN on an INDEXED file raises the at end condition; "
            + "before 2023 the rule made the first existing record available"),
        new(DialectBehavior.CompileTimeArithmeticImplementorDefined, "compile-time-arithmetic-mode", 2023,
            "12", "7.3.6.3 GR2", "E.2 item 6",
            "compile-time arithmetic expressions are evaluated in the implementor-defined mode; before 2023 the "
            + "standard prescribed standard arithmetic (the mode E.2 item 21 removed)"),
        new(DialectBehavior.FileSharingFacility, "file-sharing-facility", 2002,
            "7.39", "9.1.15", "A.4.7",
            "a file connector's sharing mode restricts other connectors, and Table 19 arbitrates every OPEN with '61', "
            + "only from COBOL-2002; at COBOL-85 the facility is not in effect and no connector restricts another"),
        new(DialectBehavior.PerformVaryingInitInnerBeforeAugmentOuter, "perform-varying-after-init-inner-first", 2002,
            "7.53", "14.9.28.4 GR13 e) 2", "§1985:VI.6.21.4 GR10 d) 1",
            "a TEST BEFORE PERFORM VARYING ... AFTER step sets the inner variable to its initialization value before "
            + "augmenting the outer one only from COBOL-2002; COBOL-85 augments the outer variable first"),
        new(DialectBehavior.ControlFootingPageGroupsSeePriorControlValues, "control-footing-page-groups-see-prior-values", 2002,
            "7.54", "13.18.16.4 GR4 a", "§1985:XIII.3.20.4 GR13",
            "the page heading and page footing a control footing's page advance reaches see the PRIOR control values only "
            + "from COBOL-2002; COBOL-85 gives the prior values to the control footing alone and every other reference the current ones"),
        new(DialectBehavior.IndexedDuplicateKeyStatusSequentialOnly, "indexed-duplicate-key-status-sequential-only", 2002,
            "7.55", "14.9.30.4 GR27", "§1985:IX.1.3.4 GRL2.1",
            "I-O status 02 for a READ whose key of reference equals the next record's key is set only by a sequential "
            + "READ from COBOL-2002; COBOL-85 also sets it for a random READ of an alternate key's duplicate set"),
        new(DialectBehavior.SearchIndexBelowTableUnsuccessful, "search-index-below-table-unsuccessful", 2002,
            "7.56", "14.9.37.4 GR4", "§1985:VI.6.22.4 GR2",
            "a format 1 SEARCH whose index is zero or negative at its start is unsuccessful at once only from COBOL-2002; "
            + "COBOL-85 tests the upper bound alone and an index below the table starts the serial scan"),
        new(DialectBehavior.InspectFormat3LengthIdentifiedOnce, "inspect-format-3-length-identified-once", 2002,
            "7.57", "14.9.22.4 GR19", "§1985:VI.6.18.4 GR17",
            "a format 3 INSPECT of an occurs-depending group identifies the group's length once, before the TALLYING half, "
            + "only from COBOL-2002; COBOL-85 takes the length anew for the REPLACING half"),
        new(DialectBehavior.SumSizeErrorBlanksPrintableItem, "sum-size-error-blanks-printable-item", 2002,
            "7.58", "13.18.54.4 GR4", "§1985:XIII.3.19.4 GR4",
            "a SUM entry whose counter holds a size error is printed as spaces, with EC-REPORT-SUM-SIZE, only from COBOL-2002; "
            + "COBOL-85 moves the counter's content to the printable item by the rules of MOVE"),
        new(DialectBehavior.TerminateDiscardsSavedNextGroup, "terminate-discards-saved-next-group", 2002,
            "7.59", "13.18.37.4 GR4 a", "§1985:XIII.3.10.8 GR3",
            "a saved absolute NEXT GROUP integer has no effect on the control footings a TERMINATE produces only from COBOL-2002; "
            + "COBOL-85 leaves it for their page fit test"),
        new(DialectBehavior.GroupIndicateConsumedByIssuedGenerate, "group-indicate-consumed-by-issued-generate", 2002,
            "7.60", "13.18.28.4 GR1", "§1985:XIII.3.13.4 GR1",
            "a GENERATE whose group SUPPRESS PRINTING kept from printing uses up the GROUP INDICATE occasion only from COBOL-2002; "
            + "COBOL-85 keys the indication to the first presentation of the detail group"),
        new(DialectBehavior.IntegerDateFormBounded, "integer-date-form-bounded-9999", 2002,
            "7.61", "15.5.2", "§1989:A.2.16.3 AR1",
            "the integer date form and the YYYY of INTEGER-OF-DATE and INTEGER-OF-DAY end at 9999-12-31 only from COBOL-2002; "
            + "the COBOL-85 intrinsic function module bounds the year below only"),
        new(DialectBehavior.NumvalCCurrencyContentRules, "numval-c-currency-content-rules", 2002,
            "7.62", "15.68.3 AR2", "§1989:A.2.30.3 AR4",
            "NUMVAL-C's argument-2 may hold no digit, none of * + - , . and no CR or DB only from COBOL-2002; "
            + "the COBOL-85 intrinsic function module asks only that it be a nonnumeric literal or alphanumeric data item"),
        new(DialectBehavior.RecordContainsRangeBindsRecordSize, "record-contains-range-binds-record-size", 2002,
            "7.63", "13.18.43.4 GR19", "§1985:VII.1.3.5",
            "a WRITE or REWRITE of a record outside a RECORD CONTAINS integer-4 TO integer-5 range is unsuccessful ('44') "
            + "only from COBOL-2002; COBOL-85 bounds only the RECORD IS VARYING clause and the range only documents the "
            + "record sizes"),
        new(DialectBehavior.RelativeWriteKeyOutOfRangeIsPermanentError, "relative-write-key-out-of-range-permanent-error", 2014,
            "7.64", "14.9.51.4 GR29 b)", "§2002:14.8.47.3 GR25 b)",
            "a random or dynamic WRITE of a relative file whose key is less than 1 or above the highest permitted record number "
            + "is a permanent error, I-O status 34, only from COBOL-2014; before it the same attempt is the invalid key "
            + "condition of a write beyond the file's boundaries, status 24"),
        new(DialectBehavior.InspectBeforeFromFirstEligiblePosition, "inspect-before-from-first-eligible-position", 2023,
            "7.65", "14.9.22.4 GR9 b", "§1985:VI.6.18.4 GR8 b",
            "an INSPECT operand with both BEFORE and AFTER looks for the BEFORE delimiter from the AFTER-derived position "
            + "only from COBOL-2023; COBOL-85, 2002 and 2014 look from the leftmost position and use the intersection"),
        new(DialectBehavior.CancelRefusesEveryActiveProgram, "cancel-refuses-every-active-program", 2002,
            "7.66", "14.9.5.4 GR5", "§1985:X.5.3.4 GR5",
            "a CANCEL naming any program in the active state, the run unit's main program included, raises "
            + "EC-PROGRAM-CANCEL-ACTIVE only from COBOL-2002; COBOL-85 forbids only a program that has been called and "
            + "has not exited, so a CANCEL of the main program, never called, takes no action"),
        new(DialectBehavior.NegativeBaseRequiresIntegerExponent, "negative-base-requires-integer-exponent", 2002,
            "7.67", "8.8.1.2 GR6c", "§1985:VI.6.2.3 GR5c",
            "a negative base raised to a non-integer exponent raises EC-SIZE-EXPONENTIATION only from COBOL-2002; "
            + "COBOL-85 raises the size error only when no real number is the result, so (-32) ** 0.2 is -2"),
    ];

    static DialectBehaviors()
    {
        // The ordinal-IS-the-index invariant this class's O(1) lookup rests on, asserted where it is authored
        // rather than only in the drift test: a member inserted mid-enum without its row moved would otherwise
        // silently answer with its neighbour's edition.
        for (int i = 0; i < Table.Length; i++)
            if ((int)Table[i].Behavior != i)
                throw new InvalidOperationException(
                    $"DialectBehaviors.Table[{i}] describes {Table[i].Behavior}; the table is ordinal-indexed.");
        if (Table.Length != Enum.GetValues<DialectBehavior>().Length)
            throw new InvalidOperationException("DialectBehaviors.Table does not cover every DialectBehavior.");
    }

    /// <summary>Every behaviour change, in enum order — the drift test's and the documentation generators'
    /// enumeration. There is no second list.</summary>
    public static ReadOnlySpan<Row> All => Table;

    /// <summary>The row describing <paramref name="behavior"/>.</summary>
    public static Row Of(DialectBehavior behavior) => Table[(int)behavior];

    /// <summary>⛔ THE ONE PREDICATE: does the edition targeted by this compilation state the NEW behaviour?
    /// <paramref name="editionYear"/> is the ISO edition the program was compiled for (the CLI's <c>--std</c>,
    /// 85 / 2002 / 2014 / 2023). Every per-edition behaviour gate — emitter-side or runtime-side — asks this
    /// and never an inline year comparison, so the edition a change landed in is written down exactly once.</summary>
    public static bool IsActive(DialectBehavior behavior, int editionYear) =>
        editionYear >= Table[(int)behavior].ChangedIn;
}
