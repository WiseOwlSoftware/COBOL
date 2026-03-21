// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

// Literals, arithmetic expressions, conditions, and comparisons.
// Imported by CobolParserCore.g4 — no options block.

parser grammar CobolExpressions;

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
    : logicalAndExpression ( OR logicalAndExpression )*
    ;

logicalAndExpression
    : unaryLogicalExpression ( AND unaryLogicalExpression )*
    ;

unaryLogicalExpression
    : NOT unaryLogicalExpression
    | primaryCondition
    ;

primaryCondition
    : comparisonExpression
    | signCondition
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
    : '+'
    | '-'
    ;

multiplicativeExpression
    : powerExpression ( mulOp powerExpression )*
    ;

mulOp
    : '*'
    | '/'
    ;

powerExpression
    : unaryExpression ( '**' unaryExpression )?
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

// FUNCTION calls (ISO 2002+)
functionCall
    : {is2002()}? FUNCTION dataReference (LPAREN argumentList? RPAREN)?
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
