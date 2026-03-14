parser grammar CobolParserOO;

options { tokenVocab = CobolLexer; }

import CobolParserCore;

// ==========================================
// CLASS DEFINITION (COBOL-2002 → 2023)
// ==========================================

classDefinition
    : identificationDivision
      environmentDivision?
      classDataDivision?
      objectDataDivision?
      methodDivision
    ;

// ------------------------------------------
// CLASS-ID Paragraph
// ------------------------------------------

classIdParagraph
    : 'CLASS-ID' DOT className classAttributes? DOT
    ;

className
    : IDENTIFIER
    ;

classAttributes
    : classAttribute+
    ;

classAttribute
    : 'FINAL'
    | 'ABSTRACT'
    | 'INHERITS' className
    | 'IMPLEMENTS' interfaceNameList
    ;

interfaceNameList
    : interfaceName (COMMA interfaceName)*
    ;

interfaceName
    : IDENTIFIER
    ;

// ------------------------------------------
// CLASS-DATA and OBJECT-DATA Divisions
// ------------------------------------------

classDataDivision
    : DATA DIVISION DOT 'CLASS' SECTION DOT dataDescriptionEntry*
    ;

objectDataDivision
    : DATA DIVISION DOT 'OBJECT' SECTION DOT dataDescriptionEntry*
    ;

// ==========================================
// METHOD DIVISION
// ==========================================

methodDivision
    : methodDeclaration+
    ;

// ------------------------------------------
// METHOD-ID / END-METHOD (full expansion)
// ------------------------------------------

methodDeclaration
    : METHOD_ID DOT methodName methodAttributes? DOT
      environmentDivision?
      dataDivision?
      procedureDivision
      END_METHOD methodName DOT
    ;

methodName
    : IDENTIFIER
    ;

methodAttributes
    : methodAttribute+
    ;

methodAttribute
    : 'STATIC'
    | 'FINAL'
    | 'OVERRIDE'
    | 'PRIVATE'
    | 'PROTECTED'
    | 'PUBLIC'
    ;

// ==========================================
// INVOKE (§14.9.21 — full OO expansion)
// ==========================================

invokeStatement
    : INVOKE invokeTarget
      invokeMethod
      invokeUsing?
      invokeReturning?
      invokeOnException?
      END_INVOKE?
      DOT?
    ;

invokeTarget
    : objectReference
    | className
    | interfaceName
    ;

invokeMethod
    : methodName
    ;

invokeUsing
    : USING invokeArgument+
    ;

invokeArgument
    : 'BY' 'VALUE' arithmeticExpression
    | 'BY' 'REFERENCE' identifier
    | 'BY' 'CONTENT' (identifier | literal)
    | identifier
    | literal
    ;

invokeReturning
    : RETURNING identifier
    ;

invokeOnException
    : ON EXCEPTION imperativeStatement
      (NOT ON EXCEPTION imperativeStatement)?
    ;

// ==========================================
// OBJECT REFERENCES
// ==========================================

objectReference
    : identifier
    | 'SELF'
    | 'SUPER'
    | 'NULL'
    ;
