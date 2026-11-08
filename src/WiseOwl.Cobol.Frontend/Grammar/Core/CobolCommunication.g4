// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// The communication facility (ISO/IEC 1989:2002 Annex A; COBOL-85's communication module; deleted at 2014): the
// communication section, its communication description entries and the statements over them. The design is
// docs/rearchitecture/DESIGN-communication-facility.md (kb/Work PB4677). Each general format below was measured on the
// printed page of the 2002 text (folios 666-667, 677-679, 680, 681, 683): SYMBOLIC, IS, FOR, the MESSAGE of MESSAGE COUNT,
// TIMES, BY, WITH, ADVANCING and the LINE of REPLACING LINE carry no underline, so each is an optional word (§2023:5.2.3).
// Every rule parses at every edition (the union grammar); VersionConformancePass gates the facility on recognition.
// Imported by CobolParserCore.g4 — no options block beyond the vocabulary.

parser grammar CobolCommunication;

options {
    tokenVocab = CobolLexer;
}

// A.1.1: `COMMUNICATION SECTION. [ { communication-description-entry [ { constant-entry | record-description-entry } ... ] } ... ]`.
// A constant entry and a record description entry ride dataDescriptionEntry, as they do in every other section.
communicationSection
    : communicationKeyword SECTION DOT communicationDescriptionEntry*
    ;

// A.1.2.1, the three formats as one rule. The written INPUT, OUTPUT or I-O decides the format, and which clause belongs to
// which format is a table (CommunicationFormats), so a clause of another format is refused by name rather than as a token
// error. The series of data-names is A.1.2.1's second option of Formats 1 and 3, its count asked by the binder.
communicationDescriptionEntry
    : cdKeyword cdName FOR? cdMode (cdClause+ | cdSeriesName+)? DOT dataDescriptionEntry*
    ;

// cd-name-1 is a user-defined word (ISO/IEC 1989:2002 8.3.1.1.1, A.1.2.1), so it does not ride dataName, whose FILLER is a reserved
// word: the fileControlClauseGroup shape, whose reservedGatedWord and the funnel's position for cobolWord
// (IsProvableUserWordPosition) make a word §2023:8.9 reserves draw COBOL0901. The actions record the name for keywordContinuesHere, and for
// communicationNameFollows: a SEND of the COBOL-85 format 1 and of the 2023 format are the same tokens.
cdName
    : (cobolWord | reservedGatedWord) { declareName(TokenStream.LT(-1)); declareCommunicationName(TokenStream.LT(-1)); }
    ;

cdMode
    : INITIAL_? (INPUT | I_O)
    | OUTPUT
    ;

cdClause
    : SYMBOLIC? QUEUE IS? dataName
    | SYMBOLIC? SUB_QUEUE_1 IS? dataName
    | SYMBOLIC? SUB_QUEUE_2 IS? dataName
    | SYMBOLIC? SUB_QUEUE_3 IS? dataName
    | messageKeyword DATE IS? dataName
    | messageKeyword TIME IS? dataName
    | SYMBOLIC? SOURCE IS? dataName
    | SYMBOLIC? TERMINAL IS? dataName
    | TEXT LENGTH IS? dataName
    | END KEY IS? dataName
    | STATUS KEY IS? dataName
    | messageKeyword? COUNT IS? dataName
    | DESTINATION COUNT IS? dataName
    | DESTINATION TABLE OCCURS integerOperand TIMES? (INDEXED BY? indexNameList)?
    | ERROR KEY IS? dataName
    | SYMBOLIC? DESTINATION IS? dataName
    ;

cdSeriesName
    : dataName
    ;

// A.2.1.1: `ACCEPT cd-name-1 MESSAGE COUNT`. No other ACCEPT writes COUNT or MESSAGE after its operand.
acceptMessageCountStatement
    : ACCEPT cobolWord messageKeyword? COUNT
    ;

// A.2.2.1 and A.2.3.1, with the COBOL-85 KEY phrase (deleted by 2002: communication-key-phrase-removed-2002).
enableStatement
    : ENABLE communicationPath cobolWord communicationKeyPhrase?
    ;

disableStatement
    : DISABLE communicationPath cobolWord communicationKeyPhrase?
    ;

communicationPath
    : INPUT TERMINAL?
    | I_O TERMINAL
    | OUTPUT
    ;

communicationKeyPhrase
    : WITH? KEY (literal | dataReference)
    ;

// A.2.4.1: `PURGE cd-name-1`.
purgeStatement
    : PURGE cobolWord
    ;

// A.2.5.1. The word after the cd-name parts it from the 2023 RECEIVE, whose operand GIVING follows. WITH is optional in
// WITH DATA and DATA is not; ConditionalStatements reads the two statementBlocks as the statement's conditional phrases.
communicationReceiveStatement
    : RECEIVE cobolWord (messageKeyword | SEGMENT) INTO receivingIdentifier
      receiveNoDataPhrase? receiveWithDataPhrase? END_RECEIVE?
    ;

receiveNoDataPhrase
    : NO DATA statementBlock
    ;

receiveWithDataPhrase
    : WITH? DATA statementBlock
    ;

// A.2.6.1 Formats 1 and 2. `SEND x FROM y` is also the 2023 format's shape, and a parse alternative cannot read which x is:
// the statement arm's predicate asks communicationNameFollows, and a SEND that writes an indicator can only be this rule.
communicationSendStatement
    : SEND cobolWord FROM sendText
    | SEND cobolWord (FROM sendText)? sendIndicator sendAdvancingPhrase? sendReplacingPhrase?
    ;

// SR2 and SR7: identifier-1 is an identifier, and a function-identifier is one.
sendText
    : functionCall
    | dataReference
    ;

sendIndicator
    : WITH? (ESI | EMI | EGI | dataReference)
    ;

sendAdvancingPhrase
    : (BEFORE | AFTER) ADVANCING?
      ( PAGE
      | (dataReference | integerLiteral | literal) (LINE | LINES)?   // literal: CCVS-85 CM102M writes BEFORE ZERO LINES, the figurative ZERO "may be used whenever 'literal' appears"
      )
    ;

sendReplacingPhrase
    : REPLACING LINE?
    ;
