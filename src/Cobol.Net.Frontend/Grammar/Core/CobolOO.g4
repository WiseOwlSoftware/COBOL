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
      factoryParagraph?
      objectParagraph?
      endClassHeader
    ;

// FACTORY definition (ISO §11.4 :13069; order per the §10.6 class-definition format :12745-12748) — factory
// data (DATA DIVISION → the factory singleton's instance fields, brief D11) + factory methods (same
// methodDefinition rule as the OBJECT paragraph, so the whole method machinery reuses verbatim). IMPLEMENTS
// is the INTERFACE-ID slice (deliberately unparsed, matching objectParagraph). END FACTORY carries NO name
// (§10.6 :12760).
factoryParagraph
    : (IDENTIFICATION DIVISION DOT)? FACTORY DOT implementsClause?
      environmentDivision?
      dataDivision?
      (PROCEDURE DIVISION DOT methodDefinition*)?
      END FACTORY DOT
    ;

classIdParagraph
    : CLASS_ID DOT className (IS? FINAL)? (INHERITS FROM className)? DOT
    ;   // [IS FINAL] precedes INHERITS per the §10.6 format (:12742-12744); a FINAL class shall not be a superclass (§11.3 GR3 — bind-gated 0839)

className
    : cobolWord
    ;

// OBJECT (instance) definition — instance data (DATA DIVISION) + instance methods (in the PROCEDURE DIVISION).
// IMPLEMENTS rides the paragraph HEADER with its OWN trailing period (§11.8.2 :13305; instance-definition
// format :12765) — never the CLASS-ID (the dead sketch put it there; spec-wrong).
objectParagraph
    : (IDENTIFICATION DIVISION DOT)? OBJECT DOT implementsClause?
      environmentDivision?
      dataDivision?
      (PROCEDURE DIVISION DOT methodDefinition*)?
      END OBJECT DOT
    ;

implementsClause
    : IMPLEMENTS interfaceName+ DOT
    ;

interfaceName
    : cobolWord
    ;

// INTERFACE definition (§11.6 :13157; §10.6 :12783-12796 — NO data division at the interface level; methods
// are PROTOTYPES: header + optional LINKAGE-only data division, no procedure body — enforced at pass-1,
// COBOLNET0840). INHERITS repetition is SUPPORTED (C# interface lists are native — the deliberate asymmetry
// with the class-side single-inheritance restriction, SSOT §18.18).
interfaceDefinition
    : (IDENTIFICATION DIVISION DOT)? INTERFACE_ID DOT interfaceName
      (INHERITS FROM interfaceName+)? DOT
      environmentDivision?
      (PROCEDURE DIVISION DOT methodDefinition*)?
      END INTERFACE interfaceName DOT
    ;

// METHOD-ID … END METHOD — a method definition (its own DATA/PROCEDURE divisions). ISO §11.7 / §12798.
methodDefinition
    : (IDENTIFICATION DIVISION DOT)? METHOD_ID DOT
      ( methodName | methodPropertySelector )
      OVERRIDE? (IS? FINAL)? DOT
      environmentDivision?
      dataDivision?
      procedureDivision?
      END METHOD methodName? DOT
    ;   // the ONLY ISO method attributes: [OVERRIDE] [IS FINAL] (§10.6 :12798-12821; Spec corrections #4 — ABSTRACT is NOT ISO); SR4a/SR3/FINAL enforced at bind (0837/0838/0839)

methodName
    : cobolWord
    ;

// METHOD-ID. GET|SET PROPERTY prop-name — the explicit property-accessor selector (§10.6 :12810-12814;
// §11.7 SR6/SR7 shape checks are pass-1, COBOLNET0842).
methodPropertySelector
    : (GET | SET) PROPERTY propertyName
    ;

propertyName
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
    : OBJECT REFERENCE FACTORY OF className   // a FACTORY-object reference (§13.18.60 :22681) — binder-staged 0899 (the universal-reference wave)
    | OBJECT REFERENCE className
    | OBJECT REFERENCE
    ;
