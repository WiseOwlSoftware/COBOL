// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

parser grammar CobolParserCore;

options {
    tokenVocab = CobolLexer;
    superClass = CobolParserCoreBase;
}

import CobolExpressions, CobolData, CobolSpecialNames, CobolReportWriter, CobolIO, CobolControlFlow, CobolExtensionsJsonXml;

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
    : END PROGRAM programName DOT
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
    : OBJECT_COMPUTER DOT computerName computerAttributes?
      programCollatingSequenceClause? DOT
    ;

programCollatingSequenceClause
    : PROGRAM COLLATING? SEQUENCE IS? IDENTIFIER
    ;

computerName
    : IDENTIFIER
    ;

computerAttributes
    : (IDENTIFIER | STRINGLIT | INTEGERLIT)+
    ;

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
    | SIGNED_INTEGERLIT
    | SUB_PLUS
    | SUB_MINUS
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
    : IDENTIFIER
    ;

// ==========================================
// DECLARATIVES
// ==========================================

declarativePart
    : DECLARATIVES DOT declarativeSection+ END DECLARATIVES DOT
    ;

declarativeSection
    : sectionName SECTION DOT sentence* declarativeParagraph*
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

procedureName
    : (IDENTIFIER | INTEGERLIT)
      ((OF | IN) (IDENTIFIER | INTEGERLIT))?
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
// MOVE (§14.9.24)
// ==========================================

moveStatement
    : MOVE CORRESPONDING dataReference TO dataReference
    | MOVE moveSendingOperand moveReceivingPhrase
    ;

moveSendingOperand
    : literal
    | functionCall
    | dataReference
    ;

moveReceivingPhrase
    : TO dataReferenceList
    | CORRESPONDING dataReference TO dataReference
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
    ;

// ==========================================
// DISPLAY (§14.9.11)
// ==========================================

displayStatement
    : DISPLAY (dataReference | literal)+
    ;

// ==========================================
// GOBACK (§14.9.16)
// ==========================================

gobackStatement
    : GOBACK
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
