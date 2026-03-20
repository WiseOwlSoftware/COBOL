// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

parser grammar CobolParserCore;

options {
    tokenVocab = CobolLexer;
    superClass = CobolParserCoreBase;
}

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
    : programUnit+
    ;

programUnit
    : identificationDivision
      environmentDivision?
      dataDivision?
      procedureDivision?
    ;

// ==========================================
// IDENTIFICATION DIVISION
// ==========================================

identificationDivision
    : IDENTIFICATION DIVISION DOT identificationBody
    ;

identificationBody
    : programIdParagraph identificationParagraph*
    ;

// ------------------------------------------
// PROGRAM-ID paragraph
// ------------------------------------------

programIdParagraph
    : PROGRAM_ID DOT programName programIdAttributes? DOT
    ;

programName
    : IDENTIFIER
    ;

programIdAttributes
    : programIdAttribute+
    ;

programIdAttribute
    : commonProgramAttribute
    | literalAttribute
    | identifierAttribute
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

identifierAttribute
    : IDENTIFIER
    ;

// ------------------------------------------
// Other identification paragraphs
// ------------------------------------------

identificationParagraph
    : authorParagraph
    | installationParagraph
    | dateWrittenParagraph
    | dateCompiledParagraph
    | securityParagraph
    | remarksParagraph
    | genericIdentificationParagraph
    ;

// AUTHOR.
authorParagraph
    : AUTHOR DOT authorContent
    ;

authorContent
    : (IDENTIFIER | STRINGLIT)+
    ;

// INSTALLATION.
installationParagraph
    : INSTALLATION DOT installationContent
    ;

installationContent
    : (IDENTIFIER | STRINGLIT)+
    ;

// DATE-WRITTEN.
dateWrittenParagraph
    : DATE_WRITTEN DOT dateWrittenContent
    ;

dateWrittenContent
    : (IDENTIFIER | STRINGLIT | INTEGERLIT)+
    ;

// DATE-COMPILED.
dateCompiledParagraph
    : DATE_COMPILED DOT dateCompiledContent
    ;

dateCompiledContent
    : (IDENTIFIER | STRINGLIT | INTEGERLIT)+
    ;

// SECURITY.
securityParagraph
    : SECURITY DOT securityContent
    ;

securityContent
    : (IDENTIFIER | STRINGLIT)+
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
    : IDENTIFIER DOT (IDENTIFIER | STRINGLIT | INTEGERLIT)*
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
    : IDENTIFIER SECTION DOT configurationParagraph*
    ;

configurationParagraph
    : sourceComputerParagraph
    | objectComputerParagraph
    | specialNamesParagraph
    | genericConfigurationParagraph
    ;

// SOURCE-COMPUTER.
sourceComputerParagraph
    : SOURCE_COMPUTER DOT computerName computerAttributes? DOT
    ;

objectComputerParagraph
    : OBJECT_COMPUTER DOT computerName computerAttributes? DOT
    ;

computerName
    : IDENTIFIER
    ;

computerAttributes
    : (IDENTIFIER | STRINGLIT | INTEGERLIT)+
    ;

// SPECIAL-NAMES.
specialNamesParagraph
    : SPECIAL_NAMES DOT specialNameEntry+
    ;

specialNameEntry
    : currencySignClause DOT?
    | decimalPointClause DOT?
    | IDENTIFIER (IDENTIFIER | literal)*
      DOT?
    ;

currencySignClause
    : CURRENCY SIGN IS? literal
    ;

decimalPointClause
    : DECIMAL_POINT IS IDENTIFIER    // DECIMAL-POINT IS COMMA (COMMA is IDENTIFIER)
    ;

// fallback for vendor extensions
genericConfigurationParagraph
    : IDENTIFIER DOT (IDENTIFIER | STRINGLIT | INTEGERLIT)*
    ;

// ==========================================
// INPUT-OUTPUT SECTION
// ==========================================

inputOutputSection
    : IDENTIFIER SECTION DOT
      fileControlParagraph?
      ioControlParagraph?
    ;

// FILE-CONTROL.
fileControlParagraph
    : FILE_CONTROL DOT fileControlEntry+
    ;

fileControlEntry
    : SELECT fileName
      ( ASSIGN TO assignTarget )?
      fileControlClauses*
      DOT
    ;

assignTarget
    : IDENTIFIER
    | STRINGLIT
    ;

fileControlClauses
    : organizationClause
    | accessModeClause
    | recordKeyClause
    | alternateKeyClause
    | fileStatusClause
    | genericFileControlClause
    ;

organizationClause
    : ORGANIZATION IS? organizationType
    ;

organizationType
    : LINE SEQUENTIAL
    | SEQUENTIAL
    | RELATIVE
    | INDEXED
    ;

accessModeClause
    : ACCESS MODE? IS? accessMode
    ;

accessMode
    : SEQUENTIAL
    | RANDOM
    | DYNAMIC
    ;

recordKeyClause
    : 'RECORD' KEY IS identifier
    ;

alternateKeyClause
    : ALTERNATE KEY IS identifier
      (WITH? DUPLICATES)?
    ;

fileStatusClause
    : FILE STATUS IS identifier
    ;

genericFileControlClause
    : IDENTIFIER (IDENTIFIER | literal)*
    ;

// I-O-CONTROL.
ioControlParagraph
    : I_O_CONTROL DOT ioControlEntry+
    ;

ioControlEntry
    : IDENTIFIER (IDENTIFIER | literal)* DOT
    ;

// ==========================================
// DATA DIVISION
// ==========================================

dataDivision
    : DATA DIVISION DOT
      fileSection?
      workingStorageSection?
      localStorageSection?
      linkageSection?
    ;

// ==========================================
// FILE SECTION
// ==========================================

fileSection
    : FILE SECTION DOT fileDescriptionEntry*
    ;

fileDescriptionEntry
    : FD fileName fileDescriptionClauses? DOT dataDescriptionEntry*
    ;

fileDescriptionClauses
    : fileDescriptionClause+
    ;

fileDescriptionClause
    : organizationClause
    | accessModeClause
    | recordKeyClause
    | alternateKeyClause
    | fileStatusClause
    | dataRecordsClause
    | genericFileDescriptionClause
    ;

// DATA RECORD(S) IS/ARE — obsolete COBOL-74 FD clause, semantically inert
dataRecordsClause
    : DATA RECORD IS? IDENTIFIER+
    ;

genericFileDescriptionClause
    : IDENTIFIER (IDENTIFIER | literal)*
    ;

// ==========================================
// OTHER DATA SECTIONS
// ==========================================

workingStorageSection
    : WORKING_STORAGE SECTION DOT dataDescriptionEntry*
    ;

localStorageSection
    : LOCAL_STORAGE SECTION DOT dataDescriptionEntry*
    ;

linkageSection
    : LINKAGE SECTION DOT linkageEntry*
    ;

linkageEntry
    : dataDescriptionEntry
    | linkageProcedureParameter
    ;

// Procedure parameters (COBOL 2002+)
linkageProcedureParameter
    : {is2002()}? levelNumber dataName? parameterDescriptionBody DOT
    ;

parameterDescriptionBody
    : parameterPassingClause (dataDescriptionClause+)?
    ;

parameterPassingClause
    : USING (BY_REFERENCE | BY_VALUE | BY_CONTENT)? identifier
    ;

// ==========================================
// DATA DESCRIPTION ENTRIES
// ==========================================

dataDescriptionEntry
    : levelNumber dataName? dataDescriptionBody DOT
    ;

levelNumber
    : INTEGERLIT
    ;

dataName
    : IDENTIFIER
    | FILLER
    ;

dataDescriptionBody
    : dataDescriptionClauses
    | renamesClause
    ;

// ==========================================
// DATA DESCRIPTION CLAUSES
// ==========================================

dataDescriptionClauses
    : dataDescriptionClause*
    ;

dataDescriptionClause
    : pictureClause
    | usageClause
    | occursClause
    | redefinesClause
    | valueClause
    | signClause
    | syncClause
    | justifiedClause
    | blankWhenZeroClause
    | typeClause
    | genericDataClause
    ;

// TYPE clause (COBOL-2023 — threaded from CobolParserGenerics)
typeClause
    : {is2023()}? TYPE IS? IDENTIFIER
    ;

genericDataClause
    : IDENTIFIER (IDENTIFIER | literal)*
    ;

// PIC Clause — PIC/PICTURE triggers PICMODE in the lexer,
// which emits a single PIC_STRING token. IS is consumed by PICMODE.
pictureClause
    : PIC PIC_STRING
    ;

// USAGE Clause
usageClause
    : USAGE IS? usageKeyword        // full form: USAGE IS DISPLAY
    | DISPLAY                        // bare keyword forms (no USAGE prefix)
    | COMPUTATIONAL                  // per ISO §13.16 — USAGE keyword is optional
    | COMP
    | COMP_1
    | COMP_2
    | COMP_3
    | BINARY
    | PACKED_DECIMAL
    | INDEX
    ;

usageKeyword
    : DISPLAY
    | COMPUTATIONAL
    | COMP
    | COMP_1
    | COMP_2
    | COMP_3
    | BINARY
    | PACKED_DECIMAL
    | INDEX
    ;

// OCCURS Clause
occursClause
    : OCCURS integerLiteral (TO integerLiteral)? timesKeyword?
      (DEPENDING ON identifier)?
      (INDEXED BY identifierList)?
    ;

timesKeyword
    : TIMES
    ;

integerLiteral
    : INTEGERLIT
    ;

// REDEFINES Clause
redefinesClause
    : REDEFINES identifier
    ;

// RENAMES (Level 66)
renamesClause
    : RENAMES identifier (THRU identifier)?
    ;

// VALUE Clause — IS is optional noise word
// For level-88 condition entries, valueItem supports THRU ranges.
valueClause
    : (VALUE | VALUES) (IS | ARE)? valueItem (COMMA? valueItem)*
    ;

valueItem
    : literal (THRU literal)?
    ;

// SIGN Clause
signClause
    : (SIGN IS?)? (LEADING | TRAILING) (SEPARATE CHARACTER?)?
    ;

// JUSTIFIED / SYNCHRONIZED
justifiedClause
    : (JUSTIFIED | JUST) RIGHT?
    ;

syncClause
    : (SYNCHRONIZED | SYNC) (LEFT | RIGHT)?
    ;

// BLANK WHEN ZERO
blankWhenZeroClause
    : BLANK_WHEN_ZERO
    ;

// 88-LEVEL CONDITION ENTRIES — handled through valueClause with THRU support.
// Level number and condition name are already consumed by dataDescriptionEntry.
// The conditionEntry88 / valueSet / valueRange rules have been removed;
// valueClause now supports THRU ranges via valueItem for level-88 entries.

// ==========================================
// PROCEDURE DIVISION
// ==========================================

procedureDivision
    : PROCEDURE DIVISION usingClause? ({is2002()}? returningClause)? DOT
      declarativePart*
      procedureSectionOrParagraph*
    ;

usingClause
    : USING identifierList
    ;

returningClause
    : RETURNING identifier
    ;

identifierList
    : identifier (COMMA? identifier)*
    ;

identifier
    : IDENTIFIER dataNameTail*
    ;

dataNameTail
    : subscriptPart
    | refModPart
    | qualification
    ;

qualification
    : (OF | IN) IDENTIFIER (subscriptPart | refModPart)*
    ;

subscriptPart
    : LPAREN subscriptList RPAREN
    ;

refModPart
    : LPAREN refModSpec RPAREN
    ;

refModSpec
    : arithmeticExpression COLON arithmeticExpression?
    ;

subscriptList
    : arithmeticExpression (COMMA? arithmeticExpression)*
    ;

fileName
    : IDENTIFIER
    ;

// ==========================================
// DECLARATIVES
// ==========================================

declarativePart
    : DECLARATIVES DOT declarativeSection+ END DECLARATIVES DOT
    ;

declarativeSection
    : sectionName SECTION DOT declarativeParagraph+
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

procedureSectionOrParagraph
    : sectionDeclaration
    | paragraphDeclaration
    ;

sectionDeclaration
    : sectionName SECTION DOT paragraphDeclaration*
    ;

sectionName
    : procedureName
    ;

paragraphDeclaration
    : paragraphName DOT sentence*
    ;

paragraphName
    : {IsAtLineStart()}? procedureName
    ;

// ==========================================
// STATEMENT DISPATCHER
// ==========================================

statement
    : acceptStatement
    | addStatement
    | callStatement
    | cancelStatement
    | closeStatement
    | computeStatement
    | deleteStatement
    | {is2023()}? deleteFileStatement
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
    | readStatement
    | releaseStatement
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
imperativeStatement
    : statement+
    ;

// ==========================================
// READ (§14.9.30 — full expansion)
// ==========================================

readStatement
    : READ fileName
      readDirection?
      readInto?
      readKey?
      readAtEnd?
      readInvalidKey?
      END_READ?
     
    ;

readDirection
    : (NEXT | PREVIOUS) RECORD
    ;

readInto
    : INTO identifier
    ;

readKey
    : KEY IS identifier
    ;

readAtEnd
    : AT END imperativeStatement
      (NOT AT END imperativeStatement)?
    ;

readInvalidKey
    : INVALID KEY imperativeStatement
      (NOT INVALID KEY imperativeStatement)?
    ;

// ==========================================
// WRITE (§14.9.46 — full expansion)
// ==========================================

writeStatement
    : WRITE recordName
      writeFrom?
      writeBeforeAfter?
      writeInvalidKey?
      END_WRITE?
     
    ;

writeFrom
    : FROM identifier
    ;

writeBeforeAfter
    : ('BEFORE' | 'AFTER') ADVANCING (identifier | integerLiteral | literal) (LINE | LINES)?
    ;

writeInvalidKey
    : INVALID KEY imperativeStatement
      (NOT INVALID KEY imperativeStatement)?
    ;

// ==========================================
// OPEN / CLOSE (§14.9.25, §14.9.7)
// ==========================================

openStatement
    : OPEN openClause+
    ;

openClause
    : openMode identifier+
    ;

openMode
    : INPUT
    | OUTPUT
    | I_O
    | EXTEND
    ;

closeStatement
    : CLOSE identifierList
    ;

// ==========================================
// IF / END-IF (§14.9.19)
// ==========================================

ifStatement
    : IF condition THEN?
      imperativeStatement*
      (ELSE imperativeStatement*)?
      END_IF?
    ;

// ==========================================
// PERFORM / END-PERFORM (§14.9.28)
// ==========================================

performStatement
    // Out-of-line: explicit forms to avoid greedy "PERFORM target" swallowing options
    : PERFORM procedureName performTimes                                       // PERFORM para N TIMES
    | PERFORM procedureName performUntil                                       // PERFORM para UNTIL cond
    | PERFORM procedureName performVarying                                     // PERFORM para VARYING ...
    | PERFORM procedureName (THRU | THROUGH) procedureName performOptions?     // PERFORM para THRU para [options]
    | PERFORM procedureName                                                    // PERFORM para (simple)
    // Inline forms
    | PERFORM performOptions+ imperativeStatement* END_PERFORM                 // PERFORM UNTIL/VARYING ... END-PERFORM
    | PERFORM imperativeStatement+ END_PERFORM                                 // PERFORM ... END-PERFORM (block)
    ;

performTarget
    : procedureName ((THRU | THROUGH) procedureName)?
    ;

procedureName
    : IDENTIFIER
    | INTEGERLIT
    ;

performOptions
    : performTimes
    | performUntil
    | performVarying
    ;

performTimes
    : (integerLiteral | identifier) TIMES
    ;

performUntil
    : UNTIL condition
    ;

performVarying
    : VARYING identifier FROM arithmeticExpression
      BY arithmeticExpression
      UNTIL condition
      performVaryingAfter*
    ;

performVaryingAfter
    : AFTER identifier FROM arithmeticExpression
      BY arithmeticExpression
      UNTIL condition
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
    : TRUE_                        // EVALUATE TRUE (condition-only mode)
    | arithmeticExpression
    ;

evaluateWhenClause
    : WHEN evaluateWhenGroup (ALSO evaluateWhenGroup)* imperativeStatement*
    | WHEN OTHER imperativeStatement*
    ;

evaluateWhenGroup
    : evaluateWhenItem+
    ;

evaluateWhenItem
    : arithmeticExpression (THRU | THROUGH) arithmeticExpression   // range
    | arithmeticExpression                                          // single value
    | condition                                                     // for EVALUATE TRUE
    | ANY                                                           // match anything
    ;

// ==========================================
// COMPUTE (§14.9.8)
// ==========================================

computeStatement
    : COMPUTE computeStore+ EQUALS arithmeticExpression computeOnSizeError? END_COMPUTE?
    ;

computeStore
    : identifier ROUNDED?
    ;

computeOnSizeError
    : ON SIZE ERROR imperativeStatement
      (NOT ON SIZE ERROR imperativeStatement)?
    | NOT ON SIZE ERROR imperativeStatement
    ;

// ==========================================
// CONTINUE / NEXT SENTENCE (§14.9.9, §14.9.19)
// ==========================================

continueStatement
    : CONTINUE
    ;

nextSentenceStatement
    : NEXT_SENTENCE
    ;

// ==========================================
// INLINE METHOD INVOCATION (COBOL 2023)
// ==========================================

inlineMethodInvocationStatement
    : identifier LPAREN argumentList? RPAREN
    ;

argumentList
    : argument (COMMA argument)*
    ;

argument
    : arithmeticExpression
    | literal
    | identifier
    ;

// ==========================================
// SHARED ARITHMETIC RULES
// ==========================================

// Unified GIVING-form receiving operand.
// COBOL-85: in any arithmetic GIVING form, the receiving operand may be
// either an identifier or a literal. One rule, one source of truth.
givingReceiver
    : identifier
    | literal
    ;

arithmeticTarget
    : identifier ROUNDED?
    ;

arithmeticOnSizeError
    : ON SIZE ERROR imperativeStatement
      (NOT ON SIZE ERROR imperativeStatement)?
    | NOT ON SIZE ERROR imperativeStatement
    ;

// ==========================================
// ADD (§14.9.1)
// ==========================================

addStatement
    : ADD CORRESPONDING identifier TO identifier ROUNDED? arithmeticOnSizeError? END_ADD?
    | ADD addOperandList addToPhrase? addGivingPhrase? arithmeticOnSizeError? END_ADD?
    ;

addOperandList
    : addOperand+
    ;

addOperand
    : identifier
    | literal
    ;

addToPhrase
    : TO arithmeticTarget+
    ;

addGivingPhrase
    : GIVING arithmeticTarget+
    ;

// ==========================================
// SUBTRACT (§14.9.42)
// ==========================================

subtractStatement
    : SUBTRACT CORRESPONDING identifier FROM identifier ROUNDED? arithmeticOnSizeError? END_SUBTRACT?
    | SUBTRACT subtractOperandList subtractFromPhrase? subtractGivingPhrase? arithmeticOnSizeError? END_SUBTRACT?
    ;

subtractOperandList
    : subtractOperand+
    ;

subtractOperand
    : identifier
    | literal
    ;

subtractFromPhrase
    : FROM subtractFromOperand
    ;

subtractFromOperand
    : arithmeticTarget (arithmeticTarget)*
    | givingReceiver
    ;

subtractGivingPhrase
    : GIVING arithmeticTarget (arithmeticTarget)*
    ;

// ==========================================
// MULTIPLY (§14.9.23)
// ==========================================

multiplyStatement
    : MULTIPLY multiplyOperand BY multiplyByOperand+ multiplyGivingPhrase? arithmeticOnSizeError? END_MULTIPLY?
    ;

multiplyOperand
    : identifier
    | literal
    ;

multiplyByOperand
    : givingReceiver ROUNDED?
    ;

multiplyGivingPhrase
    : GIVING arithmeticTarget+
    ;

// ==========================================
// DIVIDE (§14.9.12)
// ==========================================

divideStatement
    : DIVIDE divideOperand (divideIntoPhrase | divideByPhrase)
      divideGivingPhrase? divideRemainderPhrase? arithmeticOnSizeError? END_DIVIDE?
    ;

divideOperand
    : identifier
    | literal
    ;

divideIntoPhrase
    : INTO divideIntoOperand
    ;

divideIntoOperand
    : arithmeticTarget+   // identifier ROUNDED? (non-GIVING form, multiple targets)
    | literal             // numeric literal (GIVING form only)
    ;

divideByPhrase
    : BY divideOperand
    ;

divideGivingPhrase
    : GIVING arithmeticTarget+
    ;

divideRemainderPhrase
    : REMAINDER identifier
    ;

// ==========================================
// MOVE (§14.9.24)
// ==========================================

moveStatement
    : MOVE moveSource moveTarget
    ;

moveSource
    : literal
    | identifier
    ;

moveTarget
    : TO identifierList
    | CORRESPONDING identifier TO identifier
    ;

// ==========================================
// STRING (§14.9.41)
// ==========================================

stringStatement
    : STRING stringSendingPhrase+ stringIntoPhrase stringWithPointer? stringOnOverflow? END_STRING?
    ;

stringSendingPhrase
    : (identifier | literal | figurativeConstant)
      (DELIMITED BY (ALL)? (identifier | literal | figurativeConstant | SIZE))?
    ;

stringIntoPhrase
    : INTO identifier
    ;

stringWithPointer
    : WITH POINTER identifier
    ;

stringOnOverflow
    : ON OVERFLOW imperativeStatement
      (NOT ON OVERFLOW imperativeStatement)?
    ;

// ==========================================
// UNSTRING (§14.9.44)
// ==========================================

unstringStatement
    : UNSTRING identifier
      unstringDelimiterPhrase?
      unstringIntoPhrase+
      unstringWithPointer?
      unstringTallying?
      unstringOnOverflow?
      END_UNSTRING?
     
    ;

unstringDelimiterPhrase
    : DELIMITED BY (ALL)? (identifier | literal | figurativeConstant)
    ;

unstringIntoPhrase
    : INTO identifier
      (DELIMITER IN identifier)?
      (COUNT IN identifier)?
    ;

unstringWithPointer
    : WITH POINTER identifier
    ;

unstringTallying
    : TALLYING IN identifier
    ;

unstringOnOverflow
    : ON OVERFLOW imperativeStatement
      (NOT ON OVERFLOW imperativeStatement)?
    ;

// ==========================================
// REMAINING STATEMENT STUBS (to be expanded)
// ==========================================

// ==========================================
// CALL (§14.9.4)
// ==========================================

callStatement
    : CALL callTarget
      callUsingPhrase?
      callReturningPhrase?
      callOnExceptionPhrase?
      END_CALL?
     
    ;

callTarget
    : literal
    | identifier
    ;

callUsingPhrase
    : USING callArgument+
    ;

callArgument
    : callByReference
    | callByValue
    | callByContent
    ;

callByReference
    : BY 'REFERENCE'? identifier
    ;

callByValue
    : {is2002()}? BY VALUE arithmeticExpression
    ;

callByContent
    : BY 'CONTENT' (identifier | literal)
    ;

callReturningPhrase
    : RETURNING identifier
    ;

callOnExceptionPhrase
    : ON EXCEPTION imperativeStatement
      (NOT ON EXCEPTION imperativeStatement)?
    ;

// ==========================================
// CANCEL (§14.9.5)
// ==========================================

cancelStatement
    : CANCEL identifierList
    ;

// ==========================================
// SET (§14.9.39 — all forms)
// ==========================================

setStatement
    : setToValueStatement
    | setBooleanStatement
    | setAddressStatement
    | setObjectReferenceStatement
    | setIndexStatement
    ;

// SET identifier TO arithmeticExpression
setToValueStatement
    : SET identifier TO arithmeticExpression
    ;

// SET identifier TO TRUE/FALSE
setBooleanStatement
    : SET identifier TO (TRUE_ | FALSE_)
    ;

// SET ADDRESS OF identifier TO identifier
setAddressStatement
    : SET ADDRESS OF identifier TO identifier
    ;

// SET object-reference TO class/object reference (OO)
setObjectReferenceStatement
    : {is2002()}? SET identifier TO objectReference
    ;

objectReference
    : identifier
    | NULL_
    | SELF
    | SUPER
    ;

// SET index UP/DOWN BY integer
setIndexStatement
    : SET identifier ( UP | DOWN ) BY integerLiteral
    ;

// ==========================================
// REMAINING STATEMENT STUBS (to be expanded)
// ==========================================

// ==========================================
// SORT (§14.9.40)
// ==========================================

sortStatement
    : SORT sortFileName
      sortKeyPhrase*
      sortUsingPhrase?
      sortGivingPhrase?
      sortInputProcedurePhrase?
      sortOutputProcedurePhrase?
      END_SORT?
     
    ;

sortFileName
    : identifier
    ;

sortKeyPhrase
    : (ASCENDING | DESCENDING) KEY identifierList
    ;

sortUsingPhrase
    : USING identifierList
    ;

sortGivingPhrase
    : GIVING identifierList
    ;

sortInputProcedurePhrase
    : INPUT PROCEDURE IS procedureName
    ;

sortOutputProcedurePhrase
    : OUTPUT PROCEDURE IS procedureName
    ;

// ==========================================
// MERGE (§14.9.22)
// ==========================================

mergeStatement
    : MERGE mergeFileName
      mergeKeyPhrase+
      mergeUsingPhrase
      mergeOutputProcedurePhrase?
      mergeGivingPhrase?
      END_MERGE?
     
    ;

mergeFileName
    : identifier
    ;

mergeKeyPhrase
    : (ASCENDING | DESCENDING) KEY identifierList
    ;

mergeUsingPhrase
    : USING identifierList
    ;

mergeGivingPhrase
    : GIVING identifierList
    ;

mergeOutputProcedurePhrase
    : OUTPUT PROCEDURE IS procedureName
    ;

// ==========================================
// RETURN (§14.9.34)
// ==========================================

returnStatement
    : RETURN fileName RECORD
      (INTO identifier)?
      returnAtEndPhrase?
      END_RETURN?
     
    ;

returnAtEndPhrase
    : AT END imperativeStatement
      (NOT AT END imperativeStatement)?
    ;

// ==========================================
// RELEASE (§14.9.33)
// ==========================================

releaseStatement
    : RELEASE identifier
      (FROM identifier)?
     
    ;

// ==========================================
// REWRITE (§14.9.36)
// ==========================================

rewriteStatement
    : REWRITE recordName
      (FROM identifier)?
      rewriteInvalidKeyPhrase?
      END_REWRITE?
     
    ;

recordName
    : identifier
    ;

rewriteInvalidKeyPhrase
    : INVALID KEY imperativeStatement
      (NOT INVALID KEY imperativeStatement)?
    ;

// ==========================================
// DELETE FILE (§14.9.10 — COBOL 2023)
// ==========================================

deleteFileStatement
    : DELETE FILE fileName
      deleteFileOnException?
      END_DELETE?
     
    ;

deleteFileOnException
    : ON EXCEPTION imperativeStatement
      (NOT ON EXCEPTION imperativeStatement)?
    ;

// ==========================================
// DELETE RECORD (§14.9.11)
// ==========================================

deleteStatement
    : DELETE fileName RECORD?
      deleteInvalidKeyPhrase?
      END_DELETE?
     
    ;

deleteInvalidKeyPhrase
    : INVALID KEY imperativeStatement
      (NOT INVALID KEY imperativeStatement)?
    ;

// ==========================================
// REUSABLE EXCEPTION PHRASES
// ==========================================

exceptionPhrase
    : onExceptionPhrase
    | notOnExceptionPhrase
    ;

onExceptionPhrase
    : ON EXCEPTION imperativeStatement
    ;

notOnExceptionPhrase
    : NOT ON EXCEPTION imperativeStatement
    ;

// ==========================================
// REMAINING STATEMENT STUBS (to be expanded)
// ==========================================

// ==========================================
// STOP (§14.9.41)
// ==========================================

stopStatement
    : STOP RUN
    ;

// ==========================================
// GOBACK (§14.9.16)
// ==========================================

gobackStatement
    : GOBACK
    ;

// ==========================================
// EXIT (§14.9.14)
// ==========================================

exitStatement
    : EXIT ( PROGRAM | PERFORM | SECTION | PARAGRAPH | METHOD | FUNCTION )?
    ;

// ==========================================
// START (§14.9.38)
// ==========================================

startStatement
    : START fileName
      startKeyPhrase?
      startInvalidKeyPhrase?
      END_START?
     
    ;

startKeyPhrase
    : KEY IS relationalExpression
    ;

startInvalidKeyPhrase
    : INVALID KEY imperativeStatement
      (NOT INVALID KEY imperativeStatement)?
    ;

// ==========================================
// GO TO (§14.9.17)
// ==========================================

goToStatement
    : GO TO? procedureName (procedureName)* (DEPENDING ON? identifier)?
    ;

// ==========================================
// ACCEPT (§14.9.0)
// ==========================================

acceptStatement
    : ACCEPT identifier (FROM acceptSource)?
    ;

acceptSource
    : DATE
    | TIME
    | DAY
    | DAY_OF_WEEK
    ;

// ==========================================
// DISPLAY (§14.9.11)
// ==========================================

displayStatement
    : DISPLAY (identifier | literal)+
    ;

// ==========================================
// INITIALIZE (§14.9.20)
// ==========================================

initializeStatement
    : INITIALIZE identifierList initializeReplacingPhrase?
    ;

initializeReplacingPhrase
    : REPLACING initializeReplacingItem+
    ;

initializeReplacingItem
    : ALPHANUMERIC DATA BY (identifier | literal)
    | NUMERIC DATA BY (identifier | literal)
    | ALPHANUMERIC EDITED DATA BY (identifier | literal)
    | NUMERIC EDITED DATA BY (identifier | literal)
    ;

// ==========================================
// INSPECT (§14.9.21 — COBOL-85)
// ==========================================

inspectStatement
    : INSPECT identifier
      ( inspectTallyingPhrase inspectReplacingPhrase?
      | inspectReplacingPhrase
      | inspectConvertingPhrase )
    ;

// ----- TALLYING -----

inspectTallyingPhrase
    : TALLYING inspectTallyingItem+
    ;

inspectTallyingItem
    : identifier inspectForClause+
    ;

inspectForClause
    : FOR inspectCountPhrase
    ;

inspectCountPhrase
    : CHARACTERS inspectDelimiters?
    | ALL inspectChar inspectDelimiters?
    | LEADING inspectChar inspectDelimiters?
    | FIRST inspectChar inspectDelimiters?
    | TRAILING inspectChar inspectDelimiters?
    ;

inspectChar
    : identifier
    | literal
    | figurativeConstant
    ;

// ----- REPLACING -----

inspectReplacingPhrase
    : REPLACING inspectReplacingItem+
    ;

inspectReplacingItem
    : CHARACTERS BY inspectChar inspectDelimiters?
    | ALL inspectChar BY inspectChar inspectDelimiters?
    | LEADING inspectChar BY inspectChar inspectDelimiters?
    | FIRST inspectChar BY inspectChar inspectDelimiters?
    | TRAILING inspectChar BY inspectChar inspectDelimiters?
    ;

// ----- CONVERTING -----

inspectConvertingPhrase
    : CONVERTING inspectChar
      TO inspectChar
      inspectBeforeAfterPhrase*
    ;

inspectBeforeAfterPhrase
    : BEFORE INITIAL_? inspectChar
    | AFTER INITIAL_? inspectChar
    ;

inspectDelimiters
    : BEFORE INITIAL_? inspectChar (AFTER INITIAL_? inspectChar)?
    | AFTER INITIAL_? inspectChar (BEFORE INITIAL_? inspectChar)?
    ;
// ==========================================
// SEARCH (§14.9.37 — Linear Search)
// ==========================================

searchStatement
    : SEARCH identifier
      searchAtEndClause?
      searchWhenClause+
      END_SEARCH?
    ;

searchWhenClause
    : WHEN condition imperativeStatement*
    ;

searchAtEndClause
    : AT END imperativeStatement
      (NOT AT END imperativeStatement)?
    ;

// ==========================================
// SEARCH ALL (§14.9.37 — Binary Search)
// ==========================================

searchAllStatement
    : SEARCH ALL identifier
      searchAllKeyPhrase?
      searchAtEndClause?
      searchAllWhenClause
      END_SEARCH?
    ;

searchAllKeyPhrase
    : KEY IS identifier
    ;

searchAllWhenClause
    : WHEN condition imperativeStatement*
    ;
// (setStatement, sortStatement, startStatement, stopStatement are fully expanded above)

// ==========================================
// EXTENSION STUBS (overridden by import grammars)
// ==========================================
// These are defined here as stubs so CobolParserCore compiles standalone.
// CobolParserOO, CobolParserJsonXml override them with full implementations.

jsonStatement     : JSON (identifier | literal)+ ;
xmlStatement      : XML (identifier | literal)+ ;
invokeStatement   : INVOKE (identifier | literal)+ ;

// =========================
// Conditions (boolean)
// =========================

condition
    : logicalOrExpression
    ;

logicalOrExpression
    : logicalAndExpression ( 'OR' logicalAndExpression )*
    ;

logicalAndExpression
    : logicalNotExpression ( 'AND' logicalNotExpression )*
    ;

logicalNotExpression
    : relationalExpression
    ;

// =========================
// Relational
// =========================

relationalOperand
    : arithmeticExpression
    | nonNumericLiteral
    ;

relationalExpression
    : relationalOperand IS? NOT? className                         // class condition
    | relationalOperand ( relationalOperator relationalOperand )?  // existing relational + bare operand
    ;

className
    : NUMERIC
    | ALPHABETIC
    | ALPHABETIC_LOWER
    | ALPHABETIC_UPPER
    ;

relationalOperator
    // Symbolic
    : EQUALS
    | NOTEQUAL
    | LTEQUAL
    | GTEQUAL
    | LT
    | GT
    // Word forms with optional IS and optional THAN
    | IS? EQUAL (TO | THAN)?
    | IS? NOT EQUAL (TO | THAN)?
    | IS? GREATER THAN?
    | IS? NOT GREATER THAN?
    | IS? LESS THAN?
    | IS? NOT LESS THAN?
    ;

// =========================
// Arithmetic
// =========================

arithmeticExpression
    : additiveExpression
    ;

additiveExpression
    : multiplicativeExpression ( addOp multiplicativeExpression )*
    ;

addOp
    : '+'
    | '-'
    ;

multiplicativeExpression
    : powerExpression ( mulOp powerExpression )*
    ;

mulOp
    : '*'
    | '/'
    ;

powerExpression
    : unaryExpression ( '**' unaryExpression )?
    ;

unaryExpression
    : addOp unaryExpression          // unary + or -
    | primaryExpression
    ;

// =========================
// Primaries
// =========================

primaryExpression
    : numericLiteral
    | functionCall
    | identifier
    | LPAREN arithmeticExpression RPAREN
    ;

// FUNCTION calls (ISO 2002+)
functionCall
    : {is2002()}? FUNCTION identifier (LPAREN argumentList? RPAREN)?
    ;

// =========================
// Literals
// =========================

literal
    : numericLiteral
    | nonNumericLiteral
    ;

numericLiteral
    : signedNumericLiteral
    ;

nonNumericLiteral
    : STRINGLIT
    | HEXLIT
    | figurativeConstant
    ;

signedNumericLiteral
    : (PLUS | MINUS)? numericLiteralCore
    ;

// Numeric literal assembly.
// DOT-based decimals use DECIMALLIT from the lexer (maximal munch resolves
// DOT-as-decimal vs DOT-as-sentence-terminator unambiguously).
// COMMA-based decimals for DECIMAL-POINT IS COMMA are assembled here in the parser.
numericLiteralCore
    : DECIMALLIT                           // 123.45 or .45 (dot decimal from lexer)
    | INTEGERLIT COMMA INTEGERLIT          // 123,45 (comma decimal — DECIMAL-POINT IS COMMA)
    | COMMA INTEGERLIT                     // ,45 (leading comma decimal)
    | INTEGERLIT                           // 123 (integer)
    ;

figurativeConstant
    : ZERO
    | SPACE
    | HIGH_VALUE
    | LOW_VALUE
    | QUOTE_
    | ALL STRINGLIT
    | ALL HEXLIT
    | ALL ZERO
    | ALL SPACE
    | ALL HIGH_VALUE
    | ALL LOW_VALUE
    | ALL QUOTE_
    ;
