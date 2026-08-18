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
//
// ── REFERENCE-MODIFYING THE FUNCTION RESULT (fix-queue PB8, ISO §8.4.3.3.3 SR2) ────────────────────────────
// §8.4.3.1.2 Format 3 makes `identifier-1 reference-modifier-1` an IDENTIFIER, and §8.4.3.3.3 SR2 admits a
// function-identifier as identifier-1 ("If identifier-1 is a function-identifier, it shall reference an
// alphanumeric, boolean, or national function"). §8.4.3.1.4 GR1 fixes the order: (f) the argument list applies
// to the function-name on its left, THEN (g) "a reference modifier applies to the identifier on the left" — so
// the tail sits after the optional argument list, and BOTH of these are legal:
//     FUNCTION CURRENT-DATE (1:4)              -- zero-argument function, ref-modified
//     FUNCTION UPPER-CASE("abc") (1:2)         -- argument list, THEN ref-modified
// ⛔ NO LEXER CHANGE IS INVOLVED, and the fix-queue entry that said otherwise was wrong. A token dump of both
// shapes (pinned by CobolLexerModeDriftTests) shows the ref-mod paren lexed in DEFAULT mode in each case: after
// a function NAME the lexer's FUNCTION suppression already keeps it out of SUBSCRIPT mode, and after the
// argument list's ')' the previous token is not a data-name so the SUBSCRIPT trigger never fires. Both therefore
// reach the DEFAULT-mode refModPart (COLON, not SUB_COLON) that already exists for dataReference.
// ⚠ `refModPart*`, not `refModPart?`, and that is deliberate: §8.4.3.3.3 SR3 ("identifier-1 shall not be a
// reference-modification format identifier") forbids ref-modifying a ref-mod, and making the ARITY carry that
// rule here would enforce SR3 in a SECOND place — the data-reference side already counts ref-mods in
// ReferenceResolver, because `dataReferenceSuffix*` cannot express the limit either. One rule belongs in one
// place, so both sides parse the extra modifier and the binder rejects it with COBOLNET1630, giving the same
// cited message for FUNCTION F(X) (1:4)(1:2) as for A (3:4)(2:2) instead of a raw parse error on one side only.
// ⚠ The two alternatives are disjoint on the COLON and need no predicate: a functionArgList can never contain a
// DEFAULT-mode COLON (a nested data-item ref-mod inside an argument lexes SUB_COLON in SUBSCRIPT mode). §8.4.3.2
// SR6 — "if a function's definition permits arguments … the left parenthesis is ALWAYS … that function's
// arguments" — is a CATALOG question the grammar cannot answer, so the binder enforces it (IntrinsicBinder):
// a bare ref-mod directly after an argument-PERMITTING function name is an SR6/SR8 argument-list error.
// ── THE KEYWORD-OMITTED FORM FOR A RESERVED INTRINSIC NAME (fix-queue PB9) ────────────────────────────────
// §8.4.3.2.3 SR2 lets the word FUNCTION be omitted for any intrinsic the REPOSITORY declares. For an ordinary
// intrinsic name that omission is the D2 IRREDUCIBLE AMBIGUITY — `FOO(1)` is equally a subscripted data
// reference — so it has no grammar alternative and the binder re-routes a dataReference instead.
// ⛔ THAT ARGUMENT DOES NOT APPLY TO A RESERVED WORD, and that is the whole basis for this alternative: a
// reserved word can NEVER be a user-defined data name (§8.3.2.4.1), so `SUM(1 2 3)` cannot be a subscripted
// data reference and there is nothing to be ambiguous with. §8.9 ∩ §8.11 is exactly four words — LENGTH,
// RANDOM, SIGN, SUM. LENGTH already reaches the binder through the cobolWord name slot (it needs to be there
// for `START WITH LENGTH` anyway) and keeps that route; the other three arrive here.
// ⚠ WHY NOT JUST ADD THEM TO cobolWord LIKE LENGTH — this was tried and NIST NC116A caught it. A word in the
// name slot is admissible as a VALUE-clause literal (a constant-name), so the greedy literal list swallows a
// FOLLOWING data-description clause keyword:
//     01 W PICTURE S99999 VALUE ZERO
//            SIGN LEADING SEPARATE.        ->  COBOLNET1585 "takes exactly one literal"
// LENGTH is safe there only because LENGTH does not BEGIN a data-description clause; SIGN and SUM both do
// (§13.18.53 SIGN, §13.14 report-writer SUM). This alternative is confined to expression/operand positions and
// cannot reach a data description at all, so that whole class is structurally out of reach.
// ⚠ THE ARGUMENT LIST IS A `subscriptPart`, NOT `LPAREN functionArgList RPAREN`, and a token dump is what says
// so: these words carry subscriptTrigger=true, so with no FUNCTION keyword before them the lexer pushes
// SUBSCRIPT mode at the '(' and the arguments arrive as SUB_* tokens —
//     COMPUTE N = SUM(1 2 3)  ==>  COMPUTE IDENTIFIER EQUALS SUM LPAREN SUB_INTEGERLIT×3 SUB_RPAREN
// which is exactly the D2 carrier the keyword-omitted form already uses. The binder therefore re-parses it
// through the SAME `ReparseArgs` → `functionArgListFragment` path as every other keyword-omitted reference,
// rather than growing a second argument grammar. (Pinned by CobolLexerModeDriftTests.)
// ⛔ THE ARGUMENT GROUP IS REQUIRED FOR SIGN AND SUM, AND THAT IS WHAT KEEPS THIS SAFE — it is derived from the
// functions' own general formats, not an ad-hoc guard. §15.81.2 writes `FUNCTION SIGN ( argument-1 )` and
// §15.88.2 `FUNCTION SUM ( { argument-1 } … )`: neither has a no-argument form, so a BARE `SIGN` or `SUM` is
// never a function reference. Requiring the group is what stops the collision NIST NC116A found:
//     01 W PICTURE S99999 VALUE ZERO
//            SIGN LEADING SEPARATE.
// A VALUE clause operand is a `unaryExpression`, which reaches functionCall, and the operand loop is greedy —
// so admitting a BARE SIGN made the loop swallow the SIGN CLAUSE of the NEXT line as a second VALUE literal
// (COBOLNET1585). With the group required, `SIGN LEADING` cannot match a functionCall at all and the loop stops
// where it should. The same applies to the report-writer `SUM OF` clause (§13.14).
// RANDOM is different and needs no group: §15.75.2 brackets the whole parenthesised part, so the bare form is
// legal — and RANDOM begins no data-description clause, so nothing can swallow it.
// ⛔ THE ARGUMENT-LIST PARENS ARE FNARG_LPAREN / FNARG_RPAREN, NOT LPAREN / RPAREN (fix-queue PB48). §8.4.3.2.3
// SR6 makes the '(' immediately after an intrinsic-function-name ALWAYS the argument list, never a grouping
// paren, and the lexer retypes it from the _fnParenStack it already keeps. Writing the distinction into the
// token vocabulary is what stops the post-lex passes from having to re-derive it: ZeroTokenRewriter's "ZERO
// adjacent to a paren is arithmetic" rule was reading THIS paren and converting `FUNCTION LOWER-CASE(ZERO)`'s
// figurative into an arithmetic zero, so the §15.3 class screen saw class numeric and rejected legal source.
// A grouping paren INSIDE the list (`FUNCTION MAX((A + B) 2)`) is an ordinary LPAREN and is unaffected.
functionCall
    : FUNCTION functionName (FNARG_LPAREN functionArgList? FNARG_RPAREN)? refModPart*
    | reservedIntrinsicArgFn subscriptPart refModPart*
    | RANDOM subscriptPart? refModPart*
    ;

// The §8.9-reserved words that are also §8.11 intrinsic function names AND require arguments. LENGTH is the
// fourth member of §8.9 ∩ §8.11 and is absent deliberately: it already reaches the binder through the cobolWord
// name slot (where it must be anyway, for `START WITH LENGTH`), and adding it here would make `LENGTH OF x`
// ambiguous. Legal only when the REPOSITORY declares the function — a question the grammar cannot answer, so
// IntrinsicBinder enforces §8.4.3.2.3 SR2.
reservedIntrinsicArgFn
    : SIGN
    | SUM
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

// The D18 SUBSCRIPT-EXPRESSION re-parse entry (binder-invoked only; ISO §8.4.2.3.2 admits `arithmetic-expression-1`
// as a subscript and §8.4.3.3.3 SR4 as a reference-modifier position). A subscript / ref-mod SEGMENT that the
// SUBSCRIPT-mode token renderer (ReferenceResolver.RenderSegment) cannot render — today a function-identifier,
// fix-queue PB17 — is re-lexed from its VERBATIM source text and parsed through the ONE arithmeticExpression rule,
// so it binds through ExpressionBinder.BindExpr exactly as every other arithmetic expression does. That is what
// keeps the token renderer a token renderer instead of growing a THIRD hand-written expression compiler beside
// ExpressionBinder and IntrinsicRenderer.
// Referenced by nothing in compilationUnit — the functionArgListFragment precedent, so ZERO blast radius on the
// main parse; the SUBSCRIPT lexer mode and the main subscript grammar are untouched (the D10/PHASE-15 deferral
// stands).
// ⛔ DEFAULT lexer mode, NOT PrimeFunctionArgs: a subscript is not a function-argument region, so the
// §8.3.3.3.2 sign-adjacent literal twinning (which makes `MAX(A -4)` two arguments) must NOT fire here — inside a
// subscript `A -4` is the subtraction §8.7.1 says it is.
subscriptExpressionFragment : arithmeticExpression EOF ;

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

// The in-line method invocation's arguments (CobolOO.g4) — juxtaposed like every COBOL argument list; the comma
// is the optional separator (the same `SEPARATOR?` shape functionArgList uses). Swept with classValueSet.
argumentList
    : argument (COMMA? argument)*
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
// figurativeConstant is listed FIRST (kb/Work PB71): `ALL "A" & "B"` is the figurative ALL over the concatenated
// literal-1 "AB" (§8.3.3.6.3 SR2 — literal-1 "may be a concatenation expression"), and ANTLR resolves the tie
// between that reading and "a concatenation whose first operand is ALL "A"" (illegal — §8.8.3.2 SR1) toward the
// lower alternative. A concatenation that merely CONTAINS an ALL figurative (`"X" & ALL "A"`) still parses as a
// concatenation and is rejected COBOLNET1541 by ConcatFolder as before.
nonNumericLiteral
    : figurativeConstant
    | concatenationExpression
    | STRINGLIT
    | NATLIT
    | BOOLLIT
    | HEXLIT
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

// The literal-1 of the ALL figurative (§8.3.3.6.3 SR2): one literal of any class — plain / hexadecimal alphanumeric,
// national N"…" / NX"…", boolean B"…" / BX"…" — or a concatenation of them (§8.8.3; the operands shall be of one
// class, §8.8.3.2 SR1 — VersionConformancePass reports a mix, and a zero-length literal-1). Greedy: the ALL binds the
// WHOLE concatenation (kb/Work PB71).
allLiteral
    : allLiteralOperand (AMPERSAND allLiteralOperand)*
    ;

allLiteralOperand
    : STRINGLIT
    | HEXLIT
    | NATLIT
    | BOOLLIT
    ;

figurativeConstant
    : ZERO
    | SPACE
    | HIGH_VALUE
    | LOW_VALUE
    | QUOTE_
    | NULL_
    | ALL allLiteral    // Format 6 — ALL literal-1 (§8.3.3.6.3 SR2: an alphanumeric, boolean or national literal, which
                        // may be a concatenation expression; kb/Work PB71 — ONE arm for the four literal kinds)
    | ALL ZERO
    | ALL SPACE
    | ALL HIGH_VALUE
    | ALL LOW_VALUE
    | ALL QUOTE_
    ;
