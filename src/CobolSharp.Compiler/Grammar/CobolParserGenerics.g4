parser grammar CobolParserGenerics;

options { tokenVocab = CobolLexer; }

import CobolParserCore;

// --- generic typedefs ---

typeDefinitionEntry
    : TYPEDEF GENERIC? genericParameterList? dataDescriptionEntry
    ;

genericParameterList
    : LT genericParameter (COMMA genericParameter)* GT
    ;

genericParameter
    : typeParameterName ( 'OF' typeConstraint )?
    ;

typeParameterName
    : IDENTIFIER
    ;

typeConstraint
    : typeName
    ;

typeName
    : IDENTIFIER
    ;

// --- generic type specifier ---

genericTypeSpecifier
    : typeName LT typeArgument (COMMA typeArgument)* GT
    ;

typeArgument
    : typeName
    | genericTypeSpecifier
    ;

// You'll thread genericTypeSpecifier into dataDescriptionEntry, parameter lists, etc.
// e.g., by extending dataDescriptionClauses or adding a type-usage production.
