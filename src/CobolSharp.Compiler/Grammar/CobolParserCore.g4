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
    : OBJECT_COMPUTER DOT computerName computerAttributes? DOT
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
