// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

parser grammar CobolParserGenerics;

options { tokenVocab = CobolLexer; }

import CobolParserCore;

// ==========================================
// TYPEDEF GENERIC (COBOL-2023)
// ==========================================

typeDefinitionEntry
    : TYPEDEF genericMarker? genericParameterList? dataDescriptionEntry
    ;

genericMarker
    : GENERIC
    ;

// ==========================================
// GENERIC PARAMETER LIST
// ==========================================

genericParameterList
    : LT genericParameter (COMMA genericParameter)* GT
    ;

genericParameter
    : typeParameterName genericConstraint?
    ;

typeParameterName
    : IDENTIFIER
    ;

genericConstraint
    : 'OF' typeName
    ;

// ==========================================
// GENERIC TYPE SPECIFIER
// ==========================================

genericTypeSpecifier
    : typeName LT typeArgument (COMMA typeArgument)* GT
    ;

typeArgument
    : typeName
    | genericTypeSpecifier   // supports nested generics: Tree<Node<INTEGER>>
    ;

// ==========================================
// TYPE NAME
// ==========================================

typeName
    : IDENTIFIER
    ;

// ==========================================
// TYPE CLAUSE (for data descriptions)
// ==========================================
// Extends dataDescriptionClause with:
//   01 Customers TYPE MyList<Customer>.
//   01 Node TYPE Tree<Node<INTEGER>>.

typeClause
    : 'TYPE' IS? (typeName | genericTypeSpecifier)
    ;

// ==========================================
// GENERIC METHOD PARAMETERS (COBOL-2023)
// ==========================================
// Extends methodAttribute with generic parameter lists:
//   METHOD-ID. AddItem<T>.

genericMethodAttribute
    : genericParameterList
    ;

// ==========================================
// INVOKE WITH GENERIC TYPES
// ==========================================
// Extends invokeMethod with generic type arguments:
//   INVOKE MyList::AddItem<INTEGER> USING 5.

genericInvocationArguments
    : LT typeArgument (COMMA typeArgument)* GT
    ;

// ==========================================
// CALL WITH GENERIC TYPES (optional)
// ==========================================
// Extends callTarget with generic type specifiers:
//   CALL MyFactory<Customer>::Create USING params.

genericCallTarget
    : genericTypeSpecifier
    ;