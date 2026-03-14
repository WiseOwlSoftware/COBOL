parser grammar CobolParserOO;

options { tokenVocab = CobolLexer; }

import CobolParserCore;

// --- class / object data divisions and methods ---

classDefinition
    : identificationDivision
      environmentDivision?
      classDataDivision?
      objectDataDivision?
      methodDivision
    ;

classDataDivision
    : DATA DIVISION DOT 'CLASS' SECTION DOT dataDescriptionEntry*
    ;

objectDataDivision
    : DATA DIVISION DOT 'OBJECT' SECTION DOT dataDescriptionEntry*
    ;

methodDivision
    : methodDeclaration+
    ;

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
    : /* STATIC, FINAL, etc. */
      IDENTIFIER*
    ;

// --- INVOKE (also referenced from core) ---

invokeStatement
    : INVOKE methodObject
      (USING identifierList)?
      (RETURNING identifier)?
      DOT?
    ;

methodObject
    : objectReference
    | className
    | interfaceName
    ;

objectReference
    : identifier
    | 'SELF'
    | 'SUPER'
    ;

className
    : IDENTIFIER
    ;

interfaceName
    : IDENTIFIER
    ;
