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
    | vendorConfigurationParagraph
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
    | implementorSwitchEntry DOT?
    | IDENTIFIER (IDENTIFIER | literal)* DOT?
    ;

implementorSwitchEntry
    : IDENTIFIER IS IDENTIFIER
      (ON IDENTIFIER)?
      (OFF IS? IDENTIFIER)?
    ;

currencySignClause
    : CURRENCY SIGN IS? literal
    ;

decimalPointClause
    : DECIMAL_POINT IS IDENTIFIER    // DECIMAL-POINT IS COMMA (COMMA is IDENTIFIER)
    ;

// fallback for vendor extensions
vendorConfigurationParagraph
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
    : FILE_CONTROL DOT fileControlClauseGroup+
    ;

fileControlClauseGroup
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
    | vendorFileControlClause
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
    : 'RECORD' KEY IS dataReference
    ;

alternateKeyClause
    : ALTERNATE KEY IS dataReference
      (WITH? DUPLICATES)?
    ;

fileStatusClause
    : FILE STATUS IS dataReference
    ;

vendorFileControlClause
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
    : USING (BY_REFERENCE | BY_VALUE | BY_CONTENT)? dataReference
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
    | PROCEDURE    // NC205A: PROCEDURE used as a data name (77 PROCEDURE-DIVISION PIC X)
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
      (DEPENDING ON? dataReference)?
      occursKeyClause*
      (INDEXED BY? dataReferenceList)?
    ;

occursKeyClause
    : (ASCENDING | DESCENDING) KEY? IS? dataReference+
    ;

timesKeyword
    : TIMES
    ;

integerLiteral
    : INTEGERLIT
    ;

// REDEFINES Clause
redefinesClause
    : REDEFINES dataReference
    ;

// RENAMES (Level 66)
renamesClause
    : RENAMES dataReference (THRU dataReference)?
    ;

// VALUE Clause — IS is optional noise word
// For level-88 condition entries, valueItem supports THRU ranges.
valueClause
    : (VALUE | VALUES) (IS | ARE)? valueItem (COMMA? valueItem)*
    ;

valueItem
    : literal ((THRU | THROUGH) literal)?
    | literal+
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
    : IDENTIFIER dataReferenceSuffix*
    ;

dataReferenceSuffix
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

procedureUnit
    : sectionDefinition
    | paragraphDefinition
    ;

sectionDefinition
    : sectionName SECTION DOT paragraphDefinition*
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
statementBlock
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
    : INTO dataReference
    ;

readKey
    : KEY IS dataReference
    ;

readAtEnd
    : AT END statementBlock
      (NOT AT END statementBlock)?
    ;

readInvalidKey
    : INVALID KEY statementBlock
      (NOT INVALID KEY statementBlock)?
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
    : FROM dataReference
    ;

writeBeforeAfter
    : ('BEFORE' | 'AFTER') ADVANCING (dataReference | integerLiteral | literal) (LINE | LINES)?
    ;

writeInvalidKey
    : INVALID KEY statementBlock
      (NOT INVALID KEY statementBlock)?
    ;

// ==========================================
// OPEN / CLOSE (§14.9.25, §14.9.7)
// ==========================================

openStatement
    : OPEN openClause+
    ;

openClause
    : openMode dataReference+
    ;

openMode
    : INPUT
    | OUTPUT
    | I_O
    | EXTEND
    ;

closeStatement
    : CLOSE dataReferenceList
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
    | PERFORM performOptions+ statementBlock* END_PERFORM                 // PERFORM UNTIL/VARYING ... END-PERFORM
    | PERFORM statementBlock+ END_PERFORM                                 // PERFORM ... END-PERFORM (block)
    ;

performTarget
    : procedureName ((THRU | THROUGH) procedureName)?
    ;

procedureName
    : (IDENTIFIER | INTEGERLIT)
      ((OF | IN) (IDENTIFIER | INTEGERLIT))?
    ;

performOptions
    : performTimes
    | performUntil
    | performVarying
    ;

performTimes
    : (integerLiteral | dataReference) TIMES
    ;

performUntil
    : (WITH? TEST (BEFORE | AFTER))? UNTIL condition
    ;

performVarying
    : (WITH? TEST (BEFORE | AFTER))?
      VARYING dataReference FROM arithmeticExpression
      BY arithmeticExpression
      UNTIL condition
      performVaryingAfter*
    ;

performVaryingAfter
    : AFTER dataReference FROM arithmeticExpression
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
    : TRUE_                                              // EVALUATE TRUE
    | FALSE_                                             // EVALUATE FALSE
    | arithmeticExpression (IS? NOT? classCondition)?     // EVALUATE X [NUMERIC]
    ;

classCondition
    : NUMERIC
    | ALPHABETIC
    | ALPHABETIC_LOWER
    | ALPHABETIC_UPPER
    | ALPHANUMERIC
    ;

evaluateWhenClause
    : WHEN evaluateWhenGroup (ALSO evaluateWhenGroup)* statementBlock*
    | WHEN OTHER statementBlock*
    ;

evaluateWhenGroup
    : NOT? evaluateWhenItem+
    ;

evaluateWhenItem
    : arithmeticExpression (THRU | THROUGH) arithmeticExpression   // range
    | arithmeticExpression                                          // single value
    | condition                                                     // for EVALUATE TRUE / class tests
    | ANY                                                           // match anything
    ;

// ==========================================
// COMPUTE (§14.9.8)
// ==========================================

computeStatement
    : COMPUTE computeStore+ EQUALS arithmeticExpression computeOnSizeError? END_COMPUTE?
    ;

computeStore
    : dataReference ROUNDED?
    ;

computeOnSizeError
    : ON SIZE ERROR statementBlock
      (NOT ON SIZE ERROR statementBlock)?
    | NOT ON SIZE ERROR statementBlock
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
    : dataReference LPAREN argumentList? RPAREN
    ;

argumentList
    : argument (COMMA argument)*
    ;

argument
    : arithmeticExpression
    | literal
    | dataReference
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
    : dataReference ROUNDED?
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
    : ADD CORRESPONDING dataReference TO dataReference ROUNDED? arithmeticOnSizeError? END_ADD?
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
    : SUBTRACT CORRESPONDING dataReference FROM dataReference ROUNDED? arithmeticOnSizeError? END_SUBTRACT?
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
    : receivingOperand ROUNDED?
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
// MOVE (§14.9.24)
// ==========================================

moveStatement
    : MOVE CORRESPONDING dataReference TO dataReference
    | MOVE moveSendingOperand moveReceivingPhrase
    ;

moveSendingOperand
    : literal
    | dataReference
    ;

moveReceivingPhrase
    : TO dataReferenceList
    | CORRESPONDING dataReference TO dataReference
    ;

// ==========================================
// STRING (§14.9.41)
// ==========================================

stringStatement
    : STRING stringSendingPhrase+ stringIntoPhrase stringWithPointer? stringOnOverflow? END_STRING?
    ;

stringSendingPhrase
    : (dataReference | literal | figurativeConstant)
      delimitedByPhrase?
    ;

delimitedByPhrase
    : DELIMITED BY (ALL)? (dataReference | literal | figurativeConstant | SIZE)
    ;

stringIntoPhrase
    : INTO dataReference
    ;

stringWithPointer
    : WITH POINTER dataReference
    ;

stringOnOverflow
    : ON OVERFLOW statementBlock
      (NOT ON OVERFLOW statementBlock)?
    ;

// ==========================================
// UNSTRING (§14.9.44)
// ==========================================

unstringStatement
    : UNSTRING dataReference
      unstringDelimiterPhrase?
      unstringIntoPhrase+
      unstringWithPointer?
      unstringTallying?
      unstringOnOverflow?
      END_UNSTRING?
     
    ;

unstringDelimiterPhrase
    : DELIMITED BY (ALL)? (dataReference | literal | figurativeConstant)
    ;

unstringIntoPhrase
    : INTO dataReference
      (DELIMITER IN dataReference)?
      (COUNT IN dataReference)?
    ;

unstringWithPointer
    : WITH POINTER dataReference
    ;

unstringTallying
    : TALLYING IN dataReference
    ;

unstringOnOverflow
    : ON OVERFLOW statementBlock
      (NOT ON OVERFLOW statementBlock)?
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
    | dataReference
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
    : BY 'REFERENCE'? dataReference
    ;

callByValue
    : {is2002()}? BY VALUE arithmeticExpression
    ;

callByContent
    : BY 'CONTENT' (dataReference | literal)
    ;

callReturningPhrase
    : RETURNING dataReference
    ;

callOnExceptionPhrase
    : ON EXCEPTION statementBlock
      (NOT ON EXCEPTION statementBlock)?
    ;

// ==========================================
// CANCEL (§14.9.5)
// ==========================================

cancelStatement
    : CANCEL dataReferenceList
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

// SET dataReference+ TO arithmeticExpression (COBOL-85 §14.9.39 Format 1)
setToValueStatement
    : SET dataReference+ TO arithmeticExpression
    ;

// SET dataReference+ TO TRUE/FALSE (COBOL-85 §14.9.39 Format 5)
setBooleanStatement
    : SET dataReference+ TO (TRUE_ | FALSE_)
    ;

// SET ADDRESS OF dataReference TO dataReference
setAddressStatement
    : SET ADDRESS OF dataReference TO dataReference
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
    : dataReference
    ;

sortKeyPhrase
    : (ASCENDING | DESCENDING) KEY dataReferenceList
    ;

sortUsingPhrase
    : USING dataReferenceList
    ;

sortGivingPhrase
    : GIVING dataReferenceList
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
    : dataReference
    ;

mergeKeyPhrase
    : (ASCENDING | DESCENDING) KEY dataReferenceList
    ;

mergeUsingPhrase
    : USING dataReferenceList
    ;

mergeGivingPhrase
    : GIVING dataReferenceList
    ;

mergeOutputProcedurePhrase
    : OUTPUT PROCEDURE IS procedureName
    ;

// ==========================================
// RETURN (§14.9.34)
// ==========================================

returnStatement
    : RETURN fileName RECORD
      (INTO dataReference)?
      returnAtEndPhrase?
      END_RETURN?
     
    ;

returnAtEndPhrase
    : AT END statementBlock
      (NOT AT END statementBlock)?
    ;

// ==========================================
// RELEASE (§14.9.33)
// ==========================================

releaseStatement
    : RELEASE dataReference
      (FROM dataReference)?
     
    ;

// ==========================================
// REWRITE (§14.9.36)
// ==========================================

rewriteStatement
    : REWRITE recordName
      (FROM dataReference)?
      rewriteInvalidKeyPhrase?
      END_REWRITE?
     
    ;

recordName
    : dataReference
    ;

rewriteInvalidKeyPhrase
    : INVALID KEY statementBlock
      (NOT INVALID KEY statementBlock)?
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
    : ON EXCEPTION statementBlock
      (NOT ON EXCEPTION statementBlock)?
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
    : INVALID KEY statementBlock
      (NOT INVALID KEY statementBlock)?
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
    : KEY IS comparisonExpression
    ;

startInvalidKeyPhrase
    : INVALID KEY statementBlock
      (NOT INVALID KEY statementBlock)?
    ;

// ==========================================
// GO TO (§14.9.17)
// ==========================================

goToStatement
    : GO TO? procedureName (procedureName)* (DEPENDING ON? dataReference)?
    ;

// ==========================================
// ACCEPT (§14.9.0)
// ==========================================

acceptStatement
    : ACCEPT dataReference (FROM acceptSource)?
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
    : DISPLAY (dataReference | literal)+
    ;

// ==========================================
// INITIALIZE (§14.9.20)
// ==========================================

initializeStatement
    : INITIALIZE dataReferenceList initializeReplacingPhrase?
    ;

initializeReplacingPhrase
    : REPLACING initializeReplacingItem+
    ;

initializeReplacingItem
    : ALPHABETIC DATA? BY (dataReference | literal)
    | ALPHANUMERIC DATA? BY (dataReference | literal)
    | NUMERIC DATA? BY (dataReference | literal)
    | (ALPHANUMERIC EDITED | ALPHANUMERIC_EDITED) DATA? BY (dataReference | literal)
    | (NUMERIC EDITED | NUMERIC_EDITED) DATA? BY (dataReference | literal)
    ;

// ==========================================
// INSPECT (§14.9.21 — COBOL-85)
// ==========================================

inspectStatement
    : INSPECT dataReference
      ( inspectTallyingPhrase inspectReplacingPhrase?
      | inspectReplacingPhrase
      | inspectConvertingPhrase )
    ;

// ----- TALLYING -----

inspectTallyingPhrase
    : TALLYING inspectTallyingItem+
    ;

inspectTallyingItem
    : dataReference inspectForClause+
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
    : dataReference
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
    : SEARCH dataReference (VARYING dataReference)?
      searchAtEndClause?
      searchWhenClause+
      END_SEARCH?
    ;

searchWhenClause
    : WHEN condition statementBlock*
    ;

searchAtEndClause
    : AT END statementBlock
      (NOT AT END statementBlock)?
    | END statementBlock        // NIST / IBM extension: AT-less END
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
// (setStatement, sortStatement, startStatement, stopStatement are fully expanded above)

// ==========================================
// EXTENSION STUBS (overridden by import grammars)
// ==========================================
// These are defined here as stubs so CobolParserCore compiles standalone.
// CobolParserOO, CobolParserJsonXml override them with full implementations.

jsonStatement     : JSON (dataReference | literal)+ ;
xmlStatement      : XML (dataReference | literal)+ ;
invokeStatement   : INVOKE (dataReference | literal)+ ;

// =========================
// Conditions (boolean)
// =========================

condition
    : logicalOrExpression
    | TRUE_
    | FALSE_
    ;

logicalOrExpression
    : logicalAndExpression ( 'OR' logicalAndExpression )*
    ;

logicalAndExpression
    : unaryLogicalExpression ( 'AND' unaryLogicalExpression )*
    ;

unaryLogicalExpression
    : NOT unaryLogicalExpression
    | comparisonExpression
    ;

// =========================
// Relational
// =========================

comparisonOperand
    : arithmeticExpression
    | nonNumericLiteral
    ;

comparisonExpression
    : comparisonOperand IS? NOT? className                         // class condition
    | comparisonOperand ( comparisonOperator comparisonOperand )?  // existing relational + bare operand
    ;

className
    : NUMERIC
    | ALPHABETIC
    | ALPHABETIC_LOWER
    | ALPHABETIC_UPPER
    ;

comparisonOperator
    // Symbolic
    : EQUALS
    | NOTEQUAL
    | LTEQUAL
    | GTEQUAL
    | LT
    | GT
    // Abbreviated NOT + symbolic (COBOL-85 §6.3.4.2)
    | NOT EQUALS       // NOT =
    | NOT GT            // NOT >
    | NOT LT            // NOT <
    | NOT GTEQUAL       // NOT >=
    | NOT LTEQUAL       // NOT <=
    // Word forms with optional IS and optional THAN
    | IS? EQUAL (TO | THAN)?
    | IS? NOT EQUAL (TO | THAN)?
    | IS? GREATER THAN? OR EQUAL TO?
    | IS? NOT GREATER THAN? OR EQUAL TO?
    | IS? LESS THAN? OR EQUAL TO?
    | IS? NOT LESS THAN? OR EQUAL TO?
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
    | dataReference
    | LPAREN arithmeticExpression RPAREN
    ;

// FUNCTION calls (ISO 2002+)
functionCall
    : {is2002()}? FUNCTION dataReference (LPAREN argumentList? RPAREN)?
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
