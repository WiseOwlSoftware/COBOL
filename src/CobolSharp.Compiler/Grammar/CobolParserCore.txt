parser grammar CobolParserCore;

options {
    tokenVocab = CobolLexer;
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
    : IDENTIFIER (IDENTIFIER | literal)*
      DOT?
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
    : SEQUENTIAL
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
    | genericFileDescriptionClause
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
    : levelNumber dataName? parameterDescriptionBody DOT
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
    | conditionEntry88
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
    : TYPE IS? IDENTIFIER
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
    : USAGE IS? usageKeyword
    ;

usageKeyword
    : DISPLAY
    | COMP
    | COMP_1
    | COMP_2
    | COMP_3
    | BINARY
    | PACKED_DECIMAL
    | IDENTIFIER        // for 2023 types, OO types, generics
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
valueClause
    : VALUE IS? literal
    | VALUES IS? literal (literal)*
    ;

// SIGN Clause
signClause
    : SIGN IS? (LEADING | TRAILING) (SEPARATE CHARACTER)?
    ;

// JUSTIFIED / SYNCHRONIZED
justifiedClause
    : JUSTIFIED | JUST RIGHT
    ;

syncClause
    : SYNCHRONIZED | SYNC
    ;

// BLANK WHEN ZERO
blankWhenZeroClause
    : BLANK WHEN ZERO
    ;

// 88-LEVEL CONDITION ENTRIES
conditionEntry88
    : INTEGERLIT conditionName valueSet
    ;

conditionName
    : IDENTIFIER
    ;

valueSet
    : valueRange (COMMA valueRange)*
    ;

valueRange
    : literal (THRU literal)?
    ;

// ==========================================
// PROCEDURE DIVISION
// ==========================================

procedureDivision
    : PROCEDURE DIVISION usingClause? returningClause? DOT
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
    : IDENTIFIER
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
    : paragraphName DOT statement*
    ;

// ==========================================
// MAIN PROCEDURE BODY
// ==========================================

procedureSectionOrParagraph
    : sectionDeclaration
    | paragraphDeclaration
    ;

sectionDeclaration
    : sectionName SECTION DOT paragraphDeclaration*
    ;

sectionName
    : IDENTIFIER
    ;

paragraphDeclaration
    : paragraphName DOT statement*
    ;

paragraphName
    : IDENTIFIER
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
    | deleteFileStatement
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
    | jsonStatement
    | xmlStatement
    | invokeStatement
    | inlineMethodInvocationStatement
    | continueStatement
    | nextSentenceStatement
    ;

// Safety net for vendor extensions — disabled to prevent exponential backtracking
// genericStatement
//     : IDENTIFIER (IDENTIFIER | literal)* DOT?
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
      DOT?
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
      DOT?
    ;

writeFrom
    : FROM identifier
    ;

writeBeforeAfter
    : ('BEFORE' | 'AFTER') 'ADVANCING' (identifier | integerLiteral | literal)
    ;

writeInvalidKey
    : INVALID KEY imperativeStatement
      (NOT INVALID KEY imperativeStatement)?
    ;

// ==========================================
// OPEN / CLOSE (§14.9.25, §14.9.7)
// ==========================================

openStatement
    : OPEN openClause+ DOT?
    ;

openClause
    : openMode identifier+
    ;

openMode
    : INPUT
    | OUTPUT
    | EXTEND
    ;

closeStatement
    : CLOSE identifierList DOT?
    ;

// ==========================================
// IF / END-IF (§14.9.19)
// ==========================================

ifStatement
    : IF condition
      imperativeStatement*
      (ELSE imperativeStatement*)?
      END_IF?
      DOT?
    ;

// ==========================================
// PERFORM / END-PERFORM (§14.9.28)
// ==========================================

performStatement
    // Out-of-line: PERFORM PARA [THRU PARA] [options] [END-PERFORM] .
    : PERFORM performTarget performOptions* END_PERFORM? DOT?
    // Inline: PERFORM [options]+ statements END-PERFORM .
    | PERFORM performOptions+ imperativeStatement* END_PERFORM DOT?
    ;

performTarget
    : procedureName ((THRU | THROUGH) procedureName)?
    ;

procedureName
    : IDENTIFIER
    ;

performOptions
    : performTimes
    | performUntil
    | performVarying
    ;

performTimes
    : integerLiteral TIMES
    ;

performUntil
    : UNTIL condition
    ;

performVarying
    : VARYING identifier FROM arithmeticExpression
      BY arithmeticExpression
      UNTIL condition
    ;

// ==========================================
// EVALUATE / END-EVALUATE (§14.9.13)
// ==========================================

evaluateStatement
    : EVALUATE evaluateSubject+
      evaluateWhenClause+
      END_EVALUATE?
    ;

evaluateSubject
    : arithmeticExpression
    | condition
    ;

evaluateWhenClause
    : WHEN evaluateObject+ imperativeStatement*
    ;

evaluateObject
    : arithmeticExpression
    | condition
    | OTHER
    ;

// ==========================================
// COMPUTE (§14.9.8)
// ==========================================

computeStatement
    : COMPUTE identifier EQUALS arithmeticExpression END_COMPUTE? DOT?
    ;

// ==========================================
// CONTINUE / NEXT SENTENCE (§14.9.9, §14.9.19)
// ==========================================

continueStatement
    : CONTINUE DOT?
    ;

nextSentenceStatement
    : NEXT_SENTENCE DOT?
    ;

// ==========================================
// INLINE METHOD INVOCATION (COBOL 2023)
// ==========================================

inlineMethodInvocationStatement
    : identifier LPAREN argumentList? RPAREN DOT?
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
// ADD (§14.9.1)
// ==========================================

addStatement
    : ADD addOperandList addToPhrase? addGivingPhrase? addOnSizeError? END_ADD? DOT?
    ;

addOperandList
    : arithmeticExpression (COMMA arithmeticExpression)*
    ;

addToPhrase
    : TO identifierList
    ;

addGivingPhrase
    : GIVING identifierList
    ;

addOnSizeError
    : ON SIZE ERROR imperativeStatement
      (NOT ON SIZE ERROR imperativeStatement)?
    ;

// ==========================================
// SUBTRACT (§14.9.42)
// ==========================================

subtractStatement
    : SUBTRACT subtractOperandList subtractFromPhrase? subtractGivingPhrase? subtractOnSizeError? END_SUBTRACT? DOT?
    ;

subtractOperandList
    : arithmeticExpression (COMMA arithmeticExpression)*
    ;

subtractFromPhrase
    : FROM identifierList
    ;

subtractGivingPhrase
    : GIVING identifierList
    ;

subtractOnSizeError
    : ON SIZE ERROR imperativeStatement
      (NOT ON SIZE ERROR imperativeStatement)?
    ;

// ==========================================
// MULTIPLY (§14.9.23)
// ==========================================

multiplyStatement
    : MULTIPLY arithmeticExpression BY identifierList multiplyGivingPhrase? multiplyOnSizeError? END_MULTIPLY? DOT?
    ;

multiplyGivingPhrase
    : GIVING identifier
    ;

multiplyOnSizeError
    : ON SIZE ERROR imperativeStatement
      (NOT ON SIZE ERROR imperativeStatement)?
    ;

// ==========================================
// DIVIDE (§14.9.12)
// ==========================================

divideStatement
    : DIVIDE arithmeticExpression divideIntoPhrase divideGivingPhrase? divideRemainderPhrase? divideOnSizeError? END_DIVIDE? DOT?
    ;

divideIntoPhrase
    : INTO identifierList
    ;

divideGivingPhrase
    : GIVING identifier
    ;

divideRemainderPhrase
    : REMAINDER identifier
    ;

divideOnSizeError
    : ON SIZE ERROR imperativeStatement
      (NOT ON SIZE ERROR imperativeStatement)?
    ;

// ==========================================
// MOVE (§14.9.24)
// ==========================================

moveStatement
    : MOVE moveSource moveTarget DOT?
    ;

moveSource
    : arithmeticExpression
    | literal
    ;

moveTarget
    : TO identifierList
    | CORRESPONDING identifier TO identifier
    ;

// ==========================================
// STRING (§14.9.41)
// ==========================================

stringStatement
    : STRING stringSendingPhrase+ stringIntoPhrase stringWithPointer? stringOnOverflow? END_STRING? DOT?
    ;

stringSendingPhrase
    : (identifier | literal)
      (DELIMITED BY (ALL)? (identifier | literal | SIZE))?
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
      DOT?
    ;

unstringDelimiterPhrase
    : DELIMITED BY (ALL)? (identifier | literal)
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
      DOT?
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
    : BY VALUE arithmeticExpression
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
    : CANCEL identifierList DOT?
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
    : SET identifier TO arithmeticExpression DOT?
    ;

// SET identifier TO TRUE/FALSE
setBooleanStatement
    : SET identifier TO (TRUE_ | FALSE_) DOT?
    ;

// SET ADDRESS OF identifier TO identifier
setAddressStatement
    : SET ADDRESS OF identifier TO identifier DOT?
    ;

// SET object-reference TO class/object reference (OO)
setObjectReferenceStatement
    : SET identifier TO objectReference DOT?
    ;

objectReference
    : identifier
    | NULL_
    | SELF
    | SUPER
    ;

// SET index UP/DOWN BY integer
setIndexStatement
    : SET identifier ( UP | DOWN ) BY integerLiteral DOT?
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
      DOT?
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
      DOT?
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
    : RETURN fileName
      (INTO identifier)?
      returnAtEndPhrase?
      END_RETURN?
      DOT?
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
      DOT?
    ;

// ==========================================
// REWRITE (§14.9.36)
// ==========================================

rewriteStatement
    : REWRITE recordName
      (FROM identifier)?
      rewriteInvalidKeyPhrase?
      END_REWRITE?
      DOT?
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
      DOT?
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
      DOT?
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
    : STOP (RUN | literal | identifier)? DOT?
    ;

// ==========================================
// GOBACK (§14.9.16)
// ==========================================

gobackStatement
    : GOBACK DOT?
    ;

// ==========================================
// EXIT (§14.9.14)
// ==========================================

exitStatement
    : EXIT ( PROGRAM | PERFORM | SECTION | PARAGRAPH | METHOD | FUNCTION )? DOT?
    ;

// ==========================================
// START (§14.9.38)
// ==========================================

startStatement
    : START fileName
      startKeyPhrase?
      startInvalidKeyPhrase?
      END_START?
      DOT?
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
    : GO TO? identifier DOT?
    ;

// ==========================================
// ACCEPT (§14.9.0)
// ==========================================

acceptStatement
    : ACCEPT identifier (FROM identifier)? DOT?
    ;

// ==========================================
// DISPLAY (§14.9.11)
// ==========================================

displayStatement
    : DISPLAY (identifier | literal)+ DOT?
    ;

// ==========================================
// INITIALIZE (§14.9.20)
// ==========================================

initializeStatement
    : INITIALIZE identifierList DOT?
    ;

// ==========================================
// INSPECT (§14.9.21 — LL(1)-safe, keyword-discriminated)
// ==========================================

inspectStatement
    : INSPECT identifier
      inspectTallyingPhrase?
      inspectReplacingPhrase?
      inspectConvertingPhrase?
      DOT?
    ;

inspectTallyingPhrase
    : TALLYING inspectTallyingItem+
    ;

inspectTallyingItem
    : identifier FOR inspectForPhrase+
    ;

inspectForPhrase
    : ALL literal
    | LEADING literal
    | FIRST literal
    | BEFORE INITIAL_? literal
    ;

inspectReplacingPhrase
    : REPLACING inspectReplacingItem+
    ;

inspectReplacingItem
    : ALL literal BY (identifier | literal)
    | LEADING literal BY (identifier | literal)
    | FIRST literal BY (identifier | literal)
    | BEFORE INITIAL_? literal BY (identifier | literal)
    ;

inspectConvertingPhrase
    : CONVERTING identifier
      FROM (identifier | literal)
      TO   (identifier | literal)
    ;
// ==========================================
// SEARCH (§14.9.37 — Linear Search)
// ==========================================

searchStatement
    : SEARCH identifier
      searchAtEndClause?
      searchWhenClause+
      END_SEARCH?
      DOT?
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
      searchAllAtEndClause?
      searchAllWhenClause+
      END_SEARCH?
      DOT?
    ;

searchAllWhenClause
    : WHEN relationalExpression
    ;

searchAllAtEndClause
    : AT END imperativeStatement
      (NOT AT END imperativeStatement)?
    ;
// (setStatement, sortStatement, startStatement, stopStatement are fully expanded above)

// ==========================================
// EXTENSION STUBS (overridden by import grammars)
// ==========================================
// These are defined here as stubs so CobolParserCore compiles standalone.
// CobolParserOO, CobolParserJsonXml override them with full implementations.

jsonStatement     : JSON (identifier | literal)+ DOT? ;
xmlStatement      : XML (identifier | literal)+ DOT? ;
invokeStatement   : INVOKE (identifier | literal)+ DOT? ;

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
    : 'NOT' logicalNotExpression
    | relationalExpression
    ;

// =========================
// Relational
// =========================

relationalExpression
    : arithmeticExpression ( relationalOperator arithmeticExpression )?
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
    : literal
    | functionCall
    | identifier
    | LPAREN arithmeticExpression RPAREN
    ;

// FUNCTION calls (ISO 2002+)
functionCall
    : FUNCTION identifier (LPAREN argumentList? RPAREN)?
    ;

// =========================
// Literals
// =========================

literal
    : signedNumericLiteral
    | STRINGLIT
    | HEXLIT
    | figurativeConstant
    ;

signedNumericLiteral
    : (PLUS | MINUS)? (INTEGERLIT | DECIMALLIT)
    ;

figurativeConstant
    : ZERO
    | SPACE
    | HIGH_VALUE
    | LOW_VALUE
    | QUOTE_
    | ALL STRINGLIT
    | ALL HEXLIT
    ;
