// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// Object-Orientation grammar (COBOL-2002, ISO §11.2/§11.3/§11.7/§11.8 + §14.9.23 INVOKE). This is a version-factored
// fragment imported by CobolParserCore.g4 — OO rule BODIES live here; the minimal {is2002()}?-gated HOOK alternatives
// (classDefinition in compilationGroup, invokeStatement in statement, objectReferenceUsage in usageKeyword) are added
// to the core rules they extend. Keeps 2002 OO out of the COBOL-85 base (see memory feedback_grammar_version_factoring).
// Imported (no options block); shares CobolLexer's tokens and references core rules (environmentDivision, dataDivision,
// procedureDivision, cobolWord, objectReference, literal, dataReference, arithmeticExpression).

parser grammar CobolOO;

options { tokenVocab = CobolLexer; }

// ── Class definition (ISO §11.2 composition, §12739) ──
// A class compilation unit: CLASS-ID, an optional instance (OBJECT) definition holding instance data + methods, and
// END CLASS. (FACTORY, INHERITS, ENVIRONMENT/REPOSITORY are later slices.)
classDefinition
    : (IDENTIFICATION DIVISION DOT)? classIdParagraph
      environmentDivision?
      objectParagraph?
      endClassHeader
    ;

classIdParagraph
    : CLASS_ID DOT className (INHERITS FROM className)? DOT
    ;

className
    : cobolWord
    ;

// OBJECT (instance) definition — instance data (DATA DIVISION) + instance methods (in the PROCEDURE DIVISION).
objectParagraph
    : (IDENTIFICATION DIVISION DOT)? OBJECT DOT
      environmentDivision?
      dataDivision?
      (PROCEDURE DIVISION DOT methodDefinition*)?
      END OBJECT DOT
    ;

// METHOD-ID … END METHOD — a method definition (its own DATA/PROCEDURE divisions). ISO §11.7 / §12798.
methodDefinition
    : (IDENTIFICATION DIVISION DOT)? METHOD_ID DOT methodName DOT
      environmentDivision?
      dataDivision?
      procedureDivision?
      END METHOD methodName? DOT
    ;

methodName
    : cobolWord
    ;

endClassHeader
    : END CLASS className DOT
    ;

// ── INVOKE statement (ISO §14.9.23) ──
// INVOKE {class-name | object-ref} "method" [USING …] [RETURNING id]. objectReference (dataReference | NULL_ | SELF |
// SUPER) is the SET-pointer rule in CobolParserCore.g4; a class-name and an object reference are both data-references
// syntactically (the binder distinguishes them by resolved symbol kind).
invokeStatement
    : INVOKE invokeTarget invokeMethodName invokeUsing? invokeReturning? END_INVOKE?
    ;

invokeTarget
    : objectReference
    ;

invokeMethodName
    : literal
    | dataReference
    ;

invokeUsing
    : USING invokeArgument+
    ;

invokeArgument
    : BY VALUE arithmeticExpression
    | BY REFERENCE dataReference
    | BY CONTENT (dataReference | literal)
    | dataReference
    | literal
    ;

invokeReturning
    : RETURNING dataReference
    ;

// ── USAGE OBJECT REFERENCE [class-name] (ISO §13.18.60.4) ──
// Factored as its own rule (two explicit alternatives — NOT an optional-tail className?) so it left-factors cleanly
// when hooked into usageKeyword (the optional tail was a suspected ambiguity, OO design doc §6.5).
objectReferenceUsage
    : OBJECT REFERENCE className
    | OBJECT REFERENCE
    ;
