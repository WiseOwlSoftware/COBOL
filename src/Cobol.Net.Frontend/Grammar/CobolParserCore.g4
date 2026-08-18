// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

parser grammar CobolParserCore;

options {
    tokenVocab = CobolLexer;
    superClass = CobolParserCoreBase;
}

// Virtual token: ZERO rewritten to ZERO_ARITH by token rewriter when
// adjacent to arithmetic operators. Keeps ZERO and ZERO_ARITH in
// completely disjoint grammar rules — zero ambiguity.
tokens { ZERO_ARITH }

import CobolExpressions, CobolData, CobolSpecialNames, CobolReportWriter, CobolIO, CobolControlFlow, CobolOO, CobolScreen, CobolWords;

// ==========================================
// CONTEXT-SENSITIVE KEYWORDS
// ==========================================
// The cobolWord rule — tokens that have special meaning in specific contexts but are NOT COBOL-85 reserved
// words, so they may also appear as user-defined names — is GENERATED into the imported grammar
// Grammar/Core/CobolWords.g4 from tests/version-matrix/cobol-words.json (the nameSlot=true rows) by
// scripts/gen-cobol-words.ps1 (rearchitecture PHASE 04, Group A). It is single-sourced with the lexer
// subscript-trigger set (Parsing/CobolLexerWordSet.g.cs) and cross-checked by CobolWordsDriftTests, so the
// three former hand-synced copies (this rule, the lexer _dataNameTokens set, and the §8.9 ReservedWords table)
// can no longer silently desync. Do NOT re-add the rule here: edit cobol-words.json and re-run the generator.

// ==========================================
// ERROR RECOVERY
// ==========================================
//
// Statement-level sync: on parse error, skip to the nearest
// statement boundary (period or END-xxx terminator).
//
// @parser::members {
//     private void syncToStatementBoundary() {
//         while (_input.LA(1) != DOT
//             && !_input.LT(1).getText().startsWith("END-")
//             && _input.LA(1) != EOF) {
//             _input.consume();
//         }
//     }
// }
//
// Error nodes in AST:
//     public override void VisitErrorNode(IErrorNode node)
//     {
//         ast.Add(new ErrorNode(node.GetText(), node.Symbol.Line));
//     }

// --- top level ---

compilationUnit
    : compilationGroup* EOF
    ;

compilationGroup
    : (programUnit | classDefinition | interfaceDefinition)+   // OO/2002 rules live in Core/CobolOO.g4; introduction-gated post-bind by VersionConformancePass ParseArm.VisitClass/InterfaceDefinition (rearch 14g.3)
    ;

programUnit
    : identificationDivision
      environmentDivision?
      dataDivision?
      procedureDivision?
      nestedProgram*
      endProgramHeader?
    ;

nestedProgram
    : identificationDivision
      environmentDivision?
      dataDivision?
      procedureDivision?
      nestedProgram*
      endProgramHeader
    ;

endProgramHeader
    : END (PROGRAM | FUNCTION) programName DOT
    ;

// ==========================================
// IDENTIFICATION DIVISION
// ==========================================

identificationDivision
    : IDENTIFICATION DIVISION DOT identificationBody
    ;

identificationBody
    : programIdParagraph identificationParagraph*
    | functionIdParagraph identificationParagraph*
    ;

// FUNCTION-ID paragraph (COBOL-2002 user-defined function, ISO §11.5). The function unit is otherwise an
// ordinary source unit (its own ENVIRONMENT/DATA/PROCEDURE DIVISION USING…RETURNING) and is compiled as a
// callable program named after the function; a FUNCTION user-name(args) reference lowers onto it (M2-UDF-1,
// DEVLOG 615). Format 2 — `IS PROTOTYPE` (ISO §11.5 :13127 / §10.6) — is a signature-only prototype unit
// (LINKAGE-only data + a header-only procedure division, §10.6.2 SR4): it registers a signature but emits NO
// body, so a caller resolves a separately-compiled definition across the run unit (M2-UDF-3). The optional tail
// is a unique-leading-token additive change on a LOCAL rule (never a shared core), gated 2002+.
functionIdParagraph
    : FUNCTION_ID DOT programName (IS? PROTOTYPE)? DOT   // IS PROTOTYPE introduction-gated post-bind by VersionConformancePass.Run (bound-arm over CallUnit.IsPrototype; rearch 14g.5); position-safe (dedicated tail, programName consumes a bare name)
    ;

// ------------------------------------------
// PROGRAM-ID paragraph
// ------------------------------------------

// ISO §11.4.2: PROGRAM-ID. program-name [AS literal] [IS {COMMON|INITIAL|RECURSIVE}… PROGRAM].
// IS and the trailing PROGRAM are optional noise words around the attribute list (IC401M writes
// `IC401M IS INITIAL.`); the attribute list itself stays required inside the group.
programIdParagraph
    : PROGRAM_ID DOT programName (IS? programIdAttributes PROGRAM?)? DOT
    ;

programName
    : cobolWord
    ;

programIdAttributes
    : programIdAttribute+
    ;

programIdAttribute
    : commonProgramAttribute
    | literalAttribute
    | dataReferenceAttribute
    ;

commonProgramAttribute
    : INITIAL_
    | COMMON
    | RECURSIVE
    | GLOBAL
    ;

literalAttribute
    : STRINGLIT
    | INTEGERLIT
    ;

dataReferenceAttribute
    : cobolWord
    ;

// ------------------------------------------
// Other identification paragraphs
// ------------------------------------------

identificationParagraph
    : optionsParagraph
    | authorParagraph
    | installationParagraph
    | dateWrittenParagraph
    | dateCompiledParagraph
    | securityParagraph
    | remarksParagraph
    | genericIdentificationParagraph
    ;

// OPTIONS paragraph (COBOL-2002, ISO §11.9) — fully parsed into a structured clause tree (the model is consumed
// program-wide; see CobolNet.Binding.OptionsModel / OptionsBinder). Each of the seven clauses begins with a
// distinct keyword token, so `optionsClause+` is LL(1)-clean and order-independent (a superset of the spec's
// fixed clause order). Per §11.9.3 the terminating separator period is present iff at least one clause is given;
// no clause body contains a period, so the loop ends cleanly at the period.
optionsParagraph
    : OPTIONS DOT (optionsClause+ DOT)?
    ;

optionsClause
    : arithmeticClause
    | defaultRoundedClause
    | entryConventionClause
    | floatBinaryClause
    | floatDecimalClause
    | optionsInitializeClause
    | intermediateRoundingClause
    ;

// §11.9.5 — ARITHMETIC IS {NATIVE | STANDARD-BINARY | STANDARD-DECIMAL}. Bare STANDARD is also accepted (a common
// vendor spelling; the CCVS uses `ARITHMETIC IS STANDARD`).
arithmeticClause
    : ARITHMETIC IS? arithmeticMethod
    ;

arithmeticMethod
    : NATIVE
    | STANDARD_BINARY
    | STANDARD_DECIMAL
    | STANDARD
    ;

// §11.9.6 — DEFAULT ROUNDED MODE IS rounding-mode. Reuses the shared 8-mode roundingModeName.
defaultRoundedClause
    : DEFAULT ROUNDED MODE? IS? roundingModeName
    ;

// §11.9.7 — ENTRY-CONVENTION IS {COBOL | entry-convention-name}. The value is matched as cobolWord (COBOL or an
// implementor name), text-distinguished in the binder, so COBOL need not be reserved globally.
entryConventionClause
    : ENTRY_CONVENTION IS? cobolWord
    ;

// §11.9.8 — FLOAT-BINARY [DEFAULT] IS {HIGH-ORDER-LEFT | HIGH-ORDER-RIGHT}.
floatBinaryClause
    : FLOAT_BINARY DEFAULT? IS? endiannessPhrase
    ;

// §11.9.9 — FLOAT-DECIMAL [DEFAULT] IS [encoding-phrase] [endianness-phrase] (at least one phrase).
floatDecimalClause
    : FLOAT_DECIMAL DEFAULT? IS? floatDecimalEncoding
    ;

floatDecimalEncoding
    : encodingPhrase endiannessPhrase?
    | endiannessPhrase
    ;

encodingPhrase
    : BINARY_ENCODING
    | DECIMAL_ENCODING
    ;

endiannessPhrase
    : HIGH_ORDER_LEFT
    | HIGH_ORDER_RIGHT
    ;

// §11.9.10 — INITIALIZE {ALL | {LOCAL-STORAGE | SCREEN | WORKING-STORAGE}...} [SECTION]
//            TO {BINARY ZEROES | HIGH-VALUES | literal-1 | LOW-VALUES | SPACES}.
// Named distinctly from the PROCEDURE-DIVISION initializeStatement (disjoint parse contexts — no ambiguity).
optionsInitializeClause
    : INITIALIZE optionsInitializeTarget SECTION? TO optionsInitializeFill
    ;

optionsInitializeTarget
    : ALL
    | optionsInitializeSection+
    ;

optionsInitializeSection
    : LOCAL_STORAGE
    | SCREEN
    | WORKING_STORAGE
    ;

optionsInitializeFill
    : BINARY ZERO          // ZERO already covers ZEROES / ZEROS
    | HIGH_VALUE           // already covers HIGH-VALUES
    | LOW_VALUE            // already covers LOW-VALUES
    | SPACE                // already covers SPACES
    | literal              // literal-1 (one-byte hex-alphanumeric, §11.9.10.3 — checked in the binder)
    ;

// §11.9.11 — INTERMEDIATE ROUNDING IS {NEAREST-AWAY-FROM-ZERO | NEAREST-EVEN | PROHIBITED | TRUNCATION}. A
// SEPARATE 4-mode rule (NOT roundingModeName) so the grammar enforces §11.9.11's restricted set.
intermediateRoundingClause
    : INTERMEDIATE ROUNDING IS? intermediateRoundingMode
    ;

intermediateRoundingMode
    : NEAREST_AWAY_FROM_ZERO
    | NEAREST_EVEN
    | PROHIBITED
    | TRUNCATION
    ;

// AUTHOR.
authorParagraph
    : AUTHOR DOT authorContent? DOT
    ;

authorContent
    : ~DOT+
    ;

// INSTALLATION.
installationParagraph
    : INSTALLATION DOT installationContent? DOT
    ;

installationContent
    : ~DOT+
    ;

// DATE-WRITTEN.
dateWrittenParagraph
    : DATE_WRITTEN DOT dateWrittenContent? DOT
    ;

dateWrittenContent
    : ~DOT+
    ;

// DATE-COMPILED.
dateCompiledParagraph
    : DATE_COMPILED DOT dateCompiledContent? DOT
    ;

dateCompiledContent
    : ~DOT+
    ;

// SECURITY.
securityParagraph
    : SECURITY DOT securityContent? DOT
    ;

securityContent
    : ~DOT+
    ;

// REMARKS.
remarksParagraph
    : REMARKS DOT remarksContent
    ;

remarksContent
    : (IDENTIFIER | STRINGLIT)+
    ;

// Fallback for vendor extensions
genericIdentificationParagraph
    : genericClause DOT
    ;

// ==========================================
// ENVIRONMENT DIVISION
// ==========================================

environmentDivision
    : ENVIRONMENT DIVISION DOT
      configurationSection?
      inputOutputSection?
    ;

// ==========================================
// CONFIGURATION SECTION
// ==========================================

configurationSection
    : CONFIGURATION SECTION DOT configurationParagraph*
    ;

configurationParagraph
    : sourceComputerParagraph
    | objectComputerParagraph
    | specialNamesParagraph
    | repositoryParagraph
    | vendorConfigurationParagraph
    ;

// REPOSITORY paragraph (COBOL-2002, ISO §12.3.8) — declares the functions a source element references. Accepted
// (parsed) but its specifiers are not yet bound: `FUNCTION ALL INTRINSIC` and user-function declarations are a
// WS-2002-UDF follow-up (the function-prototype binding + `FUNCTION user-name(args)` invocation). Each entry
// starts with the FUNCTION specifier keyword, so the rule cannot over-run into the next section; an optional
// period after each entry tolerates both the one-period-per-paragraph and period-per-entry styles. The `AS
// external-name` phrase is deferred (avoids reserving AS as a keyword).
repositoryParagraph
    : REPOSITORY DOT (repositoryEntry DOT?)*
    ;

repositoryEntry
    : FUNCTION ALL INTRINSIC
    | FUNCTION functionName INTRINSIC?
    | CLASS className   // OO (2002): CLASS class-name [AS literal] — introduction-gated post-bind by VersionConformancePass ParseArm.VisitRepositoryEntry (rearch 14g.5); className rule in Core/CobolOO.g4
    | INTERFACE interfaceName   // OO (2002): the interface specifier — introduction-gated post-bind by VersionConformancePass ParseArm.VisitRepositoryEntry (rearch 14g.5); position-safe (entry-leading keyword in a closed alt set)
    | PROPERTY propertyName     // OO (2002): the property specifier — introduction-gated post-bind by VersionConformancePass ParseArm.VisitRepositoryEntry (rearch 14g.5); position-safe (§8.4.3.9.3 SR1)
    ;

// SOURCE-COMPUTER. [computer-name-1] . (ISO §12.3.5.2 — computer-name-1 is OPTIONAL; SR1: without it the second
// period may be omitted). The empty paragraph was legal in X3.23-1985 too; the '85 WITH DEBUGGING MODE clause hung
// off a name, and so does the attribute SINK here (a name-less `SOURCE-COMPUTER. WITH DEBUGGING MODE.` is illegal at
// every edition — '85 required the name, 2002 deleted the clause). ⚠ The sink must stay BEHIND the name: it is
// `~(DOT | …)+`, and reachable without a name it would swallow the next paragraph header (kb/Work PB78).
sourceComputerParagraph
    : SOURCE_COMPUTER DOT ((computerName computerAttributes?)? DOT)?
    ;

// OBJECT-COMPUTER. [computer-name-1] [ | CHARACTER CLASSIFICATION … | PROGRAM COLLATING SEQUENCE … | ]… . (ISO
// §12.3.6.2 — the name is OPTIONAL and the two clauses may follow the period directly, in any order, each at most
// once — the figure notes; SR4: with nothing at all the second period may be omitted). X3.23-1985 hung MEMORY SIZE /
// PROGRAM COLLATING SEQUENCE / SEGMENT-LIMIT off a REQUIRED name — the clauses WITHOUT a name are the 2002
// relaxation, gated on recognition (VersionConformancePass ParseArm.VisitObjectComputerParagraph,
// computer-name-optional-2002 — kb/Work PB78: `OBJECT-COMPUTER. PROGRAM COLLATING SEQUENCE IS REV.` was `unexpected
// 'PROGRAM'`). computerAttributes stays the token SINK for the deleted '85 clauses (MEMORY SIZE, SEGMENT-LIMIT —
// VisitComputerAttributes' token scan), reachable only behind a name; it stops at PROGRAM and CHARACTER so the two
// standard clauses are recognized, never swallowed.
objectComputerParagraph
    : OBJECT_COMPUTER DOT ((computerName computerAttributes?)? objectComputerClause* DOT)?
    ;

objectComputerClause
    : programCollatingSequenceClause
    | characterClassificationClause
    ;

// CHARACTER CLASSIFICATION {IS locale-phrase-1 [locale-phrase-2] | {FOR ALPHANUMERIC IS locale-phrase-1 | FOR
// NATIONAL IS locale-phrase-2}…} (§12.3.6.2), locale-phrase = locale-name | LOCALE | SYSTEM-DEFAULT | USER-DEFAULT.
// ⛔ PARSED SO IT CAN BE DIAGNOSED, NOT SO IT CAN BE USED — an §A.4.9 item 7 optional-locale element ("OBJECT-COMPUTER
// paragraph, CHARACTER CLASSIFICATION clause"); the binder emits the documented-non-support COBOLNET1518 the LOCALE
// clause carries (kb/Work PB78 — it was swallowed silently by the attribute sink after a name, a parse error without
// one). CLASSIFICATION is not a token (an ordinary word at '85), so the arm is predicated on the word's text after
// the CHARACTER token; the four locale-phrase spellings are all words here — the binder never gets to tell them
// apart, since the clause is rejected whole.
characterClassificationClause
    : CHARACTER {classificationAhead()}? cobolWord (classificationForPhrase+ | IS? cobolWord cobolWord?)
    ;

classificationForPhrase
    : FOR (ALPHANUMERIC | NATIONAL) IS? cobolWord
    ;

// PROGRAM COLLATING SEQUENCE {IS alphabet-name-1 [alphabet-name-2] | {FOR ALPHANUMERIC IS alphabet-name-1 |
// FOR NATIONAL IS alphabet-name-2}…} (ISO §12.3.6.2). The 85 surface is the single-name IS form; the second
// name and the FOR forms arrived with the national class (2002) — introduction-gated on recognition by
// VersionConformancePass ParseArm.VisitProgramCollatingSequenceClause (program-collating-national-2002).
// collatingForPhrase is the ONE shared FOR-class subrule (CobolIO.g4 — SORT/MERGE reuse it).
programCollatingSequenceClause
    : PROGRAM COLLATING? SEQUENCE (collatingForPhrase+ | IS? cobolWord cobolWord?)
    ;

computerName
    : cobolWord
    ;

computerAttributes
    : ~(DOT | PROGRAM | CHARACTER)+
    ;

// ==========================================
// PROCEDURE DIVISION
// ==========================================

procedureDivision
    : PROCEDURE DIVISION usingClause? (returningClause)? (raisingClause)? DOT   // returningClause + raisingClause introduction-gated post-bind by VersionConformancePass ParseArm.VisitReturning/RaisingClause (rearch 14g.4, InMethodDefinition-guarded — program-unit PDs only; this rule is SHARED with method PDs)
      declarativePart*
      sentence*          // §14.4.3: the paragraph-name-OMITTED paragraph — "one or more successive sentences
                         // following the procedure division header or a section header". Legal COBOL with no
                         // paragraph at all; NIST/CCVS never exercises it (it always writes paragraph names),
                         // which is why the GnuCOBOL external corpus was what surfaced it (DEVLOG 931).
      procedureUnit*
    ;

usingClause
    : USING usingParameter (COMMA? usingParameter)*
    ;

// The procedure-division-header using-phrase parameter forms (ISO §14.2.2 :23636):
//   { [BY REFERENCE] { [OPTIONAL] data-name-1 }… | BY VALUE { data-name-1 }… }…
// Parsed FLAT (one parameter per node, the CALL callArgument precedent) — the §14.2.3 GR4 transitivity
// ("both phrases are transitive across the parameters that follow them") is threaded by the binder
// (DataBinder.CallBindLinkage), never encoded structurally. BY VALUE is a COBOL-2002 introduction —
// gated post-bind by VersionConformancePass ParseArm.VisitUsingByValue (pd-header-by-value-2002).
usingParameter
    : usingByReference
    | usingByValue
    | OPTIONAL? dataReference
    ;

usingByReference
    : BY? REFERENCE OPTIONAL? dataReference
    ;

usingByValue
    : BY VALUE dataReference
    ;

returningClause
    : RETURNING dataReference
    ;

dataReferenceList
    : dataReference (COMMA? dataReference)*
    ;

dataReference
    // LINAGE-COUNTER special register (ISO §8.4.3.14): a read-only unsigned integer holding the
    // current line within the page body of a LINAGE file, optionally qualified by file-name when more
    // than one LINAGE file exists. Listed first so the distinct LINAGE_COUNTER token is recognized as
    // the register rather than a data name.
    : LINAGE_COUNTER ((OF | IN) cobolWord)?
    // LINE-COUNTER / PAGE-COUNTER special registers (ISO §8.4.3.15/§13.x): read-only counters the Report
    // Writer Control System maintains per report, optionally qualified by report-name. Listed before the
    // generic data-name alternative so the distinct tokens are recognized as registers, not data names.
    | LINE_COUNTER ((OF | IN) cobolWord)?
    | PAGE_COUNTER ((OF | IN) cobolWord)?
    | cobolWord dataReferenceSuffix*
    ;

dataReferenceSuffix
    : subscriptPart
    | refModPart
    | qualification
    ;

qualification
    : (OF | IN) cobolWord (subscriptPart | refModPart)*
    ;

// subscriptPart uses SUBSCRIPT-mode tokens (entered via LPAREN after IDENTIFIER).
// SUB_RPAREN pops back to default mode. Also handles ref-mod (colon form).
subscriptPart
    : LPAREN subscriptOrRefMod SUB_RPAREN
    ;

// Inside SUBSCRIPT mode: captures all content as a flat sequence of SUBSCRIPT-mode tokens.
// The binding layer interprets the content: SUB_COLON → ref-mod, else → subscript list.
// This avoids the need for the grammar to distinguish subscripts from ref-mod.
subscriptOrRefMod
    : subToken+
    ;

// Any token that can appear inside subscript/ref-mod parentheses
subToken
    : SUB_WS
    | SUB_IDENTIFIER
    | SUB_INTEGERLIT
    | SUB_DECIMALLIT
    | FLOATLIT         // 1.5E3 / -1.5E3 in a captured region (kb/Work R17 — the SUBSCRIPT-mode float forms
                       // re-type to the ONE FLOATLIT vocabulary; without this the keyword-omitted
                       // EXP(+1.5E1) failed the OUTER capture with 'no viable alternative')
    | COMMA_FLOATLIT   // 1,5E3 / -1,5E3 — the DECIMAL-POINT IS COMMA float twin (kb/Work PB98)
    | SUB_STRINGLIT
    | SUB_NATLIT       // national literal argument N"…" (ISO §15.50.3 — FUNCTION LENGTH(N"…") etc.)
    | SUB_BOOLLIT      // boolean literal argument B"…"
    | SIGNED_DECIMALLIT
    | SIGNED_INTEGERLIT
    | SUB_PLUS
    | SUB_MINUS
    | SUB_POWER
    | SUB_STAR
    | SUB_SLASH
    | SUB_COMMA
    | SUB_SEMICOLON
    | SUB_COLON
    | SUB_OF
    | SUB_IN
    | SUB_ALL
    | SUB_LPAREN subToken+ SUB_RPAREN                                  // nested parens
    ;

// refModPart for non-identifier context (default mode)
// ⛔ BOTH PAREN FLAVOURS, AND THE FNARG ONE IS NOT AN OVERSIGHT (fix-queue PB48). The lexer types a '(' that
// immediately follows `FUNCTION <name>` as FNARG_LPAREN, but §8.4.3.2.3 SR6 hands that paren to the argument list
// only "if a function's definition PERMITS arguments" — a CATALOG question no lexer or grammar can answer. So for
// a zero-argument function the very same token is the reference modifier: `FUNCTION CURRENT-DATE (1:8)` is the
// standard's own shape (D.14.3.6) and `FUNCTION PI (1:2)` is the SR2 negative case. Accepting either flavour here
// keeps the two alternatives disjoint on the COLON exactly as before — the superset parse — and leaves SR6's
// catalog half to IntrinsicBinder, which reads `functionCall.FNARG_LPAREN()` (a DIRECT child, so an argument list
// that actually parsed) to tell `FUNCTION RANDOM (1:4)` from `FUNCTION UPPER-CASE("x") (1:2)`.
refModPart
    : (LPAREN | FNARG_LPAREN) refModSpec (RPAREN | FNARG_RPAREN)
    ;

refModSpec
    : arithmeticExpression COLON arithmeticExpression?
    ;

// COBOL-85 §5.3: subscript list using SUBSCRIPT-mode tokens.
// Whitespace (SUB_WS) separates subscripts; commas are optional.
subscriptList
    : SUB_WS? subscriptEntry ( (SUB_WS+ | SUB_WS* SUB_COMMA SUB_WS*) subscriptEntry )* SUB_WS?
    ;

// Each subscript is one of the three COBOL-85 forms
subscriptEntry
    : SIGNED_INTEGERLIT                                                // +8, -3, +1
    | SUB_INTEGERLIT                                                   // 1, 10, 300
    | SUB_ALL                                                          // ALL
    | SUB_IDENTIFIER subscriptQualification* relativeOffset?           // W-2, INDEX1 + 2
    ;

// Qualification inside subscript: data-name OF/IN qualifier
subscriptQualification
    : SUB_WS? (SUB_OF | SUB_IN) SUB_WS? SUB_IDENTIFIER
    ;

// Relative subscript offset: {+|-} unsigned-integer
// The + or - is separated by whitespace from the data-name,
// distinguishing it from SIGNED_INTEGERLIT where sign is adjacent.
relativeOffset
    : SUB_WS (SUB_PLUS | SUB_MINUS) SUB_WS SUB_INTEGERLIT
    ;

fileName
    : cobolWord
    ;

// ==========================================
// DECLARATIVES
// ==========================================

declarativePart
    : DECLARATIVES DOT declarativeSection+ END DECLARATIVES DOT
    ;

// The optional integer after SECTION is the X3.23-1985 segment-number (Segmentation module, 0–99;
// ≥50 = independent segment) — an obsolete '85 element DELETED by ISO 2002. Parsed at every edition
// (accepted-inert at 85: all segments resident, a conforming posture); the EditionValidator flags it
// COBOLNET0902 ≥2002 (`segment-numbers-removed-2002`, VCR Table 7 row 7.18). The companion
// SEGMENT-LIMIT clause is row 7.8 (already gated via the computerAttributes sink).
declarativeSection
    : sectionName SECTION integerLiteral? DOT sentence* declarativeParagraph*
    ;

declarativeParagraph
    : paragraphName DOT sentence*
    ;

// ==========================================
// MAIN PROCEDURE BODY
// ==========================================

// One COBOL sentence: one or more statements, terminated by a period.
// The period is the ONLY place DOT appears in procedure body.
sentence
    : statement+ DOT
    ;

procedureUnit
    : sectionDefinition
    | paragraphDefinition
    ;

// SECTION integerLiteral? — the '85 segment-number; see the declarativeSection note (VCR Table 7 row 7.18).
sectionDefinition
    : sectionName SECTION integerLiteral? DOT
      sentence*          // §14.4.3 — same paragraph-name-omitted form after a SECTION header
      paragraphDefinition*
    ;

sectionName
    : procedureName
    ;

paragraphDefinition
    : paragraphName DOT sentence*
    ;

paragraphName
    : {IsAtLineStart()}? procedureName
    ;

procedureName
    : (cobolWord | INTEGERLIT)
      ((OF | IN) (cobolWord | INTEGERLIT))?
    ;

// ==========================================
// STATEMENT DISPATCHER
// ==========================================

statement
    : acceptStatement
    | addStatement
    | alterStatement
    | useStatement
    | callStatement
    | entryStatement
    | enterStatement
    | cancelStatement
    | closeStatement
    | computeStatement
    | deleteStatement
    | deleteFileStatement   // introduction-gated at BIND time (StatementBinder.KeyedIo → Check(DeleteFile2023)); disjoint from deleteStatement above on the 2nd token (FILE ∉ cobolWord)
    | allocateStatement
    | freeStatement
    | unlockStatement   // COBOL-2002; parses at all editions (superset), introduction-gated at BIND (BindUnlock → Check(UnlockStatement2002)) — DESIGN-version-conformance-pipeline.md residue migration #5
    | displayStatement
    | divideStatement
    | evaluateStatement
    | exitStatement
    | gobackStatement
    | goToStatement
    | ifStatement
    | initializeStatement
    | inspectStatement
    | mergeStatement
    | moveStatement
    | multiplyStatement
    | openStatement
    | performStatement
    | raiseStatement
    | readStatement
    | releaseStatement
    | resumeStatement
    | returnStatement
    | rewriteStatement
    | searchStatement
    | searchAllStatement
    | setStatement
    | sortStatement
    | startStatement
    | stopStatement
    | stringStatement
    | subtractStatement
    | unstringStatement
    | writeStatement
    | initiateStatement
    | generateStatement
    | terminateStatement
    | suppressStatement   // §14.9.45 — SUPPRESS PRINTING; SR1/GR1 (USE-BEFORE-REPORTING context) enforced at bind
    | invokeStatement   // introduction-gated at BIND time (StatementBinder.Oo → Check(Invoke2002))
    | {is2023()}? inlineMethodInvocationStatement
    // ── Wave H: RECOGNIZE-AND-NAME the facilities COBOL.NET does not implement. ISO §4.2.6 ¶3 makes the
    //    compile-time WARNING MECHANISM mandatory even though the facilities themselves are optional /
    //    processor-dependent, so a GENERIC parse error here is a live non-conformance. Each arm is
    //    KEYWORD-TOKEN-LED and predicated on facilityWord(), which fires only at the editions where §8.9
    //    actually reserves the word — an IDENTIFIER-led arm would poison the ALL(*) DFA (DEVLOG 903). ──
    | {facilityWord("RECEIVE")}?  mcsReceiveStatement
    | {facilityWord("SEND")}?     mcsSendStatement
    | {facilityWord("VALIDATE")}? validateFacilityStatement
    | {facilityWord("COMMIT")}?   commitFacilityStatement
    | {facilityWord("ROLLBACK")}? rollbackFacilityStatement
    | continueStatement
    | nextSentenceStatement
    ;

// ── The unsupported-facility statements (Wave H). The operand tails are parsed for REAL, not swallowed:
//    `statement` is reachable from `statementBlock`, so a `(~DOT)*` swallow would eat END-IF and break every
//    enclosing block — and both MCS formats carry `imperative-statement`s of their own, which a swallow would
//    also consume. §4.2.6 excuses us from diagnosing syntax errors WITHIN unsupported syntax, but not from
//    leaving the surrounding program parseable. ──

// ISO §14.9.31.2 — RECEIVE FROM data-name-1 GIVING identifier-1 data-name-2
//   [ CONTINUE AFTER { arithmetic-expression-1 SECONDS | MESSAGE RECEIVED } ]
//   [| ON EXCEPTION … |] [| NOT ON EXCEPTION … |] [ END-RECEIVE ]
// RECEIVED is not in §8.9's reserved list and is not a §8.10 context-sensitive word either — the standard
// simply never classifies it (recorded as a P14 Step-0 GAP row); matched as cobolWord, which is safe here.
// ISO 5.2.3 optional word — see the arithmeticOnSizeError note. Measured on page 732: RECEIVE, GIVING,
// CONTINUE, MESSAGE and RECEIVED carry underline rules; FROM carries NONE, so FROM may be omitted.
//
// ⚠ AFTER and SECONDS are NOT made optional here even though page 732 prints them without underlines, because
// THE STANDARD CONTRADICTS ITSELF about them. On page 634 — 14.9.9.2, the CONTINUE statement's OWN defining
// general format — AFTER and SECONDS ARE underlined while CONTINUE is not; page 732 is exactly inverted. One of
// the two pages is a typesetting defect, and page 634 is the defining occurrence, so it wins. It is also the
// only reading that makes sense: with AFTER and SECONDS optional, `CONTINUE 5` would be legal and meaningless.
// Recorded in specs/ISO_COBOL.md at both pages rather than silently resolved.
mcsReceiveStatement
    : RECEIVE FROM? dataReference GIVING dataReference dataReference
      (CONTINUE AFTER (arithmeticExpression SECONDS | MESSAGE cobolWord))?
      mcsExceptionPhrases?
      END_RECEIVE?
    ;

// ISO §14.9.38.2 — Format 1 (to-message-server) and Format 2 (message-server-response), merged: they differ
// only in the RETURNING vs RAISING tail, both optional here, which keeps one rule for one statement.
// ISO 5.2.3 optional word — see the arithmeticOnSizeError note. Measured on page 756, in BOTH send formats:
// SEND, FROM, RETURNING and RAISING carry underline rules; TO carries none, so it may be omitted.
mcsSendStatement
    : SEND TO? (literal | dataReference) FROM dataReference
      (RETURNING dataReference)?
      (RAISING (EXCEPTION cobolWord | LAST EXCEPTION))?
      mcsExceptionPhrases?
      END_SEND?
    ;

// ISO 5.2.6.4: the printed RECEIVE/SEND figures enclose the ON EXCEPTION / NOT ON EXCEPTION pair in CHOICE
// INDICATORS (verified against the PDF at 700 dpi), so BOTH may be written, each once, IN EITHER ORDER.
// ISO 5.2.3 optional word: ON is omittable here — see the arithmeticOnSizeError note.
mcsExceptionPhrases
    : ON? EXCEPTION statementBlock (NOT ON? EXCEPTION statementBlock)?
    | NOT ON? EXCEPTION statementBlock (ON? EXCEPTION statementBlock)?
    ;

// ISO §14.9.50.2 — VALIDATE { identifier-1 } …
validateFacilityStatement
    : VALIDATE dataReference+
    ;

// A.3 items 6–7: bare-verb transaction statements. Real tokens + real rules rather than refining the §8.9
// reserved-word diagnostic: binding COMMIT as a PARAGRAPH-NAME would split the enclosing paragraph and change
// control flow (PERFORM would stop at the new boundary) — a silent wrong answer under a rule that requires the
// statement to behave as CONTINUE.
commitFacilityStatement
    : COMMIT
    ;

rollbackFacilityStatement
    : ROLLBACK
    ;

// Safety net for vendor extensions — disabled to prevent exponential backtracking
// genericStatement
//     : IDENTIFIER (IDENTIFIER | literal)*
//     ;

// Imperative statement (used by AT END, ON EXCEPTION, etc.)
statementBlock
    : statement+
    ;

// ==========================================
// SHARED ARITHMETIC RULES
// ==========================================

// Unified GIVING-form receiving operand.
// COBOL-85: in any arithmetic GIVING form, the receiving operand may be
// either an dataReference or a literal. One rule, one source of truth.
receivingOperand
    : dataReference
    | literal
    ;

receivingArithmeticOperand
    : dataReference roundedPhrase?
    ;

// ROUNDED [MODE IS rounding-mode] (§14.9.4, COBOL-2002). The MODE phrase selects one of the
// eight ISO rounding modes; bare ROUNDED defaults to NEAREST-AWAY-FROM-ZERO.
roundedPhrase
    : ROUNDED (MODE IS? roundingModeName)?
    ;

roundingModeName
    : AWAY_FROM_ZERO
    | NEAREST_AWAY_FROM_ZERO
    | NEAREST_EVEN
    | NEAREST_TOWARD_ZERO
    | TOWARD_GREATER
    | TOWARD_LESSER
    | PROHIBITED
    | TRUNCATION
    ;

// ISO 5.2.6.4: the ON SIZE ERROR / NOT ON SIZE ERROR pair is enclosed in CHOICE INDICATORS (| bars inside the
// brackets of the printed general format), so BOTH may be specified, each at most once, IN ANY ORDER. The
// reversed order was rejected until 2026-07-19 (our spec transcription had dropped the bars); the shape below
// matches returnAtEndPhrase, which already carried it via the explicit SR4 in 14.9.34.3.
// ISO 5.2.3: ON is printed WITHOUT an underline in every one of these figures, so it is an OPTIONAL WORD
// that may be written or omitted. Measured, not assumed: scripts/spec/figure_extract.py reads the
// underline rectangles per word, and ON comes back plain on pages 632 (COMPUTE), 644 (DIVIDE), 703
// (MULTIPLY), 607 and 756 (ON EXCEPTION). callOnExceptionPhrase already had `ON?`; these did not, so the
// grammar contradicted itself and rejected `ADD A TO B SIZE ERROR ...` - legal COBOL.
arithmeticOnSizeError
    : ON? SIZE ERROR statementBlock
      (NOT ON? SIZE ERROR statementBlock)?
    | NOT ON? SIZE ERROR statementBlock
      (ON? SIZE ERROR statementBlock)?
    ;

// ==========================================
// ADD (§14.9.1)
// ==========================================

addStatement
    : ADD (CORRESPONDING | CORR) dataReference TO dataReference roundedPhrase? arithmeticOnSizeError? END_ADD?
    | ADD addOperandList addToPhrase? addGivingPhrase? arithmeticOnSizeError? END_ADD?
    ;

addOperandList
    : addOperand+
    ;

// ⛔ THE FOUR SENDING ARITHMETIC OPERANDS BELOW ARE ONE RULE WRITTEN FOUR TIMES, AND ALL FOUR WERE WRONG
// (fix-queue PB45). ISO §8.4.3.1.2 Format 1 makes a function-identifier an identifier, so every position a
// format writes as `identifier-n | literal-n` in a SENDING role admits one — yet `ADD FUNCTION SQRT(X) TO Y`
// was a PARSE error across the WHOLE arithmetic family (ADD/SUBTRACT/MULTIPLY/DIVIDE, every format), while
// COMPUTE accepted it because its RHS is an arithmeticExpression.
// ⚠ THEY ARE NOT COLLAPSED INTO ONE RULE, AND THAT IS A RECORDED CONSTRAINT RATHER THAN A PREFERENCE: the
// FROZEN legacy compiler (src/CobolSharp.Compiler) shares this grammar and reads `.dataReference()`/`.literal()`
// off AddOperandContext / SubtractOperandContext / MultiplyOperandContext / DivideOperandContext by name, so
// both a collapse and an alias break it — the same freeze that blocks D10 until PHASE 15 CUT 2 deletes legacy.
// The change is therefore ADDITIVE (the standing grammar discipline), and
// ArithmeticSendingOperandDriftTests pins the four alternative sets IDENTICAL so they cannot drift apart while
// they must stay separate. Collapse them to ONE rule at CUT 2.
addOperand
    : literal
    | functionCall
    | dataReference
    ;

addToPhrase
    : TO receivingArithmeticOperand+
    ;

addGivingPhrase
    : GIVING receivingArithmeticOperand+
    ;

// ==========================================
// SUBTRACT (§14.9.42)
// ==========================================

subtractStatement
    : SUBTRACT (CORRESPONDING | CORR) dataReference FROM dataReference roundedPhrase? arithmeticOnSizeError? END_SUBTRACT?
    | SUBTRACT subtractOperandList subtractFromPhrase? subtractGivingPhrase? arithmeticOnSizeError? END_SUBTRACT?
    ;

subtractOperandList
    : subtractOperand+
    ;

subtractOperand
    : literal
    | functionCall
    | dataReference
    ;

subtractFromPhrase
    : FROM subtractFromOperand
    ;

subtractFromOperand
    : receivingArithmeticOperand (receivingArithmeticOperand)*
    | receivingOperand
    ;

subtractGivingPhrase
    : GIVING receivingArithmeticOperand (receivingArithmeticOperand)*
    ;

// ==========================================
// MULTIPLY (§14.9.23)
// ==========================================

multiplyStatement
    : MULTIPLY multiplyOperand BY multiplyByOperand+ multiplyGivingPhrase? arithmeticOnSizeError? END_MULTIPLY?
    ;

multiplyOperand
    : literal
    | functionCall
    | dataReference
    ;

multiplyByOperand
    : receivingOperand roundedPhrase?
    ;

multiplyGivingPhrase
    : GIVING receivingArithmeticOperand+
    ;

// ==========================================
// DIVIDE (§14.9.12)
// ==========================================

divideStatement
    : DIVIDE divideOperand (divideIntoPhrase | divideByPhrase)
      divideGivingPhrase? divideRemainderPhrase? arithmeticOnSizeError? END_DIVIDE?
    ;

divideOperand
    : literal
    | functionCall
    | dataReference
    ;

divideIntoPhrase
    : INTO divideIntoOperand
    ;

divideIntoOperand
    : receivingArithmeticOperand+   // dataReference ROUNDED? (non-GIVING form, multiple targets)
    | literal             // numeric literal (GIVING form only)
    ;

divideByPhrase
    : BY divideOperand
    ;

divideGivingPhrase
    : GIVING receivingArithmeticOperand+
    ;

divideRemainderPhrase
    : REMAINDER dataReference
    ;

// ==========================================
// COMPUTE (§14.9.8)
// ==========================================

computeStatement
    : COMPUTE computeStore+ EQUALS arithmeticExpression computeOnSizeError? END_COMPUTE?          // F1 (§14.9.8)
    | COMPUTE computeStore+ EQUALS booleanExpression computeOnSizeError? END_COMPUTE?  // F2 boolean-compute (§14.9.8 Format 2); superset-parsed (F1 arithmetic is tried first; only a genuine boolean RHS falls here), introduction-gated at BIND (BindBoolExpr → Check(BooleanOperators2002)) — residue migration #2
    ;

computeStore
    : dataReference roundedPhrase?
    ;

// ISO 5.2.6.4 choice indicators — see the arithmeticOnSizeError note: both phrases, each once, any order.
// ISO 5.2.3 optional word: ON is omittable here — see the arithmeticOnSizeError note.
computeOnSizeError
    : ON? SIZE ERROR statementBlock
      (NOT ON? SIZE ERROR statementBlock)?
    | NOT ON? SIZE ERROR statementBlock
      (ON? SIZE ERROR statementBlock)?
    ;

// ==========================================
// MOVE (§14.9.24)
// ==========================================

moveStatement
    : MOVE (CORRESPONDING | CORR) dataReference TO dataReference
    | MOVE moveSendingOperand moveReceivingPhrase
    ;

moveSendingOperand
    : literal
    | functionCall
    | dataReference
    ;

moveReceivingPhrase
    : TO dataReferenceList
    | (CORRESPONDING | CORR) dataReference TO dataReference
    ;

// ==========================================
// CALL (§14.9.4)
// ==========================================

// ⛔ THE `AS` PHRASE IS WHAT SELECTS FORMAT 2, AND ITS ABSENCE IS WHY FORMAT 2 WAS UNREACHABLE (fix-queue PB46,
// CALL half). §14.9.4.2 Format 2 prints `CALL { identifier-1 | literal-1 } AS { NESTED | program-prototype-name-1 }`
// — a SYNTACTIC discriminator. PB46's note asserted the opposite ("the formats are NOT distinguishable at parse
// time … what selects Format 2 is whether the called name resolves to a program-prototype — a SEMANTIC
// question") and concluded the whole CALL half was blocked on the P13 prototype registry. Reading the rendered
// general format refutes that: Format 1 has no AS phrase at all.
callStatement
    : CALL callTarget
      callAsPhrase?
      callUsingPhrase?
      callReturningPhrase?
      callExceptionPhrases?
      END_CALL?

    ;

callTarget
    : literal
    | dataReference
    ;

// `AS NESTED` | `AS program-prototype-name-1`. ⚠ NESTED IS NOT LEXED AS A TOKEN and deliberately so: §8.9 makes
// it a RESERVED word from 2002 (reserved-words.json r85=false, r2002+=true), but this repo enforces reservation
// through the VisitCobolWord funnel on the word's SPELLING, not through tokenization — which is how its sibling
// MODULE-NAME phrase words (CURRENT / ACTIVATING / STACK / TOP-LEVEL) are already handled. Tokenizing it would
// break `FUNCTION MODULE-NAME(NESTED)`, force an entry in fnArgPhraseWord, and make NESTED asymmetric with those
// siblings — all to answer a question the binder answers in one comparison.
callAsPhrase
    : AS cobolWord
    ;

callUsingPhrase
    : USING callArgument+
    ;

callArgument
    : callByReference
    | callByValue
    | callByContent
    | dataReference       // bare argument = BY REFERENCE (default)
    ;

callByReference
    : BY? REFERENCE dataReference
    ;

callByValue
    : BY VALUE arithmeticExpression   // introduction-gated at BIND time (StatementBinder.Call → ConstructRegistry.Check(CallByValue2002))
    ;

// ⛔ THE TWO FORMATS' BY CONTENT OPERAND SETS DIFFER, AND ONE RULE CANNOT BE BOTH (fix-queue PB46, CALL half).
// §14.9.4.2 Format 1's BY CONTENT is `{ identifier-2 } …` and NOTHING else; Format 2's is
// `arithmetic-expression-1 | boolean-expression-1 | identifier-4 | literal-2`. This rule serves both, so it is
// parsed WIDE and narrowed in the binder by whether the AS phrase selected Format 2 — the repo's standing
// superset-parse / bind-narrow doctrine. Widening the GRAMMAR alone would trade a rejection of legal Format-2
// source for an acceptance of illegal Format-1 source, which is the trade this item's note correctly refused.
// ⚠ The alternation is the SAME shape invokeArgument uses, for the same reasons: `literal` FIRST because
// `arithmeticExpression` subsumes numeric literals; `dataReference` deliberately ABSENT because
// `arithmeticExpression` subsumes it and the identifier case is recovered in the binder from a sole-dataReference
// expression; and the boolean arm behind `{boolExprAhead()}?` because booleanExpression's leaf is valueOperand
// and an unguarded alternative is ambiguous with the arithmetic one.
// ⚠ `dataReference` STAYS IN THE ALTERNATION, unlike invokeArgument's, and §0's standing caution is why: this
// rule lives in the SHARED `CobolParserCore.g4`, and the LEGACY binder reads `callByContent.dataReference()`.
// Removing it deletes that generated accessor and breaks a compiler that shares this grammar until the P15
// cut-over — the change must be ADDITIVE. Placing it BEFORE `arithmeticExpression` is also what preserves the
// bare-identifier path: ANTLR predicts the alternative that matches the WHOLE operand, so `A` takes the
// dataReference arm and `A + 1` falls through to the expression one.
callByContent
    : BY? CONTENT ({boolExprAhead()}? booleanExpression | literal | dataReference | arithmeticExpression)
    ;

callReturningPhrase
    : RETURNING dataReference
    ;

// ISO 5.2.6.4 choice indicators — see the arithmeticOnSizeError note. CALL's ON EXCEPTION / NOT ON EXCEPTION
// pair carries them in the printed general format (Formats 1 and 2), so both may be written, each at most
// once, in either order. Held in ONE container rule rather than two independently-optional slots on
// callStatement, which admitted only the ON-then-NOT order.
callExceptionPhrases
    : callOnExceptionPhrase (callNotOnExceptionPhrase)?
    | callNotOnExceptionPhrase (callOnExceptionPhrase)?
    ;

callOnExceptionPhrase
    : ON? (EXCEPTION | OVERFLOW) statementBlock
    ;

callNotOnExceptionPhrase
    : NOT ON? (EXCEPTION | OVERFLOW) statementBlock
    ;

// ==========================================
// ENTRY (§14.9.14 — alternate entry point)
// ==========================================

entryStatement
    : ENTRY literal usingClause?
    ;

// ==========================================
// ENTER (X3.23-1985 Nucleus — obsolete '85 element DELETED by ISO 2002)
// ==========================================

// ENTER language-name-1 [routine-name-1] — the other-language entry statement. Accepted-inert at 85
// (comment-equivalent when only COBOL is supported — the conforming '85 posture; bound as a no-op);
// the EditionValidator flags it COBOLNET0902 ≥2002 (`enter-removed-2002`, VCR Table 7 row 7.16).
// The operands are deliberately NOT cobolWord: language-name-1 is a SYSTEM-name (`ENTER COBOL.` is the
// canonical '85 switch-back and COBOL is an '85 reserved word — a cobolWord slot would trip the §8.9
// funnel 0901 on conforming source), and routine-name-1 names an external routine, not a word in the
// program's name space. The classic `ENTER LINKAGE.` idiom collides with the LINKAGE SECTION keyword,
// so LINKAGE is admitted explicitly.
enterStatement
    : ENTER enterOperand enterOperand?
    ;

enterOperand
    : IDENTIFIER
    | LINKAGE
    ;

// ==========================================
// CANCEL (§14.9.5)
// ==========================================

cancelStatement
    : CANCEL cancelTarget+
    ;

cancelTarget
    : literal
    | dataReference
    ;

// ==========================================
// SET (§14.9.39 — all forms)
// ==========================================

setStatement
    : setLocaleStatement          // F11/F12 (A.4.9 item 9) — FIRST: predicated on the LOCALE word, so no other form can claim it (kb/Work PB92)
    | setLastExceptionStatement
    | setSwitchStatement
    | setEntryStatement
    | setSizeStatement
    | setToValueStatement
    | setBooleanStatement
    | setAddressStatement
    | setObjectReferenceStatement
    | setIndexStatement
    ;

// SET LOCALE {LC_ALL | LC_COLLATE | LC_CTYPE | LC_MESSAGES | LC_MONETARY | LC_NUMERIC | LC_TIME | USER-DEFAULT} TO
// {identifier-10 | locale-name-1 | USER-DEFAULT | SYSTEM-DEFAULT} (ISO §14.9.39 Format 11, set-locale) and
// SET identifier-11 TO LOCALE {LC_ALL | USER-DEFAULT} (Format 12, save-locale).
// ⛔ PARSED SO IT CAN BE DIAGNOSED, NOT SO IT CAN BE USED — Annex A.4.9 item 9 of the optional locale module, which
// COBOL.NET does not provide; the binder emits COBOLNET1518 (the LOCALE clause / CHARACTER CLASSIFICATION disposition —
// kb/Work PB92: F11 used to bind as a generic SET of a data item named LOCALE, "'LOCALE' is not defined" plus false
// 0901s about the format's own keywords; F12 was `unexpected '.'`). LOCALE / LC_* / USER-DEFAULT / SYSTEM-DEFAULT are
// plain words (reserved 2002+, §8.9 — the predicates are edition-gated like localeClauseAhead), so the arms are
// predicated on the word texts; every word inside is exempt from the §8.9 funnel, as the LOCALE clause's are.
// ⛔ THE PREDICATES ARE LEFT-EDGE. A predicate after SET is not hoisted into prediction — it is asserted only when
// the alternative is entered — and the first cut put them mid-alternative: ANTLR chose this rule for `SET IDX-1 TO
// SUB-1.` (the two arms share the SET … TO … prefix) and every NIST program with a SET died with "rule
// setLocaleStatement failed predicate". Left-edge, a false predicate makes the alternative non-viable and the
// ordinary SET forms are chosen as before.
setLocaleStatement
    : {setLocaleAhead()}? SET cobolWord cobolWord TO dataReference
    | {saveLocaleAhead()}? SET dataReference TO cobolWord cobolWord
    ;

// SET program-pointer+ TO ENTRY {literal | identifier} (ISO §14.9.39 Format 9 with the §8.4.3.13
// program-address-identifier as the sender): assign the address of the program the ENTRY operand names.
// Listed BEFORE setToValueStatement: ENTRY is a reserved token (not in cobolWord), so no other SET form can
// claim the `TO ENTRY` prefix. A not-locatable program → EC-PROGRAM-NOT-FOUND + NULL (§8.4.3.13 GR4).
setEntryStatement
    : SET dataReference+ TO ENTRY (nonNumericLiteral | dataReference)
    ;

// SET LAST EXCEPTION TO OFF (ISO §14.9.39 Format 13, saved-exception; 2002+ — binder-gated): the last
// exception status indicates no exception condition exists (§14.6.13.1.1). Listed FIRST: LAST is a reserved
// token (never a dataReference head), so no other SET form can claim the prefix.
setLastExceptionStatement
    : SET LAST EXCEPTION TO OFF
    ;

// SET mnemonic-name+ TO {ON | OFF} (COBOL-85 §14.9.39 Format 3)
// Supports compound form: SET sw-1 TO ON sw-2 TO OFF.
setSwitchStatement
    : SET (dataReference+ TO (ON | OFF))+
    ;

// SET dataReference+ TO arithmeticExpression (COBOL-85 §14.9.39 Format 1)
// SET [SIZE OF] data-name-3 TO {integer-2 | arithmetic-expression-5} (ISO §14.9.39 Format 16, COBOL-2023):
// set the current length of a DYNAMIC LENGTH elementary item. SIZE OF is the explicit form (SIZE is a reserved
// token, so it cannot head a dataReference — no ambiguity, listed before setToValueStatement). The SIZE-OF-absent
// bare form `SET dyn TO n` parses as setToValueStatement and re-routes at bind via a dynamic-length peek.
setSizeStatement
    : SET SIZE OF dataReference TO arithmeticExpression
    ;

setToValueStatement
    : SET dataReference+ TO arithmeticExpression
    ;

// SET dataReference+ TO TRUE/FALSE (COBOL-85 §14.9.39 Format 5)
setBooleanStatement
    : SET dataReference+ TO (TRUE_ | FALSE_)
    ;

// Pointer address forms (COBOL-2002 §14.9.39):
//   SET ADDRESS OF based-item TO pointer   — rebase a BASED/LINKAGE item
//   SET pointer TO ADDRESS OF identifier   — take a pointer to an item (ADDRESS OF as sender)
setAddressStatement
    : SET ADDRESS OF dataReference TO (dataReference | NULL_)   // SR19 — identifier-6 is a data-pointer or the predefined address NULL (kb/Work PB89)
    | SET dataReference TO ADDRESS OF dataReference
    ;

// ALLOCATE statement (COBOL-2002 §14.9.3): obtain dynamic storage, returned as a managed data-pointer.
//   ALLOCATE n CHARACTERS [INITIALIZED] [RETURNING p]   — allocate n bytes, return the pointer
//   ALLOCATE based-item   [INITIALIZED] [RETURNING p]   — allocate storage for a BASED item, set its address
allocateStatement
    : ALLOCATE arithmeticExpression CHARACTERS INITIALIZED? (RETURNING dataReference)?
    | ALLOCATE dataReference INITIALIZED? (RETURNING dataReference)?
    ;

// FREE statement (COBOL-2002 §14.9.15): release storage previously ALLOCATEd; set each data-pointer to NULL.
freeStatement
    : FREE dataReference+
    ;

// SET {identifier-3}... TO object/NULL/SELF (ISO 14.9.39 Format 5, OO 2002+). ANTLR-order reality
// (feedback_grammar_precedence): a dataReference SENDER parses as setToValueStatement (alternative 3
// precedes this rule and `TO arithmeticExpression` matches it) — BindSetTo re-routes SEMANTICALLY when a
// target is an object-reference item; only NULL/SELF/SUPER senders (no arithmeticExpression prefix) reach
// THIS rule. SUPER is admitted syntactically and rejected at bind (SR9 - 0867) for the better diagnostic.
setObjectReferenceStatement
    : SET dataReference+ TO objectReference   // introduction-gated at BIND time (StatementBinder.OoBindSetObjectRef → Check(SetObjectReference2002))
    ;

objectReference
    : dataReference
    | NULL_
    | SELF
    | SUPER
    ;

// SET dataReference+ UP/DOWN BY arithmeticExpression (COBOL-85 §14.9.39 Format 2)
setIndexStatement
    : SET dataReference+ ( UP | DOWN ) BY arithmeticExpression
    ;

// ==========================================
// ACCEPT (§14.9.0)
// ==========================================

acceptStatement
    : ACCEPT dataReference (FROM acceptSource)?
    ;

acceptSource
    : DATE YYYYMMDD
    | DATE
    | TIME
    | DAY YYYYDDD
    | DAY
    | DAY_OF_WEEK
    | dataReference
    ;

// ==========================================
// DISPLAY (§14.9.11)
// ==========================================

// An operand may be a function-identifier (ISO §8.4.4.1 — an identifier includes a function-identifier;
// §14.9.11.2 identifier-1): DISPLAY FUNCTION EXCEPTION-STATUS is the EC model's canonical interrogation shape.
// functionCall starts with the FUNCTION token, so the alternative is unambiguous.
displayStatement
    : DISPLAY (dataReference | literal | functionCall)+ displayUpon? displayNoAdvancing? END_DISPLAY?
    ;

displayUpon
    : UPON cobolWord
    ;

displayNoAdvancing
    : WITH? NO ADVANCING
    ;

// ==========================================
// GOBACK (§14.9.16)
// ==========================================

gobackStatement
    // RETURNING introduction-gated at BIND time (CallBindGoback → Check(GobackReturning2002)); the RAISING and
    // 2023 STATUS phrases are the mutually-exclusive §14.9.18.2 tail alternatives (statusPhrase shared with STOP).
    : GOBACK ((RETURNING | GIVING) dataReference)? (raisingPhrase | statusPhrase)?
    ;

// ==========================================
// RAISE / RESUME + the RAISING phrase (EC model, ISO §14.9.29 / §14.9.33 / §14.9.18; 2002+)
// ==========================================
// UNgated alternatives by design: the statements parse at every edition and the BINDER issues the targeted
// not-in-this-edition diagnostic at --std 85 (a no-viable-alternative parse error names nothing — the
// four-compilers rule wants the edition named).

// RAISE {EXCEPTION exception-name-1 | identifier-1} (ISO §14.9.29.2). The exception-name is a cobolWord —
// EC names are an OPEN set (EC-USER-*/EC-IMP-*, §14.6.13.1.1), so name validation is the binder's (SR1/SR2).
// RAISE identifier-1 takes objectReference (not bare dataReference) so SR2's "NULL and SUPER shall not
// be specified" gets a TARGETED diagnostic instead of a parse error, and RAISE SELF (legal) parses
// (§14.9.29.3 SR2; the EC-OO wave, deep-dive slice 6).
raiseStatement
    : RAISE (EXCEPTION cobolWord | objectReference)
    ;

// RESUME [AT] {NEXT STATEMENT | procedure-name-1} (ISO §14.9.33.2 — AT is an OPTIONAL word: it is not
// underlined in the general format, so `RESUME NEXT STATEMENT` / `RESUME procedure-name` are legal too).
resumeStatement
    : RESUME AT? (NEXT STATEMENT | procedureName)
    ;

// RAISING {EXCEPTION exception-name-1 | identifier-1 | LAST EXCEPTION} (ISO §14.9.18.2 / §14.9.14.2 F2) —
// the ONE raising-phrase rule GOBACK and EXIT PROGRAM share.
raisingPhrase
    : RAISING (EXCEPTION cobolWord | LAST EXCEPTION | dataReference)
    ;

// The PROCEDURE DIVISION header RAISING clause (ISO §14.2.1: RAISING {exception-name | class-name |
// interface-name}… — all cobolWords; classes/interfaces resolve at the OO wave).
raisingClause
    : RAISING cobolWord+
    ;

// ==========================================
// REUSABLE EXCEPTION PHRASES
// ==========================================

// (Deleted 2026-07-19: `exceptionPhrase` / `onExceptionPhrase` / `notOnExceptionPhrase` were DEAD — defined
// here but referenced by nothing. They were also a trap: `exceptionPhrase` modelled the pair as an exclusive
// one-of-two, which is exactly the 5.2.6.4 choice-indicator defect repaired across this file, so wiring them
// up would have reintroduced it. Statements with ON EXCEPTION own their phrase rules — see
// callExceptionPhrases above and deleteFileOnException in Core/CobolIO.g4.)
