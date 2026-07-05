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

import CobolExpressions, CobolData, CobolSpecialNames, CobolReportWriter, CobolIO, CobolControlFlow, CobolExtensionsJsonXml, CobolOO, CobolScreen;

// ==========================================
// CONTEXT-SENSITIVE KEYWORDS
// ==========================================
// Tokens that have special meaning in specific contexts but are NOT COBOL-85
// reserved words, so they may also appear as user-defined names (data names,
// file names, paragraph names, etc.). Any token promoted from IDENTIFIER to
// a dedicated lexer token must be listed here to remain usable as a name.
cobolWord
    : IDENTIFIER
    | LENGTH       // context: START WITH LENGTH, FUNCTION LENGTH
    | NATIONAL     // context: FOR NATIONAL
    | BIT          // context: USAGE BIT
    | NORMAL       // context: STOP RUN WITH NORMAL
    | PARSE        // context: JSON/XML PARSE (2014+); a legal user word everywhere else
    | PROCESSING   // context: XML PARSE … PROCESSING PROCEDURE (2014+); a legal user word everywhere else
    // EC exception-model words (2002+, ISO §14.6.13 family) — context-sensitive, legal user words at every
    // edition (the version-matrix continuity invariant; each is mirrored in the lexer _dataNameTokens set):
    | RAISE        // context: RAISE statement (§14.9.29)
    | RAISING      // context: GOBACK/EXIT … RAISING, PD-header RAISING (§14.9.18 / §14.2)
    | RESUME       // context: RESUME statement (§14.9.33)
    | STATEMENT    // context: RESUME AT NEXT STATEMENT (§14.9.33)
    | CONDITION    // context: USE AFTER EXCEPTION CONDITION (§14.9.49 F3)
    | EC           // context: USE AFTER EC (§14.9.49.3 SR12)
    // The 2023 logical-operator words (Annex E.2 item 25; VCR rows 32/41 — the W3 XOR regating): user-defined
    // words below 2023 (the operator is {is2023()}?-gated in CobolExpressions.g4); the §8.9 funnel rejects
    // them 0901 at 2023 (both are high-confidence table rows). Mirrored in the lexer _dataNameTokens set.
    | XOR          // context: the logical exclusive-or operator (2023, §8.8.4.9)
    | EXCLUSIVE_OR // context: = XOR (2023, §8.8.4.9)
    // The X3.23-1985 notInGrammar 85-acceptance words (VCR Table 7 rows 7.15–7.18 — the W3 batch): each
    // parses through its own dedicated rule (rerunClause / enterStatement / the USE FOR DEBUGGING format /
    // the section-header segment-number), never a name slot, so they are position-safe in the §8.9 funnel
    // (CheckedTokenTypes). '85-reserved; user-defined words at the editions where the funnel frees them
    // (RERUN/ENTER ≥2002, DEBUGGING ≥2014, the rest ≥2023 per ReservedWords.Table). Mirrored in the lexer
    // _dataNameTokens set.
    | OVERRIDE     // context: the METHOD-ID attribute slot (§11.7, 2002+; a direct token there, never a name slot — position-safe); '85 user word, 0901 >=2002 (ReservedWords.Table)
    | GET          // context: METHOD-ID GET PROPERTY (§11.7, 2002+); '85 user word, 0901 >=2002
    | PROPERTY     // context: the PROPERTY clause / selector / repository specifier (2002+); '85 user word, 0901 >=2002
    | INTERFACE    // context: END INTERFACE / repository INTERFACE specifier (2002+); '85 user word, 0901 >=2002
    | IMPLEMENTS   // context: the FACTORY/OBJECT IMPLEMENTS clause (§11.8) — §8.10 CONTEXT-SENSITIVE: a user word at EVERY edition (never funneled)
    | FACTORY      // context: the FACTORY paragraph (§11.4, 2002+; keyword occurrences parse only via factoryParagraph/END FACTORY/FACTORY OF — position-safe in the funnel); '85 user word, 0901 >=2002 (ReservedWords.Table)
    | RERUN        // context: the I-O-CONTROL RERUN clause ('85; row 7.15)
    | ENTER        // context: the ENTER statement ('85; row 7.16)
    | EVERY        // context: RERUN … EVERY ('85; row 7.15)
    | CLOCK_UNITS  // context: RERUN … EVERY n CLOCK-UNITS ('85; row 7.15)
    | DEBUGGING    // context: USE FOR DEBUGGING ('85; row 7.17)
    | REFERENCES   // context: USE FOR DEBUGGING ON ALL REFERENCES OF ('85; row 7.17)
    | PROCEDURES   // context: USE FOR DEBUGGING ON ALL PROCEDURES ('85; row 7.17)
    // Screen-related tokens that may be used as data names in non-screen contexts
    | AUTO
    | BELL
    | BLINK
    | COL
    | COLUMN
    | EOL
    | EOS
    | ERASE
    | FULL_
    | HIGHLIGHT
    | LOWLIGHT
    | REQUIRED
    | SCREEN
    | SECURE
    | UNDERLINE_
    // OPTIONS-paragraph context-sensitive words (ISO §11.9): reserved only inside OPTIONS, legal data-names
    // elsewhere. Each MUST also be mirrored in the lexer _dataNameTokens set (CobolLexer.g4) so a subscripted
    // use triggers SUBSCRIPT mode.
    | ARITHMETIC
    | DEFAULT
    | INTERMEDIATE
    | ROUNDING
    | STANDARD_BINARY
    | STANDARD_DECIMAL
    | ENTRY_CONVENTION
    | FLOAT_BINARY
    | FLOAT_DECIMAL
    | HIGH_ORDER_LEFT
    | HIGH_ORDER_RIGHT
    | BINARY_ENCODING
    | DECIMAL_ENCODING
    ;

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
    : (programUnit | {is2002()}? classDefinition | {is2002()}? interfaceDefinition)+   // OO/2002 rules live in Core/CobolOO.g4
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
// callable program named after the function — invocation via FUNCTION user-name(args) is a later slice; for now
// it is reachable as a CALL target, exercising the same RETURNING path.
functionIdParagraph
    : FUNCTION_ID DOT programName DOT
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
    | {is2002()}? CLASS className   // OO (2002): CLASS class-name [AS literal] — declares a referenced class (className rule in Core/CobolOO.g4)
    | {is2002()}? INTERFACE interfaceName   // OO (2002): the interface specifier (§12.3.8; AS-literal tail deferred like CLASS's)
    | {is2002()}? PROPERTY propertyName     // OO (2002): the property specifier (§12.3.8 — required by §8.4.3.9.3 SR1 property references)
    ;

// SOURCE-COMPUTER.
sourceComputerParagraph
    : SOURCE_COMPUTER DOT (computerName computerAttributes? DOT)?
    ;

objectComputerParagraph
    : OBJECT_COMPUTER DOT (computerName computerAttributes?
      programCollatingSequenceClause? DOT)?
    ;

programCollatingSequenceClause
    : PROGRAM COLLATING? SEQUENCE IS? cobolWord
    ;

computerName
    : cobolWord
    ;

computerAttributes
    : ~(DOT | PROGRAM)+
    ;

// ==========================================
// PROCEDURE DIVISION
// ==========================================

procedureDivision
    : PROCEDURE DIVISION usingClause? ({is2002()}? returningClause)? ({is2002()}? raisingClause)? DOT
      declarativePart*
      procedureUnit*
    ;

usingClause
    : USING dataReferenceList
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
    | SUB_STRINGLIT
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
refModPart
    : LPAREN refModSpec RPAREN
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
    : sectionName SECTION integerLiteral? DOT paragraphDefinition*
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
    | {is2023()}? deleteFileStatement
    | {is2002()}? allocateStatement
    | {is2002()}? freeStatement
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
    | {is2014()}? jsonStatement
    | {is2014()}? xmlStatement
    | {is2002()}? invokeStatement
    | {is2023()}? inlineMethodInvocationStatement
    | continueStatement
    | nextSentenceStatement
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

arithmeticOnSizeError
    : ON SIZE ERROR statementBlock
      (NOT ON SIZE ERROR statementBlock)?
    | NOT ON SIZE ERROR statementBlock
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

addOperand
    : dataReference
    | literal
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
    : dataReference
    | literal
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
    : dataReference
    | literal
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
    : dataReference
    | literal
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
    : COMPUTE computeStore+ EQUALS arithmeticExpression computeOnSizeError? END_COMPUTE?
    ;

computeStore
    : dataReference roundedPhrase?
    ;

computeOnSizeError
    : ON SIZE ERROR statementBlock
      (NOT ON SIZE ERROR statementBlock)?
    | NOT ON SIZE ERROR statementBlock
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

callStatement
    : CALL callTarget
      callUsingPhrase?
      callReturningPhrase?
      callOnExceptionPhrase?
      callNotOnExceptionPhrase?
      END_CALL?

    ;

callTarget
    : literal
    | dataReference
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
    : {is2002()}? BY VALUE arithmeticExpression
    ;

callByContent
    : BY? CONTENT (dataReference | literal)
    ;

callReturningPhrase
    : RETURNING dataReference
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
    : setLastExceptionStatement
    | setSwitchStatement
    | setToValueStatement
    | setBooleanStatement
    | setAddressStatement
    | setObjectReferenceStatement
    | setIndexStatement
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
    : SET ADDRESS OF dataReference TO dataReference
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

// SET object-reference TO class/object reference (OO)
setObjectReferenceStatement
    : {is2002()}? SET dataReference TO objectReference
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
    : GOBACK ({is2002()}? (RETURNING | GIVING) dataReference)? raisingPhrase?
    ;

// ==========================================
// RAISE / RESUME + the RAISING phrase (EC model, ISO §14.9.29 / §14.9.33 / §14.9.18; 2002+)
// ==========================================
// UNgated alternatives by design: the statements parse at every edition and the BINDER issues the targeted
// not-in-this-edition diagnostic at --std 85 (a no-viable-alternative parse error names nothing — the
// four-compilers rule wants the edition named).

// RAISE {EXCEPTION exception-name-1 | identifier-1} (ISO §14.9.29.2). The exception-name is a cobolWord —
// EC names are an OPEN set (EC-USER-*/EC-IMP-*, §14.6.13.1.1), so name validation is the binder's (SR1/SR2).
raiseStatement
    : RAISE (EXCEPTION cobolWord | dataReference)
    ;

// RESUME AT {NEXT STATEMENT | procedure-name-1} (ISO §14.9.33.2 — AT is required in the 2023 format).
resumeStatement
    : RESUME AT (NEXT STATEMENT | procedureName)
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

exceptionPhrase
    : onExceptionPhrase
    | notOnExceptionPhrase
    ;

onExceptionPhrase
    : ON EXCEPTION statementBlock
    ;

notOnExceptionPhrase
    : NOT ON EXCEPTION statementBlock
    ;
