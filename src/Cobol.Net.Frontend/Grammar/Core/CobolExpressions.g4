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
    : logicalAndExpression ( ( XOR | EXCLUSIVE_OR ) logicalAndExpression )*   // XOR/EXCLUSIVE-OR: COBOL-2023; parses at all editions (superset — a bare user-word XOR is never valid in this connective slot), gated at BIND when the operator is genuinely present (BindCondition XOR arm → Check(LogicalXorOperator2023)) — residue migration #1, DESIGN-version-conformance-pipeline.md
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
    : {boolExprAhead()}? booleanExpression ( comparisonOperator booleanExpression )?
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
// rules 1–3 + Table 4 adjacency STRUCTURALLY. The tiers are SUPERSET-parsed (no edition predicate); they are
// reached ONLY through the boolExprAhead()-gated primaryCondition ENTRY (or COMPUTE F2), so a B-op-free condition
// never enters them (the shared comparisonExpression rule is untouched — the DEVLOG-621 lesson). The COBOL-2002
// introduction gate is bind-time: Check(BooleanOperators2002) in BindBoolExpr when HasBoolOp — residue migration #2. ──
booleanExpression : booleanXorTerm ( B_OR booleanXorTerm )* ;
booleanXorTerm    : booleanAndTerm ( B_XOR booleanAndTerm )* ;
booleanAndTerm    : booleanShiftTerm ( B_AND booleanShiftTerm )* ;
// Boolean shift tier (ISO §8.8.2 rule 8, COBOL-2023). The shift's SECOND operand is an INTEGER operand (rule 5 /
// Table 4 — after a shift operator ONLY an identifier-or-literal integer may appear), never a booleanFactor. This
// fixed placement gives the shift tighter-than-B-AND binding, which realizes the default (unmixed) rule-7b case
// exactly; the context-sensitive rule-7b precedence (a shift inheriting the precedence of a preceding B-OR/B-XOR)
// is a documented refinement (see BoundBoolShift / the wave-C scout).
booleanShiftTerm  : booleanFactor booleanShiftSuffix* ;
// The shift's second operand is an INTEGER operand (ISO §8.8.2 rule 5). Permissive-superset: parse an
// arithmeticExpression (an integer literal / data item is a subset) — the count is used as an integer at runtime.
booleanShiftSuffix : (B_SHIFT_L | B_SHIFT_R | B_SHIFT_LC | B_SHIFT_RC) arithmeticExpression ;
booleanFactor     : B_NOT booleanFactor
                  | LPAREN booleanExpression RPAREN
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

// FUNCTION calls (1989 Amendment to COBOL-85 — intrinsic functions, ISO §15; function-identifier §8.4.3.2).
// P7 Step 12: the argument-list '(' after "FUNCTION functionName" stays in DEFAULT lexer mode (the lexer's
// FUNCTION suppression — §8.4.3.2 SR6: that '(' is ALWAYS the argument list), so each argument parses as a real
// arithmeticExpression through the ONE expression grammar (SR8: an argument is an identifier, literal, boolean
// expression, or arithmetic expression). Arguments are separated by separators only (§8.3.5: space, or
// comma/semicolon followed by space — both skipped by the lexer), so the list is plain juxtaposition; the
// space-vs-adjacent sign distinction (MAX(A -4) = two args, MAX(A - 4) = one subtraction; §8.7.1 operator
// spacing + §8.3.3.3.2 literal-sign adjacency) is preserved by the lexer's argument-region SIGNED_* twins.
// A nested data-reference subscript inside an argument still lexes in SUBSCRIPT mode (the D10/PHASE-15
// deferral is untouched). The empty-parens form (FUNCTION RANDOM ()) is the §8.4.3.2 SR6 NOTE's shape.
// The keyword-omitted form name(args) (§8.4.3.2 SR2) has NO grammar alternative (D2 — irreducible ambiguity
// with a subscripted dataReference); the binder re-parses its captured argument text through
// functionArgListFragment below.
functionCall
    : FUNCTION functionName (LPAREN functionArgList? RPAREN)?
    ;

// Arguments separate by space (no token — plain juxtaposition) or by the §8.3.5 comma/semicolon-plus-space
// separator, which inside an argument region survives as FNARG_SEPARATOR so a following '(' reads as a
// parenthesized ARGUMENT, never as a subscript of the previous argument's data-name.
functionArgList
    : functionArgument (FNARG_SEPARATOR? functionArgument)*
    ;

// One function argument (§8.4.3.2 SR8 + the §15 per-function phrase words). The phrase-keyword alternative
// admits the RESERVED words that appear inside §15 argument lists (TRIM LEADING/TRAILING §15.96; FIND-STRING
// LAST/START/AFTER §15.37; SUBSTITUTE FIRST/LAST §15.87; CONVERT ANY/ALPHANUMERIC/NATIONAL §15.19) — words that
// are plain IDENTIFIERs (ANYCASE, HEX, NAT, ANUM, BYTE, CURRENT, ACTIVATING, NESTED, STACK, TOP-LEVEL) arrive
// through dataReference inside arithmeticExpression and are classified by name in the binder. OMITTED is the
// §8.4.3.2.2 format's argument alternative (SR7 bars it for intrinsics — a bind-time diagnostic, not a parse
// error). A superset rule: which words a given function admits is the binder's §15 job.
functionArgument
    : fnArgPhraseWord
    | OMITTED
    | nonNumericLiteral
    | arithmeticExpression
    ;

fnArgPhraseWord
    : LEADING
    | TRAILING
    | LAST
    | FIRST
    | ANY
    | START
    | AFTER
    | ALPHANUMERIC
    | NATIONAL
    ;

// The D2 keyword-omitted re-parse entry (binder-invoked only): the argument text captured by a dataReference's
// subscriptPart, re-lexed with the lexer primed as a function-argument region (PrimeFunctionArgs), parses
// through the SAME functionArgList rule — ONE argument grammar for both reference forms.
functionArgListFragment
    : functionArgList? EOF
    ;

// ── COMPILE-TIME DIRECTIVE-EXPRESSION fragments (ISO §7.3.6 arithmetic / §7.3.7 boolean / §7.3.8 constant-
// conditional-expression). Isolated fragment entry rules — reachable ONLY from the frontend's directive-expression
// re-parse (the functionArgListFragment precedent: referenced by nothing in compilationUnit, so ZERO blast radius
// on the main parse). They reuse the existing operand sub-rules (arithmeticExpression / booleanExpression /
// nonNumericLiteral / comparisonOperator / cobolWord) — no duplicated expression grammar. The lexer is primed with
// PrimeDirectiveExpr() so DEFINED is a token and every '(' groups. Evaluated by the ONE shared
// CompileTimeExpressionEvaluator (DESIGN-compile-time-expressions.md §4). ──

// One compile-time operand (a >>DEFINE value, a >>EVALUATE selection-subject / >>WHEN object). Operand-kind
// disambiguation reuses the boolExprAhead() predicate (the primaryCondition mechanism): booleanExpression is
// entered ONLY when a real B-operator is present; otherwise an arithmetic operand (a single numeric literal too —
// GR5 reclassification is in the evaluator) or a non-numeric literal. The evaluator dispatches on WHICH sub-node
// parsed, not a token guess (booleanExpression's leaf would otherwise match every arithmetic/non-numeric operand).
compileTimeOperandFragment : compileTimeOperand EOF ;
compileTimeOperand
    : {boolExprAhead()}? booleanExpression      // a genuine boolean expression (a B-operator is present)
    | arithmeticExpression                       // numeric operand (a single numeric literal too — GR5 in eval)
    | nonNumericLiteral                          // string / national / boolean / hex literal operand
    ;

// A constant-conditional-expression (§7.3.8) — the >>IF operand and the >>EVALUATE-TRUE >>WHEN operand.
// Precedence NOT > AND > OR (§8.8.4.9), parentheses grouping (LPAREN is always a group under PrimeDirectiveExpr).
constantConditionalExpressionFragment : constantConditionalExpression EOF ;
constantConditionalExpression : cceOr ;
cceOr      : cceAnd ( OR cceAnd )* ;
cceAnd     : cceNot ( AND cceNot )* ;
cceNot     : NOT cceNot | ccePrimary ;
ccePrimary : LPAREN constantConditionalExpression RPAREN
           | definedCondition
           | cceRelationOrBoolean ;
// §7.3.8.4.4 defined-condition. DEFINED is a token only under PrimeDirectiveExpr (a primed-lexer keyword). Listed
// before cceRelationOrBoolean so a trailing DEFINED selects it; a cobolWord with no DEFINED falls through.
definedCondition : cobolWord IS? NOT? DEFINED ;
cceRelationOrBoolean
    : {boolExprAhead()}? booleanExpression                            // §8.8.4.3 simple boolean condition (length-1)
    | compileTimeOperand ( IS? NOT? comparisonOperator compileTimeOperand )? ;   // §7.3.8.2 relation (or a bare operand)

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

// A concatenation expression (ISO §8.8.3) is an ALTERNATIVE OF the non-numeric-literal rule because §8.8.3.3
// GR3 makes it "equivalent to a literal of the same class and value, [usable] anywhere a literal of that class
// may be used" — every literal position (VALUE clauses, statement operands, FUNCTION arguments, …) inherits it
// through this one rule. The concat tier is a distinct ADDITIVE alternative (first, per ANTLR first-match
// precedence); the plain single-token alternatives are untouched, so a non-concatenated literal parses with
// exactly the same shape as before. AMPERSAND appears in no other rule, so prediction is unambiguous: a literal
// followed by '&' can only be a concatenation expression.
nonNumericLiteral
    : concatenationExpression
    | STRINGLIT
    | NATLIT
    | BOOLLIT
    | HEXLIT
    | figurativeConstant
    ;

// ISO §8.8.3.1 general format: {literal-1 | concatenation-expression-1} & literal-2 — left-recursive in the
// spec, flattened here to operand (& operand)+. Operands are literals of class alphanumeric (STRINGLIT and its
// X"…" hex format), national, or boolean, or figurative constants (§8.8.3.2 SR1); numeric literals are NOT
// operands (SR1 admits only the three classes), so `5 & …` is a parse error by construction. The SR1
// same-class rule, the no-ALL-figurative rule, and the SR2–SR4 8,191-position caps are BIND-time checks
// (ConcatFolder — a superset parse, per the repo's parse-wide/bind-narrow doctrine). Gated 2002+ by the
// VersionConformancePass parse arm (concat-operator-2002 → COBOLNET0900 below 2002); the grammar itself is
// edition-agnostic (superset parse at every --std).
concatenationExpression
    : concatOperand (AMPERSAND concatOperand)+
    ;

concatOperand
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
    | SIGNED_DECIMALLIT                    // -15.6 (sign-adjacent literal — FUNCTION-argument regions only, P7 Step 12)
    | SIGNED_INTEGERLIT                    // -4 (sign-adjacent literal — FUNCTION-argument regions only)
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
