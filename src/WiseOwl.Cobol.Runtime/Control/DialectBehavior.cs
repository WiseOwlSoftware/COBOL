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
    /// 2023 CHANGED IT (Annex E.2 item 22, "READ PREVIOUS statement following an OPEN statement. Ensure that an
    /// at end condition occurs"): before 2023 the READ was the ordinary comparison of §2002:14.8.29.3 GR18c.2
    /// (§2014:14.9.29.3 GR19c.2) — "the first existing record in the physical file whose record number or key value
    /// is less than or equal to the file position indicator" — against the position OPEN set, "the characters that
    /// have the lowest ordinal position in the collating sequence" (§2002:14.8.26.3 GR12), so only a record whose
    /// key IS that lowest value is made available and the usual outcome is at end (the standard's own NOTE:
    /// "normally get an at end condition for indexed"); from 2023 the at end condition is unconditional. The
    /// prior text overturned kb/Work PB344's reading of the annex alone (kb/Work PB5639). INDEXED ONLY: the
    /// relative and sequential sub-rule blocks print rule b) unamended.</summary>
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
    /// status is '61' (§2023:9.1.13.9 GR1). It passes the admission test above the other way round: not by an Annex E
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

    /// <summary>ISO/IEC 1989:2023 §2023:14.9.28.4 GR13e 2 a-c, TEST BEFORE: when an AFTER level's condition is
    /// satisfied the inner variable is set to its initialization value FIRST and the outer variable is augmented
    /// SECOND. §1985:VI.6.21.4 GR10d 1 (Figure 2 draws the same) has the opposite order: it augments the outer
    /// variable and THEN sets the inner one, so at <c>--std 85</c> an AFTER level whose FROM reads the level to its
    /// left starts from the augmented value (kb/Work PB5101, PB436). Both editions are QUOTED (VCR row 7.53), the
    /// 2002 text being worded as 2023 is. The consumer is <c>ControlFlowBinder.BindVarying</c>, which sets
    /// <c>PerformVarying.AugmentOuterFirst</c> for the emitter.</summary>
    PerformVaryingInitInnerBeforeAugmentOuter = 3,

    /// <summary>ISO/IEC 1989:2023 §2023:13.18.16.4 GR4a — while a control break's footings print, the control data items
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
    /// program arrives at occurrence 0. So at <c>--std 85</c> an index below 1 starts the scan (kb/Work PB5121; VCR row 7.56), and
    /// the incremented index is judged against both bounds (§1985:VI.6.22.4 GR2b), "outside the permissible range of occurrence
    /// values"): an index of 0 advances to occurrence 1, one below 0 ends the search after its first pass (kb/Work PB5302). Both editions are QUOTED. The consumer is <c>SearchBinder.BindSearch</c>, which sets
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

    /// <summary>ISO/IEC 1989:2023 §2023:13.18.37.4 GR4a — the save location filled by an absolute NEXT GROUP "will have no
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
    /// integer greater than 1600 and less than 10000" (§2023:15.46.3 AR1 a, §2023:15.47.3 AR1a); the 2002 text words all three
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

    /// <summary>ISO/IEC 1989:2023 §2023:14.9.51.4 GR29b — a random or dynamic WRITE of a relative file whose key item holds
    /// "a value that is less than 1 or greater than the highest relative record number permitted for the file" is
    /// unsuccessful with I-O status '34'; the 2014 text carries the same sentence (§2014:14.9.47.3 GR26b). The 2002 text
    /// (§2002:14.8.47.3 GR25b) ends that rule after the '22' sentence and knows only the invalid key condition of a write
    /// "beyond the externally defined boundaries of the file", status '24' (§2002:14.8.47.3 GR29b); the 1985 relative-file
    /// status list has no '34' at all and gives '24' for the same attempt (§1985:VIII.1.3.4 GRL2.3c with the invalid key
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
    /// <c>CobolPower.Binary64(double, CobolExact)</c> and <c>CobolExact.PowReal</c>.</summary>
    NegativeBaseRequiresIntegerExponent = 17,
    /// <summary>ISO/IEC 1989:2023 §2023:14.9.13.4 GR4c — "If the result of the analysis is false for any pair in a WHEN phrase,
    /// no more pairs in that WHEN phrase are evaluated"; the 2002 text words it the same (§2002:14.8.12.3 GR4c), and the
    /// selection objects of a WHEN phrase are evaluated only when the phrase is analyzed. §1985:VI.6.13.4 GR1 says instead
    /// that the statement "operates as if each selection subject and selection object were evaluated" and assigned a value
    /// before any comparison (GR2), so at <c>--std 85</c> an object a WHEN phrase never reaches is evaluated too, which is
    /// observable when it references a function (X3.23a-1989 RANDOM's next number; kb/Work PB5183; VCR row 7.69). Both
    /// editions are QUOTED. The consumer is <c>EvaluateBinder.Bind</c>, which hoists the analysis of each such pair to the
    /// beginning of the statement when this behaviour is inactive.</summary>
    EvaluateObjectsInWhenPhraseOrder = 18,
    /// <summary>§2014:14.9.26.3 GR24 — "If the file connector referenced by file-name-1 is locked by a previously-executed CLOSE
    /// statement with the LOCK phrase, the execution of the OPEN statement is unsuccessful" with I-O status '38'; the 2002 text
    /// (§2002:14.8.26.3 GR24) words it the same, so from 2002 a CLOSE WITH LOCK locks the FILE CONNECTOR and another SELECT of the
    /// same physical file opens it. §1985:IX.1.3.4 gives '38' when "an OPEN statement is attempted on a file previously closed with
    /// lock", and the 1985 conventions say "The terms file referenced by file-name and file mean the physical file regardless of the
    /// file-name used in the COBOL program" (§1985:II.2), so at <c>--std 85</c> the PHYSICAL FILE is locked and every OPEN of it
    /// through any file-name answers '38' (kb/Work PB5210; VCR row 7.70). Both editions are QUOTED. COBOL-2023 removed the phrase
    /// (§2023:E.2, COBOL0902), so the 2023 edition never reaches the rule. The consumers are <c>FileRegistry.CloseWithLock</c> and
    /// <c>FileRegistry.ClosedWithLock</c>.</summary>
    CloseWithLockLocksFileConnector = 19,
    /// <summary>ISO/IEC 1989:2023 §2023:14.7.5 — a size error condition in a statement with no ON SIZE ERROR phrase is a fatal
    /// exception condition, "and processing proceeds as specified in 14.6.13.1.3, Fatal exception conditions"; the 2002 text
    /// (§2002:14.6.4) words it the same. §1985:VI.6.4.2 GR3 says instead that without the phrase "the values of the affected
    /// resultant identifiers are undefined", the others are stored, and "control is transferred to the end of the arithmetic
    /// statement"; the change record §1985:B.2.2 item 45 adds "the program will continue", a zero divisor and an exponentiation
    /// violation included (GR1, GR2). So at <c>--std 85</c> an unphrased arithmetic statement keeps the resultant it cannot form
    /// and the run unit goes on (kb/Work PB5484; VCR row 7.71). Both editions are QUOTED. The consumer is
    /// <c>ArithmeticEmitter.EmitArith</c>, which gives such a statement the two-phase shape with every raise caught when this
    /// behaviour is inactive.</summary>
    UnphrasedSizeErrorIsFatal = 20,
    /// <summary>§2002:14.8.12.2 SR9 — "The permissible combinations of selection subject and selection object operands are indicated in table 16"
    /// (§2023:14.9.13.3 SR10, Table 15), and that table's [NOT] literal row is blank under the literal subject column, so from 2002 a
    /// literal selection subject against a literal selection object is a syntax violation (COBOL1634). §1985:VI.6.13.3 SR6 a asks only that
    /// "Identifiers, literals, or arithmetic expressions appearing within a selection object must be valid operands for comparison to the
    /// corresponding operand in the set of selection subjects", and §1985:VI.6.13.4 GR1 says each literal "is assigned the value and class
    /// of the specified literal", so at <c>--std 85</c> <c>EVALUATE 5 WHEN 5</c> is legal; a relation condition's "at least one reference to a
    /// variable" (§1985:VI.6.3.1.1) is not an EVALUATE rule, and GnuCOBOL 3.2 -std=cobol85 accepts it (kb/Work PB5481; VCR row 7.75).
    /// Both editions are QUOTED. The consumer is <c>EvaluateOperandCombinations.IsPermitted</c>.</summary>
    EvaluateRefusesLiteralSubjectLiteralObject = 21,
    /// <summary>§2014:7.3.5.2 GR1 — "The results of all arithmetic operations shall be as if ARITHMETIC IS STANDARD-DECIMAL were in
    /// effect at compile time", the 34-digit SDIDI engine (§2023:8.8.1.5). §2002:7.2.5.2 GR1 says instead "Standard arithmetic
    /// shall be used for all arithmetic operations", the 2002 standard intermediate data item of 32 significant digits whose every
    /// operation result is truncated to 32 (§2002:8.8.1.3.2, §2002:8.8.1.3.4, §2002:8.8.1.3.5) in the range 10^-999 to
    /// 10^1000 - 10^968 (§2002:8.8.1.3.1.1), so at <c>--std 2002</c> a compile-time expression whose exact value has more than 32
    /// digits is cut where 2014 keeps 34 (kb/Work PB5575; VCR row 7.77). Both editions are QUOTED. From 2023 the mode is
    /// implementor-defined (<see cref="CompileTimeArithmeticImplementorDefined"/>). The consumer is
    /// <c>CompileTimeArithmetic.For</c>.</summary>
    CompileTimeArithmeticIsStandardDecimal = 22,
    /// <summary>§2014:13.18.51.3 GR2 and §2023:13.18.51.4 GR2 — "SELECT WHEN clauses of record description entries specified in the file section have no
    /// effect for statements other than READ statements, REWRITE statements with the FILE phrase, and WRITE statements with the FILE phrase", so a
    /// WRITE or REWRITE that names record-name-1 is formatted and converted by the record description entry it names. §2002:13.16.49.3 GR1 says instead
    /// that the clauses "are evaluated for READ, REWRITE, and WRITE statements associated with that file when a CODE-SET clause or FORMAT clause is
    /// specified", so at <c>--std 2002</c> every such statement selects its entry by the conditions (kb/Work PB5578; VCR row 7.78). Both editions are
    /// QUOTED. The consumer is <c>SequentialIoEmitter</c>'s record-entry argument of the governed WRITE and REWRITE.</summary>
    SelectWhenOnlyForFilePhraseStatements = 23,
    /// <summary>§2002:14.8.44.3 GR2 — "If the data item referenced by identifier-1 is a zero-length item, execution of the UNSTRING
    /// statement terminates immediately" (§2023:14.9.48.4 GR2 the same), before the overflow test of GR15a, so a zero-length sender is
    /// neither an overflow nor a completed transfer: neither phrase runs. §1985:VI.6.27.4 has no such rule, and its GR17 makes a POINTER
    /// value "greater than the size of the data item referenced by identifier-1" an overflow, so at <c>--std 85</c> a zero-length sender
    /// with a WRITTEN pointer of 1 is the overflow condition and runs ON OVERFLOW (kb/Work PB5816; VCR row 7.80). With no POINTER phrase
    /// there is no identifier-7 (GR13a: the examination begins "with the leftmost character position"), so no overflow: the first transfer
    /// of GR13 examines zero characters and NOT ON OVERFLOW runs (GR20; kb/Work PB5858). Both editions are QUOTED. The consumer is <c>StringEmitter.EmitUnstring</c>.</summary>
    UnstringZeroLengthSenderTerminates = 24,
    /// <summary>§2014:8.8.1.3.1.3.1 situation 2 — under ARITHMETIC IS STANDARD a standard intermediate data item "is the argument of a
    /// function and there is no equivalent arithmetic expression defined for the rules of the function" is rounded to 31 digits, and
    /// the 2014 INTEGER function (§2014:15.40.3 RV1) has no equivalent expression and no exception, so its argument is rounded.
    /// §2002:15.34.3 RV1 is that exception at 2002: "When standard arithmetic is specified, argument-1 is not rounded", so at
    /// <c>--std 2002</c> FUNCTION INTEGER (1 - 1 / 10 ** 32) is 0 and from 2014 it is 1 (kb/Work PB5575; VCR row 7.81). Both editions
    /// are QUOTED. The consumer is <c>IntrinsicRenderer</c>'s argument intake.</summary>
    IntegerArgumentRoundedUnderStandardArithmetic = 25,
    /// <summary>§2023:13.18.37.4 GR4a — the integer placed in the save location by an absolute NEXT GROUP lets LINE-COUNTER
    /// be set to FOOTING, "causing a page advance to take place just before any other non-dummy body group is printed for
    /// the report"; the 2002 text (13.16.35.3 GR4 a) words it the same. §1985:XIII.3.10.8 GR3 consults the saved integer
    /// only after a page advance the fit test took: rule 3b's first paragraph applies the trial sum while "a body group has
    /// been presented on the page", and its "If no body group has yet been presented on the page" paragraph reads the save
    /// location. So at <c>--std 85</c> a body group whose trial sum does not pass its lower limit (a control footing of
    /// LINE PLUS 0 after FOOTING was reached) stays on the current page and the save location is left for a later group
    /// (kb/Work PB5823; VCR row 7.82). Both editions are QUOTED. The consumer is <c>CobolReport.PresentBody</c>.</summary>
    SavedNextGroupForcesPageAdvance = 26,
    /// <summary>§2014:13.18.54.3 GR3 and §2023:13.18.54.4 GR3 — "Each addition is tested for size error; if a size error occurs,
    /// the EC-REPORT-SUM-SIZE exception condition is set to exist", at the addition. §2002:13.16.52.3 GR3 says the addition
    /// sets "the associated size error indicator" and nothing else, and its GR4 raises the exception condition when the
    /// item is presented, so at <c>--std 2002</c> a GENERATE whose addition overflows raises nothing and the condition
    /// is raised when the entry is presented (kb/Work PB5643; VCR row 7.83). Both editions are QUOTED; 1985 has no
    /// exception conditions. The consumer is <c>CobolReport.Accumulate</c>.</summary>
    SumSizeErrorRaisedAtAddition = 27,
    /// <summary>§2002:13.16.32.3 GR6 and §2023:13.18.34.4 GR6 — a LINAGE value that violates the two value rules sets EC-I-O-LINAGE, and "the
    /// LINAGE-COUNTER is set to 0 and remains at that value until the file is closed; and all subsequent WRITE statements referencing the file
    /// cause the EC-I-O-LINAGE exception condition to continue to exist until the file is closed", so the OPEN OUTPUT that found the violation
    /// leaves the file open, with a broken page. §1985:VII.3.7.4 GR2 says only "The value must be greater than zero"; the implementor's status
    /// for the violation, '90', is a 9x of §1985:VII.1.3.5 GR5 (an unsuccessfully executed statement), and §1985:VII.4.3.4 GR1 makes only a
    /// successful OPEN put the file in an open mode. So at <c>--std 85</c> the OPEN is refused, the file is not open, and a WRITE answers
    /// '48', a CLOSE '42' (kb/Work PB5845; VCR row 7.84). Both editions are QUOTED. The consumer is <c>FileRegistry.SharedOpenAttempt</c>.</summary>
    LinageViolatingOpenLeavesFileOpen = 28,
    /// <summary>§2014:14.9.24.3 GR6d.2.a and §2023:14.9.25.4 GR6d.2.a — "When a signed numeric item is the receiving item, the sign of
    /// the numeric value shall be represented in the receiving operand": the value after alignment and truncation, so a sender
    /// whose digits all truncate away stores a positive zero. §1985:VI.6.19.4 GR4b.1 and §2002:14.8.24.3 GR4b.1 say instead that "the
    /// sign of the sending operand is placed in the receiving item", so at <c>--std 85</c> and <c>--std 2002</c> <c>MOVE -100 TO
    /// S99</c> stores a NEGATIVE zero (kb/Work PB5852; VCR row 7.85; DESIGN-data-model §2.1.1). Both editions are QUOTED. The
    /// consumers are <c>SendersSignStore.Applies</c>, which <c>StorageFormPass</c>'s promotion of the receivers that can be handed a
    /// negative zero and <c>MoveEmitter.ConvertSource</c>'s numeric store both ask.</summary>
    MoveSignIsTheValuesSign = 29,
    /// <summary>§2002:9.1.12 and every later edition's I-O status clause list values the 1985 text does not have (§2002:9.1.12.7 —
    /// "I-O status = 51. The input-output statement is unsuccessful due to an attempt to access a record that is currently locked by
    /// another file connector"), and 2023 adds more ('06', '09', '71'). §1985:VII.1.3.5, §1985:VIII.1.3.4 and §1985:IX.1.3.4 give
    /// "a list of the values placed in the I-O status for the previously named conditions" for each module and an implementor's
    /// condition the form 9x. So at <c>--std 85</c> the program's I-O status is the runtime's condition projected onto those
    /// lists (<c>IoStatus1985.Report</c>; kb/Work PB5882; VCR row 7.86; docs/FILES_DESIGN.md D33), and from 2002 it is the
    /// condition itself. Both editions are QUOTED. The consumer is <c>FileConnector.ProgramStatus</c>.</summary>
    IoStatusValuesOutside1985Lists = 30,
    /// <summary>§2002:13.16.52.3 GR6 and §2023:13.18.54.4 GR6 — "The additions necessary to compute its value are completed before
    /// the adding of the operand into the current sum counter", so a group's rolled additions run in dependency order, and
    /// §2002:13.16.52.2 SR3e (§2023:13.18.54.3 SR4e) refuses a chain of references that never terminates, a counter that is its
    /// own addend included. §1985:XIII.3.19.4 GR9a says instead that "Crossfooting is performed according to the sequence in
    /// which sum counters are defined within the control footing report group", and that a counter that is its own addend
    /// adds "the initial value of that sum counter at the time of summation"; no 1985 syntax rule forbids a forward or a
    /// self addend. So at <c>--std 85</c> <c>SA SUM SB SC</c>, <c>SB SUM SC</c>, <c>SC SUM WS-A</c> gives SA the zero SB
    /// still holds, and <c>S5 SUM WS-A S5</c> is legal and doubles S5's subtotal (kb/Work PB5894, PB5895; VCR row 7.87). Both
    /// editions are QUOTED. The consumers are <c>ReportWriterEmitter.RegisterCrossEngineAdditions</c> (the order) and
    /// <c>ReportSectionBinder.ScreenRolledChains</c>'s call (the refusal).</summary>
    SumAdditionsInDependencyOrder = 31,
    /// <summary>§2023:14.9.30.4 GR15 and §2023:14.9.51.4 GR21 — a READ of a line sequential file pads a line shorter than the
    /// minimum record size with trailing spaces (no record length conflict) and truncates an over-length one with '06', after
    /// which "the file position indicator will reference the next unread character in the record", so the next READ continues
    /// the line; and a WRITE does not transfer the record's trailing spaces. Every earlier edition has no line
    /// sequential file: a file whose RECORD DELIMITER clause names LINE-SEQUENTIAL (Annex A.1 item 150) is a sequential file
    /// whose variable-length records the line delimiter frames, and "Any method used will not be reflected in the record area
    /// or the record size used within the program" (§1985:VII.2.8.4 GR1), so a WRITE writes the record at its size, and a READ
    /// of a line outside the minimum and maximum record size is the record length conflict of §1985:VII.4.4.4 GR13, "the
    /// record is truncated on the right to the maximum size" with the I-O status '04' and no remainder (§2002:14.8.29.3 GR13
    /// and §2014:14.9.29.3 GR14 the same; kb/Work PB5903; VCR row 7.88). The same frame decides two more arms before 2023: a
    /// REWRITE is the sequential size rule, '44' unless the record has the character positions of the record being replaced
    /// (§1985:VII.4.5.4 GR10, kb/Work PB5915; 2023's §2023:14.9.35.4 GR17c pads a shorter one), and the vertical positioning
    /// of an ADVANCING phrase or a LINAGE page has no image on the medium, so each WRITE releases exactly one line in WRITE
    /// order (§1985:VII.4.7.4 GR12, kb/Work PB5984). Both editions are QUOTED. The consumers are
    /// <c>SequentialConnector.ReadRecord</c>, <c>SequentialConnector.WriteRecord</c>, <c>SequentialConnector.WriteAdvancingRecord</c>
    /// and <c>SequentialConnector.RewriteRecord</c>, through <c>SequentialConnector.LineSequentialFileRules</c> and
    /// <c>SequentialConnector.LineDelimiterOnlyFrames</c>.</summary>
    LineSequentialFileRules = 32,
    /// <summary>§2002:13.16.52.2 SR3 — "Data-name-1 shall be the name of a numeric data item in the report section", screened by
    /// its sub-rules: SR3b, "If data-name-1 is specified in the same report group description as the subject of the entry,
    /// data-name-1 shall be a repeating item", and SR3g, "If data-name-1 specifies an entry in a different report description,
    /// there are no restrictions" (§2023:13.18.54.3 SR4, SR4b, SR4g the same). COBOL-85's SUM clause has one rule for an operand
    /// of the report section instead, §1985:XIII.3.19.3 SR1: "If identifier-1 is defined in the Report Section, identifier-1
    /// must reference a sum counter", which "must be defined either in the same report group that contains this SUM clause or
    /// in a report group which is at a lower level in the control hierarchy of this report", and its crossfoot (§1985:XIII.3.19.4
    /// GR9a) adds a sum counter of the SUM entry's own group, which has no repetition. So at <c>--std 85</c> a SOURCE or VALUE
    /// entry as the operand is refused (COBOL6764), a sum counter of another report is refused (COBOL2731), and a
    /// non-repeating sum counter of the same group is a crossfoot; from 2002 the first two are legal and the third is refused
    /// (COBOL2730) (kb/Work PB5022, PB5253, PB5048; VCR row 7.89). Both editions are QUOTED. The consumer is
    /// <c>ReportSectionBinder.ResolveRolledAddend</c> and its <c>RolledRepetitionFault</c>.</summary>
    SumDataNameRules = 33,
    /// <summary>§2002:13.16.37.3 GR5 and §2023:13.18.39.4 GR5 — "If integer-2 is omitted, a value of 999 is assumed for the page
    /// width": from 2002 every report has a page width, the PAGE clause's integer-2 or 999, which bounds the COLUMN clause's operands
    /// (§2023:13.18.14.3 SR6), the rightmost column of an absolute item and a line's relative tail (SR8 b, c), and the line the engine
    /// places items in (§2023:13.18.14.4 GR5, EC-REPORT-PAGE-WIDTH, the line truncated). ANSI X3.23-1985 gives a report no page width:
    /// its PAGE clause prints no width phrase (§1985:XIII.3.8.2), its COLUMN NUMBER clause no right-hand bound (§1985:XIII.3.11.3,
    /// §1985:XIII.3.11.4 GR2), and "The report writer logical record structure of the file associated with file-name-1 is defined by
    /// the implementor" (§1985:XIII.3.2.4 GR2). So at <c>--std 85</c> a printable item may stand at any column, and the line is
    /// bounded only by the report file's record (the Annex A.1 item 159 latitude; kb/Work PB6024; VCR row 7.90). Both editions are
    /// QUOTED. The consumers are <c>ReportSectionBinder</c>'s RD binding (<c>ReportModel.PageWidth</c>, null at 85) and through it
    /// the column screens and <c>ReportWriterEmitter</c>'s report construction.</summary>
    ReportPageWidth = 34,
    /// <summary>§2002:14.8.34.2 SR6 and SR7 (§2023:14.9.37.3 SR8 and SR9 the same) — a format 2 SEARCH ALL key data-name "shall be
    /// subscripted by the first index-name associated with identifier-1 along with any subscripts required to uniquely identify the
    /// data item", and "The index-name subscript shall not be followed by a '+' or a '-'"; a referenced condition-name "shall be
    /// subscripted by the first index-name associated with identifier-1". ANSI X3.23-1985 has neither sentence: §1985:VI.6.22.3 SR4
    /// asks only that "Each data-name-1, data-name-2 must be subscripted by the first index-name associated with identifier-1 along
    /// with other subscripts as required", and puts no subscript rule on a condition-name. So at <c>--std 85</c> <c>WHEN K (IX + 1) = 2</c>,
    /// <c>WHEN C1 (IX - 1)</c> and <c>WHEN C1 (JX)</c> compile, and from 2002 each is refused (COBOL1965) (kb/Work PB5193, PB5530; VCR
    /// row 7.91). Both editions are QUOTED. The consumer is <c>SearchAllFormat2Rules</c> (<c>CheckConditionNameSide</c> and
    /// <c>CheckKeySubscript</c>).</summary>
    SearchAllKeySubscriptSentences = 35,
    /// <summary>§2002:13.16.15.2 SR6 — "Data-name-1 shall be unique in any given CONTROL clause", a rule of the WRITTEN
    /// reference, since its second sentence lets two operands "refer to the same physical data item or to overlapping data
    /// items" (§2023:13.18.16.3 SR6 the same). §1985:XIII.3.7.3 SR2 asks the resolved items instead: "Each recurrence of
    /// data-name-1 must identify a different data item". So at <c>--std 85</c> <c>CONTROLS WS-A WS-A OF WS-G</c> is refused
    /// and two spellings of one name are told apart only by the item; from 2002 the written references are compared. The
    /// consumers are <c>ReportSectionBinder.BindReportDescriptionClauses</c> (the 2002 arm) and
    /// <c>ReportSectionBinder.ScreenControlItemsDistinct</c> (the 1985 arm).</summary>
    ReportControlOperandUniqueness = 36,
    /// <summary>§2002:13.16.37.2 SR5 — "Integer-1 shall not exceed 9999", and the LINE and NEXT GROUP integers by the page
    /// limit (§2002:13.16.35.2 SR1). COBOL-85 bounds each by its digits: the PAGE clause's integer-1 (§1985:XIII.3.8.3
    /// SR2), the LINE NUMBER clause's (§1985:XIII.3.15.3 SR1) and the NEXT GROUP clause's (§1985:XIII.3.16.3 SR2) "must not
    /// exceed three significant digits in length", so the bound is 999 (kb/Work PB5252). The consumer is
    /// <c>ReportSectionBinder.ExceedsThreeSignificantDigits85</c>.</summary>
    ReportVerticalIntegerLimit = 37,
    /// <summary>§2002:13.16.12.1 prints <c>CODE IS { literal-1 | identifier-1 }</c>: "Literal-1 shall be a alphanumeric
    /// literal" (§2002:13.16.12.2 SR1) and "Identifier-1 shall represent an alphanumeric data item" (SR2). §1985:XIII.3.6.2
    /// prints <c>CODE literal-1</c> alone, and "Literal-1 must be a two-character nonnumeric literal" (§1985:XIII.3.6.3
    /// SR1). So at <c>--std 85</c> an identifier, or a literal of another length, is refused. The consumers are
    /// <c>ReportSectionBinder</c>'s CODE clause binding (the identifier arm) and
    /// <c>ReportSectionBinder.Screen85CodeLiteral</c>.</summary>
    ReportCodeClauseOperand = 38,
    /// <summary>§2002:13.16.51.2 SR4 — "Identifier-1 specifies a data item defined in any section of the data division",
    /// with no restriction on control data. §1985:XIII.3.20.3 SR5: "In control footing, page heading, page footing, and
    /// report footing report groups, SOURCE clauses and associated USE statements must not reference any of the following":
    /// a group containing a control data item, an item subordinate to one, a redefinition or renaming of part of one, and
    /// in page heading and page footing groups the control data-names (kb/Work PB5079, PB5424). The consumers are
    /// <c>ReportSectionBinder.ScreenSourceControlData</c> (the SOURCE half) and
    /// <c>ReportWriterBinder.CheckControlDataReferences</c> (the USE half).</summary>
    ReportControlDataReferences = 39,
    /// <summary>§2002:13.16.52.2 SR7 — "If the current report group is a control footing, its level of control shall be a
    /// lower level than that of data-name-3" (§2023:13.18.54.3 SR8 the same): the footing's index in the CONTROL clause is
    /// greater than the operand's. §1985:XIII.3.19.3 SR4: "Data-name-2 must not be a lower level control than the
    /// associated control for the report group in which the RESET phrase appears": the operand's index is not greater than
    /// the footing's, so the footing's own control is admitted. The consumer is
    /// <c>ReportSectionBinder.ScreenResetLevel</c>.</summary>
    SumResetLevelOrder = 40,
    /// <summary>§2002:13.16.35.2 restricts the NEXT GROUP clause by group type alone (SR4: "The NEXT GROUP clause shall not
    /// be specified in a page heading or report footing"). §1985:XIII.3.16.3 SR1: "A report group entry must not contain a
    /// NEXT GROUP clause unless the description of that report group contains at least one LINE NUMBER clause". The
    /// consumer is <c>ReportSectionBinder</c>'s NEXT GROUP binding.</summary>
    NextGroupNeedsLineNumber = 41,
    /// <summary>§2002:13.16.33.3 GR2 — "each report group shall be described such that it may be printed on one page", a
    /// GENERAL rule whose violation is the run-time EC-REPORT-PAGE-LIMIT condition (§2023:13.18.35.4 GR2 the same).
    /// §1985:XIII.3.8.3 SR9 makes it a SYNTAX rule: "All report groups must be described such that they can be presented on
    /// one page", so a control heading, detail or control footing of relative lines whose unconditional lines already pass
    /// its limit from FIRST DETAIL fits no page and is refused. The consumer is <c>ReportSectionBinder</c>'s group-limit
    /// screen.</summary>
    ReportGroupPageFitAtCompileTime = 42,
    /// <summary>§2002:13.16.55.3 GR8 states each group type's limits; for a report heading not on a page by itself, GR8 b)
    /// "The lower limit for a report heading that does not appear on a page by itself is the line preceding the first line
    /// of the page heading" (§2023:13.18.57.4 GR8 the same), and a report footing is placed below the page footing's last
    /// line. COBOL-85 gives each group type its own printed rule: "The last line number on which the report heading report
    /// group can be presented is the line number that is obtained by subtracting 1 from the value of integer-3" of FIRST
    /// DETAIL (§1985:XIII.3.10.6 GR2a); a page heading the same (§1985:XIII.3.8.4 GR1c); a page footing and a report
    /// footing the page limit (§1985:XIII.3.10.9 GR2, §1985:XIII.3.10.10 GR2), and a relative report footing or page
    /// heading is placed from the final LINE-COUNTER setting the preceding group's NEXT GROUP leaves (§1985:XIII.3.10.10
    /// GR3b, §1985:XIII.3.10.6 GR5) (kb/Work PB5981). The consumers are <c>ReportSectionBinder.RelativeGroupLowerLimit</c>,
    /// <c>ReportSectionBinder.Cobol85FinalLineCounter</c> and <c>ReportSectionBinder.GroupLimits</c>.</summary>
    ReportGroupVerticalLimits = 43,
    /// <summary>§2002:13.16.33.3 GR5 places a relative first line of a page footing: "If the report group is a page
    /// footing, the line number is given by (FOOTING integer + integer-2)" (§2023:13.18.35.4 GR5b.4 the same).
    /// §1985:XIII.3.15.3 SR9: "The first LINE NUMBER clause specified within a PAGE FOOTING report group must be an
    /// absolute LINE NUMBER clause". The consumer is <c>ReportSectionBinder</c>'s LINE clause screen.</summary>
    PageFootingFirstLineAbsolute = 44,
    /// <summary>§2002:13.16.52.2 SR3f lists the report groups a SUM may stand in: a control footing, a detail ("The
    /// current report group may be a detail and data-name-1 may be defined in a different detail") and a report footing
    /// (§2023:13.18.54.3 SR4f the same). §1985:XIII.3.19.3 SR3: "A SUM clause can appear only in the description of a
    /// control footing report group". The consumer is <c>ReportSectionBinder</c>'s report group entry screen.</summary>
    SumOnlyInControlFooting = 45,
    /// <summary>§2002:13.13.2 holds an elementary report entry to SR10 ("Every elementary entry with a COLUMN clause shall
    /// also contain either a SOURCE, VALUE or SUM clause"), SR13 ("A COLUMN clause shall be specified in each elementary
    /// entry that has a VALUE clause") and SR15 ("If BLANK WHEN ZERO or JUSTIFIED is specified, a COLUMN clause shall also
    /// be specified"), and a GROUP INDICATE entry to §2002:13.16.26.2 SR1's "a COLUMN clause and a SOURCE or VALUE clause"
    /// (§2023:13.15.3 and §2023:13.18.28.3 the same). COBOL-85 holds it to the report group description entry's three
    /// formats (§1985:XIII.3.9.2) and to SR11's table: "Table 1 below shows all permissible clause combinations for a
    /// format 3 entry" (§1985:XIII.3.9.3 SR11), whose cells carry the COLUMN, SOURCE, VALUE, JUSTIFIED and BLANK WHEN ZERO
    /// conditions. The consumers are <c>ReportSectionBinder</c>'s report group entry screen
    /// (<c>ScreenReportEntryClauseTable</c>, <c>ScreenReportEntryFormats</c> and the 2002 arms) and its GROUP INDICATE
    /// placement screen.</summary>
    ReportEntryClauseRules = 46,
    /// <summary>§2002:13.16.52.3 GR1 derives the sum counter's digits from "the corresponding number of digits, excluding
    /// insertion editing characters, in the PICTURE clause" for every category. §1985:XIII.3.19.4 GR1 c. gives the
    /// alphanumeric and alphanumeric edited counter "the size of that data item, excluding any editing characters, or 18
    /// characters, whichever is smaller", so an X(20) entry has an 18-digit counter (kb/Work PB5247). The consumer is
    /// <c>ReportSectionBinder.SumCounterDigits</c>.</summary>
    SumCounterAlphanumericCap = 47,
    /// <summary>§2002:13.16.26.3 GR1 — "The GROUP INDICATE clause has the same effect as a PRESENT WHEN clause"
    /// (§2023:13.18.28.4 GR1 the same), and items subject to different PRESENT WHEN clauses may overlap. COBOL-85 has no
    /// PRESENT WHEN clause, and "Within a given print line, the printable items must be defined in ascending column number
    /// order such that each printable item defined occupies a unique sequence of contiguous character positions"
    /// (§1985:XIII.3.11.3 SR2; kb/Work PB5251). The consumer is <c>ReportSectionBinder.PresentWhenClausesOf</c>.</summary>
    GroupIndicateAsPresentWhen = 48,
    /// <summary>§2002:13.16.52.2 SR6 — "Data-name-2 shall be the name of a detail. It may be qualified by a report-name",
    /// with no restriction to the current report. §1985:XIII.3.19.3 SR2 requires a detail "described in the same report as
    /// the control footing report group in which the SUM clause appears". The consumer is <c>ReportSectionBinder</c>'s UPON
    /// phrase resolution.</summary>
    SumUponSameReport = 49,
    /// <summary>§2002:14.8.15.2 SR2 — "Report-name-1 may be used only if the referenced report description entry contains a
    /// CONTROL clause", the one condition (§2023:14.9.16.3 SR2 the same). §1985:XIII.3.20.3 SR6 adds that when a GENERATE
    /// report-name statement is specified the report description entry must include "no more than one detail report group".
    /// The consumer is <c>ReportWriterBinder</c>'s GENERATE binding.</summary>
    GenerateReportNameSingleDetail = 50,
    /// <summary>§2002:F.2 item 108 — "Index-names may be qualified in the same manner as data and condition-names, even in
    /// cases where uniqueness of reference may already be established", so from 2002 two tables may each declare one
    /// spelling and a reference that does not tell them apart is the ambiguity of §2023:8.4.2.2.3 SR1. §1985:VI.5.8.3 SR14:
    /// "Index-name-1 must be a unique word within the program", a rule of the declaration. The consumer is
    /// <c>DataBinder.ScreenIndexNameUniqueness</c>.</summary>
    IndexNameQualification = 51,
    /// <summary>§2002:13.16.10.2 SR1 — "If integer-1 is specified, integer-2 shall be greater than integer-1"
    /// (§2023:13.18.10.3 SR1 the same). The 1985 BLOCK CONTAINS clause has no syntax rules (§1985:VII.3.3) and says only
    /// that the two integers "refer to the minimum and maximum size of the physical record, respectively" (§1985:VII.3.3.3
    /// GR4), so at <c>--std 85</c> equal bounds translate (kb/Work PB5115). The consumer is <c>FileControlBinder</c>'s
    /// BLOCK CONTAINS binding.</summary>
    BlockContainsRangeOrdered = 52,
    /// <summary>§2002:8.8.4.1.1 — "A relation condition shall contain at least one reference to an operand that is not a
    /// literal", and the 2002 sign condition (§2002:8.8.4.1.6) states no such sentence. COBOL-85 asks a variable of both:
    /// "The relation condition must contain at least one reference to a variable" (§1985:VI.6.3.1.1) and
    /// "Arithmetic-expression-1 must contain at least one reference to a variable" (§1985:VI.6.3.1.5), so <c>IF 2 * 2 =
    /// 4</c> and <c>IF 1 + 1 IS POSITIVE</c> are outside the 1985 text (kb/Work PB5826; the warning severity is
    /// D-RELLITERAL's). The consumers are <c>StatementValidation.CheckRelationHasNonLiteralOperand</c> and
    /// <c>StatementValidation.CheckSignConditionHasVariable</c>.</summary>
    RelationOperandNotALiteral = 53,
    /// <summary>§2002:F.1 item 17 records the change: "In the previous COBOL standard, the value obtained for the remainder
    /// when the dividend and divisor have different signs differs depending on whether the receiving operand for the
    /// quotient is" signed; from 2002 the subsidiary quotient is signed (§2002:14.8.11.3 GR6, §2023:14.9.12.4 GR6c).
    /// §1985:VI.6.11.4 GR6 forms the remainder from "an intermediate field ... which contains the same number of digits,
    /// the same decimal point location, and the same presence or absence of a sign as the quotient", so <c>DIVIDE -11 BY 3
    /// GIVING Q REMAINDER R</c> with an unsigned Q gives -20 at <c>--std 85</c> (kb/Work PB5182). The consumer is
    /// <c>ArithmeticEmitter.UnsignedSubsidiary</c>.</summary>
    DivideRemainderSignedSubsidiary = 54,
    /// <summary>§2002:13.16.41.3 GR14b — "If a RELEASE statement is being executed, the EC-SORT-MERGE-RELEASE exception
    /// condition is set to exist and the execution of the RELEASE statement is unsuccessful" (§2023:13.18.43.4 GR14b the
    /// same); with the checking off the result is undefined (§2023:14.6.13.1.1) and the record is released (D-SMA (c)).
    /// COBOL-85 has no exception checking and states the outcome: "the output statement is unsuccessful and, except during
    /// execution of a RELEASE statement, the associated I-O status is set" (§1985:VII.3.8.4 GR10; kb/Work PB5124). The
    /// consumer is <c>SortBinder</c>'s RELEASE binding (<c>BoundRelease.RangeFailsStatement</c>).</summary>
    ReleaseOutsideRecordRangeRaisesException = 55,
    /// <summary>§2002:F.2 item 67 — "Data-names referenced in a data description entry need not be qualified when they are
    /// defined in the same group as the subject of the entry" (§2023:8.4.2.2.1 SR5 the rule). §1985:IV.4.3.8.1 SR3 excepts
    /// only "the case of redefinition": every other explicitly referenced name must be unique by qualification, so below
    /// 2002 the whole scope's candidates of an OCCURS DEPENDING ON, KEY or RENAMES operand are counted (kb/Work PB5165).
    /// The consumer is <c>SymbolTable.ClauseOperandsTakeImplicitQualifiers</c>.</summary>
    EntryClauseImplicitQualifiers = 56,
    /// <summary>From 2002 the INSPECT size rules are general rules whose violation is the run-time EC-RANGE-INSPECT-SIZE
    /// condition (§2002:14.5.12.1.5, "Size of replace items in INSPECT differs"; §2023:14.9.22.4 GR14, GR15 and GR22), so
    /// only literals are asked at compile time. §1985:VI.6.18.3 SR7, SR8 and SR9 state them as syntax rules: the size of
    /// the replacing operand "must be equal to the size of literal-1 or the data item referenced by identifier-3", so at
    /// <c>--std 85</c> a mis-sized identifier is refused (kb/Work PB5103). The consumer is
    /// <c>StatementValidation.InspectSizeRulesCoverDataItems</c>.</summary>
    InspectSizeRulesAreGeneralRules = 57,
    /// <summary>§2002:8.8.4.1.1 — a group in a relation "shall be treated as an elementary alphanumeric data item", so the
    /// elementary rule applies and a signed numeric operand is compared without its operational sign (§2023:8.8.4.2.5).
    /// §1985:VI.6.3.1.1.2 GR2 moves the numeric operand "to a group item of the same size", a move "exactly as if it were
    /// an alphanumeric to alphanumeric elementary move, except that there is no conversion of data" (§1985:VI.6.19.4 GR3),
    /// so its own character image, a SEPARATE sign included, is compared (kb/Work PB5106). The consumer is
    /// <c>ConditionRenderer.DropsOperationalSign</c>.</summary>
    GroupComparisonIsElementaryAlphanumeric = 58,
    /// <summary>§2002:8.8.4.1.1.3 — when native arithmetic is in effect "the comparison proceeds by the rules of native
    /// arithmetic" (§2023:8.8.4.2.4 the same). §1985:VI.6.3.1.1.1 knows only that "a comparison is made with respect to the
    /// algebraic value of the operands", so at <c>--std 85</c> a float operand lifts to its exact value and compares
    /// decimally (kb/Work PB5835). The consumer is <c>ConditionRenderer</c>'s numeric relation.</summary>
    NativeArithmeticComparison = 59,
    /// <summary>§2014:12.3.6.2 SR19 — "If literal-7 is an alphanumeric or national literal in hexadecimal format, the
    /// PICTURE SYMBOL phrase shall be present in the CURRENCY SIGN clause", and its NOTE exempts such a literal from the
    /// character limits ("Literals in hexadecimal format specified as literal-7 represent characters from the computer's
    /// runtime coded character set"); SR26 says of literal-8: "It shall be neither a figurative constant nor a hexadecimal
    /// literal". COBOL-2002 names no hexadecimal form: "Literal-7 shall be an alphanumeric or national literal that is not
    /// a figurative constant" (§2002:12.2.6.2 SR18), and "Literal-8 shall be an alphanumeric literal consisting of a single
    /// character. It shall not be a figurative constant" (§2002:12.2.6.2 SR22), so below 2014 a hexadecimal literal's
    /// decoded characters are checked like any other's (kb/Work PB5595). The consumers are <c>SpecialNamesBinder</c>'s
    /// CURRENCY SIGN binding (the literal-8 screen, the PICTURE SYMBOL requirement and the character exemption).</summary>
    CurrencyHexadecimalLiteralRules = 60,
    /// <summary>§2023:15.32.3 RV1/RV2 — FUNCTION EXCEPTION-STATEMENT returns "63 spaces" or "a 63-character alphanumeric
    /// character-string that is the name of the statement". §2002:15.27.2 RV1/RV2 and §2014:15.29.2 RV1/RV2 say 31 for both
    /// ("the returned value is 31 spaces", "a 31-character alphanumeric character-string"), so at <c>--std 2002</c> and
    /// <c>--std 2014</c> FUNCTION LENGTH (FUNCTION EXCEPTION-STATEMENT) is 31 and from 2023 it is 63 (kb/Work PB5698, PB5717;
    /// VCR row 7.117). Both editions are QUOTED. The consumers are <c>IntrinsicRenderer</c> (which passes the width) and
    /// <c>EcFunctions.StatementLength</c>.</summary>
    ExceptionStatementIs63Characters = 61,
    /// <summary>§2002:14.8.13.3 GR2 (and §2014:14.9.13.3 GR2) — "If an EXIT statement is executed within the range of a declarative
    /// procedure whose USE statement contains the GLOBAL phrase and that USE statement is specified in the same function, method, or
    /// program as the function format, method format, or program format EXIT statement respectively, the EC-FLOW-GLOBAL-EXIT exception
    /// condition is set to exist" (a GOBACK in a function or method operates as that EXIT, §2002:14.8.17.3 GR3 and GR4). §2023:14.9.14.4
    /// states no such rule — GR3 imports only GOBACK's GR3 and GR4, not GR6 — so from 2023 the condition is never set
    /// (docs/conformance/3/D-GLOBAL-EXIT.md). Both editions are QUOTED. The consumer is <c>EcBinder.NodeKindRaises</c>, which
    /// declares the raise only while this behaviour is inactive, so <c>CallEmitter.EmitExitProgram</c> and <c>EmitGoback</c>
    /// write the check only at 2002 and 2014 (kb/Work PB5715; VCR row 7.118).</summary>
    FlowGlobalExitIsNeverSet = 62,
    /// <summary>§2014:13.18.40.3 GR12 — a floating-point numeric-edited PICTURE's exponent "shall be '+9', '+99', '+999', '+9999',
    /// or '+9(n)' where n = 1, 2, 3, or 4", and its significand carries 1 through 36 digit positions (§2014:13.18.40.2 SR12;
    /// §2023:13.18.40.3 SR15, GR13 b). COBOL-2002 allows "'+9', '+99', '+999', or '+9(n)' where n = 1, 2, or 3"
    /// (§2002:13.16.38.3 GR12) and 1 through 31 digit positions for any numeric-edited item, counted over the significand and the
    /// exponent together (§2002:13.16.38.2 SR6; character-string-1 is both, §2002:13.16.38.3 GR12b), so at
    /// <c>--std 2002</c> <c>PIC +9.9E+9999</c> and <c>PIC +9(32)E+99</c> are refused (kb/Work PB5712; VCR row 7.119). Both editions
    /// are QUOTED. The consumer is <c>PictureAnalyzer.AnalyzeFloatEdited</c>; the implementor's own widest intermediate
    /// (<c>SendingValueTemp.WidestFloatEditedPic</c>) is analyzed at the newest edition and is not the program's PICTURE.</summary>
    FloatEditedPictureWideBounds = 63,
    /// <summary>§2014:12.3.6.2 SR25 — "If a source unit does not contain a CURRENCY SIGN clause that specifies '$' as the currency
    /// symbol (either as literal-7 or literal-8), the clause CURRENCY SIGN '$' PICTURE SYMBOL '$' is implied for that source
    /// unit", so SR28 confines that symbol, whose literal-7 is alphanumeric, to a numeric-edited item with usage display
    /// (§2023:12.3.7.3 SR25 and SR28 the same). COBOL-2002 has no such rule: its SR24 asks only a literal-7 that is written
    /// (§2002:12.2.6.2 SR24), so at <c>--std 2002</c> the default '$' is the COBOL currency sign and a national numeric-edited item
    /// may use it (§2002:13.16.38.3 GR2; kb/Work PB5614; VCR row 7.120). Both editions are QUOTED. The consumer is
    /// <c>ConfigurationSlice.FinalizeCurrencySigns</c>.</summary>
    ImpliedDollarCurrencyClause = 64,
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
            + "before 2023 it selects a record whose key is at most the lowest-ordinal position OPEN set, so at end "
            + "unless a key is that lowest value"),
        new(DialectBehavior.CompileTimeArithmeticImplementorDefined, "compile-time-arithmetic-mode", 2023,
            "12", "7.3.6.3 GR2", "E.2 item 6",
            "compile-time arithmetic expressions are evaluated in the implementor-defined mode; before 2023 the "
            + "standard prescribed standard arithmetic (the mode E.2 item 21 removed)"),
        new(DialectBehavior.FileSharingFacility, "file-sharing-facility", 2002,
            "7.39", "9.1.15", "A.4.7",
            "a file connector's sharing mode restricts other connectors, and Table 19 arbitrates every OPEN with '61', "
            + "only from COBOL-2002; at COBOL-85 the facility is not in effect and no connector restricts another"),
        new(DialectBehavior.PerformVaryingInitInnerBeforeAugmentOuter, "perform-varying-after-init-inner-first", 2002,
            "7.53", "14.9.28.4 GR13 e) 2", "§1985:VI.6.21.4 GR10d 1",
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
            "7.64", "14.9.51.4 GR29 b)", "§2002:14.8.47.3 GR25b",
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
        new(DialectBehavior.EvaluateObjectsInWhenPhraseOrder, "evaluate-objects-in-when-phrase-order", 2002,
            "7.69", "14.9.13.4 GR4 c)", "§1985:VI.6.13.4 GR1",
            "an EVALUATE selection object is evaluated only when its WHEN phrase is analyzed, and not after a false pair of "
            + "that phrase, only from COBOL-2002; COBOL-85 operates as if every selection subject and object were evaluated "
            + "before any comparison"),
        new(DialectBehavior.CloseWithLockLocksFileConnector, "close-with-lock-locks-file-connector", 2002,
            "7.70", "§2014:14.9.26.3 GR24", "§1985:II.2",
            "a CLOSE WITH LOCK locks the file connector, so another file-name of the same physical file opens it, only from "
            + "COBOL-2002; COBOL-85 locks the physical file and every OPEN of it through any file-name answers 38"),
        new(DialectBehavior.UnphrasedSizeErrorIsFatal, "unphrased-size-error-is-fatal", 2002,
            "7.71", "14.7.5", "§1985:VI.6.4.2 GR3",
            "a size error condition in an arithmetic statement with no ON SIZE ERROR phrase is a fatal exception condition "
            + "only from COBOL-2002; COBOL-85 leaves the affected resultant undefined (unchanged here), stores the others, "
            + "and the program continues"),
        new(DialectBehavior.EvaluateRefusesLiteralSubjectLiteralObject, "evaluate-refuses-literal-subject-literal-object", 2002,
            "7.75", "14.9.13.3 SR10", "§1985:VI.6.13.3 SR6 a",
            "an EVALUATE selection object that is a literal against a selection subject that is a literal is a syntax violation "
            + "(COBOL1634) only from COBOL-2002, whose combination table leaves that cell blank; COBOL-85 has no table and "
            + "selects by the value and class of the two literals"),
        new(DialectBehavior.CompileTimeArithmeticIsStandardDecimal, "compile-time-arithmetic-is-standard-decimal", 2014,
            "7.77", "§2014:7.3.5.2 GR1", "§2002:7.2.5.2 GR1",
            "compile-time arithmetic expressions are evaluated as if ARITHMETIC IS STANDARD-DECIMAL (34 digits) only from "
            + "COBOL-2014; COBOL-2002 prescribes standard arithmetic, whose intermediate data item holds 32 digits and "
            + "truncates each operation's result to them"),
        new(DialectBehavior.SelectWhenOnlyForFilePhraseStatements, "select-when-only-for-file-phrase-statements", 2014,
            "7.78", "§2014:13.18.51.3 GR2", "§2002:13.16.49.3 GR1",
            "the SELECT WHEN clauses of the file section select a record description entry for a WRITE or REWRITE only when the "
            + "statement has the FILE phrase, from COBOL-2014; COBOL-2002 evaluates them for every WRITE and REWRITE of the file, "
            + "so a record-name-1 statement there is formatted by the entry the conditions select"),
        new(DialectBehavior.UnstringZeroLengthSenderTerminates, "unstring-zero-length-sender-terminates", 2002,
            "7.80", "§2002:14.8.44.3 GR2", "§1985:VI.6.27.4 GR17",
            "an UNSTRING whose sending item is a zero-length item terminates immediately, running neither the ON OVERFLOW nor the "
            + "NOT ON OVERFLOW phrase, only from COBOL-2002; COBOL-85 has no such rule, so a written POINTER value of 1 is greater "
            + "than the sender's size of 0 and the statement is an overflow, while a statement with no POINTER phrase is none and makes its first "
            + "transfer from zero characters"),
        new(DialectBehavior.IntegerArgumentRoundedUnderStandardArithmetic, "integer-argument-rounded-under-standard-arithmetic", 2014,
            "7.81", "§2014:8.8.1.3.1.3.1", "§2002:15.34.3 RV1",
            "under ARITHMETIC IS STANDARD the argument of FUNCTION INTEGER that is a standard intermediate data item is rounded to 31 "
            + "digits, as every argument of a function with no equivalent arithmetic expression is, only from COBOL-2014; COBOL-2002 "
            + "says that argument-1 is not rounded, so INTEGER (1 - 1 / 10 ** 32) is 0 there and 1 from 2014"),
        new(DialectBehavior.SavedNextGroupForcesPageAdvance, "saved-next-group-forces-page-advance", 2002,
            "7.82", "13.18.37.4 GR4 a", "§1985:XIII.3.10.8 GR3 b",
            "the integer an absolute NEXT GROUP saved forces a page advance before the next non-dummy body group only from "
            + "COBOL-2002; COBOL-85 applies the page fit test to that group first and consults the saved integer only after "
            + "the advance a failed fit takes, so a group that fits stays on the page"),
        new(DialectBehavior.SumSizeErrorRaisedAtAddition, "sum-size-error-raised-at-addition", 2014,
            "7.83", "§2014:13.18.54.3 GR3", "§2002:13.16.52.3 GR3",
            "a size error in an addition into a SUM counter raises EC-REPORT-SUM-SIZE at the addition only from COBOL-2014; "
            + "COBOL-2002 only sets the size error indicator there and raises the condition when the entry is presented"),
        new(DialectBehavior.LinageViolatingOpenLeavesFileOpen, "linage-violating-open-leaves-file-open", 2002,
            "7.84", "§2002:13.16.32.3 GR6", "§1985:VII.4.3.4 GR1",
            "an OPEN OUTPUT whose LINAGE values violate the value rules answers '90' and leaves the file open, its LINAGE-COUNTER "
            + "at 0 until the CLOSE, only from COBOL-2002; at COBOL-85 the '90' is an unsuccessful statement, the file is not "
            + "open afterwards, and a WRITE answers '48' and a CLOSE '42'"),
        new(DialectBehavior.MoveSignIsTheValuesSign, "move-sign-is-the-values-sign", 2014,
            "7.85", "§2014:14.9.24.3 GR6d.2.a", "§2002:14.8.24.3 GR4b.1",
            "a signed numeric receiving item of a MOVE takes the sign of the value it stores, so a sender truncated to zero stores a "
            + "positive zero, only from COBOL-2014; COBOL-85 and COBOL-2002 place the sign of the sending operand, so MOVE -100 TO S99 "
            + "stores a negative zero"),
        new(DialectBehavior.IoStatusValuesOutside1985Lists, "io-status-values-outside-1985-lists", 2002,
            "7.86", "§2002:9.1.12", "§1985:VII.1.3.5",
            "the program's I-O status takes values the 1985 module lists do not have (the record operation and file sharing "
            + "conflicts, the record identification failure, the line sequential conditions) only from COBOL-2002; at COBOL-85 "
            + "each condition is reported by the 1985 value that names it or by its own documented 9x"),
        new(DialectBehavior.SumAdditionsInDependencyOrder, "sum-additions-in-dependency-order", 2002,
            "7.87", "§2002:13.16.52.3 GR6", "§1985:XIII.3.19.4 GR9a",
            "a report group's rolled additions run in dependency order, and a chain of SUM references that never terminates "
            + "(a counter that is its own addend included) is refused, only from COBOL-2002; COBOL-85 crossfoots in the order "
            + "the counters are defined, and a counter that is its own addend adds its value at the time of summation"),
        new(DialectBehavior.LineSequentialFileRules, "line-sequential-file-rules", 2023,
            "7.88", "14.9.30.4 GR15", "§2014:14.9.29.3 GR14",
            "a file the line delimiter frames follows the line sequential file's rules (a WRITE drops the record's trailing "
            + "spaces, a short line is padded with no length conflict, and an over-length line's remainder is the next READ's) "
            + "only from COBOL-2023; before, its records keep their size, and a READ of a line shorter than the minimum or longer "
            + "than the maximum record size is a record length conflict, '04', the long line truncated with no remainder, a REWRITE "
            + "of another size is '44' with the file unchanged, and an ADVANCING phrase or a LINAGE page writes no line of its own"),
        new(DialectBehavior.SumDataNameRules, "sum-data-name-rules", 2002,
            "7.89", "§2002:13.16.52.2 SR3", "§1985:XIII.3.19.3 SR1",
            "a SUM operand of the report section is screened by the data-name-1 rules only from COBOL-2002: a SOURCE or VALUE "
            + "entry and another report's sum counter are legal, and a sum counter of the SUM entry's own group must be a repeating "
            + "item; COBOL-85 asks a sum counter of the same group or of a lower control level of the same report, and adds a "
            + "non-repeating counter of the same group as a crossfoot"),
        new(DialectBehavior.ReportPageWidth, "report-page-width", 2002,
            "7.90", "§2002:13.16.37.3 GR5", "§1985:XIII.3.2.4 GR2",
            "every report has a page width, the PAGE clause's integer-2 or 999, that bounds its COLUMN operands and the line its items "
            + "are placed in, only from COBOL-2002; COBOL-85 gives a report no page width, so an item may stand at any column and "
            + "the line is bounded only by the report file's record"),
        new(DialectBehavior.SearchAllKeySubscriptSentences, "search-all-key-subscript-sentences", 2002,
            "7.91", "§2002:14.8.34.2 SR6", "§1985:VI.6.22.3 SR4",
            "a format 2 SEARCH ALL refuses a key whose first-index-name subscript is followed by a plus or a minus sign, and a condition-name "
            + "not subscripted by the first index-name, only from COBOL-2002; COBOL-85 asks only that each key be subscripted by the "
            + "first index-name along with other subscripts as required"),
        new(DialectBehavior.ReportControlOperandUniqueness, "report-control-operand-uniqueness", 2002,
            "7.92", "§2002:13.16.15.2 SR6", "§1985:XIII.3.7.3 SR2",
            "the operands of a CONTROL clause are told apart by their written references only from COBOL-2002, which "
            + "lets two operands refer to the same item; COBOL-85 requires each recurrence to identify a different data "
            + "item"),
        new(DialectBehavior.ReportVerticalIntegerLimit, "report-vertical-integer-limit", 2002,
            "7.93", "§2002:13.16.37.2 SR5", "§1985:XIII.3.8.3 SR2",
            "the PAGE, LINE and NEXT GROUP integers are bounded by the page limit or 9999 only from COBOL-2002; "
            + "COBOL-85 bounds each to three significant digits"),
        new(DialectBehavior.ReportCodeClauseOperand, "report-code-clause-operand", 2002,
            "7.94", "§2002:13.16.12.2 SR1", "§1985:XIII.3.6.3 SR1",
            "the CODE clause takes an alphanumeric literal of any length or an identifier only from COBOL-2002; "
            + "COBOL-85 takes a two-character nonnumeric literal alone"),
        new(DialectBehavior.ReportControlDataReferences, "report-control-data-references", 2002,
            "7.95", "§2002:13.16.51.2 SR4", "§1985:XIII.3.20.3 SR5",
            "a SOURCE clause or USE procedure of a footing, page heading or page footing may reference control data "
            + "only from COBOL-2002; COBOL-85 forbids the groups, parts and names of control data items there"),
        new(DialectBehavior.SumResetLevelOrder, "sum-reset-level-order", 2002,
            "7.96", "§2002:13.16.52.2 SR7", "§1985:XIII.3.19.3 SR4",
            "a control footing's RESET operand must be a higher level control than the footing's own only from "
            + "COBOL-2002; COBOL-85 also admits the footing's own level"),
        new(DialectBehavior.NextGroupNeedsLineNumber, "next-group-needs-line-number", 2002,
            "7.97", "§2002:13.16.35.2 SR4", "§1985:XIII.3.16.3 SR1",
            "a report group with no LINE clause may carry a NEXT GROUP clause only from COBOL-2002; COBOL-85 requires "
            + "at least one LINE NUMBER clause in its description"),
        new(DialectBehavior.ReportGroupPageFitAtCompileTime, "report-group-page-fit-at-compile-time", 2002,
            "7.98", "§2002:13.16.33.3 GR2", "§1985:XIII.3.8.3 SR9",
            "a body group whose relative lines cannot fit between FIRST DETAIL and its limit is a run-time condition "
            + "only from COBOL-2002; COBOL-85 refuses its description"),
        new(DialectBehavior.ReportGroupVerticalLimits, "report-group-vertical-limits", 2002,
            "7.99", "§2002:13.16.55.3 GR8", "§1985:XIII.3.10.6 GR2a",
            "a heading or footing group's vertical limits are TYPE clause GR8's only from COBOL-2002 (a report "
            + "heading ends before the page heading's first line); COBOL-85 gives each group type its printed limit (a "
            + "report heading ends before FIRST DETAIL)"),
        new(DialectBehavior.PageFootingFirstLineAbsolute, "page-footing-first-line-absolute", 2002,
            "7.100", "§2002:13.16.33.3 GR5", "§1985:XIII.3.15.3 SR9",
            "a page footing's first LINE clause may be relative, placed from the FOOTING integer, only from "
            + "COBOL-2002; COBOL-85 requires it to be absolute"),
        new(DialectBehavior.SumOnlyInControlFooting, "sum-only-in-control-footing", 2002,
            "7.101", "§2002:13.16.52.2 SR3", "§1985:XIII.3.19.3 SR3",
            "a SUM clause may stand in a detail, a control footing or a report footing only from COBOL-2002; COBOL-85 "
            + "admits it in a control footing alone"),
        new(DialectBehavior.ReportEntryClauseRules, "report-entry-clause-rules", 2002,
            "7.102", "§2002:13.13.2 SR10", "§1985:XIII.3.9.3 SR11",
            "a report group description entry is screened by its own syntax rules (a COLUMN clause with SOURCE, VALUE "
            + "or SUM; VALUE, JUSTIFIED and BLANK WHEN ZERO with COLUMN) only from COBOL-2002; COBOL-85 screens it by "
            + "the three entry formats and Table 1's permissible clause combinations"),
        new(DialectBehavior.SumCounterAlphanumericCap, "sum-counter-alphanumeric-cap", 2002,
            "7.103", "§2002:13.16.52.3 GR1", "§1985:XIII.3.19.4 GR1c",
            "the sum counter of an alphanumeric SUM entry has a digit per character position, uncapped, only from "
            + "COBOL-2002; COBOL-85 caps it at 18 characters"),
        new(DialectBehavior.GroupIndicateAsPresentWhen, "group-indicate-as-present-when", 2002,
            "7.104", "§2002:13.16.26.3 GR1", "§1985:XIII.3.11.3 SR2",
            "a GROUP INDICATE entry stands as a PRESENT WHEN clause, so it may share columns with another item of its "
            + "line, only from COBOL-2002; COBOL-85 has no PRESENT WHEN and every printable item of a line occupies its "
            + "own positions"),
        new(DialectBehavior.SumUponSameReport, "sum-upon-same-report", 2002,
            "7.105", "§2002:13.16.52.2 SR6", "§1985:XIII.3.19.3 SR2",
            "the UPON phrase may name a detail of another report only from COBOL-2002, where data-name-2 may be "
            + "qualified by a report-name; COBOL-85 requires a detail of the control footing's own report"),
        new(DialectBehavior.GenerateReportNameSingleDetail, "generate-report-name-single-detail", 2002,
            "7.106", "§2002:14.8.15.2 SR2", "§1985:XIII.3.20.3 SR6",
            "GENERATE report-name needs only a CONTROL clause from COBOL-2002; COBOL-85 also requires the report to "
            + "hold no more than one detail report group"),
        new(DialectBehavior.IndexNameQualification, "index-name-qualification", 2002,
            "7.107", "§2002:F.2 item 108", "§1985:VI.5.8.3 SR14",
            "an index-name may be qualified, so two tables may declare one spelling, only from COBOL-2002; COBOL-85 "
            + "requires each index-name to be a unique word within the program"),
        new(DialectBehavior.BlockContainsRangeOrdered, "block-contains-range-ordered", 2002,
            "7.108", "§2002:13.16.10.2 SR1", "§1985:VII.3.3.3 GR4",
            "a BLOCK CONTAINS integer-1 TO integer-2 whose integer-2 is not greater than integer-1 is refused only "
            + "from COBOL-2002; the COBOL-85 clause has no syntax rules and equal bounds are legal"),
        new(DialectBehavior.RelationOperandNotALiteral, "relation-operand-not-a-literal", 2002,
            "7.109", "§2002:8.8.4.1.1", "§1985:VI.6.3.1.1",
            "a relation condition needs an operand that is not a literal, and a sign condition needs nothing, only "
            + "from COBOL-2002; COBOL-85 requires a reference to a variable in each, so an expression of literals alone "
            + "draws the warning"),
        new(DialectBehavior.DivideRemainderSignedSubsidiary, "divide-remainder-signed-subsidiary", 2002,
            "7.110", "§2002:F.1 item 17", "§1985:VI.6.11.4 GR6",
            "the remainder of a DIVIDE is formed from a signed subsidiary quotient only from COBOL-2002; COBOL-85 "
            + "gives the subsidiary quotient identifier-3's presence or absence of a sign"),
        new(DialectBehavior.ReleaseOutsideRecordRangeRaisesException, "release-outside-record-range-raises-exception", 2002,
            "7.111", "§2002:13.16.41.3 GR14b", "§1985:VII.3.8.4 GR10",
            "a RELEASE of a record outside the RECORD clause's range raises EC-SORT-MERGE-RELEASE, the record "
            + "released when checking is off, only from COBOL-2002; COBOL-85 makes the statement unsuccessful"),
        new(DialectBehavior.EntryClauseImplicitQualifiers, "entry-clause-implicit-qualifiers", 2002,
            "7.112", "§2002:F.2 item 67", "§1985:IV.4.3.8.1 SR3",
            "a name referenced in a data description entry clause takes the groups above the entry's subject as "
            + "implicit qualifiers only from COBOL-2002; COBOL-85 counts every candidate of the scope"),
        new(DialectBehavior.InspectSizeRulesAreGeneralRules, "inspect-size-rules-are-general-rules", 2002,
            "7.113", "§2023:14.9.22.4 GR14", "§1985:VI.6.18.3 SR7",
            "INSPECT's equal-size rules over identifiers are general rules raising EC-RANGE-INSPECT-SIZE only from "
            + "COBOL-2002; COBOL-85 states them as syntax rules over data items"),
        new(DialectBehavior.GroupComparisonIsElementaryAlphanumeric, "group-comparison-is-elementary-alphanumeric", 2002,
            "7.114", "§2002:8.8.4.1.1", "§1985:VI.6.3.1.1.2 GR2",
            "a numeric operand compared with a group is compared by the elementary rule, without its operational "
            + "sign, only from COBOL-2002; COBOL-85 moves it to a group item of the same size, its sign character "
            + "included"),
        new(DialectBehavior.NativeArithmeticComparison, "native-arithmetic-comparison", 2002,
            "7.115", "§2002:8.8.4.1.1.3", "§1985:VI.6.3.1.1.1",
            "a floating-point operand under native arithmetic is compared by the rules of native arithmetic only from "
            + "COBOL-2002; COBOL-85 compares the algebraic values"),
        new(DialectBehavior.CurrencyHexadecimalLiteralRules, "currency-hexadecimal-literal-rules", 2014,
            "7.116", "§2014:12.3.6.2 SR19", "§2002:12.2.6.2 SR18",
            "a hexadecimal currency literal-7 requires the PICTURE SYMBOL phrase and is exempt from the currency "
            + "string's character limits, and literal-8 may not be hexadecimal, only from COBOL-2014; COBOL-2002 asks "
            + "an alphanumeric literal that is no figurative constant and checks its characters whatever its form"),
        new(DialectBehavior.ExceptionStatementIs63Characters, "exception-statement-is-63-characters", 2023,
            "7.117", "§2023:15.32.3 RV1", "§2014:15.29.2 RV1",
            "FUNCTION EXCEPTION-STATEMENT returns 63 characters (63 spaces when no location was saved) only from COBOL-2023; "
            + "COBOL-2002 and COBOL-2014 return 31 characters (31 spaces when no location was saved)"),
        new(DialectBehavior.FlowGlobalExitIsNeverSet, "flow-global-exit-is-never-set", 2023,
            "7.118", "§2023:14.9.14.4 GR3", "§2002:14.8.13.3 GR2",
            "an EXIT PROGRAM, EXIT METHOD or EXIT FUNCTION executed within the range of a USE ... GLOBAL declarative of the same "
            + "element sets EC-FLOW-GLOBAL-EXIT only at COBOL-2002 and COBOL-2014; COBOL-2023 states no rule that sets it"),
        new(DialectBehavior.FloatEditedPictureWideBounds, "float-edited-picture-wide-bounds", 2014,
            "7.119", "§2014:13.18.40.3 GR12", "§2002:13.16.38.3 GR12",
            "a floating-point numeric-edited PICTURE may carry a four-digit exponent and a 36-digit significand only from COBOL-2014; "
            + "COBOL-2002 allows an exponent of three digits and 31 digit positions, significand and exponent counted together"),
        new(DialectBehavior.ImpliedDollarCurrencyClause, "implied-dollar-currency-clause", 2014,
            "7.120", "§2014:12.3.6.2 SR25", "§2002:12.2.6.2 SR24",
            "a unit with no CURRENCY SIGN clause for '$' is taken to have written one, whose alphanumeric literal-7 confines '$' to usage display, "
            + "only from COBOL-2014; at COBOL-2002 the default '$' has no literal-7 and a national item may use it"),
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
