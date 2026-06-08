// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

parser grammar CobolDialect;

options { tokenVocab = CobolLexer; }

import CobolParserCore;

// ==========================================
// DIALECT OVERLAY SYSTEM
// ==========================================
//
// CobolSharp supports four COBOL standards via semantic predicates.
// The parser constructor sets the dialect:
//
//   parser.Dialect = CobolDialect.Cobol2023;
//
// Predicates gate features by standard level:
//   {is2002()}?  — OO classes, methods, INVOKE
//   {is2014()}?  — JSON/XML
//   {is2023()}?  — Generics, DELETE FILE, inline methods, END-xxx everywhere
//
// The grammar accepts a superset. Dialect validation happens in
// the semantic phase using these predicates as advisory gates.

// ==========================================
// COBOL-85 COMPATIBILITY
// ==========================================

// In COBOL-85, END was a valid standalone imperative statement (no-op).
// This allows: READ file RECORD AT END END
// COBOL-2023 removed standalone END as an imperative.

dialect85Imperative
    : {is85()}? END
    ;

// Dialect-aware imperative statement
imperativeStatement
    : dialect85Imperative
    | statement+
    ;

// ALTER statement (archaic — COBOL-85 only)
// alterStatement
//     : {is85()}? 'ALTER' alterClause+ DOT?
//     ;
//
// alterClause
//     : procedureName 'TO' 'PROCEED'? 'TO'? procedureName
//     ;

// ==========================================
// COBOL-2002+ FEATURES (OO)
// ==========================================

// OO features are gated by is2002():
//   classDefinition       — {is2002()}?
//   methodDeclaration     — {is2002()}?
//   invokeStatement       — {is2002()}?
//   classDataDivision     — {is2002()}?
//   objectDataDivision    — {is2002()}?

// ==========================================
// COBOL-2014+ FEATURES (JSON/XML)
// ==========================================

// JSON/XML features are gated by is2014():
//   jsonStatement         — {is2014()}?
//   xmlStatement          — {is2014()}?

// ==========================================
// COBOL-2023 FEATURES
// ==========================================

// 2023 features are gated by is2023():
//   typeDefinitionEntry   — {is2023()}?
//   genericParameterList  — {is2023()}?
//   deleteFileStatement   — {is2023()}?
//   inlineMethodInvocationStatement — {is2023()}?
//   END-JSON, END-XML, END-INVOKE (always accepted, but
//     semantically validated for 2023+ only)

// ==========================================
// DIALECT PREDICATE HELPERS
// ==========================================
//
// These are implemented in your parser base class (C#):
//
//   public enum CobolDialect { Cobol85, Cobol2002, Cobol2014, Cobol2023 }
//
//   private CobolDialect _dialect = CobolDialect.Cobol2023;
//
//   public bool is85()   => _dialect >= CobolDialect.Cobol85;
//   public bool is2002() => _dialect >= CobolDialect.Cobol2002;
//   public bool is2014() => _dialect >= CobolDialect.Cobol2014;
//   public bool is2023() => _dialect >= CobolDialect.Cobol2023;
//
// The >= pattern means each standard includes all previous standards.
// COBOL-2023 accepts everything; COBOL-85 rejects OO, JSON, generics.