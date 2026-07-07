// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// Literals, arithmetic expressions, conditions, and comparisons.
// Imported by CobolParserCore.g4. tokenVocab enables VSCode ANTLR4 extension token resolution.

parser grammar CobolExpressions;

options {
    tokenVocab = CobolLexer;
}

// Generic clause pattern for vendor/extension hooks.
// Shared across all grammars — one rule, one source of truth.
genericClause
    : IDENTIFIER (IDENTIFIER | literal)*
    ;

// =========================
// Value operands and ranges
// =========================

// A "value" in comparisons/EVALUATE: full arithmetic expression or literal.
valueOperand
    : arithmeticExpression
    | nonNumericLiteral
    ;

// A "value" in VALUE clauses: single value (no binary arithmetic).
// Uses unaryExpression to prevent "5 -9999" from being parsed as subtraction.
valueClauseOperand
    : unaryExpression
    | nonNumericLiteral
    ;

// Range form for EVALUATE WHEN (full arithmetic).
valueRange
    : valueOperand (THRU | THROUGH) valueOperand
    ;

// Range form for VALUE clauses (no binary arithmetic).
valueClauseRange
    : valueClauseOperand (THRU | THROUGH) valueClauseOperand
    ;

// =========================
// Conditions (boolean)
// =========================

booleanLiteral
    : TRUE_
    | FALSE_
    ;

// signCondition has been merged into comparisonExpression to eliminate
// ANTLR prediction ambiguity — both rules started with valueOperand,
// causing exponential DFA growth on files with many figurative-constant comparisons.

condition
    : logicalOrExpression
    ;

logicalOrExpression
    : logicalXorExpression ( OR ( logicalXorExpression | abbreviatedAndChain ) )*
    ;

// COBOL-2023 logical exclusive-or (ISO §8.8.4.9; precedence NOT > AND > XOR > OR). XOR and EXCLUSIVE-OR are
// equivalent. Sits between OR and AND so `a OR b XOR c` parses as `a OR (b XOR c)`. The OPERATOR is a 2023
// addition (Annex E.2 item 25 reserves both words; VCR rows 32/41 — the W3 regating of the former "2002"
// mislabel): gated {is2023()}?; below 2023 both words are USER-DEFINED words (cobolWord admits the tokens;
// the §8.9 funnel + table enforce the 2023 reservation as 0901 in provable positions).
logicalXorExpression
    : logicalAndExpression ( {is2023()}? ( XOR | EXCLUSIVE_OR ) logicalAndExpression )*
    ;

logicalAndExpression
    : unaryLogicalExpression ( AND ( abbreviatedRelation | unaryLogicalExpression ) )*
    ;

// Abbreviated AND chain: one or more abbreviated relations connected by AND.
// Used after OR when the abbreviated form includes AND chaining:
//   IF A = B OR = C AND = D   → OR (= C AND = D)
abbreviatedAndChain
    : abbreviatedRelation ( AND abbreviatedRelation )*
    ;

// Abbreviated relational condition (COBOL-85 §6.3.4.2):
// After AND/OR, the left operand (and optionally the operator) can be
// elided from the previous comparison.
//   IF A > B OR < C          →  comparisonOperator comparisonOperand
//   IF A > B AND NOT < C     →  comparisonOperator comparisonOperand
//     (NOT < is already a comparisonOperator alternative)
// Bare operands (IF A = B OR C) are already handled by the full
// logicalAndExpression/unaryLogicalExpression path.
// NOT + bare operand (IF A = B AND NOT C) is handled by unaryLogicalExpression.
abbreviatedRelation
    : comparisonOperator comparisonOperand
    ;

unaryLogicalExpression
    : NOT primaryCondition
    | primaryCondition
    ;

primaryCondition
    // COBOL-2002 boolean forms (ISO §8.8.4.2.2 relation / §8.8.4.3 simple condition) — gated by the
    // boolExprAhead() predicate so it fires ONLY when a B-operator is actually present in this condition;
    // a normal comparison returns false and falls to comparisonExpression UNCHANGED (the shared rule is
    // untouched — the DEVLOG-621 regression lesson). booleanExpression's leaf is valueOperand, so the binder
    // unwraps a B-op-free operand back to a normal operand (BindPrimaryBoolean).
    : {is2002() && boolExprAhead()}? booleanExpression ( comparisonOperator booleanExpression )?
    | comparisonExpression
    | booleanLiteral
    | LPAREN condition RPAREN
    ;

// =========================
// Relational
// =========================

comparisonOperand
    : valueOperand
    ;

// ── COBOL-2002 boolean expressions (ISO §8.8.2; precedence B-NOT > B-AND > B-XOR > B-OR, rule 7b).
// Permissive-superset doctrine: the operand SHAPES (a boolean item / boolean literal / figurative ZERO /
// ALL B"…") are enforced at BIND (the boolean-expression constraint band); the tiers enforce the formation
// rules 1–3 + Table 4 adjacency STRUCTURALLY. Every alternative involving a B-operator is {is2002()}?-gated
// so prediction kills it instantly at 85/NIST (the words behave as user words there, exactly as before). ──
booleanExpression : booleanXorTerm ( {is2002()}? B_OR booleanXorTerm )* ;
booleanXorTerm    : booleanAndTerm ( {is2002()}? B_XOR booleanAndTerm )* ;
booleanAndTerm    : booleanFactor  ( {is2002()}? B_AND booleanFactor )* ;
booleanFactor     : {is2002()}? B_NOT booleanFactor
                  | {is2002()}? LPAREN booleanExpression RPAREN
                  | valueOperand
                  ;

comparisonExpression
    : comparisonOperand IS? NOT? className                         // class condition
    | comparisonOperand IS? NOT? (POSITIVE | NEGATIVE | ZERO)      // sign condition (merged from signCondition)
    | comparisonOperand ( comparisonOperator comparisonOperand )?  // existing relational + bare operand
    ;
    // NOTE (Phase-4a increment 2, DEVLOG 621): the boolean RELATION (§8.8.4.2.2) and the simple boolean
    // CONDITION (§8.8.4.3) are STAGED RESIDUE — a booleanExpression alternative here disturbed the shared
    // parser's comparison DFA (subscripted / ref-mod comparisons at 2002+ regressed: `ELEM(I) = x` → "no
    // viable alternative"), so the condition-context boolean forms are deferred to a focused grammar pass.
    // The boolean OPERATORS work in COMPUTE Format 2 (its own dedicated computeStatement alt, isolated from
    // conditions). `IF (a B-AND b)` etc. are NOT yet supported.

className
    : NUMERIC
    | ALPHABETIC
    | ALPHABETIC_LOWER
    | ALPHABETIC_UPPER
    | cobolWord                     // user-defined CLASS from SPECIAL-NAMES
    ;

classCondition
    : NUMERIC
    | ALPHABETIC
    | ALPHABETIC_LOWER
    | ALPHABETIC_UPPER
    | ALPHANUMERIC
    ;

comparisonOperator
    // Symbolic (with optional IS prefix per §6.3.4.2)
    : IS? EQUALS
    | IS? NOTEQUAL
    | IS? LTEQUAL
    | IS? GTEQUAL
    | IS? LT
    | IS? GT
    // Abbreviated NOT + symbolic (COBOL-85 §6.3.4.2)
    | IS? NOT EQUALS       // NOT =
    | IS? NOT GT            // NOT >
    | IS? NOT LT            // NOT <
    | IS? NOT GTEQUAL       // NOT >=
    | IS? NOT LTEQUAL       // NOT <=
    // Word forms with optional IS and optional THAN
    | IS? EQUAL (TO | THAN)?
    | IS? NOT EQUAL (TO | THAN)?
    | IS? GREATER THAN? OR EQUAL TO?
    | IS? NOT GREATER THAN? OR EQUAL TO?
    | IS? LESS THAN? OR EQUAL TO?
    | IS? NOT LESS THAN? OR EQUAL TO?
    | IS? GREATER THAN?
    | IS? NOT GREATER THAN?
    | IS? LESS THAN?
    | IS? NOT LESS THAN?
    ;

// =========================
// Arithmetic
// =========================

arithmeticExpression
    : additiveExpression
    ;

additiveExpression
    : multiplicativeExpression ( addOp multiplicativeExpression )*
    ;

addOp
    : PLUS
    | MINUS
    ;

multiplicativeExpression
    : powerExpression ( mulOp powerExpression )*
    ;

mulOp
    : STAR
    | SLASH
    ;

powerExpression
    : unaryExpression ( POWER unaryExpression )*
    ;

unaryExpression
    : addOp unaryExpression          // unary + or -
    | primaryExpression
    ;

// =========================
// Primaries
// =========================

primaryExpression
    : numericLiteral
    | ZERO_ARITH                       // figurative ZERO rewritten by token rewriter in arithmetic context
    | functionCall
    | dataReference
    | LPAREN arithmeticExpression RPAREN
    ;

// FUNCTION calls (1989 Amendment to COBOL-85 — intrinsic functions, ISO §15).
// Arguments (if any) are captured in SUBSCRIPT lexer mode (like subscripts) so the COBOL
// comma/space separators that delimit arguments are preserved — e.g. MAX(-4, 7, 3, -8) must
// stay four arguments, not be re-read as "3 - 8". The binder then parses each comma/space-
// delimited segment as a full arithmetic expression (ISO §15 allows arithmetic-expression
// arguments). No-arg functions (e.g. FUNCTION PI) have no subscriptPart.
functionCall
    : FUNCTION functionName subscriptPart?
    ;

// Function names are normally IDENTIFIERs, but several intrinsic function names
// collide with reserved words (lexer tokens). List them explicitly so the parser
// accepts them after FUNCTION.
functionName
    : IDENTIFIER
    | DISPLAY
    | LENGTH
    | MERGE
    | NATIONAL
    | BIT
    | RANDOM
    | SIGN
    | SORT
    | SUM
    ;

argumentList
    : argument (COMMA argument)*
    ;

argument
    : arithmeticExpression
    | literal
    | dataReference
    ;

// =========================
// Literals
// =========================

literal
    : numericLiteral
    | nonNumericLiteral
    ;

numericLiteral
    : signedNumericLiteral
    ;

nonNumericLiteral
    : STRINGLIT
    | NATLIT
    | BOOLLIT
    | HEXLIT
    | figurativeConstant
    ;

signedNumericLiteral
    : (PLUS | MINUS)? numericLiteralCore
    ;

// Numeric literal assembly.
// DOT-based decimals use DECIMALLIT from the lexer (maximal munch resolves
// DOT-as-decimal vs DOT-as-sentence-terminator unambiguously).
// COMMA-based decimals for DECIMAL-POINT IS COMMA are assembled here in the parser.
numericLiteralCore
    : FLOATLIT                             // 1.5E3, 2.5E-2 (floating-point literal, ISO §8.3.3.3.3 — D16)
    | DECIMALLIT                           // 123.45 or .45 (dot decimal from lexer)
    | INTEGERLIT COMMA INTEGERLIT          // 123,45 (comma decimal — DECIMAL-POINT IS COMMA)
    | COMMA INTEGERLIT                     // ,45 (leading comma decimal)
    | INTEGERLIT                           // 123 (integer)
    ;

integerLiteral
    : INTEGERLIT
    ;

figurativeConstant
    : ZERO
    | SPACE
    | HIGH_VALUE
    | LOW_VALUE
    | QUOTE_
    | NULL_
    | ALL STRINGLIT
    | ALL HEXLIT
    | ALL BOOLLIT       // ALL B"…" — a boolean figurative (ISO §8.3.3.6.4 / §8.8.2 :9331); 2002+ (binder-gated)
    | ALL ZERO
    | ALL SPACE
    | ALL HIGH_VALUE
    | ALL LOW_VALUE
    | ALL QUOTE_
    ;
