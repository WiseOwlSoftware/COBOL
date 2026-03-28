Now I have the complete picture. Let me compile the remaining gaps:

---

### SECTION 12: LITERALS (§8.3.3)

**Spec literal types:**
1. Alphanumeric: `"..."` / `'...'` (Format 1), `X"..."` / `X'...'` (Format 2 hex)
2. Numeric: fixed-point (integer and decimal), floating-point (`1.5E+3`)
3. Boolean: `B"..."` / `B'...'` (Format 1), `BX"..."` / `BX'...'` (Format 2 hex-boolean)
4. National: `N"..."` / `N'...'` (Format 1), `NX"..."` / `NX'...'` (Format 2 hex-national)
5. Figurative constants (see below)

**Grammar:**
```
literal: numericLiteral | nonNumericLiteral
nonNumericLiteral: STRINGLIT | HEXLIT | figurativeConstant
STRINGLIT: '"' ... '"' | '\'' ... '\''
HEXLIT: [x] '"' [0-9a-f]+ '"' | [x] '\'' [0-9a-f]+ '\''
```

---

```
MISMATCH: literal — boolean literals B"..." / BX"..." not supported
  Spec (§8.3.3.4): boolean literal formats B"010" and BX"A3F".
  Grammar: No BOOLLIT or B_STRING token exists in CobolLexer.g4. A source string
           B"010" would either fail to lex (B matches IDENTIFIER, then "010" is a
           string literal) or be mis-tokenized.
  Gap: Boolean literals (COBOL data class BOOLEAN / BIT) cannot be expressed.
```

```
MISMATCH: literal — national literals N"..." / NX"..." not supported
  Spec (§8.3.3.5): national literal formats N"text" and NX"hex".
  Grammar: No NATLIT token in CobolLexer.g4. N"ABC" would be mis-tokenized as
           IDENTIFIER(N) then STRINGLIT("ABC").
  Gap: National literals are not supported.
```

```
MISMATCH: literal — floating-point numeric literals not supported (repeated from §1)
  Spec (§8.3.3.3.3): 1.5E+3, -2.0E-10, etc.
  Grammar: No FLOATLIT token; numericLiteralCore has no E-notation alternative.
  Gap: Floating-point literals cannot be written inline in procedure division.
```

```
MISMATCH: booleanLiteral — spec boolean literals vs grammar boolean literals
  Spec: boolean literals are B"0" / B"1" (the boolean data literal form).
  Grammar: booleanLiteral → TRUE_ | FALSE_
           TRUE and FALSE are COBOL-85 condition-name values used in SET statements
           and EVALUATE, not the ISO 2023 boolean literal format B"...".
  Note: TRUE/FALSE as booleanLiteral in conditions is a grammar extension beyond
        the spec's simple condition forms. The spec has no "IF TRUE" or "IF FALSE"
        as a condition form. This is an extension.
  Gap: booleanLiteral (TRUE/FALSE) in primaryCondition is a non-spec extension.
       Per §8.8.4, TRUE and FALSE are not listed as simple condition forms.
```

---

### SECTION 13: FIGURATIVE CONSTANTS (§8.3.3.6)

**Spec (§8.3.3.6.2):**
- Format 1 (zero): `[ALL] { ZERO | ZEROES | ZEROS }`
- Format 2 (space): `[ALL] { SPACE | SPACES }`
- Format 3 (high-value): `[ALL] { HIGH-VALUE | HIGH-VALUES }`
- Format 4 (low-value): `[ALL] { LOW-VALUE | LOW-VALUES }`
- Format 5 (quote): `[ALL] { QUOTE | QUOTES }`
- Format 6 (all-literal): `ALL literal-1`
- Format 7 (symbolic-character): `ALL symbolic-character-1`

**Grammar's `figurativeConstant`:**
```
figurativeConstant
    : ZERO | SPACE | HIGH_VALUE | LOW_VALUE | QUOTE_
    | ALL STRINGLIT | ALL HEXLIT
    | ALL ZERO | ALL SPACE | ALL HIGH_VALUE | ALL LOW_VALUE | ALL QUOTE_
    ;
```

---

```
MISMATCH: figurativeConstant — ALL without a keyword (bare figurative) is implicit
  Spec: Each figurative constant WITHOUT "ALL" is valid (Formats 1-5 without ALL).
  Grammar: ZERO, SPACE, HIGH_VALUE, LOW_VALUE, QUOTE_ are present without ALL. Correct.
```

```
MISMATCH: figurativeConstant — ALL symbolic-character-1 (Format 7) missing
  Spec (§8.3.3.6.2 Format 7): ALL symbolic-character-1
       Where symbolic-character-1 is a user-defined name from SYMBOLIC CHARACTERS clause.
  Grammar: figurativeConstant does not include ALL IDENTIFIER (for symbolic characters).
           Only ALL STRINGLIT, ALL HEXLIT, ALL ZERO, ALL SPACE, ALL HIGH_VALUE,
           ALL LOW_VALUE, ALL QUOTE_ are present.
  Gap: "ALL symbol-name" where symbol-name is from SYMBOLIC CHARACTERS is not parseable.
       This requires ALL IDENTIFIER as an alternative in figurativeConstant.
```

```
MISMATCH: figurativeConstant — ALL national-literal (Format 6 extension) missing
  Spec (§8.3.3.6.3 rule 2): literal-1 in ALL literal-1 may be alphanumeric,
       boolean, or national literal. The grammar only has ALL STRINGLIT and ALL HEXLIT.
  Grammar: ALL BOOLLIT and ALL NATLIT are absent — but this follows from the
       broader gap that boolean and national literals are not tokenized.
  Gap: Follows from the missing boolean/national literal tokens.
```

```
MISMATCH: figurativeConstant — standalone ZERO/SPACE/etc. without ALL in comparisonExpression
  Spec (§8.3.3.6.3 rule 1a): "If the literal is restricted to a numeric literal,
       the only figurative constant permitted is ZERO (ZEROS, ZEROES) without the ALL phrase."
  Grammar: signCondition has: valueOperand IS? NOT? (POSITIVE | NEGATIVE | ZERO)
           where ZERO is a literal keyword — not the figurativeConstant ZERO from
           nonNumericLiteral. This is fine.
  Assessment: The grammar correctly uses the ZERO token directly in signCondition.
```

---

### SECTION 14: CONCATENATION EXPRESSIONS (§8.8.3)

**Spec (§8.8.3.1):**
> { literal-1 | concatenation-expression-1 } & literal-2

**Grammar:** No ampersand (`&`) token exists in `CobolLexer.g4` and no `concatenationExpression` rule exists in `CobolExpressions.g4`.

---

```
MISMATCH: concatenationExpression — entirely absent
  Spec (§8.8.3): { literal | concatenation-expression } & literal
       The & operator concatenates alphanumeric, boolean, or national literals.
       Result is equivalent to a literal of the same class.
  Grammar: No AMPERSAND or CONCAT_OP token. No concatenationExpression rule.
           An expression like "HELLO" & " WORLD" cannot be parsed.
  Gap: Concatenation expressions are entirely absent from the grammar.
       The & token is listed in §8.3.2.4.2 as a special character word.
```

---

### SECTION 15: OPERATOR PRECEDENCE SUMMARY

**Spec order (§8.8.1.2 for arithmetic, §8.8.4.11.3 for logical):**
```
Arithmetic:  1. unary + −   2. **   3. * /   4. + −
Logical:     1. NOT          2. AND  3. XOR   4. OR
```

**Grammar arithmetic precedence (by rule nesting, innermost = highest):**
```
unaryExpression (highest) → powerExpression → multiplicativeExpression → additiveExpression
```
This matches spec exactly for arithmetic.

**Grammar logical precedence:**
```
unaryLogicalExpression (NOT, highest) → logicalAndExpression (AND) → logicalOrExpression (OR)
```
Missing: XOR tier between AND and OR. Otherwise correct.

---

### COMPLETE MISMATCH SUMMARY

Here is the consolidated list of all mismatches, sorted by severity:

---

**CRITICAL — Breaks conformance for valid COBOL programs:**

```
MISMATCH: condition — EXCLUSIVE-OR / XOR logical operator missing
  Spec: §8.7.6, §8.8.4.11.2: AND | OR | EXCLUSIVE-OR | XOR operators in complex conditions
  Grammar: Only AND and OR exist. No EXCLUSIVE_OR or XOR token in lexer.
           No logicalXorExpression tier in grammar.
  Gap: "IF A = B XOR C = D" fails to parse. XOR abbreviated chains also unsupported.

MISMATCH: primaryCondition — omitted-argument condition missing
  Spec: §8.8.4.8: data-name-1 IS [NOT] OMITTED
  Grammar: OMITTED token exists but never appears in any condition rule.
  Gap: "IF PARAM IS OMITTED" cannot be parsed.

MISMATCH: primaryExpression — floating-point numeric literals missing
  Spec: §8.3.3.3.3: 1.5E+3, -2.0E-10 (significand E exponent form)
  Grammar: No FLOATLIT token; numericLiteralCore has no E-notation.
  Gap: Floating-point literals cannot be written in expressions.

MISMATCH: powerExpression — chained exponentiation not supported
  Spec: §8.8.1.2: consecutive ** at same level (A ** B ** C)
  Grammar: powerExpression uses ? (zero-or-one); A**B**C does not parse.
  Gap: Change (POWER unaryExpression)? to (POWER unaryExpression)*.

MISMATCH: subscriptEntry — general arithmetic expressions as subscripts not supported
  Spec: §8.4.2.3.2: subscript = arithmetic-expression | ALL | index ± integer
  Grammar: Only integer literals, signed integers, ALL, identifier ± integer.
           TABLE(A + B * 2) and TABLE(FUNCTION LENGTH(X)) cannot be subscripted.

MISMATCH: comparisonOperator — IS <> operator form missing
  Spec: §8.7.5.1 Format 2: IS <> as relational operator
  Grammar: NOTEQUAL (<>) works, but IS NOTEQUAL (with IS prefix) has no rule.
  Gap: "IF A IS <> B" cannot be parsed.
```

**HIGH — Missing literal types:**

```
MISMATCH: literal — boolean literals B"..." / BX"..." not tokenized
  Spec: §8.3.3.4: B"010", BX"FF"
  Grammar: No BOOLLIT token. B"010" mis-tokenizes.

MISMATCH: literal — national literals N"..." / NX"..." not tokenized
  Spec: §8.3.3.5: N"text", NX"hextext"
  Grammar: No NATLIT token. N"ABC" mis-tokenizes.

MISMATCH: figurativeConstant — ALL symbolic-character-1 missing
  Spec: §8.3.3.6.2 Format 7: ALL symbolic-character-1
  Grammar: Only ALL + built-in figurative keywords; ALL IDENTIFIER absent.
```

**HIGH — Missing condition forms in class condition:**

```
MISMATCH: className — BOOLEAN missing
  Spec: §8.8.4.4.2: identifier IS [NOT] BOOLEAN
  Grammar: BOOLEAN not in className; no BOOLEAN token in lexer.

MISMATCH: className — FARTHEST-FROM-ZERO missing
  Spec: §8.8.4.4.2
  Grammar: No lexer token FARTHEST_FROM_ZERO; not in className.

MISMATCH: className — FLOAT-INFINITY missing
  Spec: §8.8.4.4.2
  Grammar: No lexer token; not in className.

MISMATCH: className — FLOAT-NOT-A-NUMBER missing
  Spec: §8.8.4.4.2
  Grammar: No lexer token; not in className.

MISMATCH: className — FLOAT-NOT-A-NUMBER-QUIET missing
  Spec: §8.8.4.4.2
  Grammar: Not present.

MISMATCH: className — FLOAT-NOT-A-NUMBER-SIGNALING missing
  Spec: §8.8.4.4.2
  Grammar: Not present.

MISMATCH: className — IN-ARITHMETIC-RANGE missing
  Spec: §8.8.4.4.2
  Grammar: No lexer token; not in className.

MISMATCH: className — NEAREST-TO-ZERO missing
  Spec: §8.8.4.4.2
  Grammar: Not present.
```

**HIGH — Missing expression type:**

```
MISMATCH: concatenationExpression — entirely absent
  Spec: §8.8.3: literal-1 & literal-2 (& operator)
  Grammar: No AMPERSAND token; no concatenationExpression rule.

MISMATCH: booleanExpression — entirely absent
  Spec: §8.8.2: boolean-expression with B-AND, B-OR, B-XOR, B-NOT, B-SHIFT-* operators
  Grammar: No boolean expression rule; no B_AND / B_OR / B_XOR / B_NOT /
           B_SHIFT_L / B_SHIFT_R / B_SHIFT_LC / B_SHIFT_RC tokens.
```

**MEDIUM — Identifier forms missing from expression contexts:**

```
MISMATCH: dataReference — NULL not usable in expressions/conditions
  Spec: §8.4.3.7, §8.4.3.10: NULL as predefined-object and predefined-address
  Grammar: NULL_ token exists but only in objectReference; not in comparisonOperand.

MISMATCH: dataReference — ADDRESS OF not usable in expressions/conditions
  Spec: §8.4.3.11: ADDRESS OF data-name as data-address-identifier
  Grammar: ADDRESS OF appears only in SET ADDRESS OF; absent from primaryExpression.

MISMATCH: functionCall — OMITTED keyword in argument list missing
  Spec: §8.4.3.2.2: argument may be OMITTED
  Grammar: OMITTED not in argument alternatives.

MISMATCH: functionCall — FUNCTION keyword always required
  Spec: §8.4.3.2.3 rule 2: FUNCTION optional if name in REPOSITORY paragraph
  Grammar: FUNCTION always required (IS 2002 feature gap).
```

**LOW — Over-acceptance / structural issues:**

```
MISMATCH: signCondition — nonNumericLiteral allowed as sign condition subject
  Spec: §8.8.4.7.3 rule 1: subject must be numeric data item or arithmetic expression
  Grammar: valueOperand includes nonNumericLiteral; "HELLO" IS POSITIVE parses.

MISMATCH: refModPart — chained ref-mod not rejected at grammar level
  Spec: §8.4.3.3.3 rule 3: identifier-1 shall not be a reference-modification identifier
  Grammar: dataReferenceSuffix* allows A(1:2)(3:4); should be binder-rejected.

MISMATCH: dataReference — subscript on qualifier allowed
  Spec: §8.4.2.3.2: subscripts apply to the whole qualified name, not mid-chain qualifiers
  Grammar: qualification allows (OF | IN) IDENTIFIER subscriptPart, enabling A OF B(1).

MISMATCH: comparisonOperator — EQUAL THAN accepted (non-standard)
  Spec: §8.7.5.1: EQUAL TO, not EQUAL THAN
  Grammar: IS? EQUAL (TO | THAN)? — THAN is a non-spec extension for EQUAL.

MISMATCH: classCondition rule is dead code
  Grammar: classCondition rule declared but never referenced.
           ALPHANUMERIC appears there but not in the active className rule.

MISMATCH: booleanLiteral (TRUE/FALSE) in conditions is a grammar extension
  Spec: §8.8.4: TRUE/FALSE are not listed as simple condition forms
  Grammar: primaryCondition includes booleanLiteral → TRUE_ | FALSE_.
           This is an extension beyond ISO 2023 spec.

MISMATCH: functionCall — boolean-expression as argument not supported
  Spec: §8.4.3.2.3 rule 8: argument-1 may be a boolean expression
  Grammar: argument lacks booleanExpression (follows from broader gap).

MISMATCH: functionCall — function-pointer calls not supported
  Spec: §8.4.3.2.2: [FUNCTION] function-pointer-name-1
  Grammar: No function-pointer path (IS 2002 OO feature).

MISMATCH: relativeOffset — leading whitespace mandatory before ±
  Spec: index-name-1 [ { + | - } integer ]  (no mandatory space specified)
  Grammar: SUB_WS before and after operator required; INDEX1+2 rejected.
```

---

### Summary Table

| # | Rule | Spec Section | Severity | Gap Description |
|---|------|-------------|----------|-----------------|
| 1 | `condition` | §8.7.6, §8.8.4.11 | CRITICAL | XOR / EXCLUSIVE-OR operator missing |
| 2 | `primaryCondition` | §8.8.4.8 | CRITICAL | `IS [NOT] OMITTED` condition absent |
| 3 | `primaryExpression` | §8.3.3.3.3 | CRITICAL | Floating-point literals (1.5E+3) not tokenized |
| 4 | `powerExpression` | §8.8.1.2 | CRITICAL | Chained `**` uses `?` instead of `*` |
| 5 | `subscriptEntry` | §8.4.2.3.2 | CRITICAL | General arithmetic expressions as subscripts absent |
| 6 | `comparisonOperator` | §8.7.5.1 | CRITICAL | `IS <>` form missing |
| 7 | `literal` | §8.3.3.4 | HIGH | Boolean literals `B"..."` / `BX"..."` not tokenized |
| 8 | `literal` | §8.3.3.5 | HIGH | National literals `N"..."` / `NX"..."` not tokenized |
| 9 | `figurativeConstant` | §8.3.3.6.2 | HIGH | `ALL symbolic-character-1` missing |
| 10 | `className` | §8.8.4.4.2 | HIGH | BOOLEAN class condition missing |
| 11 | `className` | §8.8.4.4.2 | HIGH | FARTHEST-FROM-ZERO missing |
| 12 | `className` | §8.8.4.4.2 | HIGH | FLOAT-INFINITY missing |
| 13 | `className` | §8.8.4.4.2 | HIGH | FLOAT-NOT-A-NUMBER missing |
| 14 | `className` | §8.8.4.4.2 | HIGH | FLOAT-NOT-A-NUMBER-QUIET missing |
| 15 | `className` | §8.8.4.4.2 | HIGH | FLOAT-NOT-A-NUMBER-SIGNALING missing |
| 16 | `className` | §8.8.4.4.2 | HIGH | IN-ARITHMETIC-RANGE missing |
| 17 | `className` | §8.8.4.4.2 | HIGH | NEAREST-TO-ZERO missing |
| 18 | `concatenationExpression` | §8.8.3 | HIGH | `&` operator and rule entirely absent |
| 19 | `booleanExpression` | §8.8.2 | HIGH | Entire boolean expression subsystem absent |
| 20 | `dataReference` | §8.4.3.7/10 | MEDIUM | NULL not usable in expression contexts |
| 21 | `dataReference` | §8.4.3.11 | MEDIUM | ADDRESS OF absent from expression contexts |
| 22 | `functionCall` | §8.4.3.2.2 | MEDIUM | OMITTED argument missing |
| 23 | `functionCall` | §8.4.3.2.3 | MEDIUM | FUNCTION keyword always required (no REPOSITORY path) |
| 24 | `signCondition` | §8.8.4.7.3 | LOW | nonNumericLiteral accepted as subject (over-accept) |
| 25 | `refModPart` | §8.4.3.3.3 | LOW | Chained ref-mod not grammar-rejected |
| 26 | `dataReference` | §8.4.2.3.2 | LOW | Subscript on qualifier (`A OF B(1)`) over-accepted |
| 27 | `comparisonOperator` | §8.7.5.1 | LOW | `EQUAL THAN` accepted (non-standard) |
| 28 | `classCondition` | — | LOW | Dead rule; ALPHANUMERIC not in active `className` |
| 29 | `booleanLiteral` | §8.8.4 | LOW | TRUE/FALSE as condition is grammar extension |
| 30 | `functionCall` | §8.4.3.2.3 | LOW | Boolean expression as argument absent |
| 31 | `functionCall` | §8.4.3.2.2 | LOW | Function-pointer calls not supported |
| 32 | `relativeOffset` | §8.4.2.3.2 | LOW | Leading `SUB_WS` mandatory; `INDEX1+2` rejected |