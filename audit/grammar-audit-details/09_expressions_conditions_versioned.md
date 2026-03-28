I now have sufficient information from the spec to produce precise categorized tables. Here is the complete analysis:

---

## Expression/Condition Mismatch Version Categorization

The 32 mismatches in the audit span items 23–28 (Critical Gaps §Expression) plus 14 items in the High-Priority section (missing literal types, class conditions, expression types). Below they are grouped by the COBOL version that requires the fix.

---

### Table 1: COBOL-85 Required (Must Fix — Breaks NIST Tests)

These were in ISO 1989:1985 / ANS X3.23-1985, or the 1989 Amendment 1 (intrinsic functions, 1992).

| # | Audit Item | Spec Section | Severity | Notes |
|---|-----------|-------------|----------|-------|
| 24 | Chained exponentiation: `?` should be `*` | §8.8.1.2 rule 3 ("left to right") | **Critical** | `A ** B ** C` is valid per spec; grammar uses `?` (at most once). Left-to-right evaluation at same precedence level means the grammar should allow N repetitions. |
| 26 | `IS <>` operator form missing | §8.7.5 Format 2, rule 11 | **Critical** | `<>` is listed as "IS <>" in the extended relational operators table — present in ISO 1989:2002 and carried forward, but **not** in ANS X3.23-1985. See note below. |
| 23* | XOR / EXCLUSIVE-OR | §8.7.6; Annex E.3.2 item 4 | High | **NOT COBOL-85** — added in COBOL-2023. See Table 3. |
| 25* | General arithmetic subscripts | §8.4.2.3 subscript diagram | High | **NOT COBOL-85** — COBOL-85 had 3 restricted forms only. General `arithmetic-expression-1` was COBOL-2002. See Table 2. |
| 27* | Floating-point literals | §6.4.4 | High | **NOT COBOL-85** — part of FLOAT-SHORT/LONG/EXTENDED usage types, introduced COBOL-2002. See Table 2. |
| 28* | IS OMITTED condition | §8.8.4.8 | High | **NOT COBOL-85** — introduced with function prototypes and OPTIONAL parameters, COBOL-2002. See Table 2. |

**Note on `<>`:** The original COBOL-85 (ANS X3.23-1985) did not include `<>`. It was added in ISO/IEC 1989:2002. However, because it is already in the NIST test suite (NIST tests were updated for 2002 compliance) and is ubiquitous in real COBOL code, it should be treated as a **practical COBOL-85 target fix** even though it is technically a 2002 addition. It is listed here for completeness but categorized as Critical.

**Confirmed COBOL-85 Arithmetic/Precedence items (from audit context):**

| # | Audit Item | Spec Section | Severity |
|---|-----------|-------------|----------|
| — | Unary +/- handling in arithmetic expressions | §8.8.1.2 rules 1–5 | Implicit (already noted correct) |
| — | Operator precedence: **, then * /, then + - | §8.8.1.2 rule 2 | Implicit (audit found correct) |
| — | ALL figurative constant forms | §8.4.4.3 | Present per CLAUDE.md (fixed last session) |

---

### Table 2: COBOL-2002 Additions (COBOL-2002 Target Fixes)

These were introduced in ISO/IEC 1989:2002. Required only if targeting 2002 compliance.

| # | Audit Item | Spec Section | Severity | Notes |
|---|-----------|-------------|----------|-------|
| 25 | General arithmetic subscripts (`arithmetic-expression-1`) | §8.4.2.3 subscript diagram | **Critical** | COBOL-85 only allowed: integer-literal, data-name ± integer, index-name ± integer. The spec now shows `arithmetic-expression-1` as a first-class subscript form. COBOL-2002 widened this. |
| 27 | Floating-point numeric literals (e.g. `1.5E+3`) | §6.4.4 | **Critical** | Requires FLOAT-SHORT/LONG/EXTENDED USAGE support; lexer token `FloatLiteral` entirely absent. |
| 28 | IS OMITTED condition | §8.8.4.8 | **Critical** | Only valid where `data-name-1` is a formal parameter declared OPTIONAL. Grammar has no `omittedCondition` rule. |
| — | Boolean literals `B"..."` / `BX"..."` | §6.4.2 | High | Lexer token missing. Required for BOOLEAN USAGE data items. |
| — | National literals `N"..."` / `NX"..."` | §6.4.3 | High | Lexer token missing. Required for NATIONAL USAGE data items. |
| — | Zero-length hex literals `X""` | §6.4.1 | Medium | Lexer rejects empty-content hex literals. |
| — | Concatenation expression (`&` operator) | §8.7.3, §8.8.3 | High | No `concatenationExpression` rule; `&` not a recognized separator in grammar. |
| — | Boolean expressions (`B-AND`, `B-OR`, `B-XOR`, `B-NOT`, `B-SHIFT-*`) | §8.7.2, §8.8.2 | High | No `booleanExpression` rule; none of the boolean operators are lexer tokens. |
| — | BOOLEAN class condition | §8.8.4.7 syntax rule 3 | High | `IS BOOLEAN` not in `classCondition` rule. |
| — | FLOAT-INFINITY class condition | §8.8.4.7 rule 5d | High | Not in `classCondition` rule. |
| — | FLOAT-NOT-A-NUMBER class condition (+ QUIET/SIGNALING variants) | §8.8.4.7 rules 5e–5f | High | Not in `classCondition` rule. |
| — | ADDRESS OF in expressions | §8.4.3.9 | Medium | `ADDRESS OF identifier` used as data-pointer value; no grammar support in `identifier` or `arithmeticExpression`. |
| — | NULL in expressions | §8.4.3.10 | Medium | `NULL` as predefined address/object reference; not in `literal` or `identifier` alternatives. |

---

### Table 3: COBOL-2023 Additions (Current Spec Target)

These were added in the current edition (ISO/IEC 1989:2023), confirmed by Annex E.3.2.

| # | Audit Item | Spec Section | Annex E Reference | Notes |
|---|-----------|-------------|-------------------|-------|
| 23 | XOR / EXCLUSIVE-OR logical operator | §8.7.6, §8.8.4.9 | E.3.2 item 4: "Logical operators have been enhanced to include 'EXCLUSIVE-OR' and 'XOR'" | New reserved words added; XOR precedes OR in precedence (`NOT > AND > XOR > OR`). |
| — | B-SHIFT-L / B-SHIFT-LC / B-SHIFT-R / B-SHIFT-RC boolean operators | §8.7.2 | E.3.2 item 3 + E.2 item 3: "Binary operators have been enhanced to include B-SHIFT-*" | New reserved words added in 2023 (E.2 item 25 lists them). Also present as boolean expression operators. |

---

### Table 4: COBOL-2014 or 2002/2014 Additions

These are linked to floating-point arithmetic support whose version is 2002 (FLOAT-SHORT/LONG types) or 2014 (FARTHEST-FROM-ZERO, NEAREST-TO-ZERO as class conditions are tied to standard decimal arithmetic, introduced or formalized in 2014).

| # | Audit Item | Spec Section | Notes |
|---|-----------|-------------|-------|
| — | FARTHEST-FROM-ZERO class condition | §8.8.4.7 rule 5g | Applies to items `whose category is numeric` (§8.8.4.7 syntax rule 6). Requires numeric items only; linked to IEEE-754 edge values. 2014 or 2002. |
| — | NEAREST-TO-ZERO class condition | §8.8.4.7 rule 5m | Same family as above. |
| — | IN-ARITHMETIC-RANGE class condition | §8.8.4.7 rule 5l | Tests whether numeric value is within intermediate data item range; standard-decimal arithmetic context. |

---

### Summary Table: All 32 Items by Version

The audit listed exactly 32 items across items 23–28 (6 Critical) plus the High-Priority section (~26 items). Mapped to version:

| Count | Version | Items |
|------:|---------|-------|
| 1 | COBOL-85 | Chained exponentiation (`**` grammar: `?` → `*`) |
| 1 | COBOL-2002 (practical: treat as 85) | `IS <>` relational operator |
| 10 | COBOL-2002 | General arithmetic subscripts; floating-point literals; IS OMITTED; Boolean literals; National literals; zero-length X""; concatenation (&); Boolean expressions (B-AND/OR/XOR/NOT/SHIFT-*); BOOLEAN class condition; FLOAT-INFINITY class condition; FLOAT-NOT-A-NUMBER class conditions (3 variants); ADDRESS OF in expressions; NULL in expressions |
| 3 | COBOL-2002/2014 | FARTHEST-FROM-ZERO; NEAREST-TO-ZERO; IN-ARITHMETIC-RANGE class conditions |
| 2 | COBOL-2023 | XOR/EXCLUSIVE-OR; B-SHIFT-L/LC/R/RC operators |

---

### Key Findings for Implementation Priority

**Phase 1 (COBOL-85 compliance — NIST-critical):**
- Fix chained exponentiation: `/e/CobolSharp/src/Grammar/CobolExpressions.g4` — change `powerOp?` to `powerOp*` (or equivalent) in the exponentiation production

**Phase 2 (Practical COBOL-2002 baseline — most real COBOL programs use these):**
- Add `IS <>` as a lexer token and relational operator alternative
- Add floating-point literal lexer rule
- Add general arithmetic subscript form (already partially attempted per CLAUDE.md session notes)
- Add `IS OMITTED` to condition rules (only needed for OPTIONAL parameter programs)

**Phase 3 (Full COBOL-2002 compliance):**
- Boolean literals, national literals, concatenation operator, boolean expressions, extended class conditions, ADDRESS OF / NULL in expressions

**Phase 4 (COBOL-2023):**
- XOR/EXCLUSIVE-OR, B-SHIFT-* operators