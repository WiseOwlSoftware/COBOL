// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// Control flow statements: PERFORM, IF, EVALUATE, GO TO, SEARCH, ALTER, USE,
// EXIT, STOP, CONTINUE, NEXT SENTENCE.
// Imported by CobolParserCore.g4 — no options block.

parser grammar CobolControlFlow;

options {
    tokenVocab = CobolLexer;
}

// ==========================================
// PERFORM / END-PERFORM (§14.9.28)
// ==========================================

performStatement
    // Out-of-line: explicit forms to avoid greedy "PERFORM target" swallowing options
    : PERFORM procedureName performTimes                                       // PERFORM para N TIMES
    | PERFORM procedureName performUntil                                       // PERFORM para UNTIL cond
    | PERFORM procedureName performVarying                                     // PERFORM para VARYING ...
    | PERFORM procedureName (THRU | THROUGH) procedureName performOptions?     // PERFORM para THRU para [options]
    // Inline forms — Formats 2 & 3 MERGED into ONE inline alternative (design §1.4 ⇒ zero cross-format lookahead):
    //   ≥1 WHEN ⇒ Format 3 (exception-checking, §14.9.28.2 Format 3), enforced at BIND
    //   (PF3-STRUCT-WHEN-REQUIRED / COBOLNET1597); no WHEN ⇒ Format 2 (inline). performInlineHead? absent +
    //   no WHEN = the old bare-block inline PERFORM; performInlineHead = performOptions+ = the old options inline.
    // MUST precede the simple `PERFORM procedureName` below: `PERFORM LOCATION …` is genuinely ambiguous
    // (LOCATION is a cobolWord ⇒ a valid out-of-line target), and only the inline arm's trailing END_PERFORM
    // disambiguates — so it is tried first; a period-terminated `PERFORM LOCATION.` has no END_PERFORM and
    // correctly falls through to the out-of-line alternative (the continuity invariant).
    | PERFORM performInlineHead? statementBlock*
        performWhenPhrase* performWhenOther? performWhenCommon? performFinally?
      END_PERFORM
    | PERFORM procedureName                                                    // PERFORM para (simple, out-of-line)
    ;

// The inline head: Format-2 loop control (TIMES/UNTIL/VARYING) OR the Format-3 [WITH] LOCATION phrase.
performInlineHead
    : performOptions+
    | performLocationPhrase
    ;

// [WITH] LOCATION (§14.9.28.2 Format 3, COBOL-2023) — WITH is an optional word (§8.3.2.4.3); bare LOCATION is
// 2023-only because below 2023 `PERFORM LOCATION` is an ordinary out-of-line PERFORM of a paragraph named
// LOCATION (the continuity invariant — LOCATION remains a cobolWord at every edition).
performLocationPhrase
    : WITH LOCATION
    | {is2023()}? LOCATION
    ;

// A Format-3 WHEN phrase (§14.9.28.2). Two DISJOINT operand forms; each operand list's CONTINUATION is bounded
// by whenOperandAhead() so it cannot annex the leading verb of imperative-statement-2 (design §1.2/§1.5). The
// first operand after WHEN / WHEN EXCEPTION is taken UNCONDITIONALLY (superset posture — a bad first operand
// binds and the binder emits the specific COBOLNET0711 rather than a generic parse error).
performWhenPhrase
    : WHEN EXCEPTION performWhenModeList statementBlock*      // EXCEPTION { INPUT|OUTPUT|I-O|EXTEND | file-name-1… }
    | WHEN           performWhenEcList   statementBlock*      // exception-name-1… | exception-name-2 FILE file-name-2…
    ;

// EXCEPTION { INPUT | OUTPUT | I-O | EXTEND | {file-name-1}… }. The figure's "IO" denotes I-O (§8.9; a standard
// typesetting defect — every sibling format prints I-O), so the existing I_O token is reused. A single WHEN
// EXCEPTION selects exactly ONE mode OR a file-name list — never a mix; bind-enforced (COBOLNET1598).
performWhenModeList
    : INPUT | OUTPUT | I_O | EXTEND
    | fileName ({whenOperandAhead()}? fileName)*             // gated CONTINUATION only (first operand unconditional)
    ;

// { exception-name-1 }… | { exception-name-2 FILE file-name-2 }… — the EC set is open (EC-USER-*) ⇒ cobolWord.
performWhenEcList
    : performWhenEcItem ({whenOperandAhead()}? performWhenEcItem)*   // gated CONTINUATION only
    ;

performWhenEcItem
    : cobolWord (FILE fileName)*             // inner FILE-loop is self-bounding (FILE ∉ cobolWord, leads no statement)
    ;
// KNOWN LIMITATION (documented; masked while the F3 runtime is staged): a WHEN body that is a BARE 2023
// inline-method-invocation (`WHEN EC-X  obj(args)`) whose object is a cobolWord can be annexed here as a
// spurious operand — whenOperandAhead() is token-based and cannot see the following '('. The interceptor
// wave must add an LA(2)==LPAREN gate (or equivalent). Any WHEN body with a preceding statement is unaffected.

performWhenOther
    : WHEN OTHER  EXCEPTION? statementBlock*   // the 2nd EXCEPTION is an optional word (§8.3.2.4.3)
    ;

performWhenCommon
    : WHEN COMMON EXCEPTION? statementBlock*   // the 2nd EXCEPTION is an optional word (§8.3.2.4.3)
    ;

performFinally
    : FINALLY statementBlock*
    ;

performTarget
    : procedureName ((THRU | THROUGH) procedureName)?
    ;

performOptions
    : performTimes
    | performUntil
    | performVarying
    ;

// §14.9.28.2 Format 2: `{identifier-1 | integer-1} TIMES` — an identifier includes a function-identifier
// (§8.4.3.1.2 / §8.4.3.2.4 GR1: it references a temporary item; SR2's "an integer" is met by an integer-type
// function). functionCall FIRST — it begins with the FUNCTION token, so the alternation is unambiguous (the
// DISPLAY / INSPECT / FROM slots follow the same shape). kb/Work PB86: the arm was missing (a parse error), and
// the keyword-omitted spelling bound but ran ONCE.
performTimes
    : (integerLiteral | functionCall | dataReference) TIMES
    ;

performUntil
    : (WITH? TEST (BEFORE | AFTER))? UNTIL condition
    | UNTIL EXIT                                       // §14.9.28.4 GR11 (2023) — an infinite loop; SR8 forbids TEST here
    ;

performVarying
    : (WITH? TEST (BEFORE | AFTER))?
      VARYING dataReference FROM arithmeticExpression
      (BY arithmeticExpression)?    // BY is optional per COBOL-85 spec (default = 1)
      UNTIL condition
      performVaryingAfter*
    ;

performVaryingAfter
    : AFTER dataReference FROM arithmeticExpression
      (BY arithmeticExpression)?    // BY is optional per COBOL-85 spec (default = 1)
      UNTIL condition
    ;

// ==========================================
// IF / END-IF (§14.9.19)
// ==========================================

ifStatement
    : IF condition THEN?
      statementBlock*
      (ELSE statementBlock*)?
      END_IF?
    ;

// ==========================================
// EVALUATE / END-EVALUATE (§14.9.13)
// ==========================================

evaluateStatement
    : EVALUATE evaluateSubject (ALSO evaluateSubject)*
      evaluateWhenClause+
      END_EVALUATE?
    ;

evaluateSubject
    : booleanLiteral                                     // EVALUATE TRUE / FALSE
    | valueOperand (IS? NOT? classCondition)?            // EVALUATE X [NUMERIC / class test]
    ;
    // NOTE (DEVLOG 621): an EVALUATE boolean-expression subject is STAGED RESIDUE with the condition-context
    // boolean forms (see comparisonExpression) — the boolean OPERATORS work in COMPUTE Format 2 only.

// One or more consecutive WHEN phrases share the following imperative (ISO 1989:1985
// 14.8.4): "WHEN a  WHEN b  WHEN c  imperative" executes the imperative if a, b, OR c
// matches. Each phrase is bound to its own match arm over the shared body.
evaluateWhenClause
    : evaluateWhenPhrase+ statementBlock*
    | WHEN OTHER statementBlock*
    ;

evaluateWhenPhrase
    : WHEN evaluateWhenGroup (ALSO evaluateWhenGroup)*
    ;

// ⛔ EXACTLY ONE selection-object per position — NOT a list (fix-queue PB45, the open half).
// §14.9.13.2's general format is `{ { WHEN selection-object [ ALSO selection-object ] … } … imperative-statement-1 } …`
// (verified against the PRINTED page, folio 618): selection-objects repeat ONLY through ALSO — which is
// evaluateWhenGroup's own caller — never by juxtaposition. §14.9.13.3 SR2 fixes the count against the subjects
// ("The number of selection objects within each set of selection objects shall be equal to the number of selection
// subjects"), and the transcription's 900-dpi figure note records the object brace as "a single pair of braces
// spanning ten stacked alternatives — exactly one shall be selected".
// ⚠ THIS WAS `evaluateWhenItem+`, AND THE UNLICENSED REPETITION SILENTLY MISCOMPILED A FUNCTION-IDENTIFIER OBJECT.
// `WHEN FUNCTION SQRT(W-Z) > 1` has two readings: the CORRECT one (a relation condition whose left operand is the
// function-identifier) and a PEELED one that takes `FUNCTION SQRT` as a bare zero-argument object and re-reads the
// ARGUMENT parenthesis as a second, parenthesised object `(W-Z) > 1`. The greedy/correct reading cannot consume the
// trailing `> 1` once the item ends, so under `+` it was not viable and only the peel survived — and because
// `valueOperand` precedes `condition` in evaluateWhenItem, ANTLR preferred it. The result was a VALUE object under an
// `EVALUATE TRUE` subject: a clean compile that threw at RUN TIME (and, for an alphanumeric function, a raw parse
// error). `FUNCTION PI > 1` always worked precisely because it has no argument parenthesis to peel.
// ⛔ THE FIX IS THE ARITY, NOT THE ALTERNATIVE ORDER. Putting `condition` before `valueOperand` would also retarget
// `EVALUATE X / WHEN Y` where Y is a level-88 condition-name — a value comparison silently becoming a condition test —
// and §14.9.13.4 Table 15 makes the object's legality depend on the SUBJECT, which no context-free ordering can
// express. With one item the peel is simply not a parse of this rule, and the alternative order is left untouched.
// Pinned by EvaluateSelectionObjectArityDriftTests.
evaluateWhenGroup
    : NOT? evaluateWhenItem
    ;

evaluateWhenItem
    : valueRange                         // WHEN A THRU N, WHEN 1 THRU 10
    | valueOperand                       // single value: "A", 1, VAR
    | condition                          // for EVALUATE TRUE / complex WHEN
    | ANY                                // match anything
    ;

// ==========================================
// GO TO (§14.9.17)
// ==========================================

goToStatement
    : GO TO? procedureName? (procedureName)* (DEPENDING ON? dataReference)?
    ;

// ==========================================
// SEARCH (§14.9.37 — Linear Search)
// ==========================================

searchStatement
    : SEARCH dataReference (VARYING dataReference)?
      searchAtEndClause?
      searchWhenClause+
      END_SEARCH?
    ;

searchWhenClause
    : WHEN condition statementBlock*
    ;

// ISO 5.2.3: AT is printed WITHOUT an underline in every AT END phrase in the standard — measured across all
// 11 occurrences on pages 600-829, none underlined. It is an OPTIONAL WORD, so `SEARCH … END …` is CONFORMING
// ISO, not a vendor extension. This rule was the lone hold-out: readAtEnd, returnAtEndPhrase and
// writeAtEndOfPage already carried `AT?`, and this one instead admitted the AT-less form through a separate
// alternative labelled a NIST/IBM extension — which both mis-stated the standard and silently denied the
// AT-less spelling to the NOT branch. `AT?` subsumes that alternative.
searchAtEndClause
    : AT? END statementBlock
      (NOT AT? END statementBlock)?
    ;

// ==========================================
// SEARCH ALL (§14.9.37 — Binary Search)
// ==========================================

searchAllStatement
    : SEARCH ALL dataReference
      searchAllKeyPhrase?
      searchAtEndClause?
      searchAllWhenClause+
      END_SEARCH?
    ;

searchAllKeyPhrase
    : KEY IS dataReference
    ;

searchAllWhenClause
    : WHEN condition statementBlock*
    ;

// ==========================================
// ALTER (§14.9.2)
// ==========================================

alterStatement
    : ALTER alterEntry+
    ;

alterEntry
    : procedureName TO (PROCEED TO)? procedureName
    ;

// ==========================================
// USE (§14.9.49, declaratives)
// ==========================================

// ⛔ THE OPTIONAL WORDS OF A FORMAT ARE A PROPERTY OF THE FORMAT, not of what the witness corpus happens to
// spell. §14.9.49.2 was measured off the PDF's vector rectangles (`figure_extract.py 804`, confirmed on the
// 600 dpi render of PDF page 804 / printed folio 774, and the transcription's own figure notes agree): in
// Format 1 `USE`, `GLOBAL`, `EXCEPTION`, `ERROR`, `INPUT`, `OUTPUT`, `I-O` and `EXTEND` are underlined while
// `AFTER`, `STANDARD`, `PROCEDURE` and `ON` are NOT; in Formats 3 and 4 only `USE` and the bracketed keywords
// are underlined, and `AFTER` is not. §5.2.3 and §8.3.2.4.3 make every non-underlined uppercase word an
// OPTIONAL word "specified at the user's option with no effect on the semantics of the format", so all five
// are omittable. Until kb/Work PB332 this rule required AFTER and PROCEDURE and relaxed only STANDARD and ON —
// the two words the CCVS witnesses happened to omit. The corpus is a regression net, never the authority.
//
// ⛔ FORMAT ORDER IS LOAD-BEARING, and only because AFTER became optional. `useOnTarget`'s `fileName+` arm
// accepts any cobolWord, and CONDITION / EC / OBJECT / EO are context-sensitive words — §8.10 lists them and
// makes such a word "reserved in the specified language construct or context … otherwise it is treated as a
// user-defined word", which is exactly what `cobolWord` admits — so `USE EXCEPTION CONDITION EC-ALL` can be
// read as Format 1 over two file-names. The
// specific formats are therefore listed FIRST and ANTLR's first-match-wins prediction settles it; Format 1
// still claims `USE EXCEPTION F1` unambiguously, because Formats 3 and 4 demand CONDITION/EC/OBJECT/EO right
// after EXCEPTION and Format 3 demands at least one entry after them.
useStatement
    // Format 2: USE [GLOBAL] BEFORE REPORTING identifier-1
    : USE GLOBAL? BEFORE REPORTING procedureName
    // Format 3 (exception-name, EC model 2002+ — binder-gated): USE [AFTER] {EXCEPTION CONDITION | EC}
    // {exception-name-1 | exception-name-2 {FILE file-name-2}…}… (ISO §14.9.49.2; SR12: EC ≡ EXCEPTION
    // CONDITION). Exception-names are cobolWords — an OPEN set (EC-USER-*, §14.6.13.1.1 / §7.3.25.3 SR2), so
    // name validation (and SR13/SR14) is the binder's, never a token enumeration. (§7.3.25.3 is the TURN
    // compiler directive's syntax rules, which is where the EC-USER-* name shape is fixed.)
    | USE AFTER? (EXCEPTION CONDITION | EC) useEcEntry+
    // Format 4 (ISO §14.9.49.2 — USE [AFTER] {EXCEPTION OBJECT | EO} {class-name | interface-name}, ONE
    // operand; SR15: EO ≡ EXCEPTION OBJECT): the exception-OBJECT declarative selector (GR14 — class-or-
    // subclass / IMPLEMENTS match; GR3: F4 REPLACES the F1/F3 tiers for object raises). EC-OO wave.
    | USE AFTER? (EXCEPTION OBJECT | EO) cobolWord
    // Format 1: USE [GLOBAL] [AFTER] [STANDARD] {EXCEPTION | ERROR} [PROCEDURE] [ON] {file-name+ | INPUT | OUTPUT | I-O | EXTEND}
    | USE GLOBAL? AFTER? STANDARD? (EXCEPTION | ERROR) PROCEDURE? ON? useOnTarget
    // X3.23-1985 debug-module format (obsolete '85 element DELETED by ISO 2002 — the whole facility,
    // DEBUG-* registers included, is absent from the 2023 text): USE FOR DEBUGGING ON {cd-name-1 |
    // [ALL REFERENCES OF] identifier-1 | file-name-1 | procedure-name-1 | ALL PROCEDURES}… .
    // Accepted-inert at 85 (the section is compiled as if comment lines — the conforming posture when
    // WITH DEBUGGING MODE is absent, and our permanently-off object-time switch when present); the
    // EditionValidator flags it COBOLNET0902 ≥2002 (`use-for-debugging-removed-2002`, VCR Table 7
    // row 7.17). ON is written by every CCVS-85 witness but accepted as optional (house optional-word
    // tolerance, cf. Format 1's ON).
    | USE FOR DEBUGGING ON? useDebugTarget+
    ;

// One '85 debug-operand: ALL PROCEDURES / [ALL REFERENCES OF] identifier / a bare name (file-name,
// procedure-name, cd-name, or unqualified identifier — the binder never distinguishes: the whole
// section is inert). dataReference covers the OF/IN-qualified identifier forms (DB201A writes
// `ABC1 OF AB2 OF A1`). The ALL-led alternatives precede the bare form (first-alternative-wins).
useDebugTarget
    : ALL PROCEDURES
    | ALL REFERENCES OF? dataReference
    | dataReference
    ;

// One Format-3 scope entry: an exception-name, optionally file-scoped ({FILE file-name-2}… — each file carries
// its own FILE word per the §14.9.49.2 format figure; SR13 requires an EC-I-O name when FILE is given).
useEcEntry
    : cobolWord (FILE fileName)*
    ;

useOnTarget
    : INPUT                     // all files opened for INPUT
    | OUTPUT                    // all files opened for OUTPUT
    | I_O                       // all files opened for I-O
    | EXTEND                    // all files opened for EXTEND
    | fileName+                 // specific file name(s)
    ;

// ==========================================
// EXIT (§14.9.14)
// ==========================================

exitStatement
    // EXIT PROGRAM [RAISING …] (ISO §14.9.14 Format 2 — the RAISING tail re-raises in the activator; archaic-
    // flagged in 2023, parsed at every edition and binder-gated). The METHOD/FUNCTION forms share the tail
    // syntactically (Formats 3/4); their semantics land with the OO wave.
    : EXIT ( PROGRAM raisingPhrase? | PERFORM CYCLE? | SECTION | PARAGRAPH | METHOD raisingPhrase? | FUNCTION raisingPhrase? )?
    ;

// ==========================================
// STOP (§14.9.42)
// ==========================================

stopStatement
    : STOP RUN (statusPhrase)?   // status phrase introduction-gated at BIND time (StatementBinder.BindStop → Check(StopRunStatus2002))
    | STOP literal                     // STOP literal (Format 2, obsolete)
    ;

// The shared run-unit-termination status phrase (ISO §14.9.42.2 STOP / §14.9.18.2 GOBACK). ONE rule referenced
// by BOTH stopStatement and gobackStatement — annex item 32: "GOBACK … now allows the same status phrase as
// the STOP statement" (feedback_singular_pattern). STOP-status is a 2002 introduction; GOBACK-status is 2023.
// [WITH] {ERROR|NORMAL} [STATUS [id|lit]] — WITH is an OPTIONAL word (§5.2.3, not underlined); exactly one of the
// underlined keywords ERROR/NORMAL is REQUIRED (§14.9.42.2/§14.9.18.2 brace group); the STATUS keyword introduces
// the optional operand (the operand is bracketed = optional). The former rule wrongly required WITH, bound STATUS
// to its operand, and admitted a keyword-less `STATUS operand` (P13 Wave-I review findings 1/2/3).
statusPhrase
    : WITH? (ERROR | NORMAL) (STATUS (dataReference | literal)?)?
    ;

// ==========================================
// CONTINUE (§14.9.9) / NEXT SENTENCE (a phrase of the IF statement, §14.9.19)
// ==========================================

// CONTINUE [AFTER arithmetic-expression-1 SECONDS] (ISO §14.9.9). Plain CONTINUE is a 1985-continuous no-op;
// the AFTER … SECONDS timed-pause phrase is a COBOL-2023 addition (introduction-gated on the phrase, not the verb).
continueStatement
    : CONTINUE (AFTER arithmeticExpression SECONDS)?
    ;

nextSentenceStatement
    : NEXT SENTENCE
    ;
