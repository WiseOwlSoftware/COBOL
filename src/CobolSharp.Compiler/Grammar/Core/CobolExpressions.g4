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

// A "value" in COBOL terms: numeric expression or non-numeric literal.
valueOperand
    : arithmeticExpression
    | nonNumericLiteral
    ;

// Shared range form: used by VALUE THRU and EVALUATE WHEN ranges.
valueRange
    : valueOperand (THRU | THROUGH) valueOperand
    ;

// =========================
// Conditions (boolean)
// =========================

booleanLiteral
    : TRUE_
    | FALSE_
    ;

// COBOL sign conditions: IS [NOT] POSITIVE/NEGATIVE/ZERO.
signCondition
    : valueOperand IS? NOT? (POSITIVE | NEGATIVE | ZERO)
    ;

condition
    : logicalOrExpression
    ;

logicalOrExpression
    : logicalAndExpression ( OR ( logicalAndExpression | abbreviatedAndChain ) )*
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
    : signCondition
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

comparisonExpression
    : comparisonOperand IS? NOT? className                         // class condition
    | comparisonOperand ( comparisonOperator comparisonOperand )?  // existing relational + bare operand
    ;

className
    : NUMERIC
    | ALPHABETIC
    | ALPHABETIC_LOWER
    | ALPHABETIC_UPPER
    | IDENTIFIER                    // user-defined CLASS from SPECIAL-NAMES
    ;

classCondition
    : NUMERIC
    | ALPHABETIC
    | ALPHABETIC_LOWER
    | ALPHABETIC_UPPER
    | ALPHANUMERIC
    ;

comparisonOperator
    // Symbolic
    : EQUALS
    | NOTEQUAL
    | LTEQUAL
    | GTEQUAL
    | LT
    | GT
    // Abbreviated NOT + symbolic (COBOL-85 §6.3.4.2)
    | NOT EQUALS       // NOT =
    | NOT GT            // NOT >
    | NOT LT            // NOT <
    | NOT GTEQUAL       // NOT >=
    | NOT LTEQUAL       // NOT <=
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
    | functionCall
    | dataReference
    | LPAREN arithmeticExpression RPAREN
    ;

// FUNCTION calls (1989 Amendment to COBOL-85 — intrinsic functions)
// The function name + arguments are parsed via functionName + optional subscriptPart.
// Arguments (if any) are captured as subscriptPart tokens by the SUBSCRIPT lexer mode.
// No-arg functions (e.g., FUNCTION PI) have no subscriptPart.
functionCall
    : FUNCTION functionName subscriptPart?
    ;

// Function names are normally IDENTIFIERs, but several intrinsic function names
// collide with reserved words (lexer tokens). List them explicitly so the parser
// accepts them after FUNCTION.
functionName
    : IDENTIFIER
    | DISPLAY
    | MERGE
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
    : DECIMALLIT                           // 123.45 or .45 (dot decimal from lexer)
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
    | ALL STRINGLIT
    | ALL HEXLIT
    | ALL ZERO
    | ALL SPACE
    | ALL HIGH_VALUE
    | ALL LOW_VALUE
    | ALL QUOTE_
    ;
